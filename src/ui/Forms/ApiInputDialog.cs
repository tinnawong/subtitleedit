using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public partial class ApiInputDialog : Form
    {
        private const string DefaultTitle = "API Input Dialog";
        private const string ButtonOkText = "OK";

        public ApiInputDialog(string title = DefaultTitle, string buttonOkText = ButtonOkText, bool isSave=false)
        {
            InitializeComponent();
            this.Text = title;
            buttonOk.Text = buttonOkText;

            // hidden checkbox for streaming
            // TODO: remove this when streaming is implemented
            checkBoxUseStreaming.Visible = false; 

            // Load settings
            textBoxApiUrl.Text = Properties.Settings.Default.SubtitleURL;
            textBoxVdoUrl.Text = Properties.Settings.Default.VdoURL;
            textBoxToken.Text = Properties.Settings.Default.Apikey;
            if (isSave)
            {
                // disable text boxes to not allow user to change them
                textBoxVdoUrl.Enabled = false;
                checkBoxUseStreaming.Enabled = false;
            }
        }

        public string SubtitleURL => textBoxApiUrl.Text;
        public string VdoURL => textBoxVdoUrl.Text;
        public string Apikey => textBoxToken.Text;

        public bool UseStreaming => checkBoxUseStreaming.Checked;

        private void ApiInputDialog_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Save settings
            Properties.Settings.Default.SubtitleURL = SubtitleURL;
            Properties.Settings.Default.VdoURL = VdoURL;
            Properties.Settings.Default.Apikey = Apikey;
            Properties.Settings.Default.Save();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
