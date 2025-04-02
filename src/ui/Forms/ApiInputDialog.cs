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
        public ApiInputDialog()
        {
            InitializeComponent();
        }

        public string SubtitleURL => textBoxApiUrl.Text;
        public string AdioURL => textBoxAudioUrl.Text;
        public string Token => textBoxToken.Text;

        public bool UseStreaming => checkBoxUseStreaming.Checked;

        //public string 
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

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
