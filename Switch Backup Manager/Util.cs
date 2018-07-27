using Switch_Backup_Manager.XTSSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Switch_Backup_Manager
{
    internal static class Util
    {
        public const string VERSION = "1.0.8";   //Actual application version
        public const string MIN_DB_Version = "1.0.8"; //This is the minimum version of the DB that can work

        public const string INI_FILE = "sbm.ini";
        public static string TITLE_KEYS = "titlekeys.txt";
        public static string KEYS_FILE = "keys.txt";
        public const string KEYS_DOWNLOAD_SITE = "https://pastebin.com/raw/ekSH9R8t";
        public const string HACTOOL_FILE = "hactool.exe";
        public const string HACTOOL_DOWNLOAD_SITE = "https://github.com/SciresM/hactool/releases/download/1.1.0/hactool-1.1.0.win.zip";
        public const string NSWDB_FILE = "nswdb.xml";
        public const string NSWDB_DOWNLOAD_SITE = "http://nswdb.com/xml.php";
        public const string LOCAL_FILES_DB = "SBM_Local.xml";
        public const string LOCAL_NSP_FILES_DB = "SBM_NSP_Local.xml";
        public const string CACHE_FOLDER = "cache";
        public const string LOG_FILE = "sbm.log";

        public static byte[] NcaHeaderEncryptionKey1_Prod;
        public static byte[] NcaHeaderEncryptionKey2_Prod;
        public static string Mkey;
        public static Logger logger;
        public static string log_Level = "debug";
        public static string autoRenamingPattern = "{gamename}";

        public static bool AutoUpdateNSDBOnStartup = false;
        public static bool UseTitleKeys = false;
        public static bool ScrapXCIOnSDCard = true;
        public static bool ScrapNSPOnSDCard = true;
        public static bool ScrapInstalledEshopSDCard = true;

        private static string[] Language = new string[16]
        {
            "American English",
            "British English",
            "Japanese",
            "French",
            "German",
            "Latin American Spanish",
            "Spanish",
            "Italian",
            "Dutch",
            "Canadian French",
            "Portuguese",
            "Russian",
            "Korean",
            "Taiwanese", //This is Taiwanese but their titles comes in Traditional Chinese (http://blipretro.com/notes-on-the-taiwanese-nintendo-switch/)
            "Traditional Chinese",
            "???"
        };

        public static string[] AutoRenamingTags = new string[10]
        {
            "{gamename}",
            "{titleid}",
            "{developer}",
            "{trimmed}",
            "{revision}",
            "{releasegroup}",
            "{region}",
            "{firmware}",
            "{languages}",
            "{sceneid}"
        };

        private static Image[] Icons = new Image[16];

        public static IniFile ini;
        public static XDocument XML_Local;
        public static XDocument XML_NSWDB;
        public static XDocument XML_NSP_Local;

        private static List<string> ListDirectoriesToUpdate()
        {
            List<string> list = new List<string>();
            for (int i = 0; i <= 5; i++)
            {
                string value = ini.IniReadValue("AutoScan", "Folder_0" + (i + 1));
                if (value.Trim() != "")
                {
                    int ind = value.IndexOf("?");
                    if (value.Substring(ind + 1, 1) == "1")
                    {
                        list.Add(value.Substring(0, ind));
                    }
                }
            }

            return list;
        }

        private static int UpdateDirectory(string dir)
        {
            int added_files = 0;
            XDocument xml_local = XDocument.Load(LOCAL_FILES_DB);
            XDocument xml__nsp = XDocument.Load(LOCAL_FILES_DB);

            //XCI Files (Includding splitted files)
            List<string> files_xci = GetXCIsInFolder(dir);
            List<string> files_nsp = GetNSPsInFolder(dir);
            int filesCount = files_xci.Count() + files_nsp.Count();

            int i = 0;
            foreach (string file in files_xci) //XCI Files
            {
                FrmMain.progressCurrentfile = file;

                bool found = false;
                foreach (XElement xe in xml_local.Descendants("Game"))
                {
                    if (xe.Element("FilePath").Value == file) //File is already on XML. Go to next one.
                    {
                        found = true;
                        break;
                    }
                }
                i++;

                if (!found) //File is not on XML. Add it.
                {
                    FileData data = GetFileData(file);
                    if (WriteFileDataToXML(data, LOCAL_FILES_DB))
                    {
                        added_files++;
                    }                    
                }
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            foreach (string file in files_nsp) //NSP Files
            {
                FrmMain.progressCurrentfile = file;

                bool found = false;
                foreach (XElement xe in XML_NSP_Local.Descendants("Game"))
                {
                    if (xe.Element("FilePath").Value == file) //File is already on XML. Go to next one.
                    {
                        found = true;
                        break;
                    }
                }
                i++;

                if (!found) //File is not on XML. Add it.
                {
                    FileData data = GetFileDataNSP(file);
                    if (WriteFileDataToXML(data, LOCAL_NSP_FILES_DB))
                    {
                        added_files++;
                    }
                }
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return added_files;
        }

        public static void UpdateDirectories()
        {
            RemoveMissingFilesFromXML(XML_Local, LOCAL_FILES_DB);
            RemoveMissingFilesFromXML(XML_NSP_Local, LOCAL_NSP_FILES_DB);

            foreach (string dir in ListDirectoriesToUpdate())
            {
                logger.Info("Searchng for new files on " + dir);
                int added_files = UpdateDirectory(dir);
                logger.Info("Finished search for new files on " + dir + ". " + added_files + " files added.");
            }
        }

        internal static void UpdateFilesInfo(Dictionary<string, FileData> filesList, string source)
        {
            throw new NotImplementedException();
        }

        public static string GetRenamingString(FileData data, string pattern) {
            string result ="";

            if (data != null)
            {
                result = pattern;
                result = result.Replace(AutoRenamingTags[0], data.GameName);
                result = result.Replace(AutoRenamingTags[1], data.TitleID);
                result = result.Replace(AutoRenamingTags[2], data.Developer);
                result = result.Replace(AutoRenamingTags[3], (data.IsTrimmed ? "Trimmed" : "Full ROM"));
                result = result.Replace(AutoRenamingTags[4], data.GameRevision);
                result = result.Replace(AutoRenamingTags[5], data.Group);
                result = result.Replace(AutoRenamingTags[6], data.Region);
                result = result.Replace(AutoRenamingTags[7], data.Firmware);
                result = result.Replace(AutoRenamingTags[8], data.Languages_resumed);
                result = result.Replace(AutoRenamingTags[9], Convert.ToString(data.IdScene));

                result += Path.GetExtension(data.FilePath);
            }

            return result;
        }

        public static bool TrimXCIFile(FileData file)
        {
            bool result = false;

            if (file != null)
            {
                if (!file.IsTrimmed)
                {
                    logger.Info("Trimming file "+file.FileNameWithExt+". Old size: "+Convert.ToString(file.ROMSizeBytes)+". New size: "+Convert.ToString(file.UsedSpaceBytes));
                    try
                    {
                        FileStream fileStream = new FileStream(@file.FilePath, FileMode.Open, FileAccess.Write);
                        fileStream.SetLength(file.UsedSpaceBytes);
                        fileStream.Close();
                    } catch (Exception e)
                    {
                        logger.Error("Error trimming file " + file.FilePath + "\n" + e.StackTrace);
                        return false;
                    }

                    file.ROMSizeBytes = file.UsedSpaceBytes;
                    file.ROMSize = file.UsedSpace;
                    file.IsTrimmed = true;
                    result = true;
                } else
                {
                    logger.Info("File was already trimmed");
                }
            }
            return result;
        }

        public static void TrimXCIFiles(Dictionary<string, FileData> files, string source) //source possible values: "local", "sdcard"
        {
            int filesCount = files.Count();
            int i = 0;
            logger.Info("Starting trimming " + source + " files.");

            if (source == "local")
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    if (TrimXCIFile(entry.Value))
                    {
                        UpdateXMLFromFileData(entry.Value, source);
                    }

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
                XML_Local.Save(@LOCAL_FILES_DB);
            } else
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    TrimXCIFile(entry.Value);

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
            }
            logger.Info("Finished trimming " + source + " files.");
        }

        public static void AutoRenameXCIFiles(Dictionary<string, FileData> files, string source) //source possible values: "local", "sdcard", "eshop"
        {
            int filesCount = files.Count();
            int i = 0;
            logger.Info("Starting autorename " + source + " files.");

            if (source != "sdcard")
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    if (AutoRenameXCIFile(entry.Value))
                    {
                        UpdateXMLFromFileData(entry.Value, source);
                    }

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
                if (source == "local")
                {
                    XML_Local.Save(@LOCAL_FILES_DB);
                } else if (source == "eshop")
                {
                    XML_NSP_Local.Save(LOCAL_NSP_FILES_DB);
                }                
            }
            else
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    AutoRenameXCIFile(entry.Value);

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
            }
            logger.Info("Finished autorename " + source + " files.");
        }

        private static bool AutoRenameXCIFile(FileData file)
        {
            bool result = false;

            if (file != null)
            {
                string extension = Path.GetExtension(file.FilePath);
                Regex illegalInFileName = new Regex(@"[\\/:*?""<>|™®]");
                string newFileName = Path.GetDirectoryName(file.FilePath) + "\\" + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), "");
                string newFileName_ = "";
                if (File.Exists(newFileName))
                {
                    logger.Warning("File " + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), "") + " already exists at destination path. Ignoring this file!");
                    return false;
                }
                else
                {
                    switch (extension.ToLower())
                    {
                        case ".xci":
                            logger.Info("Old name: " + file.FileNameWithExt + ". New name: " + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), ""));
                            try
                            {
                                System.IO.File.Move(file.FilePath, newFileName);
                            }
                            catch (Exception e)
                            {
                                logger.Error("Failed to rename file.\n" + e.StackTrace);
                                return false;
                            }
                            break;
                        case ".nsp":
                            logger.Info("Old name: " + file.FileNameWithExt + ". New name: " + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), ""));
                            try
                            {
                                System.IO.File.Move(file.FilePath, newFileName);
                            }
                            catch (Exception e)
                            {
                                logger.Error("Failed to rename file.\n" + e.StackTrace);
                                return false;
                            }                            
                            break;
                        default: //(.xc0, xc1, etc)
                            List<string> splited_files = GetSplitedXCIsFiles(file.FilePath);
                            newFileName_ = newFileName;

                            foreach (string splited_file in splited_files)
                            {
                                string extension_ = Path.GetExtension(splited_file);
                                logger.Info("Old name: " + Path.GetFileName(splited_file) + ". New name: " + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), "").Replace(extension, "") + extension_);
                                newFileName = Path.GetDirectoryName(file.FilePath) + "\\" + illegalInFileName.Replace(GetRenamingString(file, autoRenamingPattern), "").Replace(extension, "") + extension_;
                                try
                                {
                                    System.IO.File.Move(splited_file, newFileName);
                                }
                                catch (Exception e)
                                {
                                    logger.Error("Failed to rename file.\n" + e.StackTrace);
                                }
                            }
                            newFileName = newFileName_;
                            break;
                    }
                }

                file.FileName = Path.GetFileNameWithoutExtension(newFileName);
                file.FileNameWithExt = Path.GetFileName(newFileName);
                file.FilePath = newFileName;

                result = true;
            }
            return result;
        }

        public static void UpdateXMLFromFileData(FileData file, string source)
        {
            XElement element = null;
            if (source == "local")
            {
                element = XML_Local.Descendants("Game")
                    .FirstOrDefault(el => (string)el.Attribute("TitleID") == file.TitleID);
            } else if (source == "eshop")
            {
                element = XML_NSP_Local.Descendants("Game")
                    .FirstOrDefault(el => (string)el.Attribute("TitleID") == file.TitleID);
            }

            if (element != null)
            {
                element.Element("FilePath").Value = file.FilePath;
                element.Element("FileName").Value = file.FileName;
                element.Element("FileNameWithExt").Value = file.FileNameWithExt;
                element.Element("ROMSize").Value = file.ROMSize;
                element.Element("TitleIDBaseGame").Value = file.TitleIDBaseGame;
                element.Element("ROMSizeBytes").Value = Convert.ToString(file.ROMSizeBytes);
                element.Element("UsedSpace").Value = file.UsedSpace;
                element.Element("UsedSpaceBytes").Value = Convert.ToString(file.UsedSpaceBytes);
                element.Element("GameName").Value = file.GameName;
                element.Element("Developer").Value = file.Developer;
                element.Element("GameRevision").Value = file.GameRevision;
                element.Element("ProductCode").Value = file.ProductCode;
                element.Element("SDKVersion").Value = file.SDKVersion;
                element.Element("CartSize").Value = file.CartSize;
                element.Element("CardType").Value = file.Cardtype;
                element.Element("MasterKeyRevision").Value = file.MasterKeyRevision;
                element.Element("IsTrimmed").Value = Convert.ToString(file.IsTrimmed).ToLower();
                element.Element("Group").Value = file.Group;
                element.Element("Serial").Value = file.Serial;
                element.Element("Firmware").Value = file.Firmware;
                element.Element("Region").Value = file.Region;
                element.Element("Languages_resumed").Value = file.Languages_resumed;
                element.Element("Distribution_Type").Value = file.DistributionType;
                element.Element("ID_Scene").Value = (file.IdScene > 0 ? Convert.ToString(file.IdScene) : ""); ;
                element.Element("Distribution_Type").Value = file.DistributionType;
                element.Element("Content_Type").Value = file.ContentType;
            }
        }

        public static void RemoveMissingFilesFromXML(XDocument xml, string source_xml)
        {
            XDocument xml_ = XDocument.Load(@source_xml);

            string removeFrom = (source_xml == LOCAL_FILES_DB ? "local" : "e-shop");
            logger.Info("Start removing missing files from "+ removeFrom + " database");

            int i = 0;
            foreach (XElement xe in xml_.Descendants("Game"))
            {
                if (!File.Exists(xe.Element("FilePath").Value))
                {
                    RemoveTitleIDFromXML(xe.Attribute("TitleID").Value, @source_xml);
                    logger.Info(xe.Element("FilePath").Value + " removed.");
                    i++;
                }                
            }

            if (source_xml == LOCAL_FILES_DB)
            {
                XML_Local.Save(@source_xml);
            } else
            {
                XML_NSP_Local.Save(@source_xml);
            }
            
            logger.Info("Finished removing missing files from "+ removeFrom + " database. " + i + " files removed.");
        }

        public static bool IsTitleIDOnXML(string titleID, string xml)
        {
            bool result = false;
            XElement element;

            if (xml == LOCAL_FILES_DB)
            {
                element = XML_Local.Descendants("Game")
                   .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);
            } else
            {
                element = XML_NSP_Local.Descendants("Game")
                   .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);
            }

            if (element != null)
            {
                result = true;
            }

            return result;
        }

        private static void GetExtraInfoFromScene(FileData data)
        {
            XElement element = XML_NSWDB.Descendants("release")
                .FirstOrDefault(el => (string)el.Element("titleid") == data.TitleID);

            if (element != null)
            {
                //Try to get game name from scene releases as value retrieved from .XCI could use foreign language (Chinese!) and it may not be recognized by switch
                if (element.Element("name") != null && element.Element("name").Value.Trim() != "")
                {
                    data.GameName = element.Element("name").Value;
                }         
                if (element.Element("card") != null)
                {
                    data.Cardtype = element.Element("card").Value;
                }
                if (element.Element("group") != null)
                {
                    data.Group = element.Element("group").Value;
                }
                if (element.Element("serial") != null)
                {
                    data.Serial = element.Element("serial").Value;
                }
                if (element.Element("firmware") != null)
                {
                    data.Firmware = element.Element("firmware").Value;
                }
                if (element.Element("region") != null)
                {
                    data.Region = element.Element("region").Value;
                }
                if (element.Element("languages") != null)
                {
                    data.Languages_resumed = element.Element("languages").Value;
                }                 
                if (element.Element("id") != null)
                {
                    data.IdScene = element.Element("id").Value == "" ? 0 : Convert.ToInt32(element.Element("id").Value);
                }
            } else
            {
                if (data.DistributionType == "Download")
                {
                    data.Cardtype = "e-shop";
                }
            }
        }

        public static bool WriteFileDataToXML(FileData data, string xml)
        {
            bool result = false;

            try
            {
                if (data != null)
                {
                    logger.Debug("searching for " + data.TitleID + " on database.");
                    //Try to find the game. If exists, do nothing. If not, Append
                    if (!IsTitleIDOnXML(data.TitleID, xml))
                    {
                        logger.Debug(data.TitleID + " not found on database. Adding...");
                        string languages = "";
                        if (data.Languages != null)
                        {
                            foreach (string language in data.Languages)
                            {
                                languages += language + ",";
                            }
                            if (languages.Trim().Length > 1)
                            {
                                try
                                {
                                    languages = languages.Remove(languages.Length - 1);
                                } catch (Exception e)
                                {
                                    logger.Debug("Exception on languages.Remove for Title ID " + data.TitleID);
                                    languages = "";
                                }                                
                            }
                        } else
                        {
                            logger.Debug("data.Languages was null for Title ID " + data.TitleID);
                        }

                        XElement element = new XElement("Game", new XAttribute("TitleID", data.TitleID),
                                   new XElement("TitleIDBaseGame", data.TitleIDBaseGame),
                                   new XElement("FilePath", data.FilePath),
                                   new XElement("FileName", data.FileName),
                                   new XElement("FileNameWithExt", data.FileNameWithExt),
                                   new XElement("ROMSize", data.ROMSize),
                                   new XElement("ROMSizeBytes", data.ROMSizeBytes),
                                   new XElement("UsedSpace", data.UsedSpace),
                                   new XElement("UsedSpaceBytes", data.UsedSpaceBytes),
                                   new XElement("GameName", data.GameName),
                                   new XElement("Developer", data.Developer),
                                   new XElement("GameRevision", data.GameRevision),
                                   new XElement("ProductCode", data.ProductCode),
                                   new XElement("SDKVersion", data.SDKVersion),
                                   new XElement("CartSize", data.CartSize),
                                   new XElement("CardType", data.Cardtype),
                                   new XElement("MasterKeyRevision", data.MasterKeyRevision),
                                   new XElement("Region_Icon", data.Region_Icon),
                                   new XElement("Languages", languages),
                                   new XElement("IsTrimmed", data.IsTrimmed),
                                   new XElement("Group", data.Group),
                                   new XElement("Serial", data.Serial),
                                   new XElement("Firmware", data.Firmware),
                                   new XElement("Region", data.Region),
                                   new XElement("Languages_resumed", data.Languages_resumed),
                                   new XElement("Distribution_Type", data.DistributionType),
                                   new XElement("ID_Scene", data.IdScene),
                                   new XElement("Content_Type", data.ContentType),
                                   new XElement("Version", data.Version)
                           );
                        if (xml == LOCAL_FILES_DB)
                        {
                            logger.Debug("Adding element...");
                            XML_Local.Root.Add(element);
                            logger.Debug("Saving xml "+@xml);
                            XML_Local.Save(@xml);
                            logger.Debug("xml saved...");
                        }
                        else
                        {
                            logger.Debug("Adding element...");
                            XML_NSP_Local.Root.Add(element);
                            logger.Debug("Saving xml " + @xml);
                            XML_NSP_Local.Save(@xml);
                            logger.Debug("xml saved...");
                        }
                        result = true;
                    }
                    else
                    {
                        logger.Info(data.TitleID + " was already on database. Ignoring.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Problem writing Title ID " + data.TitleID + " on xml");
                logger.Error(e.Message + "\n" + e.StackTrace);
            }
            return result;
        }

        /// <summary>
        /// Creates a Dictionary <string, FileData> from a given XDocument. Works for local files xml </string>
        /// </summary>
        /// <param name="xml">XDocument object</param>
        /// <returns></returns>
        public static Dictionary<string, FileData> LoadXMLToFileDataDictionary(XDocument xml)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();
            foreach (XElement xe in xml.Descendants("Game"))
            {
                result.Add(xe.Attribute("TitleID").Value, GetFileData(xe));
            }
            return result;
        }

        public static Dictionary<string, FileData> LoadSceneXMLToFileDataDictionary(XDocument xml)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            foreach (XElement xe in xml.Descendants("release"))
            {
                try
                {
                    result.Add(xe.Element("titleid").Value, GetFileData(xe, true));
                } catch { System.ArgumentException ex; }
                {
                    //If TitleID is already on the list, ignore
                }                
            }

            return result;
        }

        private static bool checkDBVersion(string xml)
        {
            bool result = false;
            int ver_db  = 0;
            int ver_min = Convert.ToInt32(VERSION.Replace(".", "")); //Ex 1.0.8 -> 108

            //Check if DB is on minimum version
            XDocument xml_temp = XDocument.Load(xml);

            void saveXML() //Local Function to avoid code replication.
            {
                if (MessageBox.Show("Your "+ xml + " is outdated and needs to be created again. \nDo you want to make a backup?", "Switch Backup Manager", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        File.Copy(xml, xml + ".old", true);
                    } catch (Exception e)
                    {
                        logger.Error("Could not backup " + xml + "\n" + e.StackTrace);
                    }
                    
                }
                xml_temp = new XDocument(new XComment("List games"),
                new XElement("Games", new XAttribute("Date", DateTime.Now.ToString()), new XAttribute("Version", VERSION)));
                xml_temp.Declaration = new XDeclaration("1.0", "utf-8", "true");
                xml_temp.Save(xml);
            }

            if (xml_temp.Element("Games").Attribute("Version") != null)
            {
                ver_db = Convert.ToInt32(xml_temp.Element("Games").Attribute("Version").Value.Replace(".", ""));
                if (ver_db < ver_min)
                {
                    saveXML();
                }
            }
            else
            { //If version tag not found on XML, means its too old. Delete!
                saveXML();
            }

            return result;
        }

        public static void LoadSettings(ref RichTextBox outputLogBox)
        {
            ini = new IniFile((AppDomain.CurrentDomain.BaseDirectory) + INI_FILE);
            logger = new Logger(ref outputLogBox);

            string keys_file = ini.IniReadValue("Config", "keys_file");
            string title_keys = ini.IniReadValue("Config", "title_keys");
            if (keys_file.Trim() == "")
            {
                keys_file = KEYS_FILE;
                ini.IniWriteValue("Config", "keys_file", keys_file);
            } else
            {
                KEYS_FILE = keys_file;
            }

            if (title_keys.Trim() == "")
            {
                title_keys = TITLE_KEYS;
                ini.IniWriteValue("Config", "title_keys", title_keys);
            }
            else
            {
                TITLE_KEYS = title_keys;
            }

            log_Level = ini.IniReadValue("Log", "log_level");
            if (log_Level.Trim() == "")
            {
                ini.IniWriteValue("Log", "log_level", "info");
                log_Level = "info";
            }
            log_Level = "debug"; //Force debug on log, for now...

            autoRenamingPattern = ini.IniReadValue("AutoRenaming", "pattern");
            if (autoRenamingPattern.Trim() == "")
            {
                ini.IniWriteValue("AutoRenaming", "pattern", "{gamename}");
                autoRenamingPattern = "{gamename}";
            }

            //Searches for keys.txt
            if (!File.Exists(keys_file))
            {
                if (MessageBox.Show("keys.txt is missing.\nDo you want to automatically download it now?", "Switch Backup Manager", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(KEYS_DOWNLOAD_SITE, KEYS_FILE);
                    }
                }
                if (!File.Exists(KEYS_FILE))
                {
                    MessageBox.Show(KEYS_FILE +" failed to load.\nPlease include "+ KEYS_FILE + " in this location.");
                    Environment.Exit(0);
                }
            }

            //Searches for titlekeys.txt
            UseTitleKeys = (ini.IniReadValue("Config", "useTitleKeys") == "true" ? true : false);
            if (UseTitleKeys && !File.Exists(title_keys))
            {
                MessageBox.Show(TITLE_KEYS + " not found!\nTo correctly name DLC e-shop files you need to provide a file called " + TITLE_KEYS + " with following format inside: " +
                    "TitleID|TitleKey|Name.\nIf not provided, game name and other info for DLC will be empty.");
            }

            //TODO: Download hactool.zip and extract files
            //Searches for hactool.exe. 
            if (!File.Exists(HACTOOL_FILE))
            {
                MessageBox.Show(HACTOOL_FILE+" is missing. Please, download it at\n"+HACTOOL_DOWNLOAD_SITE);
                Environment.Exit(0);
            }

            //Searches for db.xml
            if (!File.Exists(NSWDB_FILE))
            {
                UpdateNSWDB();
            } else
            {
                string autoUpdateNSWDB_File = ini.IniReadValue("Config", "autoUpdateNSWDB").Trim().ToLower();
                if (autoUpdateNSWDB_File != "")
                {
                    if (autoUpdateNSWDB_File == "true")
                    {
                        AutoUpdateNSDBOnStartup = (autoUpdateNSWDB_File == "true");
                        UpdateNSWDB();
                    }
                } else
                {
                    ini.IniWriteValue("Config", "autoUpdateNSWDB", "false");
                }
            }

            string scrapXCI = ini.IniReadValue("SD", "scrapXCI").Trim().ToLower();
            string scrapNSP = ini.IniReadValue("SD", "scrapNSP").Trim().ToLower();
            string scrapInstalledNSP = ini.IniReadValue("SD", "scrapInstalledNSP").Trim().ToLower();
            if (scrapXCI != "") { ScrapXCIOnSDCard = (scrapXCI == "true"); } else { ini.IniWriteValue("SD", "scrapXCI", "true"); };
            if (scrapNSP != "") { ScrapNSPOnSDCard = (scrapNSP == "true"); } else { ini.IniWriteValue("SD", "scrapNSP", "true"); };
            if (scrapInstalledNSP != "") { ScrapInstalledEshopSDCard = (scrapInstalledNSP == "true"); } else { ini.IniWriteValue("SD", "scrapInstalledNSP", "false"); };

            XML_NSWDB = XDocument.Load(@NSWDB_FILE);

            //Searches for local dabases (xml) and loads it
            if (!File.Exists(LOCAL_FILES_DB))
            {
                XML_Local = new XDocument(new XComment("List games"),
                    new XElement("Games", new XAttribute("Date", DateTime.Now.ToString()), new XAttribute("Version", VERSION)));
                XML_Local.Declaration = new XDeclaration("1.0", "utf-8", "true");
                XML_Local.Save(@LOCAL_FILES_DB);
            } else
            {
                checkDBVersion(@LOCAL_FILES_DB);               
                XML_Local = XDocument.Load(@LOCAL_FILES_DB);
            }

            //Searches for local NSP dabases (xml) and loads it
            if (!File.Exists(LOCAL_NSP_FILES_DB))
            {
                XML_NSP_Local = new XDocument(new XComment("List games"),
                    new XElement("Games", new XAttribute("Date", DateTime.Now.ToString()), new XAttribute("Version", VERSION)));
                XML_NSP_Local.Declaration = new XDeclaration("1.0", "utf-8", "true");
                XML_NSP_Local.Save(@LOCAL_NSP_FILES_DB);
            }
            else
            {
                checkDBVersion(@LOCAL_NSP_FILES_DB);
                XML_NSP_Local = XDocument.Load(@LOCAL_NSP_FILES_DB);
            }

            //Create cache directory
            if (!Directory.Exists(CACHE_FOLDER))
            {
                Directory.CreateDirectory(CACHE_FOLDER);
            }
            GetKeys();
        }

        public static void UpdateNSWDB()
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(@NSWDB_DOWNLOAD_SITE, NSWDB_FILE);
                } catch (WebException ex)
                {
                    MessageBox.Show("Could not download NSWDB File from nswdb.com! \n Please check your internet connection.");
                }
                
            }
        }

        public static void GetKeys()
        {
            string text = (from x in File.ReadAllLines(KEYS_FILE)
                           select x.Split('=') into x
                           where x.Length > 1
                           select x).ToDictionary((string[] x) => x[0].Trim(), (string[] x) => x[1])["header_key"].Replace(" ", "");
            NcaHeaderEncryptionKey1_Prod = StringToByteArray(text.Remove(32, 32));
            NcaHeaderEncryptionKey2_Prod = StringToByteArray(text.Remove(0, 32));

        }

        public static string GetMkey(byte id)
        {
            switch (id)
            {
                case 0:
                case 1:
                    return "MasterKey0 (1.0.0-2.3.0)";
                case 2:
                    return "MasterKey1 (3.0.0)";
                case 3:
                    return "MasterKey2 (3.0.1-3.0.2)";
                case 4:
                    return "MasterKey3 (4.0.0-4.1.0)";
                case 5:
                    return "MasterKey4 (5.0.0+)";
                case 6:
                    return "MasterKey5 (?)";
                case 7:
                    return "MasterKey6 (?)";
                case 8:
                    return "MasterKey7 (?)";
                case 9:
                    return "MasterKey8 (?)";
                case 10:
                    return "MasterKey9 (?)";
                case 11:
                    return "MasterKey10 (?)";
                case 12:
                    return "MasterKey11 (?)";
                case 13:
                    return "MasterKey12 (?)";
                case 14:
                    return "MasterKey13 (?)";
                case 15:
                    return "MasterKey14 (?)";
                case 16:
                    return "MasterKey15 (?)";
                case 17:
                    return "MasterKey16 (?)";
                case 18:
                    return "MasterKey17 (?)";
                case 19:
                    return "MasterKey18 (?)";
                case 20:
                    return "MasterKey19 (?)";
                case 21:
                    return "MasterKey20 (?)";
                case 22:
                    return "MasterKey21 (?)";
                case 23:
                    return "MasterKey22 (?)";
                case 24:
                    return "MasterKey23 (?)";
                case 25:
                    return "MasterKey24 (?)";
                case 26:
                    return "MasterKey25 (?)";
                case 27:
                    return "MasterKey26 (?)";
                case 28:
                    return "MasterKey27 (?)";
                case 29:
                    return "MasterKey28 (?)";
                case 30:
                    return "MasterKey29 (?)";
                case 31:
                    return "MasterKey30 (?)";
                case 32:
                    return "MasterKey31 (?)";
                case 33:
                    return "MasterKey32 (?)";
                default:
                    return "?";
            }
        }

        public static bool getMKey()
        {
            string keysFile = ini.IniReadValue("Config", "keys_file");

            Dictionary<string, string> dictionary = (from x in File.ReadAllLines(keysFile)
                                                     select x.Split('=') into x
                                                     where x.Length > 1
                                                     select x).ToDictionary((string[] x) => x[0].Trim(), (string[] x) => x[1]);
            Mkey = "master_key_";
            if (NCA.NCA_Headers[0].MasterKeyRev == 0 || NCA.NCA_Headers[0].MasterKeyRev == 1)
            {
                Mkey += "00";
            }
            else if (NCA.NCA_Headers[0].MasterKeyRev < 17)
            {
                int num = NCA.NCA_Headers[0].MasterKeyRev - 1;
                Mkey = Mkey + "0" + num.ToString();
            }
            else if (NCA.NCA_Headers[0].MasterKeyRev >= 17)
            {
                int num2 = NCA.NCA_Headers[0].MasterKeyRev - 1;
                Mkey += num2.ToString();
            }
            try
            {
                Mkey = dictionary[Mkey].Replace(" ", "");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Add all XCI files found on given path to a Dictionary of FileData <TitleID, FileData>
        /// </summary>
        /// <param name="path"></param>
        public static Dictionary<string, FileData> AddFilesFromFolder(string path, string fileType)
        {
            Dictionary<string, FileData> dictionary = new Dictionary<string, FileData>();
            try
            {
                if (Directory.Exists(path) && path.Trim() != "")
                {
                    List<string> files;
                    if (fileType == "xci")
                    {
                        files = Util.GetXCIsInFolder(path);
                    } else
                    {
                        files = Util.GetNSPsInFolder(path);
                    }
                    
                    int filesCount = files.Count();
                    int i = 0;
                    if (fileType == "xci")
                    {
                        logger.Info("Adding " + filesCount + " files on local database");
                    } else
                    {
                        logger.Info("Adding " + filesCount + " files on local Eshop database");
                    }
                    
                    Stopwatch sw = Stopwatch.StartNew();

                    foreach (string file in files)
                    {
                        FileData data;
                        if (fileType == "xci")
                        {
                            data = Util.GetFileData(file);
                        } else
                        {
                            data = Util.GetFileDataNSP(file);
                        }

                        logger.Info("Scraping file " + data.FilePath + ", TitleID: " + data.TitleID);
                        FrmMain.progressCurrentfile = data.FilePath;
                        try
                        {
                            dictionary.Add(data.TitleID, data);
                        } catch (ArgumentException ex)
                        {
                            logger.Error("TitleID " + data.TitleID + " is already on database");
                        }
                            
                        i++;
                        FrmMain.progressPercent = (int)(i * 100) / filesCount;
                    }
                    sw.Stop();
                    logger.Info("Finished adding files. Total time was " + sw.Elapsed.ToString("mm\\:ss\\.ff") + ".");
                }
            }
            catch (Exception e)
            {
                logger.Error(e.StackTrace);
            }
            return dictionary;
        }

        /// <summary>
        /// Add all XCI files on a given list to a Dictionary of FileData <TitleID, FileData>
        /// </summary>
        /// <param name="files string[]">List of files to be appended</param>
        /// <param name="file_type string">valid values: xci, nsp</param>
        public static Dictionary<string, FileData> AddFiles(string[] files, string fileType)
        {
            Dictionary<string, FileData> dictionary = new Dictionary<string, FileData>();
            try
            {
                int filesCount = files.Count();
                int i = 0;
                if (fileType == "xci")
                {
                    logger.Info("Adding " + filesCount + " files on local database.");
                } else
                {
                    logger.Info("Adding " + filesCount + " files on local Eshop database.");
                }
                
                Stopwatch sw = Stopwatch.StartNew();
                FrmMain.progressCurrentfile = "";

                foreach (string file in files)
                {
                    FileData data;
                    if (fileType == "xci")
                    {
                        data = Util.GetFileData(file);
                    } else
                    {
                        data = Util.GetFileDataNSP(file);
                    }
                    
                    FrmMain.progressCurrentfile = data.FilePath;
                    try
                    {
                        dictionary.Add(data.TitleID, data);
                    }
                    catch (ArgumentException ex)
                    {
                        logger.Error("TitleID " + data.TitleID + " is already on database.");
                    }

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
                sw.Stop();
                logger.Info("Finished adding files. Total time was " + sw.Elapsed.ToString("mm\\:ss\\.ff") + ".");
            } catch (Exception e)
            {
                logger.Error(e.StackTrace);
            }

            return dictionary;
        }
        
        public static void AppendFileDataDictionaryToXML(Dictionary<string, FileData> dictionary, string xml)
        {
            foreach (KeyValuePair<string, FileData> entry in dictionary)
            {
                WriteFileDataToXML(entry.Value, xml);
            }
        }
        
        public static void AppendFileDataDictionaryToXML(Dictionary<string, FileData> dictionary)
        {
            AppendFileDataDictionaryToXML(dictionary, LOCAL_FILES_DB);
        }

        public static void RemoveFileDataDictionaryFromXML(Dictionary<string, FileData> dictionary, string xml)
        {
            foreach (KeyValuePair<string, FileData> entry in dictionary)
            {
                RemoveTitleIDFromXML(entry.Key, xml);
            }
            if (xml == LOCAL_FILES_DB)
            {
                XML_Local.Save(@xml);
            } else
            {
                XML_NSP_Local.Save(@xml);
            }            
        }

        public static void RemoveTitleIDFromXML(string titleID, string xml)
        {
            if (xml == LOCAL_FILES_DB)
            {
                XElement element = XML_Local.Descendants("Game")
                   .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);

                if (element != null)
                {
                    logger.Info("Removing Title ID " + titleID + " from local database.");
                    element.Remove();
                }
            }
            else
            {
                XElement element = XML_NSP_Local.Descendants("Game")
                   .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);

                if (element != null)
                {
                    logger.Info("Removing Title ID " + titleID + " from local e-shop database.");
                    element.Remove();
                }
            }
        }

        //0: FilesList (Dictionary), 1: DestinyPath (string), 2: Operation("copy","move")
        public static bool CopyFilesOnDictionaryToFolder(Dictionary<string, FileData> dictionary, string destiny, string operation)
        {
            Dictionary<string, FileData> dictionary_ = CloneDictionary(dictionary);
            bool result = true;

            int filesCount = dictionary_.Count();
            int i = 0;
            foreach (FileData data in dictionary_.Values)
            {
                string file_extension = Path.GetExtension(data.FilePath);
                if (operation == "copy")
                {
                    if (file_extension.ToLower() == ".xc0") //Split Files
                    {
                        List<string> list = GetSplitedXCIsFiles(data.FilePath);
                        filesCount += list.Count - 1;
                        foreach (string file_path in list)
                        {
                            FrmMain.progressCurrentfile = file_path;
                            logger.Info("Starting copy of file " + file_path + " to " + destiny + ".");
                            FileSystem.CopyFile(file_path, destiny + Path.GetFileName(file_path), UIOption.AllDialogs);
                            i++;
                        }                        
                    } else
                    {
                        FrmMain.progressCurrentfile = data.FilePath;
                        logger.Info("Starting copy of file " + data.FilePath + " to " + destiny + ".");
                        FileSystem.CopyFile(data.FilePath, destiny + data.FileNameWithExt, UIOption.AllDialogs);
                        i++;
                    }
                } else if (operation == "move")
                {
                    if (file_extension.ToLower() == ".xc0") //Split Files
                    {
                        List<string> list = GetSplitedXCIsFiles(data.FilePath);
                        filesCount += list.Count - 1;
                        foreach (string file_path in list)
                        {
                            FrmMain.progressCurrentfile = file_path;
                            logger.Info("Starting move of file " + file_path + " to " + destiny + ".");
                            FileSystem.MoveFile(file_path, destiny + Path.GetFileName(file_path), UIOption.AllDialogs);
                            i++;
                        }
                    }
                    else
                    {
                        FrmMain.progressCurrentfile = data.FilePath;
                        logger.Info("Starting move of file " + data.FileNameWithExt + " to " + destiny + ".");
                        FileSystem.MoveFile(data.FilePath, destiny + data.FileNameWithExt, UIOption.AllDialogs);
                        i++;
                    }
                }

                //i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return result;
        }

        private static MultiStream GetFileStream(string path)
        {
            MultiStream mStream = new MultiStream();

            if (Path.GetExtension(path).ToLower() == ".xc0") //Split Files
            {
                List<string> split_files = GetSplitedXCIsFiles(path);
                foreach (string filePath in split_files)
                {
                    mStream.AddStream(new FileStream(filePath, FileMode.Open, FileAccess.Read));
                }
            }
            else
            {
                mStream.AddStream(new FileStream(path, FileMode.Open, FileAccess.Read));
            }

            return mStream;
        }

        public static bool CheckXCI(string file)
        {
            MultiStream fileStream = GetFileStream(file);

            byte[] array = new byte[61440];
            byte[] array2 = new byte[16];
            fileStream.Read(array, 0, 61440);
            XCI.XCI_Headers[0] = new XCI.XCI_Header(array);
            if (!XCI.XCI_Headers[0].Magic.Contains("HEAD"))
            {
                return false;
            }
            fileStream.Position = XCI.XCI_Headers[0].HFS0OffsetPartition;
            fileStream.Read(array2, 0, 16);
            HFS0.HFS0_Headers[0] = new HFS0.HFS0_Header(array2);
            fileStream.Close();

            return true;
        }

        public static FileData GetFileDataNSP(string file)
        {
            FileData data = new FileData();

            data.FilePath = file;
            data.FileName = Path.GetFileNameWithoutExtension(file);
            data.FileNameWithExt = Path.GetFileName(file);
            data.IsTrimmed = true;
            data.Cardtype = "e-shop";

            FileInfo fi = new FileInfo(file);
            //Get File Size
            string[] array_fs = new string[5] { "B", "KB", "MB", "GB", "TB" };
            double num_fs = (double)fi.Length;
            int num2_fs = 0;
            data.ROMSizeBytes = (long)num_fs;
            data.UsedSpaceBytes = data.ROMSizeBytes;

            while (num_fs >= 1024.0 && num2_fs < array_fs.Length - 1)
            {
                num2_fs++;
                num_fs /= 1024.0;
            }
            data.ROMSize = $"{num_fs:0.##} {array_fs[num2_fs]}";
            data.UsedSpace = data.ROMSize;

            Process process = new Process();
            logger.Info("Adding NSP file: " + data.FileName);
            try
            {
                //Directory.CreateDirectory("tmp");
                process.StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "hactool.exe",
                    Arguments = "-t pfs0 " + "\""+ file +"\"" + " --outdir=tmp"
                };
                logger.Info("Extracting NSP file.");
                process.Start();
                process.WaitForExit();
                process.Close();

                List<string> listXML = new List<string>();
                if (!Directory.Exists("tmp"))
                {
                    logger.Error("Directory tmp was not created ?!");                    
                }
                try
                {
                    logger.Info("found " + Directory.GetFiles("tmp", "*.xml").First());
                    foreach (string f in Directory.GetFiles("tmp", "*.xml"))
                    {
                        listXML.Add(f);
                        break;
                    }
                } catch (Exception e)
                {
                    logger.Error(e.StackTrace);
                }

                XDocument xml = XDocument.Load(listXML.First());
                data.TitleID = xml.Element("ContentMeta").Element("Id").Value.Remove(1,2).ToUpper();                
                data.ContentType = xml.Element("ContentMeta").Element("Type").Value;
                data.Version = xml.Element("ContentMeta").Element("Version").Value;
                string ncaTarget = "";

                string titleIDBaseGame = data.TitleID;
                if (data.ContentType != "Application")
                {
                    string titleIdBase = data.TitleID.Substring(0, 13);
                    if (data.ContentType == "Patch") //UPDATE
                    {
                        titleIDBaseGame = titleIdBase + "000";
                    } else //DLC
                    {
                        long tmp = long.Parse(titleIdBase, System.Globalization.NumberStyles.HexNumber) - 1;
                        titleIDBaseGame = string.Format("0{0:X8}", tmp) + "000";
                    }
                }
                data.TitleIDBaseGame = titleIDBaseGame;

                if (data.ContentType != "AddOnContent")
                {
                    foreach (XElement xe in xml.Descendants("Content"))
                    {
                        if (xe.Element("Type").Value != "Control")
                        {
                            continue;
                        }

                        ncaTarget = xe.Element("Id").Value + ".nca";
                        break;
                    }
                    process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "hactool.exe",
                        Arguments = "-k keys.txt --romfsdir=tmp tmp/" + ncaTarget
                    };
                    logger.Info("Making some voodoo magic with " + ncaTarget);
                    process.Start();
                    process.WaitForExit();
                    process.Close();
                    byte[] flux = new byte[200];

                    byte[] source = File.ReadAllBytes("tmp\\control.nacp");
                    NACP.NACP_Datas[0] = new NACP.NACP_Data(source.Skip(0x3000).Take(0x1000).ToArray());

                    data.Region_Icon = new Dictionary<string, string>();
                    data.Languages = new List<string>();

                    for (int i = 0; i < NACP.NACP_Strings.Length; i++)
                    {
                        NACP.NACP_Strings[i] = new NACP.NACP_String(source.Skip(i * 0x300).Take(0x300).ToArray());

                        if (NACP.NACP_Strings[i].Check != 0)
                        {
                            //CB_RegionName.Items.Add(Language[i]);
                            string icon_filename = "tmp\\icon_" + Language[i].Replace(" ", "") + ".dat";
                            string icon_titleID_filename = CACHE_FOLDER + "\\icon_" + data.TitleID + "_" + Language[i].Replace(" ", "") + ".bmp";

                            if (File.Exists(icon_filename))
                            {
                                try
                                {
                                    File.Copy(icon_filename, icon_titleID_filename, true);
                                }
                                catch (System.IO.IOException)
                                {
                                    //File in use?
                                }
                                data.Region_Icon.Add(Language[i], icon_titleID_filename);
                                data.Languages.Add(Language[i]);
                            }
                        }
                    }
                    data.GameRevision = NACP.NACP_Datas[0].GameVer.Replace("\0", ""); ;
                    data.ProductCode = NACP.NACP_Datas[0].GameProd.Replace("\0", ""); ;
                    if (data.ProductCode == "")
                    {
                        data.ProductCode = "No Prod. ID";
                    }

                    for (int z = 0; z < NACP.NACP_Strings.Length; z++)
                    {
                        if (NACP.NACP_Strings[z].GameName.Replace("\0", "") != "")
                        {
                            data.GameName = NACP.NACP_Strings[z].GameName.Replace("\0", "");
                            break;
                        }
                    }
                    for (int z = 0; z < NACP.NACP_Strings.Length; z++)
                    {
                        if (NACP.NACP_Strings[z].GameAuthor.Replace("\0", "") != "")
                        {
                            data.Developer = NACP.NACP_Strings[z].GameAuthor.Replace("\0", "");
                            break;
                        }
                    }

                    if (data.ContentType == "Patch")
                    {
                        data.GameName = data.GameName + " [UPD]";
                    }

                    //data.TitleIDBaseGame = data.TitleID;
                }
                else //This is a DLC
                {
                    foreach (XElement xe in xml.Descendants("Content"))
                    {
                        if (xe.Element("Type").Value != "Meta")
                        {
                            continue;
                        }

                        ncaTarget = xe.Element("Id").Value + ".cnmt.nca";
                        break;
                    }
                    /*
                     * We need this information:
                    data.Region_Icon
                    data.Languages
                    data.GameRevision
                    data.ProductCode
                    data.GameName
                    data.Developer
                    Gonna search by TitleID on: NSP Files -> Scene -> Local Files -> TitleKeys (partial info only)
                    */
                    bool found = false;

                    //We need to infer the TitleID of base game
                    //string titleIdBase = data.TitleID.Substring(0, 13);
                    //long tmp = long.Parse(titleIdBase, System.Globalization.NumberStyles.HexNumber) - 1;
                    //titleIdBase = string.Format("0{0:X8}", tmp) + "000";
                    //data.TitleIDBaseGame = titleIdBase;
                    //data.TitleIDBaseGame = titleIDBaseGame;

                    /*
                    foreach (XElement xe in XML_NSP_Local.Descendants("Game"))
                    {
                        if (xe != null)
                        {
                            if (xe.Attribute("TitleID").Value == titleIdBase)
                            {
                                FileData data_tmp = GetFileData(xe);
                                data.Region_Icon = data_tmp.Region_Icon;
                                data.Languages = data_tmp.Languages;
                                data.GameRevision = data_tmp.GameRevision;
                                data.ProductCode = data_tmp.ProductCode;
                                data.GameName = data_tmp.GameName + " [DLC]";
                                data.Developer = data_tmp.Developer;
                                found = true;
                                break;
                            }
                        }                        
                    }
                    */
                    FileData data_tmp = null;
                    FrmMain.LocalNSPFilesList.TryGetValue(data.TitleIDBaseGame, out data_tmp); //Try to find on NSP List
                    if (data_tmp != null)
                    {
                        data.Region_Icon = data_tmp.Region_Icon;
                        data.Languages = data_tmp.Languages;
                        data.GameRevision = data_tmp.GameRevision;
                        data.ProductCode = data_tmp.ProductCode;
                        data.GameName = data_tmp.GameName + " [DLC]";
                        data.Developer = data_tmp.Developer;
                        found = true;
                    }

                    if (!found)
                    {
                        data_tmp = null;
                        FrmMain.SceneReleasesList.TryGetValue(data.TitleIDBaseGame, out data_tmp); //Try to find on Scene List
                        if (data_tmp != null)
                        {
                            data.Region_Icon = data_tmp.Region_Icon;
                            data.Languages = data_tmp.Languages;
                            data.GameRevision = data_tmp.GameRevision;
                            data.ProductCode = data_tmp.ProductCode;
                            data.GameName = data_tmp.GameName + " [DLC]";
                            data.Developer = data_tmp.Developer;
                            found = true;
                        }
                        /*
                        foreach (XElement xe in XML_NSWDB.Descendants("release"))
                        {
                            if (xe != null)
                            {
                                if (xe.Element("id").Value == titleIdBase)
                                {
                                    data_tmp = GetFileData(xe, true);
                                    data.Region_Icon = data_tmp.Region_Icon;
                                    data.Languages = data_tmp.Languages;
                                    data.GameRevision = data_tmp.GameRevision;
                                    data.ProductCode = data_tmp.ProductCode;
                                    data.GameName = data_tmp.GameName + " [DLC]";
                                    data.Developer = data_tmp.Developer;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        */
                    }

                    if (!found)
                    {
                        data_tmp = null;
                        FrmMain.LocalFilesList.TryGetValue(data.TitleIDBaseGame, out data_tmp); //Try to find on Local XCI List
                        if (data_tmp != null)
                        {
                            data.Region_Icon = data_tmp.Region_Icon;
                            data.Languages = data_tmp.Languages;
                            data.GameRevision = data_tmp.GameRevision;
                            data.ProductCode = data_tmp.ProductCode;
                            data.GameName = data_tmp.GameName + " [DLC]";
                            data.Developer = data_tmp.Developer;
                            found = true;
                        }
                        /*
                        foreach (XElement xe in XML_Local.Descendants("Game"))
                        {
                            if (xe != null)
                            {
                                if (xe.Attribute("TitleID").Value == titleIdBase)
                                {
                                    data_tmp = GetFileData(xe);
                                    data.Region_Icon = data_tmp.Region_Icon;
                                    data.Languages = data_tmp.Languages;
                                    data.GameRevision = data_tmp.GameRevision;
                                    data.ProductCode = data_tmp.ProductCode;
                                    data.GameName = data_tmp.GameName + " [DLC]";
                                    data.Developer = data_tmp.Developer;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        */
                    }

                    //Last resource, look at titlekeys
                    if (!found)
                    {
                        if (UseTitleKeys && File.Exists(TITLE_KEYS))
                        {
                            string gameName = (from x in File.ReadAllLines(TITLE_KEYS)
                                               select x.Split('|') into x
                                               where x.Length > 1
                                               select x).ToDictionary((string[] x) => x[0].Trim(), (string[] x) => x[2])[data.TitleIDBaseGame].ToLower();
                            
                            if (gameName.Trim() == "") {
                                gameName = (from x in File.ReadAllLines(TITLE_KEYS)
                                            select x.Split('|') into x
                                            where x.Length > 1
                                            select x).ToDictionary((string[] x) => x[0].Trim(), (string[] x) => x[2])[data.TitleID].ToLower();
                            } else
                            {
                                gameName += " [DLC]";
                            }

                            data.GameName = gameName;

                            /*
                            foreach (var line in File.ReadAllLines(TITLE_KEYS))
                            {
                                if (line.Contains(data.TitleID.ToLower()))
                                {

                                    break;
                                }
                            }
                            */
                        }
                    }

                }

                data.GameName = data.GameName + " [v" + data.Version + "]";

                //Lets get SDK Version, Distribution Type and Masterkey revision
                //This is far from the best aproach, but its what we have for now
                process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "hactool.exe",
                    Arguments = "-k keys.txt tmp/" + ncaTarget,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                StreamReader sr = process.StandardOutput;

                while (sr.Peek() >= 0)
                {
                    string str;
                    string[] strArray;
                    str = sr.ReadLine();
                    strArray = str.Split(':');
                    if (strArray[0] == "SDK Version")
                    {
                        data.SDKVersion = strArray[1].Trim();
                    } else if (strArray[0] == "Distribution type")
                    {
                        data.DistributionType = strArray[1].Trim();
                    } else if (strArray[0] == "Master Key Revision")
                    {
                        data.MasterKeyRevision = strArray[1].Trim();
                        break;
                    }
                }
                process.WaitForExit();
                process.Close();
            }
            catch (Exception e)
            {
                logger.Error(e.StackTrace);
            } finally
            {
                Directory.Delete("tmp", true);
            }

            return data;
        }

        public static FileData GetFileData(string filepath)
        {
            FileData result = new FileData();
            //Basic Info
            result.FilePath = filepath;
            result.FileName = Path.GetFileNameWithoutExtension(filepath);
            result.FileNameWithExt = Path.GetFileName(filepath);
            result.DistributionType = "Cartridge";
            result.ContentType = "Application";

            if (CheckXCI(filepath))
            {
                MultiStream fileStream = GetFileStream(filepath);

                //Get File Size
                string[] array_fs = new string[5] { "B", "KB", "MB", "GB", "TB" };
                double num_fs = (double)fileStream.Length;
                int num2_fs = 0;
                result.ROMSizeBytes = (long)num_fs;

                while (num_fs >= 1024.0 && num2_fs < array_fs.Length - 1)
                {
                    num2_fs++;
                    num_fs /= 1024.0;
                }
                result.ROMSize = $"{num_fs:0.##} {array_fs[num2_fs]}";

                double num3_fs = (double)(XCI.XCI_Headers[0].CardSize2 * 512 + 512);
                num2_fs = 0;
                result.UsedSpaceBytes = (long)num3_fs;

                while (num3_fs >= 1024.0 && num2_fs < array_fs.Length - 1)
                {
                    num2_fs++;
                    num3_fs /= 1024.0;
                }
                result.UsedSpace = $"{num3_fs:0.##} {array_fs[num2_fs]}";

                result.IsTrimmed = (result.UsedSpaceBytes == result.ROMSizeBytes);
                result.CartSize = GetCapacity(XCI.XCI_Headers[0].CardSize1);

                //Load Deep File Info (Probably we should clean it a bit more)
                string actualHash;
                byte[] hashBuffer;
                long offset;

                long[] SecureSize = { };
                long[] NormalSize = { };
                long[] SecureOffset = { };
                long[] NormalOffset = { };
                long gameNcaOffset = -1;
                long gameNcaSize = -1;
                long PFS0Offset = -1;
                long PFS0Size = -1;

                HFS0.HSF0_Entry[] array = new HFS0.HSF0_Entry[HFS0.HFS0_Headers[0].FileCount];
                fileStream.Position = XCI.XCI_Headers[0].HFS0OffsetPartition + 16 + 64 * HFS0.HFS0_Headers[0].FileCount;

                List<char> chars = new List<char>();
                long num = XCI.XCI_Headers[0].HFS0OffsetPartition + XCI.XCI_Headers[0].HFS0SizeParition;
                byte[] array2 = new byte[64];
                byte[] array3 = new byte[16];
                byte[] array4 = new byte[24];
                for (int i = 0; i < HFS0.HFS0_Headers[0].FileCount; i++)
                {
                    fileStream.Position = XCI.XCI_Headers[0].HFS0OffsetPartition + 16 + 64 * i;
                    fileStream.Read(array2, 0, 64);
                    array[i] = new HFS0.HSF0_Entry(array2);
                    fileStream.Position = XCI.XCI_Headers[0].HFS0OffsetPartition + 16 + 64 * HFS0.HFS0_Headers[0].FileCount + array[i].Name_ptr;
                    int num2;
                    while ((num2 = fileStream.ReadByte()) != 0 && num2 != 0)
                    {
                        chars.Add((char)num2);
                    }
                    array[i].Name = new string(chars.ToArray());
                    chars.Clear();

                    offset = num + array[i].Offset;
                    hashBuffer = new byte[array[i].HashedRegionSize];
                    fileStream.Position = offset;
                    fileStream.Read(hashBuffer, 0, array[i].HashedRegionSize);
                    actualHash = SHA256Bytes(hashBuffer);

                    HFS0.HFS0_Header[] array5 = new HFS0.HFS0_Header[1];
                    fileStream.Position = array[i].Offset + num;
                    fileStream.Read(array3, 0, 16);
                    array5[0] = new HFS0.HFS0_Header(array3);
                    if (array[i].Name == "secure")
                    {
                        SecureSize = new long[array5[0].FileCount];
                        SecureOffset = new long[array5[0].FileCount];
                    }
                    if (array[i].Name == "normal")
                    {
                        NormalSize = new long[array5[0].FileCount];
                        NormalOffset = new long[array5[0].FileCount];
                    }
                    HFS0.HSF0_Entry[] array6 = new HFS0.HSF0_Entry[array5[0].FileCount];
                    for (int j = 0; j < array5[0].FileCount; j++)
                    {
                        fileStream.Position = array[i].Offset + num + 16 + 64 * j;
                        fileStream.Read(array2, 0, 64);
                        array6[j] = new HFS0.HSF0_Entry(array2);
                        fileStream.Position = array[i].Offset + num + 16 + 64 * array5[0].FileCount + array6[j].Name_ptr;
                        if (array[i].Name == "secure")
                        {
                            SecureSize[j] = array6[j].Size;
                            SecureOffset[j] = array[i].Offset + array6[j].Offset + num + 16 + array5[0].StringTableSize + array5[0].FileCount * 64;
                        }
                        if (array[i].Name == "normal")
                        {
                            NormalSize[j] = array6[j].Size;
                            NormalOffset[j] = array[i].Offset + array6[j].Offset + num + 16 + array5[0].StringTableSize + array5[0].FileCount * 64;
                        }
                        while ((num2 = fileStream.ReadByte()) != 0 && num2 != 0)
                        {
                            chars.Add((char)num2);
                        }
                        array6[j].Name = new string(chars.ToArray());
                        chars.Clear();

                        offset = array[i].Offset + array6[j].Offset + num + 16 + array5[0].StringTableSize + array5[0].FileCount * 64;
                        hashBuffer = new byte[array6[j].HashedRegionSize];
                        fileStream.Position = offset;
                        fileStream.Read(hashBuffer, 0, array6[j].HashedRegionSize);
                        actualHash = SHA256Bytes(hashBuffer);
                    }
                }
                long num3 = -9223372036854775808L;
                for (int k = 0; k < SecureSize.Length; k++)
                {
                    if (SecureSize[k] > num3)
                    {
                        gameNcaSize = SecureSize[k];
                        gameNcaOffset = SecureOffset[k];
                        num3 = SecureSize[k];
                    }
                }
                PFS0Offset = gameNcaOffset + 32768;
                fileStream.Position = PFS0Offset;
                fileStream.Read(array3, 0, 16);
                PFS0.PFS0_Headers[0] = new PFS0.PFS0_Header(array3);
                PFS0.PFS0_Entry[] array8;
                array8 = new PFS0.PFS0_Entry[PFS0.PFS0_Headers[0].FileCount];

                for (int m = 0; m < PFS0.PFS0_Headers[0].FileCount; m++)
                {
                    fileStream.Position = PFS0Offset + 16 + 24 * m;
                    fileStream.Read(array4, 0, 24);
                    array8[m] = new PFS0.PFS0_Entry(array4);
                    PFS0Size += array8[m].Size;

                    if (m == 1) //Dump of TitleID 01009AA000FAA000 reports more than 10000000 files here, so it breaks the program. Standard is to have only 2 files
                    {
                        break;
                    }
                }
                for (int n = 0; n < PFS0.PFS0_Headers[0].FileCount; n++)
                {
                    fileStream.Position = PFS0Offset + 16 + 24 * PFS0.PFS0_Headers[0].FileCount + array8[n].Name_ptr;
                    int num4;
                    while ((num4 = fileStream.ReadByte()) != 0 && num4 != 0)
                    {
                        chars.Add((char)num4);
                    }
                    array8[n].Name = new string(chars.ToArray());
                    chars.Clear();

                    if (n == 1) //Dump of TitleID 01009AA000FAA000 reports more than 10000000 files here, so it breaks the program. Standard is to have only 2 files
                    {
                        break;
                    }

                }

                NCA.NCA_Headers[0] = new NCA.NCA_Header(DecryptNCAHeader(filepath, gameNcaOffset));
                result.TitleID = "0" + NCA.NCA_Headers[0].TitleID.ToString("X");
                result.TitleIDBaseGame = result.TitleID;
                result.SDKVersion = $"{NCA.NCA_Headers[0].SDKVersion4}.{NCA.NCA_Headers[0].SDKVersion3}.{NCA.NCA_Headers[0].SDKVersion2}.{NCA.NCA_Headers[0].SDKVersion1}";
                result.MasterKeyRevision = Util.GetMkey(NCA.NCA_Headers[0].MasterKeyRev);

                //Extra Info Is Got Here
                if (getMKey())
                {
                    //fileStream = GetFileStream(filepath);
                    for (int si = 0; si < SecureSize.Length; si++)
                    {
                        if (SecureSize[si] > 0x4E20000) continue;
                        try
                        {
                            File.Delete("meta");
                            Directory.Delete("data", true);
                        }
                        catch { }

                        using (FileStream fileStream2 = File.OpenWrite("meta"))
                        {
                            fileStream.Position = SecureOffset[si];
                            byte[] buffer = new byte[8192];
                            num = SecureSize[si];
                            int num2;
                            while ((num2 = fileStream.Read(buffer, 0, 8192)) > 0 && num > 0)
                            {
                                fileStream2.Write(buffer, 0, num2);
                                num -= num2;
                            }
                            fileStream2.Close();
                        }

                        Process process = new Process();
                        process.StartInfo = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "hactool.exe",
                            Arguments = "-k keys.txt --romfsdir=data meta"
                        };
                        process.Start();
                        process.WaitForExit();

                        if (File.Exists("data\\control.nacp"))
                        {
                            byte[] source = File.ReadAllBytes("data\\control.nacp");
                            NACP.NACP_Datas[0] = new NACP.NACP_Data(source.Skip(0x3000).Take(0x1000).ToArray());

                            result.Region_Icon = new Dictionary<string, string>();
                            result.Languages = new List<string>();
                            for (int i = 0; i < NACP.NACP_Strings.Length; i++)
                            {
                                NACP.NACP_Strings[i] = new NACP.NACP_String(source.Skip(i * 0x300).Take(0x300).ToArray());

                                if (NACP.NACP_Strings[i].Check != 0)
                                {
                                    string icon_filename = "data\\icon_" + Language[i].Replace(" ", "") + ".dat";
                                    string icon_titleID_filename = CACHE_FOLDER + "\\icon_" + result.TitleID + "_" + Language[i].Replace(" ", "") + ".bmp";

                                    if (i == 13) //Taiwanese titles are localized as Traditional Chinese
                                    {
                                        if (!File.Exists(icon_filename)) { //If no taiwanese icon is found... Use Traditional Chinese
                                            icon_filename = "data\\icon_" + Language[14].Replace(" ", "") + ".dat";
                                            icon_titleID_filename = CACHE_FOLDER + "\\icon_" + result.TitleID + "_" + Language[14].Replace(" ", "") + ".bmp";
                                        }
                                    }

                                    if (File.Exists(icon_filename))
                                    {
                                        try
                                        {
                                            File.Copy(icon_filename, icon_titleID_filename, true);
                                        }
                                        catch (System.IO.IOException e)
                                        {
                                            logger.Error(e.StackTrace); //File in use?
                                        }
                                        result.Region_Icon.Add(Language[i], icon_titleID_filename);
                                        result.Languages.Add(Language[i]);
                                    }
                                }
                            }
                            result.GameRevision = NACP.NACP_Datas[0].GameVer.Replace("\0", ""); ;
                            result.ProductCode = NACP.NACP_Datas[0].GameProd.Replace("\0", ""); ;

                            for (int z = 0; z < NACP.NACP_Strings.Length; z++)
                            {
                                if (NACP.NACP_Strings[z].GameName.Replace("\0", "") != "")
                                {
                                    result.GameName = NACP.NACP_Strings[z].GameName.Replace("\0", "");
                                    break;
                                }
                            }

                            for (int z = 0; z < NACP.NACP_Strings.Length; z++)
                            {
                                if (NACP.NACP_Strings[z].GameAuthor.Replace("\0", "") != "")
                                {
                                    result.Developer = NACP.NACP_Strings[z].GameAuthor.Replace("\0", "");
                                    break;
                                }
                            }

                            if (result.ProductCode == "")
                            {
                                result.ProductCode = "No Prod. ID";
                            }
                            try
                            {
                                File.Delete("meta");
                                Directory.Delete("data", true);
                            }
                            catch { }

                            break;
                        }
                    }
                    fileStream.Close();
                }
                GetExtraInfoFromScene(result);
            }
            return result;
        }

        public static FileData GetFileData(XElement xe)
        {
            FileData result = new FileData();

            try
            {
                result.TitleID = xe.Attribute("TitleID").Value;
                if (xe.Element("TitleIDBaseGame") != null)
                {
                    result.TitleIDBaseGame = xe.Element("TitleIDBaseGame").Value;
                }          
                if (xe.Element("Version") != null)
                {
                    result.Version = xe.Element("Version").Value;
                }
                result.CartSize = xe.Element("CartSize").Value;
                //result.Cardtype = xe.Element("CardType").Value;
                result.Developer = xe.Element("Developer").Value;
                result.FileName = xe.Element("FileName").Value;
                result.FileNameWithExt = xe.Element("FileNameWithExt").Value;
                result.FilePath = xe.Element("FilePath").Value;
                result.GameName = xe.Element("GameName").Value;
                result.GameRevision = xe.Element("GameRevision").Value;
                result.IsTrimmed = (xe.Element("IsTrimmed").Value == "true") ? true : false;

                string languages_ = xe.Element("Languages").Value;
                string[] languages_array = languages_.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                List<string> languages = new List<string>();
                for (int i = 0; i < languages_array.Length; i++)
                {
                    languages.Add(languages_array[i]);
                }
                result.Languages = languages;

                result.MasterKeyRevision = xe.Element("MasterKeyRevision").Value;
                result.ProductCode = xe.Element("ProductCode").Value;
                result.ROMSize = xe.Element("ROMSize").Value;
                result.ROMSizeBytes = Convert.ToInt64(xe.Element("ROMSizeBytes").Value);
                result.SDKVersion = xe.Element("SDKVersion").Value;
                result.UsedSpace = xe.Element("UsedSpace").Value;
                result.UsedSpaceBytes = Convert.ToInt64(xe.Element("UsedSpaceBytes").Value);

                Dictionary<string, string> Region_Icon = new Dictionary<string, string>();
                string[] regionIcons = xe.Element("Region_Icon").Value.Split("[".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < regionIcons.Length; i++)
                {
                    int ind_e = regionIcons[i].IndexOf(",");
                    string region = regionIcons[i].Substring(0, ind_e);
                    string icon = regionIcons[i].Substring(ind_e + 2, (regionIcons[i].Length - ind_e - 3)).Trim();
                    Region_Icon.Add(region, icon);
                }
                result.Region_Icon = Region_Icon;

                //Info from scene
                if (xe.Element("CardType") != null)
                {
                    result.Cardtype = xe.Element("CardType").Value;
                }
                if (xe.Element("Group") != null)
                {
                    result.Group = xe.Element("Group").Value;
                }
                if (xe.Element("Serial") != null)
                {
                    result.Serial = xe.Element("Serial").Value;
                }
                if (xe.Element("Firmware") != null)
                {
                    result.Firmware = xe.Element("Firmware").Value;
                }
                if (xe.Element("Region") != null)
                {
                    result.Region = xe.Element("Region").Value;
                }
                if (xe.Element("Languages_resumed") != null)
                {
                    result.Languages_resumed = xe.Element("Languages_resumed").Value;
                }
                if (xe.Element("Distribution_Type") != null)
                {
                    result.DistributionType = xe.Element("Distribution_Type").Value;
                }
                if (xe.Element("ID_Scene") != null)
                {
                    result.IdScene = xe.Element("ID_Scene").Value.Trim() == "" ? 0 : Convert.ToInt32(xe.Element("ID_Scene").Value);
                }
                if (xe.Element("Content_Type") != null)
                {
                    result.ContentType = xe.Element("Content_Type").Value;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace);
            }

            return result;
        }

        public static FileData GetFileData(XElement xe, bool isSceneXML)
        {
            FileData result = new FileData();

            result.TitleID = xe.Element("titleid").Value;
            result.TitleIDBaseGame = result.TitleID;
            result.CartSize = xe.Element("imagesize").Value;
            result.Developer = xe.Element("publisher").Value;
            //result.FileName = xe.Element("FileName").Value;
            //result.FileNameWithExt = xe.Element("FileNameWithExt").Value;
            //result.FilePath = xe.Element("FilePath").Value;
            result.GameName = xe.Element("name").Value;
            //result.GameRevision = xe.Element("GameRevision").Value;
            //result.IsTrimmed = (xe.Element("IsTrimmed").Value == "true") ? true : false;

            //result.MasterKeyRevision = xe.Element("MasterKeyRevision").Value;
            //result.ProductCode = xe.Element("ProductCode").Value;
            //result.ROMSize = xe.Element("ROMSize").Value;
            //result.ROMSizeBytes = Convert.ToInt64(xe.Element("ROMSizeBytes").Value);
            //result.SDKVersion = xe.Element("SDKVersion").Value;
            //result.UsedSpace = xe.Element("UsedSpace").Value;
            //result.UsedSpaceBytes = Convert.ToInt64(xe.Element("UsedSpaceBytes").Value);

            /*
            Dictionary<string, string> Region_Icon = new Dictionary<string, string>();
            string[] regionIcons = xe.Element("Region_Icon").Value.Split("[".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < regionIcons.Length; i++)
            {
                int ind_e = regionIcons[i].IndexOf(",");
                string region = regionIcons[i].Substring(0, ind_e);
                string icon = regionIcons[i].Substring(ind_e + 2, (regionIcons[i].Length - ind_e - 3)).Trim();
                Region_Icon.Add(region, icon);
            }
            result.Region_Icon = Region_Icon;
            */

            result.Group = xe.Element("group").Value;
            result.Serial = xe.Element("serial").Value;
            result.Firmware = xe.Element("firmware").Value;
            result.Cardtype = xe.Element("card").Value;
            result.ROMSizeBytes = Convert.ToInt64(xe.Element("trimmedsize").Value);
            result.Region = xe.Element("region").Value;
            result.Languages_resumed = xe.Element("languages").Value;
            result.IdScene = Convert.ToInt32(xe.Element("id").Value);

            List<string> languages = new List<string>();
            string[] languages_ = xe.Element("languages").Value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i=0; i < languages_.Length; i++)
            {
                languages.Add(languages_[i]);
            }

            result.Languages = languages;
            return result;
        }

        public static FileData GetFileData(string titleID, Dictionary<string, FileData> dictionary)
        {
            FileData  result = new FileData();

            dictionary.TryGetValue(titleID, out result);

            return result;
        }

        public static Dictionary<string, FileData> GetFileDataCollectionNSP (string path)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            List<string> list = GetNSPsInFolder(path);

            int filesCount = list.Count();
            int i = 0;

            foreach (string file in list)
            {
                FrmMain.progressCurrentfile = file;
                FileData data = GetFileDataNSP(file);
                try
                {
                    result.Add(data.TitleID, data);
                }
                catch
                {
                    logger.Error("Found duplicate file (same TitleID = " + data.TitleID + " on " + Path.GetDirectoryName(data.FilePath) + ".");
                }

                i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return result;
        }

        public static Dictionary<string, FileData> GetFileDataCollection (string path)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            List<string> list = GetXCIsInFolder(path);

            int filesCount = list.Count();
            int i = 0;

            foreach (string file in list)
            {
                FrmMain.progressCurrentfile = file;
                FileData data = GetFileData(file);
                try
                {
                    result.Add(data.TitleID, data);
                } catch
                {
                    logger.Error("Found duplicate file (same TitleID = " + data.TitleID + " on " + Path.GetDirectoryName(data.FilePath) + ".");
                }
                
                i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return result;
        }

        public static Dictionary<string, FileData> GetFileDataCollectionAll (string path)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            List<string> list = GetNSPsInFolder(path);
            list.AddRange(GetXCIsInFolder(path));

            int filesCount = list.Count();
            int i = 0;

            foreach (string file in list)
            {
                FrmMain.progressCurrentfile = file;
                FileData data;
                if (Path.GetExtension(file) == ".xci")
                {
                    data = GetFileData(file);
                } else
                {
                    data = GetFileDataNSP(file);
                }
                
                try
                {
                    result.Add(data.TitleID, data);
                }
                catch
                {
                    logger.Error("Found duplicate file (same TitleID = " + data.TitleID + " on " + Path.GetDirectoryName(data.FilePath) + ".");
                }

                i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return result;
        }

        public static List<string> GetXCIsInFolder(string folder)
        {
            List<string> list = new List<string>();

            try
            {
                foreach (string f in Directory.GetFiles(folder, "*.xci", System.IO.SearchOption.AllDirectories))
                {
                    list.Add(f);
                }

                foreach (string f in Directory.GetFiles(folder, "*.xc0", System.IO.SearchOption.AllDirectories))
                {
                    list.Add(f);
                }
            }
            catch (System.Exception execpt)
            {
                Console.WriteLine(execpt.Message);
            }

            return list;
        }

        public static List<string> GetNSPsInFolder(string folder)
        {
            List<string> list = new List<string>();

            try
            {
                foreach (string f in Directory.GetFiles(folder, "*.nsp", System.IO.SearchOption.AllDirectories))
                {
                    list.Add(f);
                }
            }
            catch (System.Exception execpt)
            {
                Console.WriteLine(execpt.Message);
            }

            return list;
        }

        public static List<string> GetSplitedXCIsFiles(string firstFile)
        {
            List<string> list = new List<string>();

            try
            {
                foreach (string f in Directory.GetFiles(Path.GetDirectoryName(firstFile), Path.GetFileNameWithoutExtension(firstFile)+".xc*", System.IO.SearchOption.AllDirectories))
                {
                    list.Add(f);
                }
            }
            catch (System.Exception execpt)
            {
                Console.WriteLine(execpt.Message);
            }

            return list;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return (from x in Enumerable.Range(0, hex.Length)
                    where x % 2 == 0
                    select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
        }

        public static string SHA256Bytes(byte[] ba)
        {
            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] hashValue;
            hashValue = mySHA256.ComputeHash(ba);
            return ByteArrayToString(hashValue);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2 + 2);
            hex.Append("0x");
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] DecryptNCAHeader(string selectedFile, long offset)
        {
            byte[] array = new byte[3072];
            if (File.Exists(selectedFile))
            {
                MultiStream fileStream = GetFileStream(selectedFile);
                fileStream.Position = offset;
                fileStream.Read(array, 0, 3072);
                File.WriteAllBytes(selectedFile + ".tmp", array);
                Xts xts = XtsAes128.Create(NcaHeaderEncryptionKey1_Prod, NcaHeaderEncryptionKey2_Prod);
                using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(selectedFile + ".tmp")))
                {
                    using (XtsStream xtsStream = new XtsStream(binaryReader.BaseStream, xts, 512))
                    {
                        xtsStream.Read(array, 0, 3072);
                    }
                }
                File.Delete(selectedFile + ".tmp");
                fileStream.Close();
            }
            return array;
        }

        public static string GetCapacity(int id)
        {
            switch (id)
            {
                case 248:
                    return "2 GB";
                case 240:
                    return "4 GB";
                case 224:
                    return "8 GB";
                case 225:
                    return "16 GB";
                case 226:
                    return "32 GB";
                default:
                    return "?";
            }
        }

        public static string BitMapToString(Bitmap image)
        {
            byte[] ByteArrayFromBitmap(ref Bitmap bitmap)
            {
                // The bitmap contents are coded with Width and Height followed by pixel colors (uint)
                byte[] b = new byte[4 * (bitmap.Height * bitmap.Width + 2)];
                int n = 0;
                // encode the width
                uint x = (uint)bitmap.Width;
                int y = (int)x;
                b[n] = (byte)(x / 0x1000000);
                x = x % (0x1000000);
                n++;
                b[n] = (byte)(x / 0x10000);
                x = x % (0x10000);
                n++;
                b[n] = (byte)(x / 0x100);
                x = x % 0x100;
                n++;
                b[n] = (byte)x;
                n++;
                // encode the height
                x = (uint)bitmap.Height;
                y = (int)x;
                b[n] = (byte)(x / 0x1000000);
                x = x % (0x1000000);
                n++;
                b[n] = (byte)(x / 0x10000);
                x = x % (0x10000);
                n++;
                b[n] = (byte)(x / 0x100);
                x = x % 0x100;
                n++;
                b[n] = (byte)x;
                n++;
                // Loop through each row
                for (int j = 0; j < bitmap.Height; j++)
                {
                    // Loop through the pixel on this row
                    for (int i = 0; i < bitmap.Width; i++)
                    {
                        x = (uint)bitmap.GetPixel(i, j).ToArgb();
                        y = (int)x;
                        b[n] = (byte)(x / 0x1000000);
                        x = x % (0x1000000);
                        n++;
                        b[n] = (byte)(x / 0x10000);
                        x = x % (0x10000);
                        n++;
                        b[n] = (byte)(x / 0x100);
                        x = x % 0x100;
                        n++;
                        b[n] = (byte)x;
                        n++;
                    }
                }
                return b;
            }

            string result = "";

            byte[] bb = ByteArrayFromBitmap(ref image);
            result = Convert.ToBase64String(bb);

            return result;
        }

        public static string BytesToGB(long bytes)
        {
            string result;
            double _bytes = bytes;
            string[] array_fs = new string[5] { "B", "KB", "MB", "GB", "TB" };
            int num2_fs = 0;

            while (_bytes >= 1024.0 && num2_fs < array_fs.Length - 1)
            {
                num2_fs++;
                _bytes /= 1024.0;
            }
            result = $"{_bytes:0.##} {array_fs[num2_fs]}";

            return result;
        }

        public static Dictionary<string, FileData> CloneDictionary(Dictionary<string, FileData> dictionary)
        {
            Dictionary<string, FileData> result = new Dictionary<string, FileData>();

            foreach(KeyValuePair<string, FileData> entry in dictionary)
            {
                result.Add(entry.Key, entry.Value);
            }

            return result;
        }

        public static XDocument CloneXDocument(XDocument xml)
        {
            return new XDocument(xml);
        }
    }
}
