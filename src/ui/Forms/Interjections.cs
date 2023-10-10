﻿using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MessageBox = Nikse.SubtitleEdit.Forms.SeMsgBox.MessageBox;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class Interjections : Form
    {
        private List<string> _interjections;

        public Interjections()
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
        }

        public string GetInterjectionsSemiColonSeparatedString()
        {
            var sb = new StringBuilder();
            foreach (var s in _interjections)
            {
                sb.Append(';');
                sb.Append(s.Trim());
            }

            return sb.ToString().Trim(';');
        }

        private void Interjections_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyData == UiUtil.HelpKeys)
            {
                UiUtil.ShowHelp("#remove_text_for_hi");
            }
        }

        public void Initialize(string semiColonSeparatedList)
        {
            _interjections = new List<string>();
            var arr = semiColonSeparatedList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in arr)
            {
                _interjections.Add(s.Trim());
            }
            FillListBox();
            Text = LanguageSettings.Current.Interjections.Title;

            // Add to interjections (or general)
            buttonRemove.Text = LanguageSettings.Current.Settings.Remove;
            buttonAdd.Text = LanguageSettings.Current.MultipleReplace.Add;

            buttonCancel.Text = LanguageSettings.Current.General.Cancel;
            buttonOK.Text = LanguageSettings.Current.General.Ok;
            UiUtil.FixLargeFonts(this, buttonOK);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            var text = textBoxInterjection.Text.Trim();
            if (text.Length == 0)
            {
                return;
            }

            if (!_interjections.Contains(text))
            {
                _interjections.Add(text);
                FillListBox();
                textBoxInterjection.Text = string.Empty;
                textBoxInterjection.Focus();
                for (var i = 0; i < listBoxInterjections.Items.Count; i++)
                {
                    if (listBoxInterjections.Items[i].ToString() == text)
                    {
                        listBoxInterjections.SelectedIndex = i;
                        var top = i - 5;
                        if (top < 0)
                        {
                            top = 0;
                        }

                        listBoxInterjections.TopIndex = top;
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show(LanguageSettings.Current.Settings.WordAlreadyExists);
            }
        }

        private void FillListBox()
        {
            _interjections.Sort();
            listBoxInterjections.BeginUpdate();
            listBoxInterjections.Items.Clear();
            foreach (var s in _interjections)
            {
                listBoxInterjections.Items.Add(s);
            }
            listBoxInterjections.EndUpdate();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            var index = listBoxInterjections.SelectedIndex;
            var text = listBoxInterjections.Items[index].ToString();
            if (index >= 0)
            {
                if (MessageBox.Show(string.Format(LanguageSettings.Current.Settings.RemoveX, text), null, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _interjections.Remove(text);
                    listBoxInterjections.Items.RemoveAt(index);
                    if (index < listBoxInterjections.Items.Count)
                    {
                        listBoxInterjections.SelectedIndex = index;
                    }
                    else if (listBoxInterjections.Items.Count > 0)
                    {
                        listBoxInterjections.SelectedIndex = index - 1;
                    }

                    listBoxInterjections.Focus();

                    return;
                }
                MessageBox.Show(LanguageSettings.Current.Settings.WordNotFound);
            }
        }

        private void listBoxInterjections_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonRemove.Enabled = listBoxInterjections.SelectedIndex >= 0;
        }

        private void textBoxInterjection_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonAdd_Click(null, null);
            }
        }

        private void textBoxInterjection_TextChanged(object sender, EventArgs e)
        {
            buttonAdd.Enabled = textBoxInterjection.Text.Trim().Length > 0;
        }
    }
}
