namespace Nikse.SubtitleEdit.Forms
{
    partial class ApiInputDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelSubtitleURL = new System.Windows.Forms.Label();
            this.labelToken = new System.Windows.Forms.Label();
            this.textBoxApiUrl = new System.Windows.Forms.TextBox();
            this.textBoxToken = new System.Windows.Forms.TextBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBoxVdoUrl = new System.Windows.Forms.TextBox();
            this.labelVdoURL = new System.Windows.Forms.Label();
            this.checkBoxUseStreaming = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // labelSubtitleURL
            // 
            this.labelSubtitleURL.Location = new System.Drawing.Point(30, 24);
            this.labelSubtitleURL.Name = "labelSubtitleURL";
            this.labelSubtitleURL.Size = new System.Drawing.Size(81, 13);
            this.labelSubtitleURL.TabIndex = 0;
            this.labelSubtitleURL.Text = "Subtitle URL:";
            this.labelSubtitleURL.Click += new System.EventHandler(this.label1_Click);
            // 
            // labelToken
            // 
            this.labelToken.AutoSize = true;
            this.labelToken.Location = new System.Drawing.Point(30, 144);
            this.labelToken.Name = "labelToken";
            this.labelToken.Size = new System.Drawing.Size(41, 13);
            this.labelToken.TabIndex = 1;
            this.labelToken.Text = "Token:";
            this.labelToken.Click += new System.EventHandler(this.label2_Click);
            // 
            // textBoxApiUrl
            // 
            this.textBoxApiUrl.Location = new System.Drawing.Point(33, 40);
            this.textBoxApiUrl.Name = "textBoxApiUrl";
            this.textBoxApiUrl.Size = new System.Drawing.Size(475, 20);
            this.textBoxApiUrl.TabIndex = 2;
            this.textBoxApiUrl.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // textBoxToken
            // 
            this.textBoxToken.Location = new System.Drawing.Point(33, 161);
            this.textBoxToken.Name = "textBoxToken";
            this.textBoxToken.Size = new System.Drawing.Size(475, 20);
            this.textBoxToken.TabIndex = 3;
            // 
            // buttonOk
            // 
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Location = new System.Drawing.Point(433, 212);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 4;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(352, 212);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // textBoxVdoUrl
            // 
            this.textBoxVdoUrl.Location = new System.Drawing.Point(33, 90);
            this.textBoxVdoUrl.Name = "textBoxVdoUrl";
            this.textBoxVdoUrl.Size = new System.Drawing.Size(475, 20);
            this.textBoxVdoUrl.TabIndex = 7;
            // 
            // labelVdoURL
            // 
            this.labelVdoURL.Location = new System.Drawing.Point(30, 74);
            this.labelVdoURL.Name = "labelVdoURL";
            this.labelVdoURL.Size = new System.Drawing.Size(81, 13);
            this.labelVdoURL.TabIndex = 6;
            this.labelVdoURL.Text = "VDO URL:";
            this.labelVdoURL.Click += new System.EventHandler(this.label3_Click);
            // 
            // checkBoxUseStreaming
            // 
            this.checkBoxUseStreaming.AutoSize = true;
            this.checkBoxUseStreaming.Location = new System.Drawing.Point(33, 116);
            this.checkBoxUseStreaming.Name = "checkBoxUseStreaming";
            this.checkBoxUseStreaming.Size = new System.Drawing.Size(161, 17);
            this.checkBoxUseStreaming.TabIndex = 8;
            this.checkBoxUseStreaming.Text = "use streaming (no waveform)";
            this.checkBoxUseStreaming.UseVisualStyleBackColor = true;
            this.checkBoxUseStreaming.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // ApiInputDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(546, 265);
            this.Controls.Add(this.checkBoxUseStreaming);
            this.Controls.Add(this.labelSubtitleURL);
            this.Controls.Add(this.textBoxApiUrl);
            this.Controls.Add(this.labelVdoURL);
            this.Controls.Add(this.textBoxVdoUrl);
            this.Controls.Add(this.labelToken);
            this.Controls.Add(this.textBoxToken);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Name = "ApiInputDialog";
            this.Text = "ApiInputDialog";
            this.Load += new System.EventHandler(this.ApiInputDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSubtitleURL;
        private System.Windows.Forms.Label labelToken;
        private System.Windows.Forms.TextBox textBoxApiUrl;
        private System.Windows.Forms.TextBox textBoxToken;
        private System.Windows.Forms.TextBox textBoxVdoUrl;
        private System.Windows.Forms.Label labelVdoURL;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkBoxUseStreaming;
    }
}