﻿using Nikse.SubtitleEdit.Core.AudioToText;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Logic;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms.AudioToText
{
    public sealed partial class WhisperModelDownload : Form
    {
        public bool AutoClose { get; internal set; }
        public WhisperModel LastDownloadedModel { get; private set; }
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _error;
        private string _downloadFileName;

        public WhisperModelDownload()
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
            Text = string.Format(LanguageSettings.Current.Settings.DownloadX, "Whisper models");
            buttonDownload.Text = LanguageSettings.Current.GetDictionaries.Download;
            buttonCancel.Text = LanguageSettings.Current.General.Cancel;
            UiUtil.FixLargeFonts(this, buttonDownload);

            var selectedIndex = 0;
            foreach (var downloadModel in WhisperHelper.GetWhisperModel().Models)
            {
                var fileName = MakeDownloadFileName(downloadModel);
                downloadModel.AlreadyDownloaded = File.Exists(fileName);

                comboBoxModels.Items.Add(downloadModel);
                if (selectedIndex == 0 && downloadModel.Name == "English")
                {
                    selectedIndex = comboBoxModels.Items.Count - 1;
                }
            }

            comboBoxModels.SelectedIndex = selectedIndex;
            labelPleaseWait.Text = string.Empty;
            textBoxError.Visible = false;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void AudioToTextDownload_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyData == UiUtil.HelpKeys)
            {
                UiUtil.ShowHelp("#audio_to_text");
                e.SuppressKeyPress = true;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (comboBoxModels.SelectedIndex < 0)
            {
                return;
            }

            LastDownloadedModel = (WhisperModel)comboBoxModels.Items[comboBoxModels.SelectedIndex];
            var url = _error ? LastDownloadedModel.UrlSecondary : LastDownloadedModel.UrlPrimary;
            try
            {
                labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait;
                buttonDownload.Enabled = false;
                Refresh();
                Cursor = Cursors.WaitCursor;
                var httpClient = HttpClientHelper.MakeHttpClient();

                var folder = WhisperHelper.GetWhisperModel().ModelFolder;
                if (!Directory.Exists(folder))
                {
                    WhisperHelper.GetWhisperModel().CreateModelFolder();
                }

                _downloadFileName = MakeDownloadFileName(LastDownloadedModel) + ".$$$";
                using (var downloadStream = new FileStream(_downloadFileName, FileMode.Create, FileAccess.Write))
                {
                    var downloadTask = httpClient.DownloadAsync(url, downloadStream, new Progress<float>((progress) =>
                    {
                        var pct = (int)Math.Round(progress * 100.0, MidpointRounding.AwayFromZero);
                        labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait + "  " + pct + "%";
                    }), _cancellationTokenSource.Token);

                    while (!downloadTask.IsCompleted && !downloadTask.IsCanceled)
                    {
                        Application.DoEvents();
                    }

                    if (downloadTask.IsCanceled)
                    {
                        DialogResult = DialogResult.Cancel;
                        labelPleaseWait.Refresh();
                        try
                        {
                            File.Delete(_downloadFileName);
                        }
                        catch
                        {
                            // ignore
                        }
                        return;
                    }

                    CompleteDownload(downloadStream);
                }
            }
            catch (Exception exception)
            {
                labelPleaseWait.Text = string.Empty;
                buttonDownload.Enabled = true;
                Cursor = Cursors.Default;
                MessageBox.Show($"Unable to download {url}!" + Environment.NewLine + Environment.NewLine +
                                exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
                _error = true;
            }
        }

        private static string MakeDownloadFileName(WhisperModel model)
        {
            return Path.Combine(WhisperHelper.GetWhisperModel().ModelFolder, model.Name + WhisperHelper.ModelExtension());
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource.Cancel();
            DialogResult = DialogResult.Cancel;
        }

        private void CompleteDownload(Stream downloadStream)
        {
            if (downloadStream.Length == 0)
            {
                throw new Exception("No content downloaded - missing file or no internet connection!");
            }

            var newFileName = _downloadFileName.Replace(".$$$", string.Empty);
            try
            {
                File.Delete(newFileName);
            }
            catch
            {
                // ignore
            }

            downloadStream.Flush();
            downloadStream.Close();

            Application.DoEvents();
            File.Move(_downloadFileName, newFileName);

            Cursor = Cursors.Default;
            labelPleaseWait.Text = string.Empty;

            if (AutoClose)
            {
                DialogResult = DialogResult.OK;
                return;
            }

            buttonDownload.Enabled = true;
            labelPleaseWait.Text = string.Format(LanguageSettings.Current.SettingsFfmpeg.XDownloadOk, "Whisper model");
        }
    }
}
