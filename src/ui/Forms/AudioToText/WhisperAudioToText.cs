﻿using Nikse.SubtitleEdit.Core.AudioToText;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms.AudioToText
{
    public sealed partial class WhisperAudioToText : Form
    {
        private readonly string _videoFileName;
        private readonly int _audioTrackNumber;
        private bool _cancel;
        private bool _batchMode;
        private int _batchFileNumber;
        private readonly List<string> _filesToDelete;
        private readonly Form _parentForm;
        private bool _useCenterChannelOnly;
        private int _initialWidth = 725;
        private readonly Regex _timeRegex = new Regex(@"^\[\d\d:\d\d[\.,]\d\d\d --> \d\d:\d\d[\.,]\d\d\d\]", RegexOptions.Compiled);
        private List<ResultText> _resultList;
        private string _languageCode;
        private ConcurrentBag<string> _outputText = new ConcurrentBag<string>();

        public Subtitle TranscribedSubtitle { get; private set; }

        public WhisperAudioToText(string videoFileName, int audioTrackNumber, Form parentForm)
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
            UiUtil.FixLargeFonts(this, buttonGenerate);
            _videoFileName = videoFileName;
            _audioTrackNumber = audioTrackNumber;
            _parentForm = parentForm;

            Text = LanguageSettings.Current.AudioToText.Title;
            labelInfo.Text = LanguageSettings.Current.AudioToText.WhisperInfo;
            groupBoxModels.Text = LanguageSettings.Current.AudioToText.LanguagesAndModels;
            labelModel.Text = LanguageSettings.Current.AudioToText.ChooseModel;
            labelChooseLanguage.Text = LanguageSettings.Current.AudioToText.ChooseLanguage;
            linkLabelOpenModelsFolder.Text = LanguageSettings.Current.AudioToText.OpenModelsFolder;
            checkBoxUsePostProcessing.Text = LanguageSettings.Current.AudioToText.UsePostProcessing;
            buttonGenerate.Text = LanguageSettings.Current.Watermark.Generate;
            buttonCancel.Text = LanguageSettings.Current.General.Cancel;
            buttonBatchMode.Text = LanguageSettings.Current.AudioToText.BatchMode;
            groupBoxInputFiles.Text = LanguageSettings.Current.BatchConvert.Input;
            linkLabeWhisperWebSite.Text = LanguageSettings.Current.AudioToText.WhisperWebsite;

            buttonAddFile.Text = LanguageSettings.Current.DvdSubRip.Add;
            buttonRemoveFile.Text = LanguageSettings.Current.DvdSubRip.Remove;
            buttonClear.Text = LanguageSettings.Current.DvdSubRip.Clear;

            columnHeaderFileName.Text = LanguageSettings.Current.JoinSubtitles.FileName;

            checkBoxUsePostProcessing.Checked = Configuration.Settings.Tools.VoskPostProcessing;

            comboBoxLanguages.Items.Clear();
            comboBoxLanguages.Items.AddRange(WhisperLanguage.Languages.ToArray<object>());
            var lang = WhisperLanguage.Languages.FirstOrDefault(p => p.Code == Configuration.Settings.Tools.WhisperLanguageCode);
            comboBoxLanguages.Text = lang != null ? lang.ToString() : "English";

            FillModels(comboBoxModels, string.Empty);

            textBoxLog.Visible = false;
            textBoxLog.Dock = DockStyle.Fill;
            labelProgress.Text = string.Empty;
            labelTime.Text = string.Empty;
            _filesToDelete = new List<string>();

            if (string.IsNullOrEmpty(videoFileName))
            {
                _batchMode = true;
                buttonBatchMode.Enabled = false;
            }
            else
            {
                listViewInputFiles.Items.Add(videoFileName);
            }

            listViewInputFiles.Visible = false;
            labelFC.Text = string.Empty;
        }

        public static void FillModels(ComboBox comboBoxModels, string lastDownloadedModel)
        {
            var modelsFolder = WhisperModel.ModelFolder;
            var selectName = string.IsNullOrEmpty(lastDownloadedModel) ? Configuration.Settings.Tools.WhisperModel : lastDownloadedModel;
            comboBoxModels.Items.Clear();

            if (!Directory.Exists(modelsFolder))
            {
                WhisperModel.CreateModelFolder();
            }

            foreach (var fileName in Directory.GetFiles(modelsFolder))
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                var model = WhisperModel.Models.FirstOrDefault(p => p.Name == name);
                if (model == null)
                {
                    continue;
                }

                comboBoxModels.Items.Add(model);
                if (model.Name == selectName)
                {
                    comboBoxModels.SelectedIndex = comboBoxModels.Items.Count - 1;
                }
            }

            if (comboBoxModels.SelectedIndex < 0 && comboBoxModels.Items.Count > 0)
            {
                comboBoxModels.SelectedIndex = 0;
            }
        }

        private void ButtonGenerate_Click(object sender, EventArgs e)
        {
            if (comboBoxModels.Items.Count == 0)
            {
                buttonDownload_Click(null, null);
                return;
            }

            _useCenterChannelOnly = Configuration.Settings.General.FFmpegUseCenterChannelOnly &&
                                    FfmpegMediaInfo.Parse(_videoFileName).HasFrontCenterAudio(_audioTrackNumber);

            _languageCode = GetLanguage(comboBoxLanguages.Text);

            if (_batchMode)
            {
                if (listViewInputFiles.Items.Count == 0)
                {
                    buttonAddFile_Click(null, null);
                    return;
                }

                timer1.Start();
                GenerateBatch();
                TaskbarList.SetProgressState(_parentForm.Handle, TaskbarButtonProgressFlags.NoProgress);
                timer1.Stop();
                return;
            }

            ShowProgressBar();
            buttonGenerate.Enabled = false;
            buttonDownload.Enabled = false;
            buttonBatchMode.Enabled = false;
            comboBoxLanguages.Enabled = false;
            comboBoxModels.Enabled = false;
            var waveFileName = GenerateWavFile(_videoFileName, _audioTrackNumber);
            _outputText.Add("Wav file name: " + waveFileName);
            progressBar1.Style = ProgressBarStyle.Blocks;
            timer1.Start();
            var transcript = TranscribeViaWhisper(waveFileName);
            timer1.Stop();
            if (_cancel && (transcript == null || transcript.Count == 0 || MessageBox.Show(LanguageSettings.Current.AudioToText.KeepPartialTranscription, Text, MessageBoxButtons.YesNoCancel) != DialogResult.Yes))
            {
                DialogResult = DialogResult.Cancel;
                return;
            }

            var postProcessor = new AudioToTextPostProcessor(_languageCode)
            {
                ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
            };
            TranscribedSubtitle = postProcessor.Generate(transcript, checkBoxUsePostProcessing.Checked, true, true, true, true);
            DialogResult = DialogResult.OK;
        }

        private void ShowProgressBar()
        {
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            progressBar1.Refresh();
            progressBar1.Top = labelProgress.Bottom + 3;
            if (!textBoxLog.Visible)
            {
                progressBar1.BringToFront();
            }
        }

        private void GenerateBatch()
        {
            groupBoxInputFiles.Enabled = false;
            _batchFileNumber = 0;
            var errors = new StringBuilder();
            var errorCount = 0;
            _outputText.Add("Batch mode");
            foreach (ListViewItem lvi in listViewInputFiles.Items)
            {
                _batchFileNumber++;
                var videoFileName = lvi.Text;
                listViewInputFiles.SelectedIndices.Clear();
                lvi.Selected = true;
                ShowProgressBar();
                buttonGenerate.Enabled = false;
                buttonDownload.Enabled = false;
                buttonBatchMode.Enabled = false;
                comboBoxModels.Enabled = false;
                comboBoxLanguages.Enabled = false;
                var waveFileName = GenerateWavFile(videoFileName, _audioTrackNumber);
                if (!File.Exists(waveFileName))
                {
                    errors.AppendLine("Unable to extract audio from: " + videoFileName);
                    errorCount++;
                    continue;
                }

                _outputText.Add(string.Empty);
                _outputText.Add("Wav file name: " + waveFileName);
                progressBar1.Style = ProgressBarStyle.Blocks;
                var transcript = TranscribeViaWhisper(waveFileName);
                if (_cancel)
                {
                    TaskbarList.SetProgressState(_parentForm.Handle, TaskbarButtonProgressFlags.NoProgress);
                    if (!_batchMode)
                    {
                        DialogResult = DialogResult.Cancel;
                    }

                    groupBoxInputFiles.Enabled = true;
                    return;
                }

                var postProcessor = new AudioToTextPostProcessor(_languageCode)
                {
                    ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
                };
                TranscribedSubtitle = postProcessor.Generate(transcript, checkBoxUsePostProcessing.Checked, true, true, true, true);

                SaveToSourceFolder(videoFileName);
                TaskbarList.SetProgressValue(_parentForm.Handle, _batchFileNumber, listViewInputFiles.Items.Count);
            }

            progressBar1.Visible = false;
            labelTime.Text = string.Empty;

            TaskbarList.StartBlink(_parentForm, 10, 1, 2);

            if (errors.Length > 0)
            {
                MessageBox.Show($"{errorCount} error(s)!{Environment.NewLine}{errors}");
            }

            MessageBox.Show(string.Format(LanguageSettings.Current.AudioToText.XFilesSavedToVideoSourceFolder, listViewInputFiles.Items.Count - errorCount));

            groupBoxInputFiles.Enabled = true;
            buttonGenerate.Enabled = true;
            buttonDownload.Enabled = true;
            buttonBatchMode.Enabled = true;
            DialogResult = DialogResult.Cancel;
        }

        private void SaveToSourceFolder(string videoFileName)
        {
            var format = SubtitleFormat.FromName(Configuration.Settings.General.DefaultSubtitleFormat, new SubRip());
            var text = TranscribedSubtitle.ToText(format);

            var fileName = Path.Combine(Utilities.GetPathAndFileNameWithoutExtension(videoFileName)) + format.Extension;
            if (File.Exists(fileName))
            {
                fileName = $"{Path.Combine(Utilities.GetPathAndFileNameWithoutExtension(videoFileName))}.{Guid.NewGuid().ToByteArray()}.{format.Extension}";
            }

            File.WriteAllText(fileName, text, Encoding.UTF8);
            _outputText.Add("Subtitle written to : " + fileName);
        }

        internal static string GetLanguage(string name)
        {
            var language = WhisperLanguage.Languages.FirstOrDefault(l => l.Name == name);
            return language != null ? language.Code : "en";
        }

        public List<ResultText> TranscribeViaWhisper(string waveFileName)
        {
            var model = comboBoxModels.Items[comboBoxModels.SelectedIndex] as WhisperModel;
            if (model == null)
            {
                return new List<ResultText>();
            }

            labelProgress.Text = LanguageSettings.Current.AudioToText.Transcribing;
            if (_batchMode)
            {
                labelProgress.Text = string.Format(LanguageSettings.Current.AudioToText.TranscribingXOfY, _batchFileNumber, listViewInputFiles.Items.Count);
            }
            else
            {
                TaskbarList.SetProgressValue(_parentForm.Handle, 1, 100);
            }

            labelProgress.Refresh();
            Application.DoEvents();
            _resultList = new List<ResultText>();
            var process = GetWhisperProcess(waveFileName, model.Name, comboBoxLanguages.Text, OutputHandler);
            _outputText.Add("Calling whisper with : whisper " + process.StartInfo.Arguments);
            ShowProgressBar();
            progressBar1.Style = ProgressBarStyle.Marquee;
            buttonCancel.Visible = true;
            try
            {
                process.PriorityClass = ProcessPriorityClass.Normal;
            }
            catch
            {
                // ignored
            }

            _cancel = false;

            labelProgress.Text = LanguageSettings.Current.AudioToText.Transcribing;
            while (!process.HasExited)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(100);

                Invalidate();
                if (_cancel)
                {
                    process.Kill();
                    progressBar1.Visible = false;
                    buttonCancel.Visible = false;
                    DialogResult = DialogResult.Cancel;
                    return null;
                }
            }

            Application.DoEvents();
            System.Threading.Thread.Sleep(100);

            return _resultList;
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrWhiteSpace(outLine.Data))
            {
                return;
            }

            _outputText.Add(outLine.Data.Trim() + Environment.NewLine);

            foreach (var line in outLine.Data.SplitToLines())
            {
                if (_timeRegex.IsMatch(line))
                {
                    var start = line.Substring(1, 10);
                    var end = line.Substring(14, 10);
                    var text = line.Remove(0, 25).Trim();
                    var rt = new ResultText
                    {
                        Start = GetSeconds(start),
                        End = GetSeconds(end),
                        Text = Utilities.AutoBreakLine(text, _languageCode),
                    };

                    _resultList.Add(rt);
                }
            }
        }

        private static decimal GetSeconds(string timeCode)
        {
            return (decimal)(TimeCode.ParseToMilliseconds(timeCode) / 1000.0);
        }

        private string GenerateWavFile(string videoFileName, int audioTrackNumber)
        {
            var outWaveFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
            _filesToDelete.Add(outWaveFile);
            var process = GetFfmpegProcess(videoFileName, audioTrackNumber, outWaveFile);
            process.Start();
            ShowProgressBar();
            progressBar1.Style = ProgressBarStyle.Marquee;
            double seconds = 0;
            buttonCancel.Visible = true;
            try
            {
                process.PriorityClass = ProcessPriorityClass.Normal;
            }
            catch
            {
                // ignored
            }

            _cancel = false;
            string targetDriveLetter = null;
            if (Configuration.IsRunningOnWindows)
            {
                var root = Path.GetPathRoot(outWaveFile);
                if (root.Length > 1 && root[1] == ':')
                {
                    targetDriveLetter = root.Remove(1);
                }
            }

            while (!process.HasExited)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(100);
                seconds += 0.1;
                if (seconds < 60)
                {
                    labelProgress.Text = string.Format(LanguageSettings.Current.AddWaveform.ExtractingSeconds, seconds);
                }
                else
                {
                    labelProgress.Text = string.Format(LanguageSettings.Current.AddWaveform.ExtractingMinutes, (int)(seconds / 60), (int)(seconds % 60));
                }

                Invalidate();
                if (_cancel)
                {
                    process.Kill();
                    progressBar1.Visible = false;
                    buttonCancel.Visible = false;
                    DialogResult = DialogResult.Cancel;
                    return null;
                }

                if (targetDriveLetter != null && seconds > 1 && Convert.ToInt32(seconds) % 10 == 0)
                {
                    try
                    {
                        var drive = new DriveInfo(targetDriveLetter);
                        if (drive.IsReady)
                        {
                            if (drive.AvailableFreeSpace < 50 * 1000000) // 50 mb
                            {
                                labelInfo.ForeColor = Color.Red;
                                labelInfo.Text = LanguageSettings.Current.AddWaveform.LowDiskSpace;
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            Application.DoEvents();
            System.Threading.Thread.Sleep(100);

            if (!File.Exists(outWaveFile))
            {
                SeLogger.Error("Generated wave file not found: " + outWaveFile + Environment.NewLine +
                               "ffmpeg: " + process.StartInfo.FileName + Environment.NewLine +
                               "Parameters: " + process.StartInfo.Arguments + Environment.NewLine +
                               "OS: " + Environment.OSVersion + Environment.NewLine +
                               "64-bit: " + Environment.Is64BitOperatingSystem);
            }

            return outWaveFile;
        }

        private Process GetFfmpegProcess(string videoFileName, int audioTrackNumber, string outWaveFile)
        {
            if (!File.Exists(Configuration.Settings.General.FFmpegLocation) && Configuration.IsRunningOnWindows)
            {
                return null;
            }

            var audioParameter = string.Empty;
            if (audioTrackNumber > 0)
            {
                audioParameter = $"-map 0:a:{audioTrackNumber}";
            }

            labelFC.Text = string.Empty;
            var fFmpegWaveTranscodeSettings = "-i \"{0}\" -vn -ar 16000 -ac 1 -ab 32k -af volume=1.75 -f wav {2} \"{1}\"";
            if (_useCenterChannelOnly)
            {
                fFmpegWaveTranscodeSettings = "-i \"{0}\" -vn -ar 16000 -ab 32k -af volume=1.75 -af \"pan=mono|c0=FC\" -f wav {2} \"{1}\"";
                labelFC.Text = "FC";
            }

            //-i indicates the input
            //-vn means no video output
            //-ar 44100 indicates the sampling frequency.
            //-ab indicates the bit rate (in this example 160kb/s)
            //-af volume=1.75 will boot volume... 1.0 is normal
            //-ac 2 means 2 channels
            // "-map 0:a:0" is the first audio stream, "-map 0:a:1" is the second audio stream

            var exeFilePath = Configuration.Settings.General.FFmpegLocation;
            if (!Configuration.IsRunningOnWindows)
            {
                exeFilePath = "ffmpeg";
            }

            var parameters = string.Format(fFmpegWaveTranscodeSettings, videoFileName, outWaveFile, audioParameter);
            return new Process { StartInfo = new ProcessStartInfo(exeFilePath, parameters) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true } };
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (buttonGenerate.Enabled)
            {
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                _cancel = true;
            }
        }

        public static Process GetWhisperProcess(string waveFileName, string model, string language, DataReceivedEventHandler dataReceivedHandler = null)
        {
            // whisper --model tiny.en --language English --fp16 False a.wav
            var parameters = $"--model {model} --language \"{language}\" {Configuration.Settings.Tools.WhisperExtraSettings} \"{waveFileName}\"";
            var process = new Process { StartInfo = new ProcessStartInfo("whisper", parameters) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true } };

            if (!string.IsNullOrEmpty(Configuration.Settings.General.FFmpegLocation) && process.StartInfo.EnvironmentVariables["Path"] != null)
            {
                process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + Configuration.Settings.General.FFmpegLocation;
            }

            var whisperFolder = WhisperHelper.GetWhisperFolder();
            if (!string.IsNullOrEmpty(whisperFolder) && process.StartInfo.EnvironmentVariables["Path"] != null)
            {
                process.StartInfo.EnvironmentVariables["Path"] = process.StartInfo.EnvironmentVariables["Path"].TrimEnd(';') + ";" + whisperFolder;
            }

            if (dataReceivedHandler != null)
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += dataReceivedHandler;
                process.ErrorDataReceived += dataReceivedHandler;
            }

            process.Start();

            if (dataReceivedHandler != null)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return process;
        }

        private void linkLabelWhisperWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UiUtil.OpenUrl("https://github.com/openai/whisper");
        }

        private void AudioToText_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (comboBoxModels.SelectedItem is WhisperModel model)
            {
                Configuration.Settings.Tools.WhisperModel = model.Name;
            }

            if (comboBoxLanguages.SelectedItem is WhisperLanguage language)
            {
                Configuration.Settings.Tools.WhisperLanguageCode = language.Code;
            }

            Configuration.Settings.Tools.VoskPostProcessing = checkBoxUsePostProcessing.Checked;

            foreach (var fileName in _filesToDelete)
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void AudioToText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                if (textBoxLog.Visible)
                {
                    textBoxLog.Visible = false;
                }
                else
                {
                    UpdateLog();
                    textBoxLog.Visible = true;
                    textBoxLog.BringToFront();
                }

                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape && buttonGenerate.Enabled)
            {
                DialogResult = DialogResult.Cancel;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyData == UiUtil.HelpKeys)
            {
                UiUtil.ShowHelp("#audio_to_text");
                e.SuppressKeyPress = true;
            }
        }

        private void linkLabelOpenModelFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UiUtil.OpenFolder(WhisperModel.ModelFolder);
        }

        private void UpdateLog()
        {
            if (_outputText.IsEmpty)
            {
                return;
            }

            textBoxLog.AppendText(string.Join(Environment.NewLine, _outputText));
            _outputText = new ConcurrentBag<string>();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateLog();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            using (var form = new WhisperModelDownload { AutoClose = true })
            {
                form.ShowDialog(this);
                FillModels(comboBoxModels, form.LastDownloadedModel != null ? form.LastDownloadedModel.Name : string.Empty);
            }
        }

        private void buttonAddFile_Click(object sender, EventArgs e)
        {
            using (var openFileDialog1 = new OpenFileDialog())
            {
                openFileDialog1.Title = LanguageSettings.Current.General.OpenVideoFileTitle;
                openFileDialog1.FileName = string.Empty;
                openFileDialog1.Filter = UiUtil.GetVideoFileFilter(true);
                openFileDialog1.Multiselect = true;
                if (openFileDialog1.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                foreach (var fileName in openFileDialog1.FileNames)
                {
                    AddInputFile(fileName);
                }
            }
        }

        private void buttonRemoveFile_Click(object sender, EventArgs e)
        {
            for (var i = listViewInputFiles.SelectedIndices.Count - 1; i >= 0; i--)
            {
                listViewInputFiles.Items.RemoveAt(listViewInputFiles.SelectedIndices[i]);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            listViewInputFiles.Items.Clear();
        }

        private void buttonBatchMode_Click(object sender, EventArgs e)
        {
            _batchMode = !_batchMode;
            ShowHideBatchMode();
        }

        private void ShowHideBatchMode()
        {
            if (_batchMode)
            {
                groupBoxInputFiles.Enabled = true;
                Height = checkBoxUsePostProcessing.Bottom + progressBar1.Height + buttonCancel.Height + 450;
                listViewInputFiles.Visible = true;
                buttonBatchMode.Text = LanguageSettings.Current.Split.Basic;
                MinimumSize = new Size(MinimumSize.Width, Height - 75);
                FormBorderStyle = FormBorderStyle.Sizable;
                MaximizeBox = true;
                MinimizeBox = true;
            }
            else
            {
                groupBoxInputFiles.Enabled = false;
                var h = checkBoxUsePostProcessing.Bottom + progressBar1.Height + buttonCancel.Height + 70;
                MinimumSize = new Size(MinimumSize.Width, h - 10);
                Height = h;
                Width = _initialWidth;
                listViewInputFiles.Visible = false;
                buttonBatchMode.Text = LanguageSettings.Current.AudioToText.BatchMode;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
            }
        }

        private void AudioToText_Load(object sender, EventArgs e)
        {
            ShowHideBatchMode();
            listViewInputFiles.Columns[0].Width = -2;
        }

        private void AudioToText_Shown(object sender, EventArgs e)
        {
            buttonGenerate.Focus();
            _initialWidth = Width;
        }

        private void listViewInputFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (!buttonGenerate.Visible)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void listViewInputFiles_DragDrop(object sender, DragEventArgs e)
        {
            var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);

            System.Threading.SynchronizationContext.Current.Post(TimeSpan.FromMilliseconds(25), () =>
            {
                listViewInputFiles.BeginUpdate();
                foreach (var fileName in fileNames.OrderBy(Path.GetFileName))
                {
                    if (File.Exists(fileName))
                    {
                        AddInputFile(fileName);
                    }
                }

                listViewInputFiles.EndUpdate();
            });
        }

        private void AddInputFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if ((Utilities.AudioFileExtensions.Contains(ext) || Utilities.VideoFileExtensions.Contains(ext)) && File.Exists(fileName))
            {
                listViewInputFiles.Items.Add(fileName);
            }
        }

        private void AudioToText_ResizeEnd(object sender, EventArgs e)
        {
            listViewInputFiles.AutoSizeLastColumn();
        }

        private void listViewInputFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control) //Ctrl+V = Paste from clipboard
            {
                e.SuppressKeyPress = true;
                if (Clipboard.ContainsFileDropList())
                {
                    foreach (var fileName in Clipboard.GetFileDropList())
                    {
                        AddInputFile(fileName);
                    }
                }
                else if (Clipboard.ContainsText())
                {
                    foreach (var fileName in Clipboard.GetText().SplitToLines())
                    {
                        AddInputFile(fileName);
                    }
                }
            }
        }
    }
}
