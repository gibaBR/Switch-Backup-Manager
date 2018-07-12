using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Switch_Backup_Manager
{
    public partial class FrmMain : Form
    {
        private Dictionary<string, FileData> LocalFilesList;
        private Dictionary<string, FileData> LocalFilesListSelectedItems;
        private Dictionary<string, FileData> SceneReleasesList;
        private Dictionary<string, FileData> SceneReleasesSelectedItems;
        private Dictionary<string, FileData> SDCardList;
        private Dictionary<string, FileData> SDCardListSelectedItems;

        private FormWindowState currentWindowState;
        private bool updateCbxRemoveableFiles;
        private bool updateFileListAfterMove;

        //To update Statusbar wheile adding files
        public static int progressPercent = 0;
        public static string progressCurrentfile = "";

        public FrmMain()
        {
            InitializeComponent();

            //Need to think a way of auto resizing columns based on screen resolution to ocupy all space available
            //this.Width = Screen.PrimaryScreen.Bounds.Width;

            lblSpaceAvailabeOnSD.Visible = false;

            Util.LoadSettings();
            LocalFilesList = new Dictionary<string, FileData>();
            SceneReleasesList = new Dictionary<string, FileData>();
            SDCardList = new Dictionary<string, FileData>();

            SetupOLVs();

            UpdateSceneReleasesList();
            UpdateLocalGamesList();
        }

        private void SetupOLVs()
        {
            noneToolStripMenuItem1.Checked = true;
            OLVLocalFiles.SetObjects(LocalFilesList.Values);
            OLVSceneList.SetObjects(SceneReleasesList.Values);
            sizeColumnROMSizeScene.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            localRomSizeColumn.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            localUsedSpaceColumn.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnROMSizeSD.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };
            olvColumnUsedSpaceSD.AspectToStringConverter = delegate (object x) { return Util.BytesToGB((long)x); };

            olvColumnLanguagesLocal.AspectToStringConverter = delegate (object x) {
                string result = "";
                foreach (string language in (List<string>) x)
                {
                    result += language + ", ";
                }
                result = result.Remove(result.Length - 2);
                return result; 
            };

            olvColumnLanguagesSD.AspectToStringConverter = delegate (object x) {
                string result = "";
                foreach (string language in (List<string>)x)
                {
                    result += language + ", ";
                }
                result = result.Remove(result.Length - 2);
                return result;
            };

            olvColumnLanguagesScene.AspectToStringConverter = delegate (object x) {
                string result = "";
                foreach (string language in (List<string>)x)
                {
                    result += language + ", ";
                }
                result = result.Remove(result.Length - 2);
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


            //this.olvColumnIsTrimmed.AspectToStringConverter = delegate (object cellValue) { return (cellValue == "true") ? "No" : "Yes"; };

        }

        public void UpdateSceneReleasesList()
        {
            SceneReleasesList = Util.LoadSceneXMLToFileDataDictionary(Util.XML_NSWDB);
            OLVSceneList.SetObjects(SceneReleasesList.Values);
            SceneReleasesSelectedItems = new Dictionary<string, FileData>();
            SumarizeLocalGamesList("scene");
        }

        public void UpdateSDCardList()
        {
            SDCardList = Util.GetFileDataCollection(cbxRemoveableDrives.SelectedItem.ToString());
            OLV_SDCard.SetObjects(SDCardList.Values);
            SDCardListSelectedItems = new Dictionary<string, FileData>();
            SumarizeLocalGamesList("sdcard");
/*
            BeginInvoke((MethodInvoker)delegate {
                SDCardList = Util.GetFileDataCollection(cbxRemoveableDrives.SelectedItem.ToString());
                OLV_SDCard.SetObjects(SDCardList.Values);
                SDCardListSelectedItems = new Dictionary<string, FileData>();
                SumarizeLocalGamesList("sdcard");
            });
*/
        }

        public void UpdateLocalGamesList()
        {
            LocalFilesList = Util.LoadXMLToFileDataDictionary(Util.XML_Local);
            OLVLocalFiles.SetObjects(LocalFilesList.Values);
            LocalFilesListSelectedItems = new Dictionary<string, FileData>();
            SumarizeLocalGamesList("local");
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
                    foreach (KeyValuePair<string, FileData> entry in LocalFilesList)
                    {
                        FileData data = entry.Value;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                        count++;
                    }
                    break;
                case ("sdcard"):
                    foreach (KeyValuePair<string, FileData> entry in SDCardList)
                    {
                        FileData data = entry.Value;
                        size += Convert.ToInt64(data.ROMSizeBytes);
                        count++;
                    }
                    break;
                case ("scene"):
                    foreach (KeyValuePair<string, FileData> entry in SceneReleasesList)
                    {
                        FileData data = entry.Value;
                        size += Convert.ToInt64(data.ROMSizeBytes);
                        count++;
                    }
                    break;
            }

            toolStripStatusLabel2.Text = Convert.ToString(count) + " Total (" + Util.BytesToGB(size) + ")";
        }

        private void DisplayGameInformation(string TitleID, Dictionary<string, FileData> list, string sourceList) //Possible values for sourceList ("local", "sdcard", "scene")
        {
            FileData data = Util.GetFileData(TitleID, list);

            if (sourceList == "local" || sourceList == "sdcard")
            {
                if (data != null && File.Exists(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value))
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
                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + data.Region_Icon.First().Value))
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
        }

        public void ClearGameInformation()
        {
            PB_GameIcon.BackgroundImage = null;
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
                            OLVLocalFiles.BuildGroups(olvColumn2, SortOrder.Ascending);
                            break;
                        case "developer":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumn10, SortOrder.Ascending);
                            break;
                        case "trimmed":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnIsTrimmed, SortOrder.Ascending);
                            break;
                        case "cartsize":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumn6, SortOrder.Ascending);
                            break;
                        case "type":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumnCardTypeLocal, SortOrder.Ascending);
                            break;
                        case "masterkeyrevision":
                            OLVLocalFiles.ShowGroups = true;
                            OLVLocalFiles.BuildGroups(olvColumn12, SortOrder.Ascending);
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

        private Dictionary<string, FileData> DiffLists(Dictionary<string, FileData> list1, Dictionary<string, FileData> list2)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            foreach (FileData data in list1.Values)
            {
                FileData dummy;
                if (!list2.TryGetValue(data.TitleID, out dummy))
                {
                    result.Add(data.TitleID, data);
                }
            }

            return result;
        }

        private Dictionary<string, FileData> ContainsLists(Dictionary<string, FileData> list1, Dictionary<string, FileData> list2)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            foreach (FileData data in list1.Values)
            {
                FileData dummy;
                if (list2.TryGetValue(data.TitleID, out dummy))
                {
                    result.Add(data.TitleID, data);
                }
            }

            return result;
        }

        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("It can take some time to add a large library but once added, it will be fast, I promisse :)");

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {                
                string selectedPath = dialog.FileName;
                menuLocalFiles.Enabled = false;
                //OLVLocalFiles.Enabled = false;
                if (!backgroundWorkerAddFilesFromDirectory.IsBusy)
                {
                    toolStripStatusFilesOperation.Text = Properties.Resources.EN_FileOperationScraping;
                    toolStripStatusFilesOperation.Visible = true;
                    toolStripProgressAddingFiles.Visible = true;
                    toolStripStatusLabelGame.Text = "";
                    toolStripStatusLabelGame.Visible = true;
                    toolStripProgressAddingFiles.Value = 0;
                    timer1.Enabled = true;
                    backgroundWorkerAddFilesFromDirectory.RunWorkerAsync(selectedPath);
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

                if (e.IsSelected)
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVLocalFiles.SelectedItems;
                    //string FirstTitleIDSelected = selectedItems[selectedItems.Count-1].Text;
                    //DisplayGameInformation(FirstTitleIDSelected);

                    LocalFilesListSelectedItems.Clear();
                    string titleID = selectedItems[0].Text;

                    int count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        FileData data = Util.GetFileData(titleID, LocalFilesList);
                        LocalFilesListSelectedItems.Add(titleID, data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the first selected item
                    DisplayGameInformation(titleID, LocalFilesList, "local");
                }
                else
                {
                    ListView.SelectedListViewItemCollection selectedItems = OLVLocalFiles.SelectedItems;
                    int count = 0;
                    long size = 0;
                    LocalFilesListSelectedItems.Clear();
                    foreach (ListViewItem item in selectedItems)
                    {
                        string titleID = item.Text;
                        FileData data = Util.GetFileData(titleID, LocalFilesList);
                        LocalFilesListSelectedItems.Add(titleID, data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";

                }
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            OLVLocalFiles.SelectedItems.Clear();
            OLVSceneList.SelectedItems.Clear();

            switch (tabControl1.SelectedIndex)
            {
                case 0: //Files
                    SumarizeLocalGamesList("local");
                    break;
                case 1: //SD Card
                    SumarizeLocalGamesList("sdcard");
                    break;
                case 2: //Scene
                    SumarizeLocalGamesList("scene");
                    break;
                case 3:

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

                    int count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        FileData data = Util.GetFileData(titleID, SceneReleasesList);
                        SceneReleasesSelectedItems.Add(titleID, data);
                        count++;
                        size += Convert.ToInt64(data.ROMSizeBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the first selected item
                    DisplayGameInformation(titleID, LocalFilesList, "scene"); //Has to be Locallist as we dont store scene info other than its xml file...
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
                        FileData data = Util.GetFileData(titleID, SceneReleasesList);
                        SceneReleasesSelectedItems.Add(titleID, data);
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
            Util.AppendFileDataDictionaryToXML(Util.AddFilesFromFolder((string)e.Argument));
        }

        private void backgroundWorkerAddFiles_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Does not work... USing Timer instead
            //toolStripProgressAddingFiles.Value = progressPercent;
        }

        private void backgroundWorkerAddFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Visible = false;
            menuLocalFiles.Enabled = true;
            //OLVLocalFiles.Enabled = true;
            UpdateLocalGamesList();

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

        private void ClearGroupingMenuChecks(string local) //local options: "local", "sdcard", "scene" 
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

                    int count = 0;
                    long size = 0;
                    foreach (ListViewItem item in selectedItems)
                    {
                        titleID = item.Text;
                        FileData data = Util.GetFileData(titleID, SDCardList);
                        string icon_titleID_filename = data.Region_Icon.First().Value;

                        SDCardListSelectedItems.Add(titleID, data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                    //Display information of the first selected item
                    DisplayGameInformation(titleID, SDCardList, "sdcard");
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
                        FileData data = Util.GetFileData(titleID, SDCardList);
                        SDCardListSelectedItems.Add(titleID, data);
                        count++;
                        size += Convert.ToInt64(data.UsedSpaceBytes);
                    }

                    toolStripStatusLabel1.Text = Convert.ToString(count) + " Selected (" + Util.BytesToGB(size) + ")";
                }
            }
        }

        private void itemsNotOnSceneReleasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<string, FileData> list = DiffLists(LocalFilesList, SceneReleasesList);
            FileData dummy;
            OLVLocalFiles.Select();
            OLVLocalFiles.HideSelection = false;
            OLVLocalFiles.SelectedItems.Clear();
            foreach (ListViewItem item in OLVLocalFiles.Items)
            {
                if (list.TryGetValue(item.SubItems[0].Text, out dummy))
                {
                    item.Selected = true;

                }
            }
        }

        private void itemsOnSceneReleasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<string, FileData> list = ContainsLists(SceneReleasesList, LocalFilesList);
            FileData dummy;
            OLVLocalFiles.Select();
            OLVLocalFiles.HideSelection = false;
            OLVLocalFiles.SelectedItems.Clear();
            foreach (ListViewItem item in OLVLocalFiles.Items)
            {
                if (list.TryGetValue(item.SubItems[0].Text, out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void itemsOnSDCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<string, FileData> list = ContainsLists(SDCardList, LocalFilesList);
                FileData dummy;
                OLVLocalFiles.Select();
                OLVLocalFiles.HideSelection = false;
                OLVLocalFiles.SelectedItems.Clear();
                foreach (ListViewItem item in OLVLocalFiles.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = DiffLists(LocalFilesList, SDCardList);
                FileData dummy;
                OLVLocalFiles.Select();
                OLVLocalFiles.HideSelection = false;
                OLVLocalFiles.SelectedItems.Clear();
                foreach (ListViewItem item in OLVLocalFiles.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = ContainsLists(LocalFilesList, SDCardList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = DiffLists(SDCardList, LocalFilesList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = ContainsLists(SceneReleasesList, SDCardList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = DiffLists(SceneReleasesList, SDCardList);
                FileData dummy;
                OLV_SDCard.Select();
                OLV_SDCard.HideSelection = false;
                OLV_SDCard.SelectedItems.Clear();
                foreach (ListViewItem item in OLV_SDCard.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
            Dictionary<string, FileData> list = ContainsLists(LocalFilesList, SceneReleasesList);
            FileData dummy;
            OLVSceneList.Select();
            OLVSceneList.HideSelection = false;
            OLVSceneList.SelectedItems.Clear();
            foreach (ListViewItem item in OLVSceneList.Items)
            {
                if (list.TryGetValue(item.SubItems[0].Text, out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void toolStripMenuItem59_Click(object sender, EventArgs e)
        {
            Dictionary<string, FileData> list = DiffLists(SceneReleasesList, LocalFilesList);
            FileData dummy;
            OLVSceneList.Select();
            OLVSceneList.HideSelection = false;
            OLVSceneList.SelectedItems.Clear();
            foreach (ListViewItem item in OLVSceneList.Items)
            {
                if (list.TryGetValue(item.SubItems[0].Text, out dummy))
                {
                    item.Selected = true;
                }
            }
        }

        private void toolStripMenuItem61_Click(object sender, EventArgs e)
        {
            if (cbxRemoveableDrives.Items.Count > 0 && cbxRemoveableDrives.SelectedIndex > -1)
            {
                Dictionary<string, FileData> list = ContainsLists(SDCardList, SceneReleasesList);
                FileData dummy;
                OLVSceneList.Select();
                OLVSceneList.HideSelection = false;
                OLVSceneList.SelectedItems.Clear();
                foreach (ListViewItem item in OLVSceneList.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
                Dictionary<string, FileData> list = DiffLists(SceneReleasesList, SDCardList);
                FileData dummy;
                OLVSceneList.Select();
                OLVSceneList.HideSelection = false;
                OLVSceneList.SelectedItems.Clear();
                foreach (ListViewItem item in OLVSceneList.Items)
                {
                    if (list.TryGetValue(item.SubItems[0].Text, out dummy))
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
            if (e.ColumnIndex == this.olvColumnIsTrimmed.Index)
            {
                FileData data = (FileData)e.Model;
                if (!data.IsTrimmed)
                    e.SubItem.BackColor = Color.IndianRed;
            }
        }

        private void OLVSceneList_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
        {
            if (e.ColumnIndex == this.columnSceneRegionScene.Index)
            {
                e.SubItem.Text = "";

            }
        }

        private void filesToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XCI Files (*.XCI)|*.xci";
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
                    backgroundWorkerAddFile.RunWorkerAsync(openFileDialog.FileNames);
                }
            }
        }

        private void backgroundWorkerAddFile_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Util.AppendFileDataDictionaryToXML(Util.AddFiles((string[])e.Argument));
        }

        private void backgroundWorkerAddFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Visible = false;
            menuLocalFiles.Enabled = true;
            //OLVLocalFiles.Enabled = true;
            UpdateLocalGamesList();

            //Its so fast that I dont think it needs a message
            //MessageBox.Show("Done");
        }

        private void RemoveSelectedFiles()
        {
            if (LocalFilesListSelectedItems.Count() > 0)
            {
                Util.RemoveFileDataDictionaryFromXML(LocalFilesListSelectedItems);
                LocalFilesListSelectedItems.Clear();
                OLVLocalFiles.SelectedItems.Clear();
                UpdateLocalGamesList();
                //MessageBox.Show("Done");
            }
            else
            {
                MessageBox.Show("No files selected!");
            }
        }

        private void selectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveSelectedFiles();
        }

        private void objectListView1_KeyDown(object sender, KeyEventArgs e)
        {            
            if (e.KeyCode == System.Windows.Forms.Keys.Delete)
            {
                RemoveSelectedFiles();
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

            Dictionary<string, FileData> filesList = (Dictionary<string, FileData>)parameters[0];
            string destinyPath = (string)parameters[1];
            string operation = (string)parameters[2];

            Util.CopyFilesOnDictionaryToFolder(filesList, destinyPath, operation);
        }

        private void backgroundWorkerCopyFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Visible = false;
            menuLocalFiles.Enabled = true;
            menuSDFiles.Enabled = true;

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
                Util.RemoveFileDataDictionaryFromXML(LocalFilesListSelectedItems);
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
            timer1.Enabled = false;
            toolStripStatusFilesOperation.Visible = false;
            toolStripProgressAddingFiles.Visible = false;
            toolStripStatusLabelGame.Visible = false;
            menuSDFiles.Enabled = true;
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
            Util.RemoveMissingFilesFromXML(Util.XML_Local);
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
            Util.RemoveMissingFilesFromXML(Util.XML_Local);
            UpdateLocalGamesList();
            MessageBox.Show("Done.");
        }

        private void updateNswdbcomListToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Util.UpdateNSWDB();
            Util.XML_NSWDB = XDocument.Load(@Util.NSWDB_FILE);
            UpdateSceneReleasesList();
            MessageBox.Show("Done.");
        }

        private void TrimSelectedFiles(Dictionary<string, FileData> dictionary, string source) //source possible values: "local", "sdcard"
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
                        totalBytesSelectedFiles += data.UsedSpaceBytes;
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
                    //OLVLocalFiles.Enabled = false;
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
                        totalBytesSelectedFiles += data.UsedSpaceBytes;
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

        private void RenameSelectedFiles(Dictionary<string, FileData> localFilesListSelectedItems, string source) //source possible values: "local", "sdcard"
        {
            Util.AutoRenameXCIFiles(localFilesListSelectedItems, source);
        }

        private void selectedFilesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OperationRenameSelectedLocalFiles();
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
            if (SDCardListSelectedItems.Count == 0)
            {
                e.Cancel = true;
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
            MessageBox.Show(Properties.Resources.EN_Soon);
        }
    }
}
