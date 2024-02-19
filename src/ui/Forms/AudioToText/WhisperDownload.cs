﻿using Nikse.SubtitleEdit.Core.AudioToText;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Http;
using Nikse.SubtitleEdit.Logic;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MessageBox = Nikse.SubtitleEdit.Forms.SeMsgBox.MessageBox;

namespace Nikse.SubtitleEdit.Forms.AudioToText
{
    public sealed partial class WhisperDownload : Form
    {
        private const string DownloadUrl64Cpp = "https://github.com/ggerganov/whisper.cpp/releases/download/v1.5.2/whisper-blas-bin-x64.zip";
        private const string DownloadUrl32Cpp = "https://github.com/ggerganov/whisper.cpp/releases/download/v1.5.2/whisper-blas-bin-Win32.zip";
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _whisperChoice;

        private static readonly string[] Sha512HashesCpp =
        {
            "1667a86007a6f6d36a94fae0c315c3321eb2572274be8ac540d141be198993d306554aabce1b5f34ac10ffdae09b4c227efba8a4f16978addd82836dc2156c34", // v1.5.2/whisper-blas-bin-x64.zip
            "647c727417bc6a7c90c7460100214426fc1b82fee1ce9924eaec71b46466920b1045c1a534a72782a0d6dcc31541a85a5ad62bfb635c815738c95fafea368cd4", // v1.5.2/whisper-blas-bin-Win32.zip
        };

        private static readonly string[] OldSha512HashesCpp =
        {
            "4dad22644af9770ecd05f1959adbe516e0948fb717d0bc33d5f987513f619162159aa2092b54a535e909846caca8dbf53f34c9060dadb43fc57b2c28e645dd73", // v1.5.1/whisper-blas-bin-x64.zip
            "00af057d6ba4005ac1758a713bbe21091796202a81ec6b7dcce5cd9e7680734730c430b75b88a6834b76378747bc4edbcf14a4ed7429b07ea9a394754f4e3368", // v1.5.1/whisper-blas-bin-Win32.zip
            "102cd250958c3158b96453284f101fadebbb0484762c78145309f7d7499aa2b9c9e01e5926a634bd423aee8701f65c7d851a19cb5468364697e624a2c53a325d", // v1.5.0/whisper-blas-bin-x64.zip
            "0bc8df7ca4fdd32a80a9f8e7568b8668221a4205ff8fc3d04963081c14677c6a97e4510e7bb12d7b110fc9a88553aeaa53eff558262ff2d725cef52b3100b149", // v1.5.0/whisper-blas-bin-Win32.zip
            "fc1878c3b7200d0531c376bbe52319a55575e3ceeeacecbee54a366116c30eb1aa3d0a34c742f9fd5a47ffb9f24cba75653d1498e95e4f6f86c00f6d5e593d2a", // v1.4.0/whisper-blas-bin-x64.zip
            "44cb0f326ece26c1b41bd0b20663bc946990a7c3b56150966eebefb783496089289b6002ce93d08f1862bf6600e9912ac62057c268698672397192c55eeb30a2", // v1.4.0/whisper-blas-bin-Win32.zip
            "e193e845380676b59deade82d3f1de70ac54da3b5ffa70b4eabb3a2a96ad312d8f197403604805cef182c85c3a3370dd74a2b0b7bccf2d95b1022c10ce8c7b79", // 64-bit OpenBLAS
            "4218423f79d856096cdc8d88aad2e361740940e706e0b1d07dc3455571022419ad14cfef717f63e8fc61a7a1ef67b6722cec8b3c4c25ad7f087a23b1b89c5d91", // 32-bit OpenBLAS
            "a6a75a5d63b933c3529a500b7dd8b330530894b09461bb0a715dbedb31bf2e3493238e86af6d7cc64f3af196a6d61d96bb23853f98d21c8172d5d53d7aad33d9", // 64-bit OpenBLAS
            "92f64f207c400c7c0f1fc27006bf2a1e4170fdc63d045dfdf0a0848b3d727f2763eccfb55e10b6e745e9d39892d24cb9b4c471594011d041458c1ff8722e1ffc", // 32-bit OpenBLAS
            "f2073d5ce928e59f7717a82f0330e4d628c81e6cb421b934b4792ac16fb9a33fb9482812874f39d4c7ca02a47a9739d5dd46ddc2e0abc0eb1097dc60bb0616b2", // AVX2 64-bit
            "0d65839e3b05274b3edeef7bbb123c9a6ba04b89729011b758210a80f74563a1e58ca7da0a25e63e42229a2d3dd57a2cb6ce993474b13381871f343b75c3140f", // SSE2 64-bit
            "9f9ce1b39610109bc597b296cb4c1573fa61d33eeaef2a38af44bb2d696fa7c1da297520630ada2470d740edb18a17fe3cca922ad12a307476e27862906450e5", // AVX2 32-bit
            "aab8e7349a7051fb35f2294da3c4993731f47ce2d45ba4c6d4b2b106d0e3236a0082b68e67eb612fec1540e60ae9994183bd41f7fee31c23ec192cbd4155e3c2", // SSE2 32-bit
            "b69bd16bd4d11191b7b1980157d09cb1e489c804219cd336cd0b58182d357b5fff491b29ab8796d1991a9c8f6c8537f475592400b7f4e1244fdfdda8c970a80c", // AVX2 64-bit
            "8e45e147397b688e0ff814f6ef87fd6703748a4f9170fa6498b9428db47bbf9140c7479d016b8e201340ac6627e3f9632c70aa36e7a883355b9abf30e6796ae9", // SSE2 64-bit
            "87799433a5a29b3beaa5a58dfc22471e2c1eb1c9821b6a337b40d8e3c1b4dae9118e10d3a278664fe0f36ba6543ac554108593045640f62711b95f4c2a113b74", // SSE2 32-bit
            "58834559f7930c8c3dff6e20963cb86a89ca0228752d35b2486f907e59435a9adc5f5fb13c644f66bedea9ce0368c193d9632366719d344abbd3c0eb547e7110", // SSE2 64-bit
            "999863541ffbbce142df271c7577f31fef3f386e3ccbb0c436cb21bb13c7557a28602a2f2c25d6a32a6bca7953a21e086a4af3fbdc84b295e994d3452d3af5bc",
            "3c8a360d1e097d229500f3ccdd66a6dc30600fd8ea1b46405ed2ec03bb0b1c26c72cac983440b5793d24d6983c3d76482a17673251dd89f0a894a54a6d42d169", // AVX2 64-bit
            "96f8e6c073afc75062d230200c9406c38551d8ce65f609e433b35fb204dc297b415eb01181adb6b1087436ae82c4e582b20e97f6b204acf446189bde157187b7", // AVX2 32-bit
            "2a9e10f746a1ebe05dffa86e9f66cd20848faa6e849f3300c2281051c1a17b0fc35c60dc435f07f5974aa1191000aaf2866a4f03a5fe35ecffd4ae0919778e63", // SSE2 32-bit
        };


        private const string DownloadUrl64CppCuBlas = "https://github.com/ggerganov/whisper.cpp/releases/download/v1.5.2/whisper-cublas-12.2.0-bin-x64.zip";

        private static readonly string[] Sha512HashesCppCuBlas =
        {
            "d2b74dc753602447d32d9acfe583751516357461302a5a7b01b9727a917d6cc7c07bf7c68011d4d19042ab87b298e556912394995d8c0d067ff0814ed7690918", // v1.5.2/whisper-cublas-12.2.0-bin-x64.zip
        };

        private static readonly string[] OldSha512HashesCppCuBlas =
        {
            "2bb46c3a4337ff1273299bf068720e5433dadff914629122bc01f2910b18c129c14a526ae5db57d4cff563afd46b6de63d5980e6be8d9e64b589edc9254f5df6", // v1.5.1/whisper-cublas-12.2.0-bin-x64.zip
            "b17175eb9e2e4359c54dcf207d70f5f0edfacbded795d402873e86ad5466f182a172e4ca1b42a72e76e191452098431c65edbe343f542c5d8532f7909c93e919", // 1.5.1
            "f5bbad24cb99333cb6d9ec95e6797dda9e36f834fc0d4cee84d839637a06b5564df09b49f5ca6c16b2cf681429155b1ceee808d411024da658fba52d503cebdf", // 1.5.0 second upload
            "de5b6fe7487f4cdc5e883ef6825dd0ecfe3ce4f9c914b0e02ba19b89c138e47e76584ae221a75eb7aed1a96893d4764401e065e196e0455a2e72050209252780", // 1.5.0 first upload
        };

        private const string DownloadUrlConstMe = "https://github.com/Const-me/Whisper/releases/download/1.12.0/cli.zip";

        private static readonly string[] Sha512HashesConstMe =
        {
            "bc329789c58887f14cfcdb14d4ced4e6322b6f8c8c4625bddc40321a46e9bae7c45107f4a757793b02d0df456ac839d4ef66529e79d3144b1813a2ae49e7a1ca", // 1.12
        };

        private static readonly string[] OldSha512HashesConstMe =
        {
            "e7169149864f3385df2904aadff224f66e39e93dff57135b078c8d8c44947b07fdcd57ce10221533afd417e3e864b8562a4d605b008be4efd6e208bb7b43efcd",
            "18e5aab30946e27d7ee88a11d88c4582be980fe4e6a86db85609d81067778b0014a9de5bed6c82176aa6f3ba7b4e0c8f13b3c1b91f6883529161fa54f2a00e7e",
            "76b9004121fb152cc11641ce4afb33de4e503d549dcfb4f1e17b5f2655d5bb8e912120b4b273937693014cdb6bbb242210b0572b4de767d7bd0d7e0c4144f3c8",
            "a4681b139c93d7b4b6cefbb4d72de175b3980a4c6052499ca9db473e817659479d2ef8096dfd0c50876194671b09b25985f6db56450b6b5f8a4117851cfd9f1f",
        };


        private const string DownloadUrlPurfviewFasterWhisper = "https://github.com/Purfview/whisper-standalone-win/releases/download/faster-whisper/Whisper-Faster_r167.2_windows.zip";

        private static readonly string[] Sha512HashesPurfviewFasterWhisper =
        {
            "a16e2b5460d7f4b0d45de3f0e07b231d58ad4c79d077ad6b9c84a4e2ced4bd1cd3a7d9f01689f1d847ec8ff59c8f81cb742fcf2b153291ed6f15ec8b27adb998", // r167.2
        };

        private static readonly string[] OldSha512HashesPurfviewFasterWhisper =
        {
            "1995feca9dd971eccfb41f8dc330d418a531e615cee56eac7cc053fd343fe5200f9e64e2b4feafdde49b018ac518d1ee1b244aedd32dcb84e3fb69c1035b8a4f", // r160.7
            "10ac03f098f991fe9474430a7f44c6fe0574dfb88d37ea4a31b764c540337918c529c4eceaf0524e88975b11b771c61dd67501d2a59fe05008a10195d2768edf", // r160.6
            "9d65922c41a8848e70f04af8deed7279f827264e1fa305c165849e391917713f0336eee07320b2c2cbb6191167953f4d6d1e23a378bfa5a4273c6065a0eba5b3", // r160.5
            "ab2d9f0955a618474cb07141837f280192d6fe9198bab56a62c3e4e76c8bfd6a7a1b8e2d1ce106993e00e00a3305c24f17ec53d5829174dd51a69ad0f82e4b63", // r160.4
            "f66572f08bd93f684c91e40bf873c9c5207d3558ddbea2edaecd6e673300d0349e26ad41e084b7b8a4b74993fb1fd51acb4b9858f7a7c7e9ef1df4de00d07646", // r160.3
            "6a3a0e2e7ae69ec259a0d347bf0970cb276d1ce271a71e8785729fe4a453e71e807e31599223ce2d65f6d8eb8e52d6eee53c3d1d22c373e407155d7717a45ceb", // r160
            "d5d81f3450254f988537bba400b316983fba80142027dbae7ed5abcb06ef6ec367dd2f0699f4096458e783573e02b117d8489b4fa03294dc928b40178d316daa", // r153
            "c40229a04f67409bf92b58a5f959a9aed3cb9bf467ae6d7dd313a1dc6bff7ed6a2f583a3fa8913684178bf14df71d49bdb933cf9ef8bb6915e4794fc4a2dff08", // r149.1
            "22e07106f50301b9a7b818791b825a89c823df25e25e760efd45a7b3b1ea5a5d2048ed24e669729f8bd09dade9ea3902e6452564dd90b88d85cd046cc9eb6efc", // r146
            "fee96c9f8f3a9b67c2e1923fa0f5ef388d645aa3788b1b00c9f12392ef2db4b905d84f5c00ab743a284c8ba2750121e08e9fee1edc76d9c0f12ae51d61b1b12a", // r145.3.zip
            "b689f5ff7329f0ae8f08e3d42b1a2f71bcbe2717cf1231395791cf3b90e305ba4e92955a62ebe946a73c5ca83f61bc60b2e4cff1065cc0f49cfc1f3c665fa587", // r145.2 
            "75ba2bcee9fef0846e54ce367df3fb54f3b9f4cb0f8ac33f01bdde44dc313cd01b3263b43c899271af5901f765ef6257358dcf68c11024652299942405afe289", //  r145.1
            "5414c15bb1682efc2f737f3ab5f15c4350a70c30a6101b631297420bbc4cb077ef9b88cb6e5512f4adcdafbda85eb894ff92eae07bd70c66efa0b28a08361033", // Whisper-Faster r141.4
        };

        private const string DownloadUrlPurfviewFasterWhisperCuda = "https://github.com/Purfview/whisper-standalone-win/releases/download/libs/cuBLAS.and.cuDNN_win_v2.zip";

        private static readonly string[] Sha512HashesPurfviewFasterWhisperCuda =
        {
            "6f3f12162b4537cc8a6dadd51718dc72b3f36dccc27e6c4b5f56d0cfca06bcddaa6877a1984873c0d3ac22ab5fbf56c4cbb87e437d655363b55df4c632574291", // V2
//            "d5bf3ba7fd8a2af7790ff4349175692f9bdecea81e60abb8ad8b88de9f6892d1bfbbdb5f87dcc2fd9cc355760f2894e4928805e3a833c8b9d77698c0c3e94e8c", // V3
        };

        private static readonly string[] OldSha512HashesPurfviewFasterWhisperCuda =
        {
            "8d3499298bf4ee227c2587ab7ad80a2a6cbac6b64592a2bb2a887821465d20e19ceec2a7d97a4473a9fb47b477cbbba8c69b8e615a42201a6f5509800056a73b",
        };


        public WhisperDownload(string whisperChoice)
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
            Text = LanguageSettings.Current.GetTesseractDictionaries.Download;
            labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait;
            labelDescription1.Text = LanguageSettings.Current.GetTesseractDictionaries.Download + " " + whisperChoice;
            _cancellationTokenSource = new CancellationTokenSource();
            _whisperChoice = whisperChoice;
        }

        private void WhisperDownload_Shown(object sender, EventArgs e)
        {
            var downloadUrl = IntPtr.Size * 8 == 32 ? DownloadUrl32Cpp : DownloadUrl64Cpp;
            if (_whisperChoice == WhisperChoice.CppCuBlas)
            {
                downloadUrl = DownloadUrl64CppCuBlas;
            }
            else if (_whisperChoice == WhisperChoice.ConstMe)
            {
                downloadUrl = DownloadUrlConstMe;
            }
            else if (_whisperChoice == WhisperChoice.PurfviewFasterWhisper)
            {
                downloadUrl = DownloadUrlPurfviewFasterWhisper;
            }
            else if (_whisperChoice == WhisperChoice.PurfviewFasterWhisperCuda || _whisperChoice == WhisperChoice.CppCuBlasLib)
            {
                downloadUrl = DownloadUrlPurfviewFasterWhisperCuda;
            }

            try
            {
                var httpClient = DownloaderFactory.MakeHttpClient();
                using (var downloadStream = new MemoryStream())
                {
                    var downloadTask = httpClient.DownloadAsync(downloadUrl, downloadStream, new Progress<float>((progress) =>
                    {
                        var pct = (int)Math.Round(progress * 100.0, MidpointRounding.AwayFromZero);
                        labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait + "  " + pct + "%";
                        labelPleaseWait.Refresh();
                    }), _cancellationTokenSource.Token);

                    while (!downloadTask.IsCompleted && !downloadTask.IsCanceled)
                    {
                        Application.DoEvents();
                    }

                    if (downloadTask.IsCanceled)
                    {
                        DialogResult = DialogResult.Cancel;
                        labelPleaseWait.Refresh();
                        return;
                    }

                    CompleteDownload(downloadStream);
                }
            }
            catch (Exception exception)
            {
                labelPleaseWait.Text = string.Empty;
                Cursor = Cursors.Default;
                MessageBox.Show($"Unable to download {downloadUrl}!" + Environment.NewLine + Environment.NewLine +
                                exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
                DialogResult = DialogResult.Cancel;
            }
        }

        private void CompleteDownload(MemoryStream downloadStream)
        {
            if (downloadStream.Length == 0)
            {
                throw new Exception("No content downloaded - missing file or no internet connection!");
            }

            downloadStream.Position = 0;
            var hash = Utilities.GetSha512Hash(downloadStream.ToArray());
            string[] hashes;
            if (_whisperChoice == WhisperChoice.CppCuBlas)
            {
                hashes = Sha512HashesCppCuBlas;
            }
            else if (_whisperChoice == WhisperChoice.ConstMe)
            {
                hashes = Sha512HashesConstMe;
            }
            else if (_whisperChoice == WhisperChoice.PurfviewFasterWhisper)
            {
                hashes = Sha512HashesPurfviewFasterWhisper;
            }
            else if (_whisperChoice == WhisperChoice.PurfviewFasterWhisperCuda || _whisperChoice == WhisperChoice.CppCuBlasLib)
            {
                hashes = Sha512HashesPurfviewFasterWhisperCuda;
            }
            else
            {
                hashes = Sha512HashesCpp;
            }

            if (!hashes.Contains(hash))
            {
                MessageBox.Show("Whisper SHA-512 hash does not match!"); ;
                DialogResult = DialogResult.Cancel;
                return;
            }

            var folder = Path.Combine(Configuration.DataDirectory, "Whisper");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (_whisperChoice == WhisperChoice.Cpp)
            {
                folder = Path.Combine(folder, "Cpp");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            if (_whisperChoice == WhisperChoice.CppCuBlas)
            {
                folder = Path.Combine(folder, WhisperChoice.CppCuBlas);

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            if (_whisperChoice == WhisperChoice.ConstMe)
            {
                folder = Path.Combine(folder, "Const-me");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                try
                {
                    File.WriteAllText(Path.Combine(folder, "models.txt"), "Whisper Const-me uses models from Whisper.cpp");
                }
                catch
                {
                    // ignore
                }
            }

            if (_whisperChoice == WhisperChoice.PurfviewFasterWhisper)
            {
                folder = Path.Combine(folder, "Purfview-Whisper-Faster");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                using (var zip = ZipExtractor.Open(downloadStream))
                {
                    var dir = zip.ReadCentralDir();
                    foreach (var entry in dir)
                    {
                        if (entry.FilenameInZip.EndsWith(WhisperHelper.GetExecutableFileName(WhisperChoice.PurfviewFasterWhisper)))
                        {
                            var path = Path.Combine(folder, Path.GetFileName(entry.FilenameInZip));
                            zip.ExtractFile(entry, path);
                        }
                    }
                }
            }
            else if (_whisperChoice == WhisperChoice.PurfviewFasterWhisperCuda ||
                     _whisperChoice == WhisperChoice.CppCuBlasLib)
            {

                if (Configuration.Settings.Tools.WhisperChoice == WhisperChoice.CppCuBlas)
                {
                    folder = Path.Combine(folder, WhisperChoice.CppCuBlas);
                }
                else
                {
                    folder = Path.Combine(folder, "Purfview-Whisper-Faster");
                }

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                using (var zip = ZipExtractor.Open(downloadStream))
                {
                    var dir = zip.ReadCentralDir();
                    foreach (var entry in dir)
                    {
                        if (entry.FileSize > 0)
                        {
                            var path = Path.Combine(folder, Path.GetFileName(entry.FilenameInZip));
                            zip.ExtractFile(entry, path);
                        }
                    }
                }
            }
            else
            {
                var skipFileNames = new[] { "command.exe", "stream.exe", "talk.exe", "bench.exe" };
                using (var zip = ZipExtractor.Open(downloadStream))
                {
                    var dir = zip.ReadCentralDir();
                    foreach (var entry in dir)
                    {
                        var path = Path.Combine(folder, entry.FilenameInZip);
                        if (!skipFileNames.Contains(entry.FilenameInZip))
                        {
                            zip.ExtractFile(entry, path);
                        }
                    }
                }
            }

            Cursor = Cursors.Default;
            labelPleaseWait.Text = string.Empty;
            DialogResult = DialogResult.OK;
        }

        public static bool IsOld(string fullPath, string whisperChoice)
        {
            var hash = Utilities.GetSha512Hash(FileUtil.ReadAllBytesShared(fullPath));

            if (whisperChoice == WhisperChoice.ConstMe)
            {
                return OldSha512HashesConstMe.Contains(hash);
            }

            if (whisperChoice == WhisperChoice.PurfviewFasterWhisper)
            {
                return OldSha512HashesPurfviewFasterWhisper.Contains(hash);
            }

            return OldSha512HashesCpp.Contains(hash);
        }

        public static bool IsOldVersion(string fullPath, string whisperChoice)
        {
            var hash = Utilities.GetSha512Hash(FileUtil.ReadAllBytesShared(fullPath));

            if (whisperChoice == WhisperChoice.ConstMe)
            {
                var hashVer111 = "1885a6818287a552e955e4df7876d26810a05acd979e8ca3920f78f434965fe0e2052419a6ea089d8d1fa8158a5f7251935e972bc8dcda76abbb0001782d9ec0";
                return hash == hashVer111;
            }

            if (whisperChoice == WhisperChoice.PurfviewFasterWhisper)
            {
                var hashVersion153 = "5e5822bc2d7a5b0d7e50f35460cbbf5bd145eaef03fa7cb3001d4c43622b7796243692a5ce3261e831b9d935f2441bbf7edbe8f119ea92c53fe077885fd10708";
                var hashVersion160_3 = "104b85753ce74a81cdec13a2f8665d6af8c2974a3ebef8833cccad15624f311ae17a0e9b9325e3c25cf34edf024127f824c5f000069d7e52459123c9546e1266";
                var hashVersion160_4 = "88d1f0c620b0f7c0e87d41da0fd67a3f8b8fb8083e6bb6579d3ca3ee8f7ec919458518965f0071f49068384b86667871213aa90370bcd65c5d9334e7dd1b681e";
                var hashVersion160_5 = "6983c90c96e47f53fb1451c1f0a32151ef144fe2e549affc7319d0c7666ea44dcbb0d7dc87ccdaaf0b3d8b2abe92060440e151495109f2681b99940f0eec5ad0";
                var hashVersion160_6 = "f616a4fecfb40e74b3e096207f08fbe84a0d08ad872380cf2791eba8458ed854399de2d547be98bc35c65ce0b6959a149b981e745aa75876ffa8eb2fc6a8719e";
                var hashVersion160_7 = "0f6b5b0a8d3d169ca7947866552dec30ac43406cda6b7e748c273ed78574087e330571925d8a36d48e5a3ea197d450be0289277677fdbad069038ac0788ea82e";
                return hash == hashVersion153 || hash == hashVersion160_3 || hash == hashVersion160_4 || hash == hashVersion160_5 || hash == hashVersion160_6 || hash == hashVersion160_7;
            }

            if (whisperChoice == WhisperChoice.Cpp)
            {
                var version130WhisperBlasBinX64 = "9164d033ac8bb9a2f4694da570c9878d24dcaee0bd2eedd26692493a47f916973f3e555c688ba28b337a57dc7effda9a116c1ed5bb8a620ce2c7d5ce42148a64";
                var version130WhisperBlasBinX32 = "ddf75452afc283ada3d686c4cd3eb8bd79b98a4960549585916c9523dee3f9c1a1176a59fa04ffdb33e070e0eaac12b1a263f790afa1dfd4bcb806c02431469d";
                var version140WhisperBlasBinX64 = "c43fed38d1ae99e6fbbd8c842c2d550b4949081c0c7fba72cd2e2e8435ff05eac4f64e659efb09d597c3c062edf1e5026acc375d2a07290fa3c0fca9ac3bd7a2";
                var version150WhisperBlasBinX64 = "3d98347bd89f37dcfaa97ad6a69ee84435ae209d3c2c30b407122f94d5fd512d86fef6f7699f09f8e3adf6236bc3757860b8784406c666328da968964411ecc7";
                var version150WhisperBlasBinX32 = "708a9b98c474f3523b73fd6a41105feb76a07ccf23220b6f068ed5c7a5720ed9b8d851af55368d3a479f5fdf1ae055ac07d128cc951cca70ba4950daab79cc5f";
                var version151WhisperBlasBinX64 = "bcdc1716ddf3e3e08edae6762a6a6122e7ef158ee84bd183669d7ce696bc13253d8645912154bf73d914248b64989aaafb0aa504654d5a117a102303f59d210c";
                var version151WhisperBlasBinX32 = "921012e6de8f6801cc8240ffbbc655826bcd60c5e564e7483cb492082f77fcd8fdb84a46f0d4ea393f9f713863d11a0989e6b13f68bb87ecfd08366b215700e6";
                return hash == version130WhisperBlasBinX64 ||
                       hash == version130WhisperBlasBinX32 ||
                       hash == version140WhisperBlasBinX64 ||
                       hash == version150WhisperBlasBinX64 ||
                       hash == version150WhisperBlasBinX32 ||
                       hash == version151WhisperBlasBinX64 ||
                       hash == version151WhisperBlasBinX32;
            }

            if (whisperChoice == WhisperChoice.CppCuBlas)
            {
                var version150WhisperCppCublass = "b1a88508c07c61f3ed3998ca61b730e389f6a8dddbefbbeb84641dc3ba953fad5696b3e09900327fe3e74e3a7bd9ddacf2caed7e55baa1aa736af434aff73ac7";
                var version151WhisperCppCublass = "3d7f86d816785b980734ccffeb1209b0218bbfbc7cc4e34f6d5b7999d63cf99e36e253db3f88ace0dbfed19ac54c3d04d2fcbb37f39f3df3cc1c3ef1be8bae65";
                return hash == version150WhisperCppCublass ||
                       hash == version151WhisperCppCublass;
            }

            return false;
        }
    }
}
