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
        private string autoRenamingPatternNSP;
        private FileData gameExample;
        private FileData gameExampleNSP;

        public FormConfigs()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;

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
            gameExample.Languages = new List<string> { "American English", "British English", "Japanese", "French", "German",
                "Latin American Spanish", "Spanish", "Italian", "Dutch", "Canadian French", "Russian" };
            gameExample.Languages_resumed = "en,fr,de,it,es,nl,ru,ja";
            gameExample.IdScene = 38;
            gameExample.ContentType = "Patch";
            gameExample.Version = "0";

            gameExampleNSP = new FileData("XCI", "C:\\Switch\\1-2-Switch [01000320000cc000][v0].nsp", "1-2-Switch [01000320000cc000][v0]", "1-2-Switch [01000320000cc000][v0].nsp", 
                "1,38 GB", 1481339176, "1,38 GB", 1481339176, "01000320000CC000" , "01000320000CC000", "1-2-Switch", "Nintendo", "1.0.0", "No Prod. ID", "0.12.12.0", "e-shop", 
                "0 (1.0.0-2.3.0)", new Dictionary<string, string> { { "American English", "cache\\icon_01000320000CC000_AmericanEnglish.bmp" }, { "Japanese", "cache\\icon_01000320000CC000_Japanese.bmp" } },
                new List<string> { "American English", "Japanese" }, "en, ja", true, "", "", "0", "e-shop", "", false, "Download", 0, "Application", "0", true, "", "Nintendo", "Mar 03, 2017", 
                "2 players simultaneous", new List<string> { "Party", "Multiplayer", "Action" }, 0);

            cbxTagsXCI.Items.Clear();
            for (int i = 0; i < Util.AutoRenamingTags.Length; i++)
            {
                if (Util.AutoRenamingTags[i] != "{nspversion}")
                    cbxTagsXCI.Items.Add(Util.AutoRenamingTags[i]);
                cbxTagsNSP.Items.Add(Util.AutoRenamingTags[i]);
            }

            LoadConfig();
        }

        public void WriteConfig()
        {
            Util.autoRenamingPattern = this.autoRenamingPattern;
            Util.autoRenamingPatternNSP = this.autoRenamingPatternNSP;
            Util.ini.IniWriteValue("AutoRenaming", "pattern", this.autoRenamingPattern);
            Util.ini.IniWriteValue("AutoRenaming", "patternNSP", this.autoRenamingPatternNSP);
            int maxFileNameSize = 0;
            try
            {
                maxFileNameSize = Convert.ToInt16(textLimitFileNameSizeNSP.Value);                
            }
            catch { }
            Util.MaxSizeFilenameNSP = maxFileNameSize;
            Util.ini.IniWriteValue("AutoRenaming", "MaxSizeFilenameNSP", Convert.ToString(maxFileNameSize));

            Util.ScrapXCIOnSDCard = this.cbScrapXCIOnSD.Checked;
            Util.ScrapNSPOnSDCard = this.cbScrapNSPOnSD.Checked;
            Util.ScrapExtraInfoFromWeb = this.cbScrapExtraInfoFromWeb.Checked;
            Util.ScrapInstalledEshopSDCard = this.cbScrapLayerFSOnSD.Checked;
            Util.AutoRemoveMissingFiles = this.cbAutoRemoveMissingFiles.Checked;
            Util.ini.IniWriteValue("SD", "scrapXCI", cbScrapXCIOnSD.Checked ? "true" : "false");
            Util.ini.IniWriteValue("SD", "scrapNSP", cbScrapNSPOnSD.Checked ? "true" : "false");
            Util.ini.IniWriteValue("SD", "scrapInstalledNSP", cbScrapLayerFSOnSD.Checked ? "true" : "false");
            Util.ini.IniWriteValue("Config", "scrapExtraInfoFromWeb", cbScrapExtraInfoFromWeb.Checked ? "true" : "false");
            Util.ini.IniWriteValue("Config", "autoRemoveMissingFiles", cbAutoRemoveMissingFiles.Checked ? "true" : "false");

            Util.AutoUpdateNSDBOnStartup = this.cbAutoUpdateScene.Checked;
            Util.ini.IniWriteValue("Config", "autoUpdateNSWDB", cbAutoUpdateScene.Checked ? "true" : "false");
            Util.UseTitleKeys = this.cbUseTitleKeys.Checked;
            Util.ini.IniWriteValue("Config", "useTitleKeys", cbUseTitleKeys.Checked ? "true" : "false");

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
            this.autoRenamingPatternNSP = Util.ini.IniReadValue("AutoRenaming", "patternNSP");
            switch (this.autoRenamingPattern)
            {
                case "{gamename}":
                    rbRenamingGameNameXCI.Checked = true;
                    break;
                case "{titleid} - {gamename}":
                    rbRenamingTitleIDGameNameXCI.Checked = true;
                    break;
                case "{titleid} - {gamename} - {releasegroup}":
                    rbRenamingTitleIDGameNameReleaseGroupXCI.Checked = true;
                    break;
                case "{gamename} ({region})":
                    rbRenamingGameNameRegionXCI.Checked = true;
                    break;
                case "{gamename} ({region}) ({firmware})":
                    rbRenamingGameNameRegionFirmwareXCI.Checked = true;
                    break;
//                case "{CDNSP}":
//                    rbRenamingCDNSP.Checked = true;
//                    break;
                default:
                    textBoxCustomPaternXCI.Text = autoRenamingPattern;
                    rbRenamingCustomXCI.Checked = true;
                    break;
            }

            switch (this.autoRenamingPatternNSP)
            {
                case "{gamename}":
                    rbRenamingGameNameNSP.Checked = true;
                    break;
                case "{titleid} - {gamename}":
                    rbRenamingTitleIDGameNameNSP.Checked = true;
                    break;
                case "{titleid} - {gamename} - {releasegroup}":
                    rbRenamingTitleIDGameNameReleaseGroupNSP.Checked = true;
                    break;
                case "{gamename} ({region})":
                    rbRenamingGameNameRegionNSP.Checked = true;
                    break;
                case "{gamename} ({region}) ({firmware})":
                    rbRenamingGameNameRegionFirmwareNSP.Checked = true;
                    break;
                case "{CDNSP}":
                    rbRenamingCDNSP.Checked = true;
                    break;
                default:
                    textBoxCustomPaternNSP.Text = autoRenamingPatternNSP;
                    rbRenamingCustomNSP.Checked = true;
                    break;
            }

            textLimitFileNameSizeNSP.Value = Util.MaxSizeFilenameNSP;

            this.cbAutoUpdateScene.Checked = Util.AutoUpdateNSDBOnStartup;
            this.cbUseTitleKeys.Checked = Util.UseTitleKeys;
            this.cbScrapLayerFSOnSD.Checked = Util.ScrapInstalledEshopSDCard;
            this.cbScrapNSPOnSD.Checked = Util.ScrapNSPOnSDCard;
            this.cbScrapXCIOnSD.Checked = Util.ScrapXCIOnSDCard;
            this.cbScrapExtraInfoFromWeb.Checked = Util.ScrapExtraInfoFromWeb;
            this.cbAutoRemoveMissingFiles.Checked = Util.AutoRemoveMissingFiles;

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
            if (textBoxCustomPaternXCI.Text.Trim() != "")
            {
                this.autoRenamingPattern = textBoxCustomPaternXCI.Text;
                lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cbxTagsXCI.SelectedIndex != -1)
            {
                textBoxCustomPaternXCI.Text = textBoxCustomPaternXCI.Text.Insert(textBoxCustomPaternXCI.SelectionStart, cbxTagsXCI.Text);
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

        private void btnApply_Click(object sender, EventArgs e)
        {
            WriteConfig();
        }

        private void btnAddNSP_Click(object sender, EventArgs e)
        {
            if (cbxTagsNSP.SelectedIndex != -1)
            {
                textBoxCustomPaternNSP.Text = textBoxCustomPaternNSP.Text.Insert(textBoxCustomPaternNSP.SelectionStart, cbxTagsNSP.Text);
            }
        }

        private void textBoxCustomPaternNSP_TextChanged(object sender, EventArgs e)
        {
            if (textBoxCustomPaternNSP.Text.Trim() != "")
            {
                this.autoRenamingPatternNSP = textBoxCustomPaternNSP.Text;
                lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
            }
        }

        private void textLimitFileNameSizeNSP_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void rbRenamingGameNameXCI_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingGameNameRegionXCI_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename} ({region})";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingGameNameRegionFirmwareXCI_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{gamename} ({region}) ({firmware})";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingTitleIDGameNameXCI_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{titleid} - {gamename}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingTitleIDGameNameReleaseGroupXCI_CheckedChanged(object sender, EventArgs e)
        {
            gbCustom.Visible = false;
            this.autoRenamingPattern = "{titleid} - {gamename} - {releasegroup}";
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
        }

        private void rbRenamingCustomXCI_CheckedChanged(object sender, EventArgs e)
        {
            lblExample.Text = "";
            gbCustom.Visible = true;
            this.autoRenamingPattern = textBoxCustomPaternXCI.Text;
            lblExample.Text = Util.GetRenamingString(gameExample, this.autoRenamingPattern);
            textBoxCustomPaternXCI.Select();
        }

        private void rbRenamingCustomNSP_CheckedChanged(object sender, EventArgs e)
        {
            lblExampleNSP.Text = "";
            gbCustomNSP.Visible = true;
            this.autoRenamingPatternNSP = textBoxCustomPaternNSP.Text;
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
            textBoxCustomPaternNSP.Select();
        }

        private void rbRenamingGameNameNSP_CheckedChanged(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{gamename}";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }

        private void rbRenamingGameNameRegionNSP_CheckedChanged(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{gamename} ({region})";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }

        private void rbRenamingGameNameRegionFirmwareNSP_CheckedChanged(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{gamename} ({region}) ({firmware})";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }

        private void rbRenamingTitleIDGameNameNSP_CheckedChanged(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{titleid} - {gamename}";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }

        private void rbRenamingCDNSP_CheckedChanged_1(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{CDNSP}";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }

        private void rbRenamingTitleIDGameNameReleaseGroupNSP_CheckedChanged(object sender, EventArgs e)
        {
            gbCustomNSP.Visible = false;
            this.autoRenamingPatternNSP = "{titleid} - {gamename} - {releasegroup}";
            lblExampleNSP.Text = Util.GetRenamingString(gameExampleNSP, this.autoRenamingPatternNSP);
        }
    }
}
