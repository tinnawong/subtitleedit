using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Http;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public partial class DownloadVDO : Form
    {
        private String VDOURL;
        private String PathFile;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public DownloadVDO(String vdoUrl, String pathfile)
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);

            Text = "Downloading VDO";
            labelPleaseWait.Text = LanguageSettings.Current.General.PleaseWait;
            labelDescription1.Text = LanguageSettings.Current.GetTesseractDictionaries.Download + " VDO";
            _cancellationTokenSource = new CancellationTokenSource();
            VDOURL = vdoUrl;
            PathFile = pathfile;
        }

        public void Download(object sender, EventArgs e)
        {
            try
            {
                Utilities.SetSecurityProtocol();
                using (var httpClient = DownloaderFactory.MakeHttpClient())
                using (var downloadStream = new MemoryStream())
                {
                    var downloadTask = httpClient.DownloadAsync(VDOURL, downloadStream, new Progress<float>((progress) =>
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
                        return;
                    }

                    CompleteDownload(downloadStream);
                }
            }
            catch (Exception exception)
            {
                labelPleaseWait.Text = string.Empty;
                Cursor = Cursors.Default;
                MessageBox.Show($"Unable to download {VDOURL}!" + Environment.NewLine + Environment.NewLine +
                                exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace);
                DialogResult = DialogResult.Cancel;
                return;
            }
        }

        private void CompleteDownload(MemoryStream downloadStream)
        {
            if (downloadStream.Length == 0)
            {
                throw new Exception("No content downloaded - missing file or no internet connection!" + Environment.NewLine +
                                    $"For more info see: {Path.Combine(Configuration.DataDirectory, "error_log.txt")}");
            }

            // Write the downloaded stream to a file
            using (var fileStream = new FileStream(PathFile, FileMode.Create, FileAccess.Write))
            {
                downloadStream.WriteTo(fileStream);
            }
            downloadStream.Close();

            Cursor = Cursors.Default;
            labelPleaseWait.Text = string.Empty;
            Cursor = Cursors.Default;
            DialogResult = DialogResult.OK;
        }

    }


}
