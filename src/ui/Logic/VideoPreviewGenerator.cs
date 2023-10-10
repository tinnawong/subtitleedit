﻿using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Nikse.SubtitleEdit.Logic
{
    public class VideoPreviewGenerator
    {
        public static string GetVideoPreviewFileName()
        {
            var previewFileName = Path.Combine(Configuration.DataDirectory, "preview.mkv");
            if (File.Exists(previewFileName))
            {
                return previewFileName;
            }

            try
            {
                var process = GenerateVideoFile(previewFileName, 3, 720, 480, Color.Black, true, 25, null);
                process.Start();
                process.WaitForExit();

                return previewFileName;
            }
            catch
            {
                return null;
            }
        }

        public static Process GenerateVideoFile(string previewFileName, int seconds, int width, int height, Color color, bool checkered, decimal frameRate, Bitmap bitmap, DataReceivedEventHandler dataReceivedHandler = null, bool addTimeCode = false, string addTimeColor = "white")
        {
            Process processMakeVideo;

            if (bitmap != null)
            {
                var tempImageFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                var backgroundImage = ExportPngXml.ResizeBitmap(bitmap, width, height);
                backgroundImage.Save(tempImageFileName, ImageFormat.Png);
                processMakeVideo = GetFFmpegProcess(tempImageFileName, previewFileName, backgroundImage.Width, backgroundImage.Height, seconds, frameRate, addTimeCode, addTimeColor);
            }
            else if (checkered)
            {
                const int rectangleSize = 9;
                var backgroundImage = TextDesigner.MakeBackgroundImage(width, height, rectangleSize, Configuration.Settings.General.UseDarkTheme);
                var tempImageFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                backgroundImage.Save(tempImageFileName, ImageFormat.Png);
                processMakeVideo = GetFFmpegProcess(tempImageFileName, previewFileName, backgroundImage.Width, backgroundImage.Height, seconds, frameRate, addTimeCode, addTimeColor);
            }
            else
            {
                processMakeVideo = GetFFmpegProcess(color, previewFileName, width, height, seconds, frameRate, addTimeCode, addTimeColor);
            }

            SetupDataReceiveHandler(dataReceivedHandler, processMakeVideo);

            return processMakeVideo;
        }

        private static void SetupDataReceiveHandler(DataReceivedEventHandler dataReceivedHandler, Process processMakeVideo)
        {
            if (dataReceivedHandler != null)
            {
                processMakeVideo.StartInfo.RedirectStandardOutput = true;
                processMakeVideo.StartInfo.RedirectStandardError = true;
                processMakeVideo.OutputDataReceived += dataReceivedHandler;
                processMakeVideo.ErrorDataReceived += dataReceivedHandler;
            }
        }

        /// <summary>
        /// Generate a video with a burned-in Advanced Sub Station Alpha subtitle.
        /// </summary>
        public static Process GenerateHardcodedVideoFile(string inputVideoFileName, string assaSubtitleFileName, string outputVideoFileName, int width, int height, string videoEncoding, string preset, string crf, string audioEncoding, bool forceStereo, string sampleRate, string tune, string audioBitRate, string pass, string twoPassBitRate, DataReceivedEventHandler dataReceivedHandler = null, string cutStart = null, string cutEnd = null)
        {
            var videoEncodingSettings = string.Empty;
            if (!string.IsNullOrEmpty(videoEncoding))
            {
                videoEncodingSettings = $"-c:v {videoEncoding}";
                if (videoEncoding == "libx265")
                {
                    videoEncodingSettings += " -tag:v hvc1";
                }
            }

            var audioSettings = $"-c:a {audioEncoding}";
            if (audioEncoding != "copy")
            {
                audioSettings += $" -ar {sampleRate}";
                if (forceStereo)
                {
                    audioSettings += " -ac 2";
                }
            }

            var presetSettings = string.Empty;
            if (!string.IsNullOrEmpty(preset))
            {
                if (videoEncoding == "prores_ks")
                {
                    if (preset == "proxy")
                    {
                        preset = "0";
                    }
                    else if (preset == "lt")
                    {
                        preset = "1";
                    }
                    else if (preset == "standard")
                    {
                        preset = "2";
                    }
                    else if (preset == "hq")
                    {
                        preset = "3";
                    }
                    else if (preset == "4444")
                    {
                        preset = "4";
                    }
                    else if (preset == "4444xq")
                    {
                        preset = "5";
                    }
                    else
                    {
                        preset = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(preset))
                    {
                        presetSettings = $" -profile:v {preset}";
                    }
                }
                else
                {
                    presetSettings = $" -preset {preset}";
                }
            }

            var crfSettings = string.Empty;
            if (!string.IsNullOrEmpty(crf) && string.IsNullOrEmpty(pass))
            {
                if (videoEncoding == "h264_nvenc" || videoEncoding == "hevc_nvenc")
                {
                    crfSettings = $" -cq {crf}";
                }
                else if (videoEncoding == "h264_amf" || videoEncoding == "hevc_amf")
                {
                    crfSettings = $" -quality {crf}";
                }
                else
                {
                    crfSettings = $" -crf {crf}";
                }
            }

            var tuneParameter = string.Empty;
            if (!string.IsNullOrEmpty(tune))
            {
                tuneParameter = $" -tune {tune}";
            }

            outputVideoFileName = $"\"{outputVideoFileName}\"";

            var passSettings = string.Empty;
            if (!string.IsNullOrEmpty(pass) && !string.IsNullOrEmpty(twoPassBitRate))
            {
                passSettings = $" -b:v {twoPassBitRate} -pass {pass}";

                if (!string.IsNullOrEmpty(audioBitRate))
                {
                    passSettings += $" -b:a {audioBitRate}";
                }

                if (pass == "1")
                {
                    outputVideoFileName = Configuration.IsRunningOnWindows ? "-f null NUL" : "-f null /dev/null";
                }
            }

            if (!string.IsNullOrEmpty(cutStart))
            {
                cutStart = " " + cutStart.Trim() + " ";
            }
            else
            {
                cutStart = " ";
            }

            if (!string.IsNullOrEmpty(cutEnd))
            {
                cutEnd = " " + cutEnd.Trim() + " ";
            }
            else
            {
                cutEnd = " ";
            }

            var processMakeVideo = new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"{cutStart}-i \"{inputVideoFileName}\"{cutEnd}-vf \"ass={Path.GetFileName(assaSubtitleFileName)}\",yadif,format=yuv420p -g 30 -bf 2 -s {width}x{height} {videoEncodingSettings} {passSettings} {presetSettings} {crfSettings} {audioSettings}{tuneParameter} -use_editlist 0 -movflags +faststart {outputVideoFileName}".TrimStart(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(assaSubtitleFileName) ?? string.Empty,
                }
            };

            processMakeVideo.StartInfo.Arguments = processMakeVideo.StartInfo.Arguments.Trim();
            SetupDataReceiveHandler(dataReceivedHandler, processMakeVideo);
            return processMakeVideo;
        }

        private static Process GetFFmpegProcess(string imageFileName, string outputFileName, int videoWidth, int videoHeight, int seconds, decimal frameRate, bool addTimeCode = false, string addTimeColor = "white")
        {
            var drawText = MakeDrawText(addTimeCode, frameRate, addTimeColor);

            return new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"-t {seconds} -loop 1 -r {frameRate.ToString(CultureInfo.InvariantCulture)} -i \"{imageFileName}\" -c:v libx264 -tune stillimage -shortest -s {videoWidth}x{videoHeight}{drawText} \"{outputFileName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        private static Process GetFFmpegProcess(Color color, string outputFileName, int videoWidth, int videoHeight, int seconds, decimal frameRate, bool addTimeCode = false, string addTimeColor = "white")
        {
            var htmlColor = $"#{(color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2")).ToUpperInvariant()}";

            var drawText = MakeDrawText(addTimeCode, frameRate, addTimeColor);

            return new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"-t {seconds} -f lavfi -i color=c={htmlColor}:r={frameRate.ToString(CultureInfo.InvariantCulture)}:s={videoWidth}x{videoHeight} -c:v libx264 -tune stillimage -shortest -s {videoWidth}x{videoHeight}{drawText} \"{outputFileName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        private static string MakeDrawText(bool addTimeCode, decimal frameRate, string addTimeColor)
        {
            var drawText = string.Empty;
            if (addTimeCode)
            {
                drawText = $" -vf \"drawtext=timecode='00\\:00\\:00\\:00':r={frameRate.ToString(CultureInfo.InvariantCulture)}:x=10:y=10:fontsize=34:fontcolor={addTimeColor}\"";
            }

            return drawText;
        }

        public static string GetScreenShot(string inputFileName, string timeCode, string colorMatrix = "")
        {
            var outputFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            var vfMatrix = string.Empty;
            if (!string.IsNullOrEmpty(colorMatrix))
            {
                vfMatrix = $"-vf colormatrix={colorMatrix}";
            }

            var process = new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"-ss {timeCode} -i \"{inputFileName}\" {vfMatrix} -frames:v 1 -q:v 2 \"{outputFileName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return outputFileName;
        }

        public static string[] GetScreenShotsForEachFrame(string videoFileName, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);
            var outputFileName = Path.Combine(outputFolder, "image%05d.png");
            var process = new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"-i \"{videoFileName}\" -vf \"select=1\" -vsync vfr \"{outputFileName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return Directory.GetFiles(outputFolder, "*.png").OrderBy(p => p).ToArray();
        }

        private static string GetFfmpegLocation()
        {
            var ffmpegLocation = Configuration.Settings.General.FFmpegLocation;
            if (!Configuration.IsRunningOnWindows && (string.IsNullOrEmpty(ffmpegLocation) || !File.Exists(ffmpegLocation)))
            {
                ffmpegLocation = "ffmpeg";
            }

            return ffmpegLocation;
        }

        public static Process GenerateSoftCodedVideoFile(string inputVideoFileName, List<VideoPreviewGeneratorSub> softSubs, string outputVideoFileName, DataReceivedEventHandler outputHandler)
        {
            var subsInput = string.Empty;
            var subsMap = string.Empty;
            var subsMeta = string.Empty;
            var subsFormat = string.Empty;

            var ffmpegInfo = FfmpegMediaInfo.Parse(inputVideoFileName);
            var audioTrackCount = ffmpegInfo.Tracks.Count(p => p.TrackType == FfmpegTrackType.Audio);

            var isMp4 = outputVideoFileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);

            var count = 1;
            var number = 0;
            foreach (var softSub in softSubs)
            {
                subsInput += $" -i \"{softSub.FileName}\"";
                subsMap += $" -map {count}";

                if (!string.IsNullOrEmpty(softSub.Language))
                {
                    var lang = string.IsNullOrEmpty(softSub.Language) ? string.Empty : softSub.Language.ToLowerInvariant();
                    var threeLetterCode = Iso639Dash2LanguageCode.GetThreeLetterCodeFromTwoLetterCode(lang);
                    if (lang.Length == 3)
                    {
                        threeLetterCode = lang;
                    }
                    else if (lang.IndexOf('-') == 2)
                    {
                        threeLetterCode = Iso639Dash2LanguageCode.GetThreeLetterCodeFromTwoLetterCode(lang.Substring(0, 2));
                    }

                    var languageName = Iso639Dash2LanguageCode.List.FirstOrDefault(p => p.ThreeLetterCode == threeLetterCode)?.EnglishName;
                    if (languageName == null)
                    {
                        languageName = Iso639Dash2LanguageCode.List.FirstOrDefault(p => p.TwoLetterCode == lang || p.EnglishName.ToLowerInvariant() == lang)?.EnglishName;
                    }

                    if (!string.IsNullOrEmpty(threeLetterCode) && !string.IsNullOrEmpty(languageName))
                    {
                        subsMeta += $" -metadata:s:s:{number} language=\"{threeLetterCode}\"";
                        subsMeta += $" -metadata:s:s:{number} title=\"{languageName}\"";
                    }
                    else if (!string.IsNullOrEmpty(softSub.Language))
                    {
                        subsMeta += $" -metadata:s:s:{number} language=\"{softSub.Language}\"";
                        subsMeta += $" -metadata:s:s:{number} title=\"{softSub.Language}\"";
                    }
                }

                if (softSub.IsDefault)
                {
                    subsMeta += $" -disposition:s:s:{number} default";
                }

                if (softSub.IsForced)
                {
                    subsMeta += $" -disposition:s:s:{number} forced";
                    subsMeta += $" -metadata:s:s:{number} forced=1";
                }

                if (isMp4)
                {
                    subsFormat = " -c:s mov_text";
                }
                else if (softSub.SubtitleFormat == null && softSub.Format == "Blu-ray sup")
                {
                    subsFormat += $" -c:s:s:{number} copy"; // should be "pgs" or "pgssub" or ?
                }
                else if (softSub.SubtitleFormat?.GetType() == typeof(SubRip))
                {
                    subsFormat += $" -c:s:s:{number} srt";
                }
                else if (softSub.SubtitleFormat?.GetType() == typeof(AdvancedSubStationAlpha))
                {
                    subsFormat += $" -c:s:s:{number} ass";
                }
                else if (softSub.SubtitleFormat?.GetType() == typeof(SubStationAlpha))
                {
                    subsFormat += $" -c:s:s:{number} ssa";
                }
                else if (softSub.SubtitleFormat?.GetType() == typeof(WebVTT) ||
                         softSub.SubtitleFormat?.GetType() == typeof(WebVTTFileWithLineNumber))
                {
                    subsFormat += $" -c:s:s:{number} webvtt";
                }

                count++;
                number++;
            }

            subsInput = " " + subsInput.Trim();
            if (subsInput.Trim().Length == 0)
            {
                subsInput = string.Empty;
            }

            subsMap = " " + subsMap.Trim();
            if (subsMap.Trim().Length == 0)
            {
                subsMap = string.Empty;
            }

            subsFormat = " " + subsFormat.Trim();
            if (subsFormat.Trim().Length == 0)
            {
                subsFormat = string.Empty;
            }

            subsMeta = " " + subsMeta.Trim();
            if (subsMeta.Trim().Length == 0)
            {
                subsMeta = string.Empty;
            }

            var processMakeVideo = new Process
            {
                StartInfo =
                {
                    FileName = GetFfmpegLocation(),
                    Arguments = $"-i \"{inputVideoFileName}\"{subsInput} -map 0 -c copy -map -0:s{subsMap}{subsFormat}{subsMeta} \"{outputVideoFileName}\"".TrimStart(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            processMakeVideo.StartInfo.Arguments = processMakeVideo.StartInfo.Arguments.Trim();
            SetupDataReceiveHandler(outputHandler, processMakeVideo);
            return processMakeVideo;
        }
    }
}
