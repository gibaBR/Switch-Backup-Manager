using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Switch_Backup_Manager
{
    public partial class FormConfigs : Form
    {
        private string autoRenamingPattern;
        private FileData gameExample;

        public FormConfigs()
        {
            InitializeComponent();

            //Hide tabs not ready
            //tabControl1.TabPages.Remove(tabPage1);
            tabControl1.TabPages.Remove(tabPage2);

            //This is Just an example, to put in the Label
            gameExample = new FileData();
            gameExample.GameName = "SUPER MARIO ODYSSEY";
            gameExample.TitleID = "0100000000010000";
            gameExample.Developer = "Nintendo";
            gameExample.GameRevision = "1.0.0";
            gameExample.IsTrimmed = true;
            gameExample.FilePath = @"c:\switch\mario.xci";
            gameExample.Group = "BigBlueBox";
            gameExample.Region = "WLD";
            gameExample.Firmware = "3.0.1";
            gameExample.Languages_resumed = "en,fr,de,it,es,nl,ru,ja";
            gameExample.IdScene = 38;

            cbxTags.Items.Clear();
            for (int i = 0; i < Util.AutoRenamingTags.Length; i++)
            {
                cbxTags.Items.Add(Util.AutoRenamingTags[i]);
            }

            LoadConfig();
        }

        public void WriteConfig()
        {
            Util.autoRenamingPattern = this.autoRenamingPattern;
            Util.ini.IniWriteValue("AutoRenaming", "pattern", this.autoRenamingPattern);

            Util.ScrapXCIOnSDCard = this.cbScrapXCIOnSD.Checked;
            Util.ScrapNSPOnSDCard = this.cbScrapNSPOnSD.Checked;
            Util.ScrapInstalledEshopSDCard = this.cbScrapLayerFSOnSD.Checked;
            Util.ini.IniWriteValue("SD", "scrapXCI", cbScrapXCIOnSD.Checked ? "true" : "false");
            Util.ini.IniWriteValue("SD", "scrapNSP", cbScrapNSPOnSD.Checked ? "true" : "false");
            Util.ini.IniWriteValue("SD", "scrapInstalledNSP", cbScrapLayerFSOnSD.Checked ? "true" : "false");

            Util.AutoUpdateNSDBOnStartup = this.cbAutoUpdateScene.Checked;
            Util.ini.IniWriteValue("Config", "autoUpdateNSWDB", cbAutoUpdateScene.Checked ? "true" : "false");

            for (int j = 1; j <= 5; j++ )
            {
                Util.ini.IniWriteValue("AutoScan", "Folder_0" + Convert.ToString(j), "");
            }

            int i = 1;
            foreach (string item in checkedListBoxAutoScanFolders.Items)
            {
                Util.ini.IniWriteValue("AutoScan", "Folder_0" + Convert.ToString(i), item+"?" + (checkedListBoxAutoScanFolders.CheckedItems.IndexOf(item) == -1 ? "0" : "1"));
                i++;
            }

        }

        public void LoadConfig()
        {
            this.autoRenamingPattern = Util.ini.IniReadValue("AutoRenaming", "pattern");
            switch (this.autoRenamingPattern)
            {
                case "{gamename}":
                    rbRenamingGameName.Checked = true;
                    break;
                case "{titleid} - {gamename}":
                    rbRenamingTitleIDGameName.Checked = true;
                    break;
                case "{titleid} - {gamename} - {releasegroup}":
                    rbRenamingTitleIDGameNameReleaseGroup.Checked = true;
                    break;
                case "{gamename} ({region})":
                    rbRenamingGameNameRegion.Checked = true;
                    break;
                case "{gamename} ({region}) ({firmware})":
                    rbRenamingGameNameRegionFirmware.Checked = true;
                    break;
                default:
                    textBoxCustomPatern.Text = autoRenamingPattern;
                    rbRenamingCustom.Checked = true;
                    break;
            }

            this.cbAutoUpdateScene.Checked = Util.AutoUpdateNSDBOnStartup;
            this.cbScrapLayerFSOnSD.Checked = Util.ScrapInstalledEshopSDCard;
            this.cbScrapNSPOnSD.Checked = Util.ScrapNSPOnSDCard;
            this.cbScrapXCIOnSD.Checked = Util.ScrapXCIOnSDCard;

            for (int i = 1; i <= 5; i++ )
            {
                string value = Util.ini.IniReadValue("AutoScan", "Folder_0" + i);
                if (value.Trim() != "")
                {
                    int ind = value.IndexOf("?");
                    if (value.Substring(ind + 1, 1) == "1")
                    {
                        checkedListBoxAutoScanFolders.Items.Add(value.Substring(0, ind), true);
                    } else
                    {
                        checkedListBoxAutoScanFolders.Items.Add(value.Substring(0, ind), false);
                    }                    
                }
            }
        }

        private void rbRenamingGameName_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingTitleIDGameName_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{titleid} - {gamename}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingTitleIDGameNameReleaseGroup_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{titleid} - {gamename} - {releasegroup}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingGameNameRegion_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename} ({region})";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingGameNameRegionFirmware_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename} ({region}) ({firmware})";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingCustom_CheckedChanged(object sender, EventArgs e)
        {
            lblExample.Text = "";
            gbCustom.Visible = true;
            this.autoRenamingPattern = textBoxCustomPatern.Text;
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
            textBoxCustomPatern.Select();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            WriteConfig();
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBoxCustomPatern_TextChanged(object sender, EventArgs e)
        {
            if (textBoxCustomPatern.Text.Trim() != "")
            {
                this.autoRenamingPattern = textBoxCustomPatern.Text;
                lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cbxTags.SelectedIndex != -1)
            {
                textBoxCustomPatern.Text = textBoxCustomPatern.Text.Insert(textBoxCustomPatern.SelectionStart, cbxTags.Text);
            }
        }

        private void btnAddFolderAutoScan_Click(object sender, EventArgs e)
        {
            if (checkedListBoxAutoScanFolders.Items.Count >= 5)
            {
                MessageBox.Show("Too many folders selected!");
                return;
            }

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                if (checkedListBoxAutoScanFolders.Items.IndexOf(selectedPath) < 0)
                {
                    checkedListBoxAutoScanFolders.Items.Add(selectedPath);
                }
            }
        }

        private void btnRemFolderAutoScan_Click(object sender, EventArgs e)
        {
            if (checkedListBoxAutoScanFolders.SelectedIndex >= 0)
            {
                checkedListBoxAutoScanFolders.Items.RemoveAt(checkedListBoxAutoScanFolders.SelectedIndex);
            }
        }
    }
}
