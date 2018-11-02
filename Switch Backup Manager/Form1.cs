using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using BrightIdeasSoftware;
using HtmlAgilityPack;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;

namespace Switch_Backup_Manager
{
    public partial class FrmMain : Form
    {
        internal static Dictionary<Tuple<string, string>, FileData> LocalFilesList;
        private Dictionary<Tuple<string, string>, FileData> LocalFilesListSelectedItems;
        internal static Dictionary<Tuple<string, string>, FileData> LocalNSPFilesList;
        private Dictionary<Tuple<string, string>, FileData> LocalNSPFilesListSelectedItems;
        internal static Dictionary<Tuple<string, string>, FileData> SceneReleasesList;
        private Dictionary<Tuple<string, string>, FileData> SceneReleasesSelectedItems;
        private Dictionary<Tuple<string, string>, FileData> SDCardList;
        private Dictionary<Tuple<string, string>, FileData> SDCardListSelectedItems;
        private FileData TitleToEdit;
        private TextMatchFilter filterContentTypeEShop;
        internal static Dictionary<string, int> TitleVersionList;
        public static int TitleVersionUpdate;

        private bool updateCbxRemoveableFiles;
        private bool updateFileListAfterMove;
        private bool updateLog;
        private string clipboardInfoEShop;
        private string clipboardInfoLocal;
        private string clipboardInfoSD;
        private string clipboardInfoScene;

        private string SDCardSelected;

        //To update Statusbar wheile adding files
        public static int progressPercent = 0;
        public static string progressCurrentfile = "";

        public FrmMain()
        {
            InitializeComponent();

            this.Text = "Switch Backup Manager v"+Util.VERSION;
            //Need to think a way of auto resizing columns based on screen resolution to ocupy all space available
            //this.Width = Screen.PrimaryScreen.Bounds.Width;

            lblSpaceAvailabeOnSD.Visible = false;

            Util.LoadSettings(ref this.richTextBoxLog);

            updateLog = false;
            if (File.Exists(Util.LOG_FILE))
            {
                string[] lines = File.ReadAllLines(Util.LOG_FILE);
                richTextBoxLog.Suspend();
                foreach (string line in lines)
                {
                    Color color = richTextBoxLog.ForeColor;

                    if (line.Contains("[DEBUG]"))
                    {
                        color = Color.DarkGreen;
                    }
                    else if (line.Contains("[ERROR]"))
                    {
                        color = Color.DarkRed;
                    }
                    else if (line.Contains("[WARNING]"))
                    {
                        color = Color.IndianRed;
                    }                    
                    richTextBoxLog.AppendText(line+"\n", color);
                }                
                richTextBoxLog.SelectionStart = richTextBoxLog.Text.Length;
                richTextBoxLog.ScrollToCaret();
                richTextBoxLog.Resume();
            }
            updateLog = true;

            LocalFilesList = new Dictionary<Tuple<string, string>, FileData>();
            LocalNSPFilesList = new Dictionary<Tuple<string, string>, FileData>();
            SceneReleasesList = new Dictionary<Tuple<string, string>, FileData>();
            SDCardList = new Dictionary<Tuple<string, string>, FileData>();

            foreach (ColumnHeader column in OLVLocalFiles.Columns)
            {
                cbxFilterLocal.Items.Add(column.Text);
            }
            cbxFilterLocal.SelectedIndex = cbxFilterLocal.Items.IndexOf("Game title");
            OLVLocalFiles.UseFiltering = true;
            textBoxFilterLocal.Select();

            foreach (ColumnHeader column in OLV_SDCard.Columns)
            {
                cbxFilterSD.Items.Add(column.Text);
            }
            cbxFilterSD.SelectedIndex = cbxFilterSD.Items.IndexOf("Game title");
            OLV_SDCard.UseFiltering = true;

            foreach (ColumnHeader column in OLVSceneList.Columns)
            {
                cbxFilterScene.Items.Add(column.Text);
            }
            cbxFilterScene.SelectedIndex = cbxFilterScene.Items.IndexOf("Game title");
            OLVSceneList.UseFiltering = true;

            foreach (ColumnHeader column in OLVEshop.Columns)
            {
                if (column.Text != "Content Type")
                    cbxFilterEshop.Items.Add(column.Text);
            }
            cbxFilterEshop.SelectedIndex = cbxFilterEshop.Items.IndexOf("Game title");
            OLVEshop.UseFiltering = true;

            SetupOLVs();

            UpdateTitleVersionList();
            UpdateSceneReleasesList();
            UpdateLocalGamesList();
            UpdateLocalNSPGamesList();
            FilterEshopByContentType();

            try
            {
                if (File.Exists("confXCI.bin"))
                {
                    OLVLocalFiles.RestoreState(File.ReadAllBytes("confXCI.bin"));
                }
                if (File.Exists("confSCN.bin"))
                {
                    OLVSceneList.RestoreState(File.ReadAllBytes("confSCN.bin"));
                }
                if (File.Exists("confSDC.bin"))
                {
                    OLV_SDCard.RestoreState(File.ReadAllBytes("confSDC.bin"));
                }
                if (File.Exists("confNSP.bin"))
                {
                    OLVEshop.RestoreState(File.ReadAllBytes("confNSP.bin"));
                }                               
            }
            catch (Exception e)
            {
                Util.logger.Warning("Could not recover environment settings.");
            }

            ScanFolders();

            tabControl1_SelectedIndexChanged(this, new EventArgs());
        }

        /*
                delegate void OnLogChangedDelegate(object source, FileSystemEventArgs e); //Safe Thread
                private void OnLogChanged(object source, FileSystemEventArgs e) //Safe Thread
                {
                    if (e.Name == Util.LOG_FILE)
                    {
                        if (richTextBoxLog.InvokeRequired)
                        {
                            OnLogChangedDelegate d = new OnLogChangedDelegate(OnLogChanged);
                            try
                            {
                                this.Invoke(d, new object[] { source, e });
                            } catch{}                    
                        } else
                        {
                            richTextBoxLog.Suspend();
                            richTextBoxLog.Clear();
                            richTextBoxLog.Text = File.ReadAllText(Util.LOG_FILE);
                            richTextBoxLog.SelectionStart = richTextBoxLog.Text.Length;
                            richTextBoxLog.ScrollToCaret();
                            richTextBoxLog.Resume();
                        }
                    }
                }
        */

        private void FrmMain_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Maximised)
            {
                WindowState = FormWindowState.Maximized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else if (Properties.Settings.Default.Minimised)
            {
                WindowState = FormWindowState.Minimized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else
            {
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            splitContainer1.SplitterDistance = Properties.Settings.Default.SplitterDistanceVert;
            splitContainer2.SplitterDistance = Properties.Settings.Default.SplitterDistanceHor;

            if (!Properties.Settings.Default.LeftPanelVisible)
            {
                splitContainer1.Panel1Collapsed = true;
                leftPanelToolStripMenuItem.Checked = false;
            }
            else
            {
                splitContainer1.Panel1Collapsed = false;
                leftPanelToolStripMenuItem.Checked = true;
            }
            if (!Properties.Settings.Default.BottomPanelVisible)
            {
                splitContainer2.Panel2Collapsed = true;
                bottonPanelToolStripMenuItem.Checked = false;
            }
            else
            {
                splitContainer2.Panel2Collapsed = false;
                bottonPanelToolStripMenuItem.Checked = true;
            }

            cbBaseGame.Checked = Properties.Settings.Default.ShowBaseGames;
            cbDLC.Checked = Properties.Settings.Default.ShowDLCFilterEshop;
            cbUpdates.Checked = Properties.Settings.Default.ShowUpdatesFilterEShop;
        }

        private void SaveEnvironment()
        {
            File.WriteAllBytes("confXCI.bin", OLVLocalFiles.SaveState());
            File.WriteAllBytes("confSCN.bin", OLVSceneList.SaveState());
            File.WriteAllBytes("confSDC.bin", OLV_SDCard.SaveState());
            File.WriteAllBytes("confNSP.bin", OLVEshop.SaveState());

            if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximised = true;
                Properties.Settings.Default.Minimised = false;
            }
            else if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Maximised = false;
                Properties.Settings.Default.Minimised = false;
            }
            else
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximised = false;
                Properties.Settings.Default.Minimised = true;
            }

            Properties.Settings.Default.SplitterDistanceVert = splitContainer1.SplitterDistance;
            Properties.Settings.Default.SplitterDistanceHor = splitContainer2.SplitterDistance;
            Properties.Settings.Default.LeftPanelVisible = !splitContainer1.Panel1Collapsed;
            Properties.Settings.Default.BottomPanelVisible = !splitContainer2.Panel2Collapsed;

            Properties.Settings.Default.ShowBaseGames = cbBaseGame.Checked;
            Properties.Settings.Default.ShowDLCFilterEshop = cbDLC.Checked;
            Properties.Settings.Default.ShowUpdatesFilterEShop = cbUpdates.Checked;

            Properties.Settings.Default.Save();
        }

        private void ScanFolders()
        {
            if (!backgroundWorkerScanNewFiles.IsBusy)
            {
                toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScanningNewFiles;
                toolStripStatusFilesOperation.Visible = true;
                toolStripProgressAddingFiles.Visible = true;
                toolStripStatusLabelGame.Text = "";
                toolStripStatusLabelGame.Visible = true;
                toolStripProgressAddingFiles.Value = 0;
                timer1.Enabled = true;
                //object[] parameters = { selectedPath, "xci" }; //0: FilesList (string[]), 1: FileType ("xci", "nsp") 
                backgroundWorkerScanNewFiles.RunWorkerAsync();
            }
        }

        private void SetupOLVs()
        {
            OLVLocalFiles.OwnerDraw = true;
            OLV_SDCard.OwnerDraw = true;
            OLVSceneList.OwnerDraw = true;
            noneToolStripMenuItem1.Checked = true;

            if (Util.ShowCompletePathFiles)
            {
                olvColumnFilePathLocal.AspectName = "FilePath";
                olvColumnFilePathSD.AspectName = "FilePath";
                olvColumnFilePathEShop.AspectName = "FilePath";
            } else
            {
                olvColumnFilePathLocal.AspectName = "FileNameWithExt";
                olvColumnFilePathSD.AspectName = "FileNameWithExt";
                olvColumnFilePathEShop.AspectName = "FileNameWithExt";
            }

            //olvColumnContentTypeEShop.IsVisible = false; //Does not work! WTF!?
            OLVEshop.Sort(olvColumnGameNameLocal, SortOrder.Ascending);

            OLVLocalFiles.SetObjects(LocalFilesList.Values);
            OLVSceneList.SetObjects(SceneReleasesList.Values);
            OLVEshop.SetObjects(LocalNSPFilesList.Values);


            sizeColumnROMSizeScene.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnROMSizeLocal.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnUsedSpaceLocal.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnROMSizeSD.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnUsedSpaceSD.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnROMSizeEshop.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };

            olvColumnContentTypeEShop.AspectToStringConverter = delegate (object x)
            {
                string content = (string)x;
                switch (content)
                {
                    case "AddOnContent":
                        content = "DLC";
                        break;
                    case "Patch":
                        content = "Update";
                        break;
                    case "Application":
                        content = "Base Game";
                        break;
                }
                return content;
            };

            olvColumnLanguagesLocal.AspectToStringConverter = delegate (object x) {
                string result = "";
                try
                {
                    foreach (string language in (List<string>)x)
                    {
                        result += language + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };

            olvColumnLanguagesEShop.AspectToStringConverter = delegate (object x) {
                string result = "";
                try
                {
                    foreach (string language in (List<string>)x)
                    {
                        result += language + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };

            olvColumnLanguagesSD.AspectToStringConverter = delegate (object x) {
                string result = "";
                try
                {
                    foreach (string language in (List<string>)x)
                    {
                        result += language + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };

            olvColumnLanguagesScene.AspectToStringConverter = delegate (object x) {
                string result = "";
                try
                {
                    foreach (string language in (List<string>)x)
                    {
                        result += language + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };


            olvColumnCategoriesLocal.AspectToStringConverter = delegate (object x) {
                string result = "";

                try
                {
                    foreach (string category in (List<string>)x)
                    {
                        result += category + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };

            olvColumnCategoriesEShop.AspectToStringConverter = delegate (object x) {
                string result = "";

                try
                {
                    foreach (string category in (List<string>)x)
                    {
                        result += category + ", ";
                    }
                    if (result.Trim().Length > 1)
                    {
                        result = result.Remove(result.Length - 2);
                    }
                }
                catch { }
                return result;
            };


            olvColumnGameNameEShop.AspectGetter = delegate (object x)
            {
                string result = "";
                FileData data = (FileData)x;

                if (data != null)
                {
                    result = data.GameName;

                    switch (data.ContentType)
                    {
                        case "Application":

                            break;
                        case "Patch":
                            result += " [UPD]";
                            break;
                        case "AddOnContent":
                            result += " [DLC]";
                            break;
                    }
                    result += " [" + data.Version + "]";
                }

                return result;
            };

            olvColumnGameNameSD.AspectGetter = delegate (object x)
            {
                string result = "";
                FileData data = (FileData)x;

                if (data != null)
                {
                    result = data.GameName;

                    switch (data.ContentType)
                    {
                        case "Application":

                            break;
                        case "Patch":
                            result += " [UPD]";
                            break;
                        case "AddOnContent":
                            result += " [DLC]";
                            break;
                    }
                    if (data.DistributionType == "Download")
                    {
                        result += " [" + data.Version + "]";
                    }
                }

                return result;
            };

            columnSceneRegionScene.ImageGetter = delegate (object rowObject)
            {
                FileData data = (FileData)rowObject;
                switch (data.Region)
                {
                    case "WLD":
                        return Properties.Resources.WLD;
                        //break;
                    case "USA":
                        return Properties.Resources.USA;
                        //break;
                    case "JPN":
                        return Properties.Resources.JPN;
                        //break;
                    case "EUR":
                        return Properties.Resources.EUR;
                        //break;
                    case "CHN":
                        return Properties.Resources.CHN;
                        //break;
                    case "GER":
                        return Properties.Resources.GER;
                        //break;
                    case "KOR":
                        return Properties.Resources.KOR;
                        //break;
                    case "SPA":
                        return Properties.Resources.SPA;
                        //break;
                    case "UKV":
                        return Properties.Resources.UKV;
                        //break;
                    default:
                        return Properties.Resources.WLD;
                        //break;
                }
            };

            this.olvColumnIsTrimmedLocal.AspectToStringConverter = delegate (object x) { return ((bool)x == true) ? "Yes" : "No"; };
            this.olvColumnIsTrimmedSD.AspectToStringConverter = delegate (object x) { return ((bool)x == true) ? "Yes" : "No"; };
            this.olvColumnSceneID.AspectToStringConverter = delegate (object x) { return string.Format("{0:D4}", (int)x); };
        }

        public void UpdateSceneReleasesList()
        {
            SceneReleasesList = Util.LoadSceneXMLToFileDataDictionary(Util.XML_NSWDB);
            OLVSceneList.SetObjects(SceneReleasesList.Values);

            SceneReleasesSelectedItems = new Dictionary<Tuple<string, string>, FileData>();
            SumarizeLocalGamesList("scene");
        }

        public void UpdateSDCardList()
        {
            if (Util.ScrapXCIOnSDCard & Util.ScrapNSPOnSDCard)
            {
                SDCardList = Util.GetFileDataCollectionAll(SDCardSelected);
            }
            else if (Util.ScrapNSPOnSDCard)
            {
                SDCardList = Util.GetFileDataCollectionNSP(SDCardSelected);
            }
            else if (Util.ScrapXCIOnSDCard)
            {
                SDCardList = Util.GetFileDataCollection(SDCardSelected);
            }

            //SDCardList = Util.GetFileDataCollection(cbxRemoveableDrives.SelectedItem.ToString());
        }

        public void UpdateLocalGamesList()
        {
            LocalFilesList = Util.LoadXMLToFileDataDictionary(Util.XML_Local);
            OLVLocalFiles.SetObjects(LocalFilesList.Values);

            LocalFilesListSelectedItems = new Dictionary<Tuple<string, string>, FileData>();
            SumarizeLocalGamesList("local");
        }

        public void UpdateLocalNSPGamesList()
        {
            LocalNSPFilesList = Util.LoadXMLToFileDataDictionary(Util.XML_NSP_Local);
            OLVEshop.SetObjects(LocalNSPFilesList.Values);

            LocalNSPFilesListSelectedItems = new Dictionary<Tuple<string, string>, FileData>();
            SumarizeLocalGamesList("eshop");
        }

        /// <summary>
        /// Possible values for list: "local, sdcard, scene"
        /// </summary>
        /// <param name="list"></param>
        public void SumarizeLocalGamesList(string list)
        {
            int count = 0;
            long size = 0;

            switch (list)
            {
                case "local":
                    foreach (ListViewItem item in OLVLocalFiles.Items)
                    {
                        size += Convert.ToInt64(((FileData)((OLVListItem)item).RowObject).ROMSizeBytes);
                        count++;
                    }
                    break;
                case ("sdcard"):
                    foreach (ListViewItem item in OLV_SDCard.Items)
                    {
                        size += Convert.ToInt64(((FileData)((OLVListItem)item).RowObject).ROMSizeBytes);
                        count++;
                    }
                    break;
                case ("scene"):
                    foreach (ListViewItem item in OLVSceneList.Items)
                    {
                        size += Convert.ToInt64(((FileData)((OLVListItem)item).RowObject).ROMSizeBytes);
                        count++;
                    }
                    break;
                case ("eshop"):
                    foreach (ListViewItem item in OLVEshop.Items)
                    {
                        size += Convert.ToInt64(((FileData)((OLVListItem)item).RowObject).ROMSizeBytes);
                        count++;
                    }
                    break;
            }

            toolStripStatusLabel2.Text = Convert.ToString(count) + " Total (" + Util.BytesToGB(size) + ")";
        }

        private void DisplayGameInformation(string TitleID, string TitleIDBase, string Version, Dictionary<Tuple<string, string>, FileData> list, string sourceList) //Possible values for sourceList ("local", "sdcard", "scene")
        {
            ClearGameInformation();
            FileData data = Util.GetFileData(TitleIDBase, Version, list);
            FileData data2 = Util.GetFileData(TitleID, Version, list);

            if (sourceList == "local" || sourceList == "sdcard")
            {
                if (data != null && data.Region_Icon != null && data.Region_Icon.Count > 0 && File.Exists(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value))
                {
                    PB_GameIcon.BackgroundImage = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value);
                }
                else
                {
                    PB_GameIcon.BackgroundImage = Properties.Resources.image_not_available;
                    PB_GameIcon.Refresh();
                }
            } else
            {
                if (data != null)
                {
                    if (data.Region_Icon != null && data.Region_Icon.Count > 0 && File.Exists(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value))
                    {
                        PB_GameIcon.BackgroundImage = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value);
                    }
                    else
                    {
                        PB_GameIcon.BackgroundImage = Properties.Resources.image_not_available;
                        PB_GameIcon.Refresh();
                    }
                } else
                {
                    //Example: icon_0100A7F002830000_AmericanEnglish
                    string[] files = Directory.GetFiles(Util.CACHE_FOLDER, "icon_" + TitleID + "*.bmp");
                    if (files.Length == 0)
                    {
                        files = Directory.GetFiles(Util.CACHE_FOLDER, "icon_" + TitleIDBase + "*.bmp");
                    }
                    if (files.Length > 0)
                    {
                        PB_GameIcon.BackgroundImage = Image.FromFile(@files[0]);
                    }
                    else
                    {
                        PB_GameIcon.BackgroundImage = Properties.Resources.image_not_available;
                        PB_GameIcon.Refresh();
                    }
                }
            }

            string url = "https://ec.nintendo.com/apps/" + TitleID + "/US";
            lnkInfo.Links.Clear();
            lnkInfo.Links.Add(0, lnkInfo.Text.Length, url);
            lnkInfo.Visible = true;

            if (data2 != null)
            {
                if (data2.HasExtendedInfo)
                {
                    string categories = "";
                    foreach (string cat in data2.Categories)
                    {
                        categories += cat + "\n";
                    }

                    richTextBoxGameDescription.Text = (data2.Description.Trim() != "") ?
                        Regex.Replace(Regex.Replace(data2.Description.Trim(), "(\n +){2,}", "\n\n"), "\n +", " ") : Properties.Resources.EN_Not_Available;
                    lblNumberOfPlayers.Text = (data2.NumberOfPlayers.Trim() != "") ? data2.NumberOfPlayers : Properties.Resources.EN_Not_Available;
                    lblReleaseDate.Text = (data2.ReleaseDate.Trim() != "") ? data2.ReleaseDate : Properties.Resources.EN_Not_Available;
                    lblPublisher.Text = (data2.Publisher.Trim() != "") ? data2.Publisher : Properties.Resources.EN_Not_Available;
                    lblCategory.Text = (categories.Trim() != "") ? categories : Properties.Resources.EN_Not_Available;
                }
            }
            panelGameExtraInfo.Visible = true;
        }

        private void GetWebInfo(string url)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(url);
                string releaseDate = doc.DocumentNode.SelectNodes("//*[@id=\"overview\"]/div[2]/dl/div[2]")[0].InnerText;
                string numberOfPlayers = doc.DocumentNode.SelectNodes("//*[@id=\"overview\"]/div[2]/dl/div[3]")[0].InnerText; 
                string category = doc.DocumentNode.SelectNodes("//*[@id=\"overview\"]/div[2]/dl/div[4]")[0].InnerText;
                string publisher = doc.DocumentNode.SelectNodes("//*[@id=\"overview\"]/div[2]/dl/div[5]")[0].InnerText;
                string developer = doc.DocumentNode.SelectNodes("//*[@id=\"overview\"]/div[2]/dl/div[6]")[0].InnerText;                
                lblReleaseDate.Text = releaseDate;
                lblNumberOfPlayers.Text = numberOfPlayers;
                lblCategory.Text = category;
            }
            catch (Exception e)
            {
                Util.logger.Error(e.StackTrace);
            }
        }

        public void ClearGameInformation()
        {
            PB_GameIcon.BackgroundImage = null;
            PB_GameIcon.Refresh();

            lnkInfo.Visible = false;
            lnkInfo.Links.Clear();

            richTextBoxGameDescription.Clear();
            richTextBoxGameDescription.Text = "";
            lblNumberOfPlayers.Text = Properties.Resources.EN_Not_Available;
            lblReleaseDate.Text = Properties.Resources.EN_Not_Available;
            lblPublisher.Text = Properties.Resources.EN_Not_Available;
            lblCategory.Text = Properties.Resources.EN_Not_Available;
            panelGameExtraInfo.Visible = false;
        }

        private void InvertSelection(ListView lv)
        {
            for (int k = 0; k < lv.Items.Count; k++)
            {
                lv.Items[k].Selected = !(lv.Items[k].Selected);
            }
            lv.Select();
        }

        private void ChangeGroupOnFileList(string list, string groupby)
        {
            switch (list)
            {
                case "local":
                    switch (groupby)
                    {
                        case "none":
                            OLVLocalFiles.ShowGroups = false;
                            break;
                        case "gametitle":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnGameNameLocal, SortOrder.Ascending);
                            break;
                        case "developer":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnDeveloperLocal, SortOrder.Ascending);
                            break;
                        case "trimmed":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnIsTrimmedLocal, SortOrder.Ascending);
                            break;
                        case "cartsize":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnCartSizeLocal, SortOrder.Ascending);
                            break;
                        case "type":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnCardTypeLocal, SortOrder.Ascending);
                            break;
                        case "masterkeyrevision":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnMasterKeyLocal, SortOrder.Ascending);
                            break;
                    }
                    break;
                case "sdcard":
                    switch (groupby)
                    {
                        case "none":
                            OLV_SDCard.ShowGroups = false;
                            break;
                        case "gametitle":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnGameNameSD, SortOrder.Ascending);
                            break;
                        case "developer":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnDeveloperSD, SortOrder.Ascending);
                            break;
                        case "trimmed":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnIsTrimmedSD, SortOrder.Ascending);
                            break;
                        case "cartsize":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnCartSizeSD, SortOrder.Ascending);
                            break;
                        case "type":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnCardTypeSD, SortOrder.Ascending);
                            break;
                        case "masterkeyrevision":
                            OLV_SDCard.ShowGroups = true;
                            OLV_SDCard.BuildGroups(olvColumnMasterKeySD, SortOrder.Ascending);
                            break;
                    }
                    break;
                case "scene":
                    switch (groupby)
                    {
                        case "none":
                            OLVSceneList.ShowGroups = false;
                            break;
                        case "gametitle":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnGameNameScene, SortOrder.Ascending);
                            break;
                        case "developer":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnDeveloperScene, SortOrder.Ascending);
                            break;
                        case "cartsize":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnCartSizeScene, SortOrder.Ascending);
                            break;
                        case "cardtype":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnCardTypeScene, SortOrder.Ascending);
                            break;
                        case "firmware":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnFirmwareScene, SortOrder.Ascending);
                            break;
                        case "region":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(columnSceneRegionScene, SortOrder.Ascending);
                            break;
                        case "releasegroup":
                            OLVSceneList.ShowGroups = true;
                            OLVSceneList.BuildGroups(olvColumnGroupScene, SortOrder.Ascending);
                            break;
                    }
                    break;
                case "eshop":
                    switch (groupby)
                    {
                        case "none":
                            OLVEshop.ShowGroups = false;
                            break;
                        case "gametitle":
                            OLVEshop.ShowGroups = true;
                            OLVEshop.BuildGroups(olvColumnGameNameScene, SortOrder.Ascending);
                            break;
                        case "developer":
                            OLVEshop.ShowGroups = true;
                            OLVEshop.BuildGroups(olvColumnDeveloperEShop, SortOrder.Ascending);
                            break;
                        case "masterkeyrevision":
                            OLVEshop.ShowGroups = true;
                            OLVEshop.BuildGroups(olvColumnMasterKeyRevisionEShop, SortOrder.Ascending);
                            break;
                    }
                    break;
            }
        }

        public void PopulateRemoveableDrives()
        {
            cbxRemoveableDrives.Items.Clear();
            var driveList = DriveInfo.GetDrives();
            foreach (DriveInfo drive in driveList)
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    cbxRemoveableDrives.Items.Add(drive.Name);
                }
            }
        }

        private Dictionary<Tuple<string, string>, FileData> DiffLists(Dictionary<Tuple<string, string>, FileData> list1, Dictionary<Tuple<string, string>, FileData> list2)
        {
            Dictionary<Tuple<string, string>, FileData> result = new Dictionary<Tuple<string, string>, FileData>();

            foreach (FileData data in list1.Values)
            {
                FileData dummy;
                if (!list2.TryGetValue(new Tuple<string, string>(data.TitleID, data.DistributionType == "Cartridge" ? data.Firmware : data.Version), out dummy))
                {
                    result.Add(new Tuple<string, string>(data.TitleID, data.DistributionType == "Cartridge" ? data.Firmware : data.Version), data);
                }
            }

            return result;
        }

        private Dictionary<Tuple<string, string>, FileData> ContainsLists(Dictionary<Tuple<string, string>, FileData> list1, Dictionary<Tuple<string, string>, FileData> list2)
        {
            Dictionary<Tuple<string, string>, FileData> result = new Dictionary<Tuple<string, string>, FileData>();

            foreach (FileData data in list1.Values)
            {
                FileData dummy;
                if (list2.TryGetValue(new Tuple<string, string>(data.TitleID, data.DistributionType == "Cartridge" ? data.Firmware : data.Version), out dummy))
                {
                    result.Add(new Tuple<string, string>(data.TitleID, data.DistributionType == "Cartridge" ? data.Firmware : data.Version), data);
                }
            }

            return result;
        }

        private Dictionary<Tuple<string, string>, FileData> ContainsListsIgnoreVersion(Dictionary<Tuple<string, string>, FileData> list1, Dictionary<Tuple<string, string>, FileData> list2)
        {
            Dictionary<Tuple<string, string>, FileData> result = new Dictionary<Tuple<string, string>, FileData>();
            Dictionary<Tuple<string, string>, FileData> list1_ = new Dictionary<Tuple<string, string>, FileData>();
            Dictionary<Tuple<string, string>, FileData> list2_ = new Dictionary<Tuple<string, string>, FileData>();

            //Clear version field
            foreach (KeyValuePair<Tuple<string, string>, FileData> entry in list1)
            {
                try
                {
                    list1_.Add(new Tuple<string, string>(entry.Key.Item1, ""), entry.Value);
                } catch { }                
            }
            foreach (KeyValuePair<Tuple<string, string>, FileData> entry in list2)
            {
                try
                {
                    list2_.Add(new Tuple<string, string>(entry.Key.Item1, ""), entry.Value);
                } catch { }                
            }

            foreach (FileData data in list1_.Values)
            {
                FileData dummy;
                if (list2_.TryGetValue(new Tuple<string, string>(data.TitleID, ""), out dummy))
                {
                    try
                    {
                        result.Add(new Tuple<string, string>(data.TitleID, ""), data);
                    } catch { }                    
                }
            }

            return result;
        }

        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {                
                string selectedPath = dialog.FileName;
                menuLocalFiles.Enabled = false;
                if (!backgroundWorkerAddFilesFromDirectory.IsBusy)
                {
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { selectedPath, "xci" }; //0: FilesList (string[]), 1: FileType ("xci", "nsp") 
                    backgroundWorkerAddFilesFromDirectory.RunWorkerAsync(parameters);
                }
            }
        }

        private void objectListView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (OLVLocalFiles.SelectedItems.Count == 0)
            {
                LocalFilesListSelectedItems.Clear();
                ClearGameInformation();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }
            else
            {
                if (updateFileListAfterMove) //To prevent user changing selection during file operations...
                {
                    return;
                }

                FileData data = null;
                int count = 0;
                if (e.IsSelected)
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVLocalFiles.SelectedItems;
                    //string FirstTitleIDSelected = selectedItems[selectedItems.Count-1].Text;
                    //DisplayGameInformation(FirstTitleIDSelected);

                    LocalFilesListSelectedItems.Clear();
                    string titleID = selectedItems[0].Text;
                    string titleIDBase = titleID;
                    string rev = Convert.ToString(((FileData)((OLVListItem)selectedItems[0]).RowObject).Firmware);

                    long size = 0;

                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;                      
                        rev = Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware);
                        data = Util.GetFileData(titleID, rev, LocalFilesList);
                        LocalFilesListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        titleIDBase = data.TitleIDBaseGame;
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the last selected item
                    DisplayGameInformation(titleID, titleIDBase, rev, LocalFilesList, "local");
                }
                else
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVLocalFiles.SelectedItems;
                    long size = 0;
                    LocalFilesListSelectedItems.Clear();
                    foreach (ListViewItem item in selectedItems)
                    {
                        string titleID = item.Text;
                        string rev = Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware);
                        data = Util.GetFileData(titleID, rev, LocalFilesList);
                        LocalFilesListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                }
                if (count == 1)
                {
                    panelEditTitle.Visible = true;
                    TitleToEdit = data;
                    LoadFieldsForEdition();
                } else
                {
                    panelEditTitle.Visible = false;
                }
            }
        }

        private void LoadFieldsForEdition()
        {
            if (this.TitleToEdit != null)
            {
                textBoxGameTitle.Text = this.TitleToEdit.GameName;
                textBoxCardType.Text = this.TitleToEdit.Cardtype;
                textBoxCategory.Text = Util.ListToComaSeparatedString(this.TitleToEdit.Categories);
                textBoxLanguages.Text = Util.ListToComaSeparatedString(this.TitleToEdit.Languages);
                textBoxDeveloper.Text = this.TitleToEdit.Developer;
                textBoxPublisher.Text = this.TitleToEdit.Publisher;
                textBoxFirmware.Text = this.TitleToEdit.Firmware;
                textBoxReleaseDate.Text = this.TitleToEdit.ReleaseDate;
                textBoxPlayers.Text = this.TitleToEdit.NumberOfPlayers;
                richTextBoxDescription.Text = (this.TitleToEdit.Description.Trim() != "") ?
                    Regex.Replace(Regex.Replace(this.TitleToEdit.Description.Trim(), "(\n +){2,}", "\n\n"), "\n +", " ") : "";
            }
        }

        private void SaveEditedTitle(string source) //source = "local", "eshop"
        {
            TitleToEdit.GameName = textBoxGameTitle.Text;
            TitleToEdit.Languages = Util.ComaSeparatedStringToList(textBoxLanguages.Text);
            TitleToEdit.Cardtype = textBoxCardType.Text;
            TitleToEdit.Developer = textBoxDeveloper.Text;
            TitleToEdit.Publisher = textBoxPublisher.Text;
            TitleToEdit.Firmware = textBoxFirmware.Text;
            TitleToEdit.ReleaseDate = textBoxReleaseDate.Text;
            TitleToEdit.NumberOfPlayers = textBoxPlayers.Text;
            TitleToEdit.Categories = Util.ComaSeparatedStringToList(textBoxCategory.Text);
            TitleToEdit.Description = richTextBoxDescription.Text;
            TitleToEdit.HasExtendedInfo = true;

            if (this.TitleToEdit != null)
            {
                Util.UpdateXMLFromFileData(TitleToEdit, source);
            }
            UpdateLocalGamesList();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            OLVLocalFiles.SelectedItems.Clear();
            OLVSceneList.SelectedItems.Clear();
            ClearGameInformation();
            toolStripStatusLabel1.Text = "0 Selected (0MB)";

            panelEditTitle.Visible = false;
            toolStripStatusLabel3.Visible = false;
            toolStripStatusLabel4.Visible = false;
            toolStripStatusLabel5.Visible = false;

            switch (tabControl1.SelectedIndex)
            {
                case 0: //Files
                    SumarizeLocalGamesList("local");
                    break;
                case 1: //SD Card
                    SumarizeLocalGamesList("sdcard");
                    break;
                case 2: //Scene
                    if (Util.HighlightXCIOnScene)
                    {
                        toolStripStatusLabel3.BackColor = Util.HighlightXCIOnScene_color;
                        toolStripStatusLabel3.Visible = true;
                    }
                    if (Util.HighlightNSPOnScene)
                    {
                        toolStripStatusLabel4.BackColor = Util.HighlightNSPOnScene_color;
                        toolStripStatusLabel4.Visible = true;
                    }
                    if (Util.HighlightBothOnScene)
                    {
                        toolStripStatusLabel5.BackColor = Util.HighlightBothOnScene_color;
                        toolStripStatusLabel5.Visible = true;
                    }

                    SumarizeLocalGamesList("scene");
                    break;
                case 3: //Eshop
                    SumarizeLocalGamesList("eshop");
                    break;
            }
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OLVLocalFiles.Select();
            OLVLocalFiles.SelectAll();
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OLVLocalFiles.SelectedItems.Clear();
        }

        private void invertSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvertSelection(OLVLocalFiles);
        }

        private void OLVSceneList_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (OLVSceneList.SelectedItems.Count == 0)
            {
                SceneReleasesSelectedItems.Clear();
                ClearGameInformation();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }
            else
            {
                if (e.IsSelected)
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVSceneList.SelectedItems;

                    SceneReleasesSelectedItems.Clear();
                    string titleID = selectedItems[0].Text;
                    string titleIDBase = titleID;
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)selectedItems[0]).RowObject).DistributionType);
                    string rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)selectedItems[0]).RowObject).Firmware : ((FileData)((OLVListItem)selectedItems[0]).RowObject).Version);

                    int count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                        rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version);
                        FileData data = Util.GetFileData(titleID, rev, SceneReleasesList);
                        SceneReleasesSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        titleIDBase = data.TitleIDBaseGame;
                        count++;
                        size += Convert.ToInt64(data.ROMSizeBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the first selected item
                    DisplayGameInformation(titleID, titleIDBase, rev, LocalFilesList, "scene"); //Has to be Locallist as we dont store scene info other than its xml file...
                }
                else
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVSceneList.SelectedItems;
                    int count = 0;
                    long size = 0;
                    SceneReleasesSelectedItems.Clear();
                    foreach (ListViewItem item in selectedItems)
                    {
                        string titleID = item.Text;
                        string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                        string rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version);
                        FileData data = Util.GetFileData(titleID, rev, SceneReleasesList);
                        SceneReleasesSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        count++;
                        size += Convert.ToInt64(data.ROMSizeBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                }
            }
        }
        
        private void backgroundWorkerAddFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            object[] parameters = e.Argument as object[];

            string folder = (string)parameters[0];
            string fileType = (string)parameters[1];
            if (fileType == "xci")
            {
                Util.AppendFileDataDictionaryToXML(Util.AddFilesFromFolder(folder, fileType));
            }
            else
            {
                Util.AppendFileDataDictionaryToXML(Util.AddFilesFromFolder(folder, fileType), Util.LOCAL_NSP_FILES_DB);
            }
            e.Result = fileType;
        }

        private void backgroundWorkerAddFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;
            menuLocalFiles.Enabled = true;

            string fileType = e.Result as string;
            if (fileType == "xci")
            {
                UpdateLocalGamesList();
                menuLocalFiles.Enabled = true;
            }
            else
            {
                UpdateLocalNSPGamesList();
                menuEShop.Enabled = true;
            }

            MessageBox.Show("Done");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripProgressAddingFiles.Value = progressPercent;
            toolStripStatusLabelGame.Text = progressCurrentfile;
        }

        private void noneToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "none");
            noneToolStripMenuItem1.Checked = true;
        }

        private void ClearGroupingMenuChecks(string local) //local options: "local", "sdcard", "scene", "eshop" 
        {
            switch (local)
            {
                case "local":
                    noneToolStripMenuItem1.Checked = false;
                    developerToolStripMenuItem.Checked = false;
                    gameTitleToolStripMenuItem.Checked = false;
                    trimmedToolStripMenuItem.Checked = false;
                    cartSizeToolStripMenuItem.Checked = false;
                    typeToolStripMenuItem.Checked = false;
                    masterkeyRevisionToolStripMenuItem.Checked = false;
                    break;
                case "sdcard":
                    toolStripMenuItemGroupNoneSD.Checked = false;
                    toolStripMenuItemGroupGameTitleSD.Checked = false;
                    toolStripMenuItemGroupTrimmedSD.Checked = false;
                    toolStripMenuItemGroupCartSizeSD.Checked = false;
                    toolStripMenuItemGroupTypeSD.Checked = false;
                    toolStripMenuItemGroupDeveloperSD.Checked = false;
                    toolStripMenuItemGroupMasterkeySD.Checked = false;
                    break;
                case "scene":
                    toolStripMenuItemGroupNoneScene.Checked = false;
                    toolStripMenuItemGroupGameTitleScene.Checked = false;
                    toolStripMenuItemGroupTypeScene.Checked = false;
                    toolStripMenuItemGroupDeveloperScene.Checked = false;
                    toolStripMenuItemGroupFirmwareScene.Checked = false;
                    regionToolStripMenuItemRegionScene.Checked = false;
                    releaseGroupToolStripMenuItemReleaseGroupScene.Checked = false;
                    cartSizeToolStripMenuItemCartSizeScene.Checked = false;
                    toolStripMenuItemGroupTypeScene.Checked = false;
                    break;
                case "eshop":
                    toolStripMenuItemGroupingNoneEShop.Checked = false;
                    toolStripMenuItemGroupingGameTitleEShop.Checked = false;
                    toolStripMenuItemGroupingDeveloperEShop.Checked = false;
                    toolStripMenuItemGroupingMasterKeyEShop.Checked = false;
                    break;
            }
        }

        private void gameTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "gametitle");
            gameTitleToolStripMenuItem.Checked = true;
        }

        private void developerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "developer");
            developerToolStripMenuItem.Checked = true;
        }

        private void trimmedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "trimmed");
            trimmedToolStripMenuItem.Checked = true;
        }

        private void cartSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "cartsize");
            cartSizeToolStripMenuItem.Checked = true;
        }

        private void typeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "type");
            typeToolStripMenuItem.Checked = true;
        }

        private void masterkeyRevisionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("local");
            ChangeGroupOnFileList("local", "masterkeyrevision");
            masterkeyRevisionToolStripMenuItem.Checked = true;
        }

        private void cbxRemoveableDrives_DropDown(object sender, EventArgs e)
        {
            PopulateRemoveableDrives();
        }

        private void cbxRemoveableDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.SelectedIndex >= 0)
            {
                var driveList = DriveInfo.GetDrives();
                foreach (DriveInfo drive in driveList)
                {
                    if (drive.DriveType == DriveType.Removable && drive.Name == cbxRemoveableDrives.SelectedItem.ToString())
                    {
                        cbxRemoveableDrives.Items.Add(drive.Name);
                        lblSpaceAvailabeOnSD.Text = "Available space: " + Util.BytesToGB(drive.TotalFreeSpace);
                        lblSpaceAvailabeOnSD.Visible = true;
                    }
                }

 //Using Thread is not working good here.. (Working again?)
                if (!backgroundWorkerLoadSDCardFiles.IsBusy)
                {
                    menuSDFiles.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    SDCardSelected = cbxRemoveableDrives.SelectedItem.ToString();
                    backgroundWorkerLoadSDCardFiles.RunWorkerAsync();
                }


//                UpdateSDCardList();
            }
            else
            {
                SDCardList.Clear();
                SDCardListSelectedItems.Clear();
            }
        }

        private void cbxRemoveableDrives_DropDownClosed(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.SelectedIndex < 0)
            {
                SDCardList.Clear();
                lblSpaceAvailabeOnSD.Visible = false;
                OLV_SDCard.SetObjects(SDCardList.Values);
                if (SDCardListSelectedItems != null)
                {
                    SDCardListSelectedItems.Clear();
                }
            }
        }

        private void OLV_SDCard_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (OLV_SDCard.SelectedItems.Count == 0)
            {
                SDCardListSelectedItems.Clear();
                ClearGameInformation();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }
            else
            {
                if (e.IsSelected)
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLV_SDCard.SelectedItems;
                    //string FirstTitleIDSelected = selectedItems[selectedItems.Count-1].Text;
                    //DisplayGameInformation(FirstTitleIDSelected);

                    SDCardListSelectedItems.Clear();
                    string titleID = selectedItems[0].Text;
                    string titleIDBase = titleID;
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)selectedItems[0]).RowObject).DistributionType);
                    string rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)selectedItems[0]).RowObject).Firmware : ((FileData)((OLVListItem)selectedItems[0]).RowObject).Version);

                    int count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                        rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version);
                        FileData data = Util.GetFileData(titleID, rev, SDCardList);
                        //string icon_titleID_filename = data.Region_Icon.First().Value;

                        SDCardListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        titleIDBase = data.TitleIDBaseGame;
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the first selected item
                    DisplayGameInformation(titleID, titleIDBase, rev, SDCardList, "sdcard");
                }
                else
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLV_SDCard.SelectedItems;
                    int count = 0;
                    long size = 0;
                    SDCardListSelectedItems.Clear();
                    foreach (ListViewItem item in selectedItems)
                    {
                        string titleID = item.Text;
                        string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                        string rev = Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version);
                        FileData data = Util.GetFileData(titleID, rev, SDCardList);
                        SDCardListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                }
            }
        }

        private void itemsNotOnSceneReleasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<Tuple<string, string>, FileData> list = DiffLists(LocalFilesList, SceneReleasesList);
            FileData dummy;
            OLVLocalFiles.Select();
            OLVLocalFiles.HideSelection = false;
            OLVLocalFiles.SelectedItems.Clear();
            foreach (ListViewItem item in OLVLocalFiles.Items)
            {
                if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void itemsOnSceneReleasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<Tuple<string, string>, FileData> list = ContainsLists(SceneReleasesList, LocalFilesList);
            FileData dummy;
            OLVLocalFiles.Select();
            OLVLocalFiles.HideSelection = false;
            OLVLocalFiles.SelectedItems.Clear();
            foreach (ListViewItem item in OLVLocalFiles.Items)
            {
                if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void itemsOnSDCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = ContainsLists(SDCardList, LocalFilesList);
                FileData dummy;
                OLVLocalFiles.Select();
                OLVLocalFiles.HideSelection = false;
                OLVLocalFiles.SelectedItems.Clear();
                foreach (ListViewItem item in OLVLocalFiles.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void itemsNotOnSDCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = DiffLists(LocalFilesList, SDCardList);
                FileData dummy;
                OLVLocalFiles.Select();
                OLVLocalFiles.HideSelection = false;
                OLVLocalFiles.SelectedItems.Clear();
                foreach (ListViewItem item in OLVLocalFiles.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = ContainsLists(LocalFilesList, SDCardList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = DiffLists(SDCardList, LocalFilesList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            OLV_SDCard.Select();
            OLV_SDCard.SelectAll();
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            OLV_SDCard.SelectedItems.Clear();
        }

        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            InvertSelection(OLV_SDCard);
        }

        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = ContainsLists(SceneReleasesList, SDCardList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = DiffLists(SDCardList, SceneReleasesList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem58_Click(object sender, EventArgs e)
        {
            Dictionary<Tuple<string, string>, FileData> list = ContainsLists(LocalFilesList, SceneReleasesList);
            FileData dummy;
            OLVSceneList.Select();
            OLVSceneList.HideSelection = false;
            OLVSceneList.SelectedItems.Clear();
            foreach (ListViewItem item in OLVSceneList.Items)
            {
                string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void toolStripMenuItem59_Click(object sender, EventArgs e)
        {
            Dictionary<Tuple<string, string>, FileData> list = DiffLists(SceneReleasesList, LocalFilesList);
            FileData dummy;
            OLVSceneList.Select();
            OLVSceneList.HideSelection = false;
            OLVSceneList.SelectedItems.Clear();
            foreach (ListViewItem item in OLVSceneList.Items)
            {
                if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Firmware)), out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void toolStripMenuItem61_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = ContainsLists(SDCardList, SceneReleasesList);
                FileData dummy;
                OLVSceneList.Select();
                OLVSceneList.HideSelection = false;
                OLVSceneList.SelectedItems.Clear();
                foreach (ListViewItem item in OLVSceneList.Items)
                {
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            } else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItem62_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = DiffLists(SceneReleasesList, SDCardList);
                FileData dummy;
                OLVSceneList.Select();
                OLVSceneList.HideSelection = false;
                OLVSceneList.SelectedItems.Clear();
                foreach (ListViewItem item in OLVSceneList.Items)
                {
                    string distributionType = Convert.ToString(((FileData)((OLVListItem)item).RowObject).DistributionType);
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(distributionType == "Cartridge" ? ((FileData)((OLVListItem)item).RowObject).Firmware : ((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void objectListView1_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
        {
            //Highlights when not trimmed
            if (e.ColumnIndex == this.olvColumnIsTrimmedLocal.Index)
            {
                FileData data = (FileData)e.Model;
                if (!data.IsTrimmed)
                    e.SubItem.BackColor = Color.IndianRed;
            }
            else if (e.ColumnIndex == this.olvColumnSourceLocal.Index)
            {
                FileData data = (FileData)e.Model;
                if ((data.Source.Contains("NSP") && data.Source != "CDNSP") || data.Source.Contains("NCA"))
                    e.SubItem.BackColor = Color.IndianRed;
            }
        }

        private void OLVSceneList_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
        {
            if (e.ColumnIndex == this.columnSceneRegionScene.Index)
            {
                e.SubItem.Text = "";
            }

            FileData data = (FileData)e.Model;
            switch (data.sceneFound)
            {
                case "NSP":
                    if (Util.HighlightNSPOnScene)
                    {
                        e.SubItem.BackColor = Util.HighlightNSPOnScene_color;
                    }
                    break;
                case "XCI":
                    if (Util.HighlightXCIOnScene)
                    {
                        e.SubItem.BackColor = Util.HighlightXCIOnScene_color;
                    }                    
                    break;
                case "BOTH":
                    if (Util.HighlightBothOnScene)
                    {
                        e.SubItem.BackColor = Util.HighlightBothOnScene_color;
                    }
                    break;
            }
        }

        private void filesToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XCI Files (*.XCI;*.XC0)|*.xci;*.xc0";
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Switch Backup Manager - Add Files";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (!backgroundWorkerAddFile.IsBusy)
                {
                    menuLocalFiles.Enabled = false;
                    //OLVLocalFiles.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { openFileDialog.FileNames, "xci" }; //0: FilesList (string[]), 1: FileType ("xci", "nsp") 
                    backgroundWorkerAddFile.RunWorkerAsync(parameters);
                    //backgroundWorkerAddFile.RunWorkerAsync(openFileDialog.FileNames);
                }
            }
        }

        private void backgroundWorkerAddFile_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            object[] parameters = e.Argument as object[];

            string [] filesList = (string[])parameters[0];
            string fileType = (string)parameters[1];
            if (fileType == "xci")
            {
                Util.AppendFileDataDictionaryToXML(Util.AddFiles(filesList, fileType));
            } else
            {
                Util.AppendFileDataDictionaryToXML(Util.AddFiles(filesList, fileType), Util.LOCAL_NSP_FILES_DB);
            }
            e.Result = fileType;
        }

        private void backgroundWorkerAddFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;

            string fileType = e.Result as string;
            if (fileType == "xci")
            {
                menuLocalFiles.Enabled = true;
                UpdateLocalGamesList();
            } else
            {
                menuEShop.Enabled = true;
                UpdateLocalNSPGamesList();
            }

            //Its so fast that I dont think it needs a message
            MessageBox.Show("Done");
        }

        private void RemoveSelectedFiles(string list)
        {
            if (list == "eshop")
            {
                if (LocalNSPFilesListSelectedItems.Count() > 0)
                {
                    Util.RemoveFileDataDictionaryFromXML(LocalNSPFilesListSelectedItems, Util.LOCAL_NSP_FILES_DB);
                    LocalNSPFilesListSelectedItems.Clear();
                    OLVEshop.SelectedItems.Clear();
                    UpdateLocalNSPGamesList();
                }
                else
                {
                    MessageBox.Show("No files selected!");
                }
            }
            else
            {
                if (LocalFilesListSelectedItems.Count() > 0)
                {
                    Util.RemoveFileDataDictionaryFromXML(LocalFilesListSelectedItems, Util.LOCAL_FILES_DB);
                    LocalFilesListSelectedItems.Clear();
                    OLVLocalFiles.SelectedItems.Clear();
                    UpdateLocalGamesList();
                }
                else
                {
                    MessageBox.Show("No files selected!");
                }
            }
        }

        private void selectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedFiles("local");
        }

        private void objectListView1_KeyDown(object sender, KeyEventArgs e)
        {            
            if (e.KeyCode == System.Windows.Forms.Keys.Delete)
            {
                RemoveSelectedFiles("local");
            }                
        }

        private void copyFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalListToSDCard();
        }

        private void backgroundWorkerCopyFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            object[] parameters = e.Argument as object[];

            Dictionary<Tuple<string, string>, FileData> filesList = (Dictionary<Tuple<string, string>, FileData>)parameters[0];
            string destinyPath = (string)parameters[1];
            string operation = (string)parameters[2];
            string source = (string)parameters[3];

            Util.CopyFilesOnDictionaryToFolder(filesList, destinyPath, operation);            
        }

        private void backgroundWorkerCopyFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;
            menuLocalFiles.Enabled = true;
            menuSDFiles.Enabled = true;
            menuEShop.Enabled = true;

            //OLVLocalFiles.Enabled = false;
            if (updateCbxRemoveableFiles)
            {

                SDCardListSelectedItems.Clear();
                OLV_SDCard.SelectedItems.Clear();
                cbxRemoveableDrives_SelectedIndexChanged(this, new EventArgs());

                updateCbxRemoveableFiles = false;
            }
            if (updateFileListAfterMove)
            {
                //Util.RemoveFileDataDictionaryFromXML(LocalFilesListSelectedItems, Util.LOCAL_FILES_DB);
                Util.RemoveMissingFilesFromXML(Util.XML_Local, Util.LOCAL_FILES_DB);
                Util.RemoveMissingFilesFromXML(Util.XML_NSP_Local, Util.LOCAL_NSP_FILES_DB);
                updateFileListAfterMove = false;
            }
            LocalFilesListSelectedItems.Clear();
            OLVLocalFiles.SelectedItems.Clear();
            UpdateLocalGamesList();


            //When the copy operations ends, it minimizes the main form... So... Try to bring it back
            MessageBox.Show("Done");
            this.Activate();
        }

        private void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {
            //UpdateSDCardList();
        }

        private void backgroundWorkerLoadSDCardFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            UpdateSDCardList();
        }

        private void backgroundWorkerLoadSDCardFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OLV_SDCard.SetObjects(SDCardList.Values);

            SDCardListSelectedItems = new Dictionary<Tuple<string, string>, FileData>();
            SumarizeLocalGamesList("sdcard");

            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;
            menuSDFiles.Enabled = true;
            SDCardSelected = "";
            //cbxRemoveableDrives_SelectedIndexChanged(this, new EventArgs());
        }

        private void moveFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalListToSDCard();
        }

        private void copyFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalListToFolder();
        }

        private void moveFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalListToFolder();
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Util.RemoveMissingFilesFromXML(Util.XML_Local, Util.LOCAL_FILES_DB);
            UpdateLocalGamesList();
            MessageBox.Show("Done.");
        }

        private void updateNswdbcomListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Util.UpdateNSWDB();
            Util.XML_NSWDB = XDocument.Load(@Util.NSWDB_FILE);
            UpdateSceneReleasesList();
            MessageBox.Show("Done.");
        }

        private void updateLocalDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScanFolders();
        }

        private void updateNswdbcomListToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Util.UpdateNSWDB();
            Util.XML_NSWDB = XDocument.Load(@Util.NSWDB_FILE);
            UpdateSceneReleasesList();
            MessageBox.Show("Done.");
        }

        private void TrimSelectedFiles(Dictionary<Tuple<string, string>, FileData> dictionary, string source) //source possible values: "local", "sdcard"
        {
            Util.TrimXCIFiles(dictionary, source);
        }

        private void selectedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationTrimSelectedLocalFiles();
        }

        private void allFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OLVLocalFiles.Select();
            OLVLocalFiles.SelectAll();
            selectedFilesToolStripMenuItem_Click(sender, e);
        }

        private void htmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void cSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void exportListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void trimFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationTrimSelectedLocalFiles();
        }

        private void contextMenuLocalList_Opening(object sender, CancelEventArgs e)
        {
            if (LocalFilesListSelectedItems.Count == 0)
            {
                e.Cancel = true;
            } else {
                if (Util.UserCanDeleteFiles)
                {
                    toolStripMenuItem97LocalFiles.Visible = true;
                    deleteSelectedFileserasesFromDiskToolStripMenuItemLocalFiles.Visible = true;
                }
                else
                {
                    toolStripMenuItem97LocalFiles.Visible = false;
                    deleteSelectedFileserasesFromDiskToolStripMenuItemLocalFiles.Visible = false;
                }
            }
        }

        private void sDCardToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalListToSDCard();
        }

        private void folderToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalListToFolder();
        }

        private void sDCardToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalListToSDCard();
        }

        private void folderToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalListToFolder();
        }

        private void OperationCopySelectedFilesFromLocalListToSDCard()
        {
            long totalBytesSelectedFiles = 0;
            long spaceAvailableOnSDCard = 0;

            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                if (LocalFilesListSelectedItems.Count > 0)
                {
                    foreach (FileData data in LocalFilesListSelectedItems.Values)
                    {
                        totalBytesSelectedFiles += data.ROMSizeBytes;
                    }
                }
                else
                {
                    MessageBox.Show("No files selected");
                    return;
                }

                var driveList = DriveInfo.GetDrives();
                foreach (DriveInfo drive in driveList)
                {
                    if (drive.DriveType == DriveType.Removable && drive.Name == cbxRemoveableDrives.SelectedItem.ToString())
                    {
                        spaceAvailableOnSDCard = drive.TotalFreeSpace;
                    }
                }

                //Do we have enought free space on destiny?
                if (totalBytesSelectedFiles > spaceAvailableOnSDCard)
                {
                    MessageBox.Show("The selected SD card doesn't have enought space available!");
                    return;
                }

                if (!backgroundWorkerCopyFiles.IsBusy)
                {
                    updateCbxRemoveableFiles = true;
                    menuLocalFiles.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { LocalFilesListSelectedItems, cbxRemoveableDrives.Text, "copy", "local" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                    backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void OperationCopySelectedFilesFromLocalEShopListToSDCard()
        {
            long totalBytesSelectedFiles = 0;
            long spaceAvailableOnSDCard = 0;

            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                if (LocalNSPFilesListSelectedItems.Count > 0)
                {
                    foreach (FileData data in LocalNSPFilesListSelectedItems.Values)
                    {
                        totalBytesSelectedFiles += data.ROMSizeBytes;
                    }
                }
                else
                {
                    MessageBox.Show("No files selected");
                    return;
                }

                var driveList = DriveInfo.GetDrives();
                foreach (DriveInfo drive in driveList)
                {
                    if (drive.DriveType == DriveType.Removable && drive.Name == cbxRemoveableDrives.SelectedItem.ToString())
                    {
                        spaceAvailableOnSDCard = drive.TotalFreeSpace;
                    }
                }

                //Do we have enought free space on destiny?
                if (totalBytesSelectedFiles > spaceAvailableOnSDCard)
                {
                    MessageBox.Show("The selected SD card doesn't have enought space available!");
                    return;
                }

                if (!backgroundWorkerCopyFiles.IsBusy)
                {
                    updateCbxRemoveableFiles = true;
                    menuEShop.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    progressPercent = 0;
                    progressCurrentfile = "";
                    timer1.Enabled = true;
                    object[] parameters = { LocalNSPFilesListSelectedItems, cbxRemoveableDrives.Text, "copy", "eshop" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard", "eshop") 
                    backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void OperationMoveSelectedFilesFromLocalEShopListToSDCard()
        {
            long totalBytesSelectedFiles = 0;
            long spaceAvailableOnSDCard = 0;

            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                if (LocalNSPFilesListSelectedItems.Count > 0)
                {
                    foreach (FileData data in LocalNSPFilesListSelectedItems.Values)
                    {
                        totalBytesSelectedFiles += data.ROMSizeBytes;
                    }
                }
                else
                {
                    MessageBox.Show("No files selected");
                    return;
                }

                var driveList = DriveInfo.GetDrives();
                foreach (DriveInfo drive in driveList)
                {
                    if (drive.DriveType == DriveType.Removable && drive.Name == cbxRemoveableDrives.SelectedItem.ToString())
                    {
                        spaceAvailableOnSDCard = drive.TotalFreeSpace;
                    }
                }

                //Do we have enought free space on destiny?
                if (totalBytesSelectedFiles > spaceAvailableOnSDCard)
                {
                    MessageBox.Show("The selected SD card doesn't have enought space available!");
                    return;
                }

                if (!backgroundWorkerCopyFiles.IsBusy)
                {
                    updateCbxRemoveableFiles = true;
                    menuEShop.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    progressPercent = 0;
                    progressCurrentfile = "";
                    timer1.Enabled = true;
                    object[] parameters = { LocalNSPFilesListSelectedItems, cbxRemoveableDrives.Text, "move", "eshop" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard", "eshop") 
                    backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void OperationMoveSelectedFilesFromLocalListToSDCard()
        {
            long totalBytesSelectedFiles = 0;
            long spaceAvailableOnSDCard = 0;

            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                if (LocalFilesListSelectedItems.Count > 0)
                {
                    foreach (FileData data in LocalFilesListSelectedItems.Values)
                    {
                        totalBytesSelectedFiles += data.ROMSizeBytes;
                    }
                }
                else
                {
                    MessageBox.Show("No files selected");
                    return;
                }

                var driveList = DriveInfo.GetDrives();
                foreach (DriveInfo drive in driveList)
                {
                    if (drive.DriveType == DriveType.Removable && drive.Name == cbxRemoveableDrives.SelectedItem.ToString())
                    {
                        spaceAvailableOnSDCard = drive.TotalFreeSpace;
                    }
                }

                //Do we have enought free space on destiny?
                if (totalBytesSelectedFiles > spaceAvailableOnSDCard)
                {
                    MessageBox.Show("The selected SD card doesn't have enought space available!");
                    return;
                }

                if (!backgroundWorkerCopyFiles.IsBusy)
                {
                    updateCbxRemoveableFiles = true;
                    updateFileListAfterMove = true;
                    menuLocalFiles.Enabled = false;
                    //OLVLocalFiles.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationMove;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { LocalFilesListSelectedItems, cbxRemoveableDrives.Text, "move", "local" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                    backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void OperationCopySelectedFilesFromEshopListToFolder()
        {
            if (LocalNSPFilesListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        menuEShop.Enabled = false;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        progressCurrentfile = "";
                        progressPercent = 0;
                        timer1.Enabled = true;
                        object[] parameters = { LocalNSPFilesListSelectedItems, destination, "copy", "eshop" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationMoveSelectedFilesFromEshopListToFolder()
        {
            if (LocalNSPFilesListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        updateFileListAfterMove = true;
                        menuEShop.Enabled = false;
                        //OLVLocalFiles.Enabled = false;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationMove;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        progressPercent = 0;
                        progressCurrentfile = "";
                        timer1.Enabled = true;
                        object[] parameters = { LocalNSPFilesListSelectedItems, destination, "move", "eshop" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard", "eshop") 
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationCopySelectedFilesFromLocalListToFolder()
        {
            if (LocalFilesListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        menuLocalFiles.Enabled = false;
                        //OLVLocalFiles.Enabled = false;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        timer1.Enabled = true;
                        object[] parameters = { LocalFilesListSelectedItems, destination, "copy", "local" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationCopySelectedFilesFromSDCardToFolder()
        {
            if (SDCardListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        menuSDFiles.Enabled = false;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        timer1.Enabled = true;
                        object[] parameters = { SDCardListSelectedItems, destination, "copy", "sdcard" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationMoveSelectedFilesFromLocalListToFolder()
        {
            if (LocalFilesListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        updateFileListAfterMove = true;
                        menuLocalFiles.Enabled = false;
                        //OLVLocalFiles.Enabled = false;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationMove;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        timer1.Enabled = true;
                        object[] parameters = { LocalFilesListSelectedItems, destination, "move", "local" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: source ("local", "sdcard") 
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationMoveSelectedFilesFromSDCardToFolder()
        {
            if (SDCardListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    dialog.RestoreDirectory = true;
                    string destination = dialog.FileName + @"\";

                    if (!backgroundWorkerCopyFiles.IsBusy)
                    {
                        menuSDFiles.Enabled = false;
                        updateCbxRemoveableFiles = true;
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationCopy;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        timer1.Enabled = true;
                        object[] parameters = { SDCardListSelectedItems, destination, "move", "sdcard" }; //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move"), 3: Source ("local", "sdcard")
                        backgroundWorkerCopyFiles.RunWorkerAsync(parameters);
                    }
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationTrimSelectedLocalFiles()
        {
            if (LocalFilesListSelectedItems.Count > 0)
            {
                TrimSelectedFiles(LocalFilesListSelectedItems, "local");
                OLVLocalFiles.RefreshSelectedObjects();
                MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationTrimSelectedSDCardFiles()
        {
            if (SDCardListSelectedItems.Count > 0)
            {
                TrimSelectedFiles(SDCardListSelectedItems, "sdcard");
                OLV_SDCard.RefreshSelectedObjects();
                MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationRenameSelectedLocalFiles()
        {
            if (LocalFilesListSelectedItems.Count > 0)
            {
                RenameSelectedFiles(LocalFilesListSelectedItems, "local");
                OLVLocalFiles.RefreshSelectedObjects();
                MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationRenameSelectedLocalEShopFiles()
        {
            if (LocalNSPFilesListSelectedItems.Count > 0)
            {
                RenameSelectedFiles(LocalNSPFilesListSelectedItems, "eshop");
                OLVEshop.RefreshSelectedObjects();
                MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationRenameSelectedSDCardFiles()
        {
            if (SDCardListSelectedItems.Count > 0)
            {
                RenameSelectedFiles(SDCardListSelectedItems, "sdcard");
                OLV_SDCard.RefreshSelectedObjects();
                MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void RenameSelectedFiles(Dictionary<Tuple<string, string>, FileData> localFilesListSelectedItems, string source) //source possible values: "local", "sdcard", "eshop"
        {
            Util.AutoRenameXCIFiles(localFilesListSelectedItems, source);
        }

        private void selectedFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedLocalFiles();
        }

        private void OperationDeleteSelectedEshopFiles()
        {
            if (LocalNSPFilesListSelectedItems.Count > 0)
            {
                DeleteSelectedFiles(LocalNSPFilesListSelectedItems, "eshop");
            } else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationDeleteSelectedLocalFiles()
        {
            if (LocalFilesListSelectedItems.Count > 0)
            {
                DeleteSelectedFiles(LocalFilesListSelectedItems, "local");
            } else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void OperationDeleteSelectedSDCardFiles()
        {
            if (SDCardListSelectedItems.Count > 0)
            {
                DeleteSelectedFiles(SDCardListSelectedItems, "sdcard");
            } else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void DeleteSelectedFiles(Dictionary<Tuple<string, string>, FileData> selectedItems, string source) //source possible values: "local", "sdcard", "eshop"
        {
            Util.DeleteSelectedFiles(selectedItems, source);
        }

        private void deleteSelectedFilesToolStripMenuItemEshop_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you wnat to delete the selected files from disk?", "Switch Backup Manager", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                OperationDeleteSelectedEshopFiles();
                UpdateLocalNSPGamesList();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }

        }

        private void allFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OLVLocalFiles.Select();
            OLVLocalFiles.SelectAll();
            selectedFilesToolStripMenuItem1_Click(sender, e);
        }

        private void autoRenameFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedLocalFiles();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItemGroupNoneSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "none");
            toolStripMenuItemGroupNoneSD.Checked = true;
        }

        private void toolStripMenuItemGroupGameTitleSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "gametitle");
            toolStripMenuItemGroupGameTitleSD.Checked = true;
        }

        private void toolStripMenuItemGroupTrimmedSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "trimmed");
            toolStripMenuItemGroupTrimmedSD.Checked = true;
        }

        private void toolStripMenuItemGroupCartSizeSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "cartsize");
            toolStripMenuItemGroupCartSizeSD.Checked = true;
        }

        private void toolStripMenuItemGroupTypeSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "type");
            toolStripMenuItemGroupTypeSD.Checked = true;
        }

        private void toolStripMenuItemGroupDeveloperSD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "developer");
            toolStripMenuItemGroupDeveloperSD.Checked = true;
        }

        private void toolStripMenuItemGroupMasterkeySD_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("sdcard");
            ChangeGroupOnFileList("sdcard", "masterkeyrevision");
            toolStripMenuItemGroupMasterkeySD.Checked = true;
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromSDCardToFolder();
        }

        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromSDCardToFolder();
        }

        private void toolStripMenuItem46_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void toolStripMenuItem48_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void toolStripMenuItem49_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Properties.Resources.EN_Soon);
        }

        private void contextMenuStripSDCard_Opening(object sender, CancelEventArgs e)
        {
            if (SDCardListSelectedItems == null || SDCardListSelectedItems.Count == 0)
            {
                e.Cancel = true;
            } else {
                if (Util.UserCanDeleteFiles)
                {
                    toolStripMenuItem97SDCard.Visible = true;
                    deleteSelectedFileserasesFromDiskToolStripMenuItemSDCard.Visible = true;
                }
                else
                {
                    toolStripMenuItem97SDCard.Visible = false;
                    deleteSelectedFileserasesFromDiskToolStripMenuItemSDCard.Visible = false;
                }
            }
        }

        private void copyToFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromSDCardToFolder();
        }

        private void moveToFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromSDCardToFolder();
        }

        private void toolStripMenuItem42_Click(object sender, EventArgs e)
        {
            OLV_SDCard.Select();
            OLV_SDCard.SelectAll();
            toolStripMenuItem41_Click(sender, e);
        }

        private void toolStripMenuItem41_Click(object sender, EventArgs e)
        {
            OperationTrimSelectedSDCardFiles();
        }

        private void trimFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OperationTrimSelectedSDCardFiles();
        }

        private void toolStripMenuItemRenameSelectedFilesOnSDCard_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedSDCardFiles();
        }

        private void OLV_SDCard_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
        {
            //Highlights when not trimmed
            if (e.ColumnIndex == this.olvColumnIsTrimmedSD.Index)
            {
                FileData data = (FileData)e.Model;
                if (!data.IsTrimmed)
                    e.SubItem.BackColor = Color.IndianRed;
            }

            if (e.ColumnIndex == this.olvColumnGameNameSD.Index)
            {
                FileData data = (FileData)e.Model;
                if (data.ContentType == "AddOnContent") //DLC
                    e.SubItem.ForeColor = Color.ForestGreen;
                if (data.ContentType == "Patch") //DLC
                    e.SubItem.ForeColor = Color.OrangeRed;
            }
            else if (e.ColumnIndex == this.olvColumnSourceSD.Index)
            {
                FileData data = (FileData)e.Model;
                if (data.Source.Contains("XCI") || (data.Source.Contains("NSP") && data.Source != "CDNSP") || data.Source.Contains("NCA"))
                    e.SubItem.BackColor = Color.IndianRed;
            }
        }

        private void toolStripMenuItemRenameAllFilesOnSDCard_Click(object sender, EventArgs e)
        {
            OLV_SDCard.Select();
            OLV_SDCard.SelectAll();
            toolStripMenuItemRenameSelectedFilesOnSDCard_Click(sender, e);
        }

        private void autoRenameFilesToolStripMenuItemRenameSD_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedSDCardFiles();
        }

        private void toolStripMenuItemGroupNoneScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "none");
            toolStripMenuItemGroupNoneScene.Checked = true;
        }

        private void toolStripMenuItemGroupGameTitleScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "gametitle");
            toolStripMenuItemGroupGameTitleScene.Checked = true;
        }

        private void toolStripMenuItemGroupTypeScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "cardtype");
            toolStripMenuItemGroupTypeScene.Checked = true;
        }

        private void toolStripMenuItemGroupDeveloperScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "developer");
            toolStripMenuItemGroupDeveloperScene.Checked = true;
        }

        private void toolStripMenuItemGroupMasterKeyScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "firmware");
            toolStripMenuItemGroupFirmwareScene.Checked = true;
        }

        private void regionToolStripMenuItemRegionScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "region");
            regionToolStripMenuItemRegionScene.Checked = true;
        }

        private void releaseGroupToolStripMenuItemReleaseGroupScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "releasegroup");
            releaseGroupToolStripMenuItemReleaseGroupScene.Checked = true;
        }

        private void cartSizeToolStripMenuItemCartSizeScene_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("scene");
            ChangeGroupOnFileList("scene", "cartsize");
            cartSizeToolStripMenuItemCartSizeScene.Checked = true;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var configForm = new FormConfigs();
            configForm.StartPosition = FormStartPosition.CenterParent;
            configForm.ShowDialog(this);
        }

        private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((OLVLocalFiles.Items.Count > 0) && (OLVLocalFiles.SelectedItems.Count == 1))
            {
                FileData data = (FileData)OLVLocalFiles.SelectedObject;
                if (data != null)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(data.FilePath));
                } else
                {
                    MessageBox.Show("Select one item from the list.");
                }
            } else
            {
                MessageBox.Show("Select one item from the list.");
            }
        }

        private void showInExplorerToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if ((OLV_SDCard.Items.Count > 0) && (OLV_SDCard.SelectedItems.Count == 1))
            {
                FileData data = (FileData)OLV_SDCard.SelectedObject;
                if (data != null)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(data.FilePath));
                }
                else
                {
                    MessageBox.Show("Select one item from the list.");
                }
            }
            else
            {
                MessageBox.Show("Select one item from the list.");
            }
        }

        private void textBoxFilterLocal_TextChanged(object sender, EventArgs e)
        {
            OLVLocalFiles.SelectedItems.Clear();
            ClearGameInformation();
            toolStripStatusLabel1.Text = "0 Selected (0MB)";

            TextMatchFilter filterText = TextMatchFilter.Contains(OLVLocalFiles, textBoxFilterLocal.Text);

            switch (cbxFilterLocal.Text)
            {
                case "Title ID":
                    filterText.Columns = new[] { olvColumnTitleIDLocal };
                    break;
                case "ROM size":
                    filterText.Columns = new[] { olvColumnROMSizeLocal };
                    break;
                case "Game title":
                    filterText.Columns = new[] { olvColumnGameNameLocal };
                    break;
                case "Used space":
                    filterText.Columns = new[] { olvColumnUsedSpaceLocal };
                    break;
                case "Cart size":
                    filterText.Columns = new[] { olvColumnCartSizeLocal };
                    break;
                case "Languages":
                    filterText.Columns = new[] { olvColumnLanguagesLocal };
                    break;
                case "Card type":
                    filterText.Columns = new[] { olvColumnCardTypeLocal };
                    break;
                case "Filename":
                    filterText.Columns = new[] { olvColumnFilePathLocal };
                    break;
                case "Developer":
                    filterText.Columns = new[] { olvColumnDeveloperLocal };
                    break;
                case "Game revision":
                    filterText.Columns = new[] { olvColumnGameRevisionLocal };
                    break;
                case "Version":
                    filterText.Columns = new[] { olvColumnVersionLocal };
                    break;
                case "Latest":
                    filterText.Columns = new[] { olvColumnLatestLocal };
                    break;
                case "Masterkey revision":
                    filterText.Columns = new[] { olvColumnMasterKeyLocal };
                    break;
                case "Firmware":
                    filterText.Columns = new[] { olvColumnFirmwareLocal };
                    break;
                case "Trimmed":
                    filterText.Columns = new[] { olvColumnIsTrimmedLocal };
                    break;
                case "Publisher":
                    filterText.Columns = new[] { olvColumnPublisherLocal };
                    break;
                case "Release date":
                    filterText.Columns = new[] { olvColumnReleaseDateLocal };
                    break;
                case "Nº of players":
                    filterText.Columns = new[] { olvColumnNumberOfPlayersLocal };
                    break;
                case "Category":
                    filterText.Columns = new[] { olvColumnCategoriesLocal };
                    break;
                case "Imported Date":
                    filterText.Columns = new[] { olvColumnImportedDateLocal };
                    break;
                case "Source":
                    filterText.Columns = new[] { olvColumnSourceLocal };
                    break;
                default:
                    filterText = null;
                    break;
            }
            
            OLVLocalFiles.ModelFilter = new CompositeAllFilter(new List<IModelFilter> { filterText });
            OLVLocalFiles.DefaultRenderer = new HighlightTextRenderer(filterText);
            SumarizeLocalGamesList("local");
        }

        private void cbxFilterLocal_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxFilterLocal_TextChanged(sender, e);
        }

        private void cbxFilterSD_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxFilterSD_TextChanged(sender, e);
        }

        private void textBoxFilterSD_TextChanged(object sender, EventArgs e)
        {
            OLV_SDCard.SelectedItems.Clear();
            ClearGameInformation();
            toolStripStatusLabel1.Text = "0 Selected (0MB)";

            TextMatchFilter filterText = TextMatchFilter.Contains(OLV_SDCard, textBoxFilterSD.Text);

            switch (cbxFilterSD.Text)
            {
                case "Title ID":
                    filterText.Columns = new[] { olvColumnTitleIDSD };
                    break;
                case "Game title":
                    filterText.Columns = new[] { olvColumnGameNameSD };
                    break;
                case "ROM size":
                    filterText.Columns = new[] { olvColumnROMSizeSD };
                    break;
                case "Used space":
                    filterText.Columns = new[] { olvColumnUsedSpaceSD };
                    break;
                case "Trimmed":
                    filterText.Columns = new[] { olvColumnIsTrimmedSD };
                    break;
                case "Cart size":
                    filterText.Columns = new[] { olvColumnCartSizeSD };
                    break;
                case "Languages":
                    filterText.Columns = new[] { olvColumnLanguagesSD };
                    break;
                case "Card type":
                    filterText.Columns = new[] { olvColumnCardTypeSD };
                    break;
                case "Filename":
                    filterText.Columns = new[] { olvColumnFilePathSD };
                    break;
                case "Developer":
                    filterText.Columns = new[] { olvColumnDeveloperSD };
                    break;
                case "Game revision":
                    filterText.Columns = new[] { olvColumnGameRevisionSD };
                    break;
                case "Version":
                    filterText.Columns = new[] { olvColumnVersionSD };
                    break;
                case "Latest":
                    filterText.Columns = new[] { olvColumnLatestSD };
                    break;
                case "Masterkey revision":
                    filterText.Columns = new[] { olvColumnMasterKeySD };
                    break;
                case "Source":
                    filterText.Columns = new[] { olvColumnSourceSD };
                    break;
                default:
                    filterText = null;
                    break;
            }

            OLV_SDCard.ModelFilter = new CompositeAllFilter(new List<IModelFilter> { filterText });
            OLV_SDCard.DefaultRenderer = new HighlightTextRenderer(filterText);
            SumarizeLocalGamesList("sdcard");
        }

        private void btnClearFilterSD_Click(object sender, EventArgs e)
        {
            textBoxFilterSD.Clear();
        }

        private void cbxFilterScene_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxFilterScene_TextChanged(sender, e);
        }

        private void textBoxFilterScene_TextChanged(object sender, EventArgs e)
        {
            OLVSceneList.SelectedItems.Clear();
            ClearGameInformation();
            toolStripStatusLabel1.Text = "0 Selected (0MB)";

            TextMatchFilter filterText = TextMatchFilter.Contains(OLVSceneList, textBoxFilterScene.Text);

            switch (cbxFilterScene.Text)
            {
                case "Title ID":
                    filterText.Columns = new[] { olvColumnTitleIDScene };
                    break;
                case "ID":
                    filterText.Columns = new[] { olvColumnSceneID };
                    break;
                case "Game title":
                    filterText.Columns = new[] { olvColumnGameNameScene };
                    break;
                case "ROM size":
                    filterText.Columns = new[] { olvColumnROMSizeSD };
                    break;
                case "Trimmed size":
                    filterText.Columns = new[] { sizeColumnROMSizeScene };
                    break;
                case "Developer":
                    filterText.Columns = new[] { olvColumnDeveloperScene };
                    break;
                case "Region":
                    filterText.Columns = new[] { columnSceneRegionScene };
                    break;
                case "Languages":
                    filterText.Columns = new[] { olvColumnLanguagesScene };
                    break;
                case "Release group":
                    filterText.Columns = new[] { olvColumnGroupScene };
                    break;
                case "Cart size":
                    filterText.Columns = new[] { olvColumnCartSizeScene };
                    break;
                case "Serial number":
                    filterText.Columns = new[] { olvColumnSerialScene };
                    break;
                case "Firmware":
                    filterText.Columns = new[] { olvColumnFirmwareScene };
                    break;
                case "Card type":
                    filterText.Columns = new[] { olvColumnCardTypeScene };
                    break;
                default:
                    filterText = null;
                    break;
            }

            OLVSceneList.ModelFilter = new CompositeAllFilter(new List<IModelFilter> { filterText });
            OLVSceneList.DefaultRenderer = new HighlightTextRenderer(filterText);
            SumarizeLocalGamesList("scene");
        }

        private void filesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "NSP Files (*.NSP)|*.nsp";
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Switch Backup Manager - Add Files";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (!backgroundWorkerAddFile.IsBusy)
                {
                    menuEShop.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { openFileDialog.FileNames, "nsp" }; //0: FilesList (string[]), 1: FileType ("xci", "nsp") 
                    backgroundWorkerAddFile.RunWorkerAsync(parameters);
                }
            }
        }

        private void OLVEshop_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {

            if (OLVEshop.SelectedItems.Count == 0)
            {
                LocalNSPFilesListSelectedItems.Clear();
                ClearGameInformation();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }
            else
            {
                if (updateFileListAfterMove) //To prevent user changing selection during file operations...
                {
                    return;
                }

                FileData data = null;
                int count = 0;
                if (e.IsSelected)
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVEshop.SelectedItems;
                    //string FirstTitleIDSelected = selectedItems[selectedItems.Count-1].Text;
                    //DisplayGameInformation(FirstTitleIDSelected);

                    LocalNSPFilesListSelectedItems.Clear();
                    string titleID = selectedItems[0].Text;
                    string rev = Convert.ToString(((FileData)((OLVListItem)selectedItems[0]).RowObject).Version);
                    string titleIDBaseGame = "";

                    count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        rev = Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version);
                        data = Util.GetFileData(titleID, rev, LocalNSPFilesList);
                        titleIDBaseGame = data.TitleIDBaseGame;
                        LocalNSPFilesListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the last selected item
                    /*
                    if (titleIDBaseGame != "")
                    {
                        titleID = titleIDBaseGame;
                    }
                    */
                    DisplayGameInformation(titleID, titleIDBaseGame, rev, LocalNSPFilesList, "eshop");
                }
                else
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVEshop.SelectedItems;
                    count = 0;
                    long size = 0;
                    LocalNSPFilesListSelectedItems.Clear();
                    foreach (ListViewItem item in selectedItems)
                    {
                        string titleID = item.Text;
                        string rev = Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version);
                        data = Util.GetFileData(titleID, rev, LocalNSPFilesList);
                        LocalNSPFilesListSelectedItems.Add(new Tuple<string, string>(titleID, rev), data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                }
                if (count == 1)
                {
                    panelEditTitle.Visible = true;
                    TitleToEdit = data;
                    LoadFieldsForEdition();
                }
                else
                {
                    panelEditTitle.Visible = false;
                }

            }
        }

        private void OLVEshop_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Delete)
            {
                RemoveSelectedFiles("eshop");
            }
        }

        private void folderToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                menuEShop.Enabled = false;
                if (!backgroundWorkerAddFilesFromDirectory.IsBusy)
                {
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    object[] parameters = { selectedPath, "nsp" }; //0: FilesList (string[]), 1: FileType ("xci", "nsp") 
                    backgroundWorkerAddFilesFromDirectory.RunWorkerAsync(parameters);
                }
            }
        }

        private void toolStripMenuItemSelectAllEshop_Click(object sender, EventArgs e)
        {
            OLVEshop.Select();
            OLVEshop.SelectAll();
        }

        private void toolStripMenuItemSelectNoneEShop_Click(object sender, EventArgs e)
        {
            OLVEshop.SelectedItems.Clear();
        }

        private void toolStripMenuItemSelectInvertEShop_Click(object sender, EventArgs e)
        {
            InvertSelection(OLVEshop);
        }

        private void toolStripMenuItemSelectSDCardItemsOnSDEShop_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = ContainsLists(SDCardList, LocalNSPFilesList);
                FileData dummy;
                OLVEshop.Select();
                OLVEshop.HideSelection = false;
                OLVEshop.SelectedItems.Clear();
                foreach (ListViewItem item in OLVEshop.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItemSelectSDCardItemsNotOnSDCardEShop_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<Tuple<string, string>, FileData> list = DiffLists(LocalNSPFilesList, SDCardList);
                FileData dummy;
                OLVEshop.Select();
                OLVEshop.HideSelection = false;
                OLVEshop.SelectedItems.Clear();
                foreach (ListViewItem item in OLVEshop.Items)
                {
                    if (list.TryGetValue(new Tuple<string, string>(item.Text, Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version)), out dummy))
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please, first select a SD card from the list.");
            }
        }

        private void toolStripMenuItemGroupingNoneEShop_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("eshop");
            ChangeGroupOnFileList("eshop", "none");
            toolStripMenuItemGroupingNoneEShop.Checked = true;

        }

        private void toolStripMenuItemGroupingGameTitleEShop_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("eshop");
            ChangeGroupOnFileList("eshop", "gametitle");
            toolStripMenuItemGroupingGameTitleEShop.Checked = true;
        }

        private void toolStripMenuItemGroupingDeveloperEShop_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("eshop");
            ChangeGroupOnFileList("eshop", "developer");
            toolStripMenuItemGroupingDeveloperEShop.Checked = true;
        }

        private void toolStripMenuItemGroupingMasterKeyEShop_Click(object sender, EventArgs e)
        {
            ClearGroupingMenuChecks("eshop");
            ChangeGroupOnFileList("eshop", "masterkeyrevision");
            toolStripMenuItemGroupingMasterKeyEShop.Checked = true;
        }

        private void toolStripMenuItemRemoveSelectedEShop_Click(object sender, EventArgs e)
        {
            RemoveSelectedFiles("eshop");
        }

        private void toolStripMenuItemCopyFilesToSDEShop_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalEShopListToSDCard();
        }

        private void toolStripMenuItemMoveFilesToSDEShop_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalEShopListToSDCard();
        }

        private void toolStripMenuItemCopyFilesToFolderEShop_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromEshopListToFolder();
        }

        private void updateEshopLocalDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Util.RemoveMissingFilesFromXML(Util.XML_NSP_Local, Util.LOCAL_NSP_FILES_DB);
            UpdateLocalNSPGamesList();
            MessageBox.Show("Done.");
        }

        private void toolStripMenuItemMoveFilesToFolderEShop_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromEshopListToFolder();
        }

        private void toolStripMenuItemEShopShowInExplorer_Click(object sender, EventArgs e)
        {
            if ((OLVEshop.Items.Count > 0) && (OLVEshop.SelectedItems.Count == 1))
            {
                FileData data = (FileData)OLVEshop.SelectedObject;
                if (data != null)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(data.FilePath));
                }
                else
                {
                    MessageBox.Show("Select one item from the list.");
                }
            }
            else
            {
                MessageBox.Show("Select one item from the list.");
            }
        }

        private void toolStripMenuItemEShopAutoRename_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedLocalEShopFiles();
        }

        private void sDCardToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromLocalEShopListToSDCard();
        }

        private void folderToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            OperationCopySelectedFilesFromEshopListToFolder();
        }

        private void sDCardToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromLocalEShopListToSDCard();
        }

        private void folderToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            OperationMoveSelectedFilesFromEshopListToFolder();
        }

        private void OLVEshop_CellOver(object sender, CellOverEventArgs e)
        {
            if (e.Item != null)
            {
                string value = "";
                if (e.Item.GetSubItem(e.ColumnIndex) != null)
                {
                    value = e.Item.GetSubItem(e.ColumnIndex).Text;
                }
                clipboardInfoEShop = value;
            }            
        }

        private void OLVSceneList_CellOver(object sender, CellOverEventArgs e)
        {
            if (e.Item != null)
            {
                string value = "";
                if (e.Item.GetSubItem(e.ColumnIndex) != null)
                {
                    value = e.Item.GetSubItem(e.ColumnIndex).Text;
                }
                clipboardInfoScene = value;
            }
        }

        private void OLV_SDCard_CellOver(object sender, CellOverEventArgs e)
        {
            if (e.Item != null)
            {
                string value = "";
                if (e.Item.GetSubItem(e.ColumnIndex) != null)
                {
                    value = e.Item.GetSubItem(e.ColumnIndex).Text;
                }
                clipboardInfoSD = value;
            }            
        }

        private void OLVLocalFiles_CellOver(object sender, CellOverEventArgs e)
        {
            if (e.Item != null)
            {
                string value = "";
                if (e.Item.GetSubItem(e.ColumnIndex) != null)
                {
                    value = e.Item.GetSubItem(e.ColumnIndex).Text;
                }
                clipboardInfoLocal = value;
            }
        }

        private void copyInfoToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (clipboardInfoEShop != "")
            {
                Clipboard.SetText(clipboardInfoEShop);
            }            
        }

        private void copyInfoToClipboardToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (clipboardInfoLocal != "")
            {
                Clipboard.SetText(clipboardInfoLocal);
            }            
        }

        private void copyInfoToClipboardToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (clipboardInfoSD != "")
            {
                Clipboard.SetText(clipboardInfoSD);
            }
        }

        private void copyInfoToClipboardToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (clipboardInfoScene != "")
            {
                Clipboard.SetText(clipboardInfoScene);
            }            
        }

        private void contextMenuStripScene_Opening(object sender, CancelEventArgs e)
        {
            if (SceneReleasesSelectedItems == null || SceneReleasesSelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
        }

        private void contextMenuStripEShop_Opening(object sender, CancelEventArgs e)
        {
            if (LocalNSPFilesListSelectedItems == null || LocalNSPFilesListSelectedItems.Count == 0)
            {
                e.Cancel = true;
            } else
            {
                if (Util.UserCanDeleteFiles)
                {
                    toolStripMenuItem97Eshop.Visible = true;
                    deleteSelectedFilesToolStripMenuItemEshop.Visible = true;                
                } else
                {
                    toolStripMenuItem97Eshop.Visible = false;
                    deleteSelectedFilesToolStripMenuItemEshop.Visible = false;
                }
            }
        }

        private void cbxFilterEshop_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxFilterEShop_TextChanged(sender, e);
        }

        private void textBoxFilterEShop_TextChanged(object sender, EventArgs e)
        {
            OLVEshop.SelectedItems.Clear();
            ClearGameInformation();
            toolStripStatusLabel1.Text = "0 Selected (0MB)";

            TextMatchFilter filterText = TextMatchFilter.Contains(OLVEshop, textBoxFilterEShop.Text);
            switch (cbxFilterEshop.Text)
            {
                case "Title ID":
                    filterText.Columns = new[] { olvColumnTitleIDEShop };
                    break;
                case "Game title":
                    filterText.Columns = new[] { olvColumnGameNameScene };
                    break;
                case "ROM size":
                    filterText.Columns = new[] { olvColumnROMSizeEshop };
                    break;
                case "Languages":
                    filterText.Columns = new[] { olvColumnLanguagesEShop };
                    break;
                case "Filename":
                    filterText.Columns = new[] { olvColumnFilePathEShop };
                    break;
                case "Developer":
                    filterText.Columns = new[] { olvColumnDeveloperEShop };
                    break;
                case "Game revision":
                    filterText.Columns = new[] { olvColumnGameRevisionEShop };
                    break;
                case "Version":
                    filterText.Columns = new[] { olvColumnVersionEShop };
                    break;
                case "Latest":
                    filterText.Columns = new[] { olvColumnLatestEShop };
                    break;
                case "Masterkey revision":
                    filterText.Columns = new[] { olvColumnMasterKeyRevisionEShop };
                    break;
                case "Firmware":
                    filterText.Columns = new[] { olvColumnFirmwareEShop };
                    break;
                case "Distribution":
                    filterText.Columns = new[] { olvColumnDistributionType };
                    break;
                case "Publisher":
                    filterText.Columns = new[] { olvColumnPublisherEshop };
                    break;
                case "Release date":
                    filterText.Columns = new[] { olvColumnReleaseDateEshop };
                    break;
                case "Nº of players":
                    filterText.Columns = new[] { olvColumnNumberOfPlayersEshop };
                    break;
                case "Category":
                    filterText.Columns = new[] { olvColumnCategoriesEShop };
                    break;
                case "Imported Date":
                    filterText.Columns = new[] { olvColumnImportedDateEShop};
                    break;
                case "Source":
                    filterText.Columns = new[] { olvColumnSourceEShop };
                    break;
                default:
                    filterText = null;
                    break;
            }

            OLVEshop.ModelFilter = new CompositeAllFilter(new List<IModelFilter> { filterText, filterContentTypeEShop });
            OLVEshop.DefaultRenderer = new HighlightTextRenderer(filterText);
            SumarizeLocalGamesList("eshop");
        }

        private void btnClearFilterScene_Click(object sender, EventArgs e)
        {
            textBoxFilterScene.Clear();
        }

        private void btnClearFilterEShop_Click(object sender, EventArgs e)
        {
            textBoxFilterEShop.Clear();
        }

        private void backgroundWorkerScanNewFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Util.UpdateDirectories();
        }

        private void backgroundWorkerScanNewFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;

            UpdateLocalGamesList();
            UpdateLocalNSPGamesList();
            tabControl1_SelectedIndexChanged(this, new EventArgs());
        }

        private void toolStripMenuItemEShopUpdateInfo_Click(object sender, EventArgs e)
        {
            if (LocalNSPFilesListSelectedItems.Count > 0)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;

                if (!backgroundWorkerUpdateFiles.IsBusy)
                {
                    menuEShop.Enabled = false;
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationUpdateFiles;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    progressCurrentfile = "";
                    progressPercent = 0;
                    timer1.Enabled = true;
                    object[] parameters = { LocalNSPFilesListSelectedItems, "eshop" }; //0: FilesList (Dictionary), 1: source ("local", "sdcard", "eshop") 
                    backgroundWorkerUpdateFiles.RunWorkerAsync(parameters);
                }
            }
            else
            {
                MessageBox.Show("No files selected");
                return;
            }
        }

        private void backgroundWorkerUpdateFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            object[] parameters = e.Argument as object[];

            Dictionary<Tuple<string, string>, FileData> filesList = (Dictionary<Tuple<string, string>, FileData>)parameters[0];
            string source = (string)parameters[1];

            Util.UpdateFilesInfo(filesList, source);
        }

        private void OLVEshop_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.ColumnIndex == this.olvColumnGameNameEShop.Index)
            {
                FileData data = (FileData)e.Model;
                if (data.ContentType == "AddOnContent") //DLC
                    e.SubItem.ForeColor = Color.ForestGreen;
                if (data.ContentType == "Patch") //DLC
                    e.SubItem.ForeColor = Color.OrangeRed;
            }
            else if (e.ColumnIndex == this.olvColumnSourceEShop.Index)
            {
                FileData data = (FileData)e.Model;
                if (data.Source.Contains("XCI") || data.Source.Contains("NCA"))
                    e.SubItem.BackColor = Color.IndianRed;
            }
        }

        private void richTextBoxLog_TextChanged(object sender, EventArgs e)
        {
            if (updateLog)
            {
                richTextBoxLog.Suspend();
                richTextBoxLog.SelectionStart = richTextBoxLog.Text.Length;
                richTextBoxLog.ScrollToCaret();
                richTextBoxLog.Resume();
            }
        }

        private void btnClearLogFile_Click(object sender, EventArgs e)
        {
            richTextBoxLog.Clear();
            if (File.Exists(Util.LOG_FILE))
            {
                try
                {
                    File.Delete(Util.LOG_FILE);
                } catch (Exception ex)
                {
                    Util.logger.Error("Could not delete log file. " + ex.StackTrace);
                }                
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveEnvironment();
        }

        private void lnkInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkInfo.LinkVisited = true;
            System.Diagnostics.Process.Start(e.Link.LinkData as string);
        }

        private void ScrapExtraInfoFromWeb(string source)
        {
            switch (source)
            {
                case "local":
                    Util.logger.Info("Start scraping info from web for XCI files.");
                    UpdateGamesInfoFromWeb("local_all");
                    break;
                case "eshop":
                    Util.logger.Info("Start scraping info from web for NSP files.");
                    UpdateGamesInfoFromWeb("eshop_all");
                    break;
            }
        }

        /// <summary>
        /// Update a list of files with information from web
        /// </summary>
        /// <param name="source">Which list? Valid values are local, sdcard, eshop, scene</param>
        private void UpdateGamesInfoFromWeb(string source)
        {
            Dictionary<Tuple<string, string>, FileData> list = null;

            switch (source)
            {
                case "local":
                    list = LocalFilesListSelectedItems;
                    break;
                case "local_all":
                    list = LocalFilesList;
                    break;
                case "sdcard":
                    list = SDCardListSelectedItems;
                    break;
                case "eshop":
                    list = LocalNSPFilesListSelectedItems;
                    break;
                case "eshop_all":
                    list = LocalNSPFilesList;
                    break;
                case "scene":
                    list = SceneReleasesSelectedItems;
                    break;
            }

            if (list != null)
            {
                if (list.Count > 0)
                {
                    if (!backgroundWorkerScrapExtraInfo.IsBusy)
                    {
                        //updateFileListAfterMove = true;
                        switch (source)
                        {
                            case "local":
                                menuLocalFiles.Enabled = false;
                                break;
                            case "local_all":
                                menuLocalFiles.Enabled = false;
                                break;
                            case "eshop":
                                menuEShop.Enabled = false;
                                break;
                            case "eshop_all":
                                menuEShop.Enabled = false;
                                break;
                        }
                        
                        toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationsScrapFromWeb;
                        toolStripStatusFilesOperation.Visible = true;
                        toolStripProgressAddingFiles.Visible = true;
                        toolStripStatusLabelGame.Text = "";
                        toolStripStatusLabelGame.Visible = true;
                        toolStripProgressAddingFiles.Value = 0;
                        timer1.Enabled = true;
                        object[] parameters = { list, source }; //0: FilesList (Dictionary), 1: source ("local", "sdcard", "eshop", "scene") 
                        backgroundWorkerScrapExtraInfo.RunWorkerAsync(parameters);
                    }
                }
                else
                {
                    MessageBox.Show("No files selected");
                    return;
                }
            }
        }

        private void backgroundWorkerScrapExtraInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            object[] parameters = e.Argument as object[];

            Dictionary<Tuple<string, string>, FileData> filesList = (Dictionary<Tuple<string, string>, FileData>)parameters[0];
            string source = (string)parameters[1];
            
            Util.GetExtendedInfo(filesList, source);
            e.Result = source;
        }

        private void backgroundWorkerScrapExtraInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //updateFileListAfterMove = false;
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Text = "";
            toolStripStatusLabelGame.Visible = false;

            string source = e.Result as string;

            if (source == "local" || source == "local_all")
            {
                LocalFilesListSelectedItems.Clear();
                UpdateLocalGamesList();
                menuLocalFiles.Enabled = true;
            } else if (source == "eshop" || source == "eshop_all")
            {
                LocalNSPFilesListSelectedItems.Clear();
                UpdateLocalNSPGamesList();
                menuEShop.Enabled = true;
            }

            tabControl1_SelectedIndexChanged(this, new EventArgs());
            MessageBox.Show("Done");
        }

        private void updateGameInfoFromWebToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateGamesInfoFromWeb("local");
        }

        private void btnClearFilterLocal_Click(object sender, EventArgs e)
        {
            textBoxFilterLocal.Clear();
        }

        private void updateGameInfoFromWebToolStripMenuItemEshop_Click(object sender, EventArgs e)
        {
            UpdateGamesInfoFromWeb("eshop");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string source_list = "";

            switch (tabControl1.SelectedIndex)
            {
                case 0: //Files
                    source_list = "local";
                    break;
                case 1: //SD Card
                    source_list = "sdcard";
                    break;
                case 2: //Scene
                    source_list = "scene";
                    break;
                case 3: //Eshop
                    source_list = "eshop";
                    break;
            }

            SaveEditedTitle(source_list);
        }

        private void scrapExtendedInfoFromWebToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string source_list = "";

            switch (tabControl1.SelectedIndex)
            {
                case 0: //Files
                    source_list = "local";
                    break;
                case 1: //SD Card
                    source_list = "sdcard";
                    break;
                case 2: //Scene
                    source_list = "scene";
                    break;
                case 3: //Eshop
                    source_list = "eshop";
                    break;
            }

            ScrapExtraInfoFromWeb(source_list);
        }

        private void leftPanelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
            leftPanelToolStripMenuItem.Checked = !leftPanelToolStripMenuItem.Checked;
        }

        private void bottonPanelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer2.Panel2Collapsed = !splitContainer2.Panel2Collapsed;
            bottonPanelToolStripMenuItem.Checked = !bottonPanelToolStripMenuItem.Checked;
        }

        private void FilterEshopByContentType()
        {
            olvColumnContentTypeEShop.IsVisible = true;
            string[] filter = { "ABCBDBABD^", "ABCBDBABD^", "ABCBDBABD^" };
            if (cbBaseGame.Checked)
            {
                filter[0] = "Base Game";
            }
            if (cbDLC.Checked)
            {
                filter[1] = "DLC";
            }
            if (cbUpdates.Checked)
            {
                filter[2] = "Update";
            }

            filterContentTypeEShop = TextMatchFilter.Contains(OLVEshop, filter);
            filterContentTypeEShop.Columns = new[] { olvColumnContentTypeEShop };

            OLVEshop.ModelFilter = new CompositeAllFilter(new List<IModelFilter> { filterContentTypeEShop });
            OLVEshop.DefaultRenderer = new HighlightTextRenderer(filterContentTypeEShop);
            textBoxFilterEShop_TextChanged(this, new EventArgs());

            SumarizeLocalGamesList("eshop");
        }

        private void cbBaseGame_CheckedChanged(object sender, EventArgs e)
        {
            FilterEshopByContentType();
        }

        private void cbUpdates_CheckedChanged(object sender, EventArgs e)
        {
            FilterEshopByContentType();
        }

        private void cbDLC_CheckedChanged(object sender, EventArgs e)
        {
            FilterEshopByContentType();
        }

        private void deleteSelectedFileserasesFromDiskToolStripMenuItemLocalFiles_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you wnat to delete the selected files from disk?", "Switch Backup Manager", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                OperationDeleteSelectedLocalFiles();
                UpdateLocalGamesList();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
            }
        }

        private void deleteSelectedFileserasesFromDiskToolStripMenuItemSDCard_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you wnat to delete the selected files from disk?", "Switch Backup Manager", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                OperationDeleteSelectedSDCardFiles();
                UpdateSDCardList();
                toolStripStatusLabel1.Text = "0 Selected (0MB)";
                MessageBox.Show("Done");
            }
        }

        private void outdatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SortedDictionary<Tuple<String, int>, FileData> updates = new SortedDictionary<Tuple<string, int>, FileData>();
            Dictionary<Tuple<String, int>, String> updates_to_delete = new Dictionary<Tuple<String, int>, string>();

            //This gives us a SortedDictionary containing only updates
            foreach (FileData file in LocalNSPFilesList.Values)
            {
                if (file.ContentType == "Patch")
                {
                    try
                    {
                        updates.Add(new Tuple<string, int>(file.TitleID, Convert.ToInt32(file.Version)), file);
                    } catch (Exception ex)
                    {
                        Util.logger.Error("Error on " + file.TitleID + ", " + file.Version);
                    }                    
                }               
            }

            int index = 0;
            string titleID = updates.ElementAt(0).Value.TitleID;
            int version = -1;
            try
            {
                 version = Convert.ToInt32(updates.ElementAt(0).Value.Version);
            } catch
            {
                Util.logger.Error("Error on " + titleID + ", " + updates.ElementAt(0).Value.Version);
            }
            
            foreach (FileData file in updates.Values)
            {
                if (index <= updates.Count - 2)
                {
                    updates_to_delete.Add(new Tuple<string, int>(file.TitleID, Convert.ToInt32(file.Version)), "");
                }

                if (file.TitleID != titleID)
                {
                    updates_to_delete.Remove(new Tuple<string, int>(titleID, Convert.ToInt32(version)));
                }

                titleID = updates.ElementAt(index).Value.TitleID;
                version = Convert.ToInt32(updates.ElementAt(index).Value.Version);
                index++;
            }

            OLVEshop.Select();
            OLVEshop.HideSelection = false;
            OLVEshop.SelectedItems.Clear();
            foreach (ListViewItem item in OLVEshop.Items)
            {
                string dummy;
                try
                {
                    if (updates_to_delete.TryGetValue(new Tuple<string, int>(item.Text, Convert.ToInt32(Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version))), out dummy))
                    {
                        item.Selected = true;
                    }
                } catch
                {
                    //Util.logger.Error("Error on " + item.Text + ", " + Convert.ToString(((FileData)((OLVListItem)item).RowObject).Version));
                }
            }
//            OLVEshop.RefreshSelectedObjects();
        }

        private void toolStripMenuItemSelectSceneOnEShop_Click(object sender, EventArgs e)
        {

        }

        private void itemsOnEshjToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<Tuple<string, string>, FileData> list = ContainsListsIgnoreVersion(LocalNSPFilesList, LocalFilesList);
//            Dictionary<Tuple<string, string>, FileData> list = ContainsListsIgnoreVersion(LocalFilesList, LocalNSPFilesList);
            FileData dummy;
            OLVLocalFiles.Select();
            OLVLocalFiles.HideSelection = false;
            OLVLocalFiles.SelectedItems.Clear();
            foreach (ListViewItem item in OLVLocalFiles.Items)
            {
                if (list.TryGetValue(new Tuple<string, string>(item.Text, ""), out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        public void UpdateTitleVersionList()
        {
            TitleVersionList = Util.LoadVersionListToDictionary();
        }

        private void updateVersionListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Util.UpdateVersionList();
            UpdateTitleVersionList();
            MessageBox.Show("Done.");

            if (!backgroundWorkerUpdateVersionList.IsBusy)
            {
                backgroundWorkerUpdateVersionList.RunWorkerAsync();
            }
        }

        private void backgroundWorkerUpdateVersionList_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileData data in LocalFilesList.Values)
            {
                int latest = -1;
                TitleVersionList.TryGetValue(data.TitleIDBaseGame.Substring(0, 13).ToUpper() + "000", out latest);
                if (latest != -1)
                {
                    data.Latest = latest.ToString();
                }
            }

            foreach (FileData data in LocalNSPFilesList.Values)
            {
                if (data.ContentType != "AddOnContent")
                {
                    int latest = -1;
                    TitleVersionList.TryGetValue(data.TitleIDBaseGame.Substring(0, 13).ToUpper() + "000", out latest);
                    if (latest != -1)
                    {
                        data.Latest = latest.ToString();
                    }
                }
            }

            foreach (FileData data in SDCardList.Values)
            {
                if (data.ContentType != "AddOnContent")
                {
                    int latest = -1;
                    TitleVersionList.TryGetValue(data.TitleIDBaseGame.Substring(0, 13).ToUpper() + "000", out latest);
                    if (latest != -1)
                    {
                        data.Latest = latest.ToString();
                    }
                }
            }
        }

        private void backgroundWorkerUpdateVersionList_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}
