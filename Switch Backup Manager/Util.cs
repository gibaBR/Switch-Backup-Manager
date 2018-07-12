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

namespace Switch_Backup_Manager
{
    internal static class Util        
    {
        public const string INI_FILE="sbm.ini";
        public static string KEYS_FILE = "keys.txt";
        public const string KEYS_DOWNLOAD_SITE = "https://pastebin.com/raw/ekSH9R8t";
        public const string HACTOOL_FILE = "hactool.exe";
        public const string HACTOOL_DOWNLOAD_SITE = "https://github.com/SciresM/hactool/releases/download/1.1.0/hactool-1.1.0.win.zip";
        public const string NSWDB_FILE = "nswdb.xml";
        public const string NSWDB_DOWNLOAD_SITE = "http://nswdb.com/xml.php";
        public const string LOCAL_FILES_DB = "SBM_Local.xml";
        public const string CACHE_FOLDER = "cache";

        public static byte[] NcaHeaderEncryptionKey1_Prod;
        public static byte[] NcaHeaderEncryptionKey2_Prod;
        public static string Mkey;

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
            "Taiwanese",
            "Chinese",
            "???"
        };

        private static Image[] Icons = new Image[16];

        public static IniFile ini;
        public static XDocument XML_Local;
        public static XDocument XML_NSWDB;

        public static bool TrimXCIFile(FileData file)
        {
            bool result = false;

            if (file != null)
            {
                if (!file.IsTrimmed)
                {
                    FileStream fileStream = new FileStream(@file.FilePath, FileMode.Open, FileAccess.Write);
                    fileStream.SetLength(file.UsedSpaceBytes);
                    fileStream.Close();
                    file.ROMSizeBytes = file.UsedSpaceBytes;
                    file.ROMSize = file.UsedSpace;
                    file.IsTrimmed = true;
                    result = true;
                }
            }
            return result;
        }

        public static void TrimXCIFiles(Dictionary<string, FileData> files, string source) //source possible values: "local", "sdcard"
        {
            int filesCount = files.Count();
            int i = 0;

            if (source == "local")
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    if (TrimXCIFile(entry.Value))
                    {
                        UpdateXMLFromFileData(entry.Value);
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
        }

        public static void AutoRenameXCIFiles(Dictionary<string, FileData> files, string source) //source possible values: "local", "sdcard"
        {
            int filesCount = files.Count();
            int i = 0;

            if (source == "local")
            {
                foreach (KeyValuePair<string, FileData> entry in files)
                {
                    FrmMain.progressCurrentfile = entry.Value.FilePath;

                    if (AutoRenameXCIFile(entry.Value))
                    {
                        UpdateXMLFromFileData(entry.Value);
                    }

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }
                XML_Local.Save(@LOCAL_FILES_DB);
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
        }

        private static bool AutoRenameXCIFile(FileData file)
        {
            bool result = false;

            if (file != null)
            {
                string newFileName = Path.GetDirectoryName(file.FilePath) + "\\" +file.GameName;

                if (File.Exists(newFileName+".xci"))
                {
                    /*
                    newFileName += "_" + file.TitleID + ".xci";
                    if (!File.Exists(newFileName))
                    {
                        System.IO.File.Move(file.FilePath, newFileName);
                    } 
                    */
                } else
                {
                    newFileName += ".xci";
                    System.IO.File.Move(file.FilePath, newFileName);
                }


                file.FileName = Path.GetFileNameWithoutExtension(newFileName);
                file.FileNameWithExt = Path.GetFileName(newFileName);
                file.FilePath = newFileName;


                /*
                FileStream fileStream = new FileStream(@file.FilePath, FileMode.Open, FileAccess.Write);
                fileStream.SetLength(file.UsedSpaceBytes);
                fileStream.Close();
                file.ROMSizeBytes = file.UsedSpaceBytes;
                file.ROMSize = file.UsedSpace;
                file.IsTrimmed = true;
                */
                result = true;
            }
            return result;
        }

        public static void UpdateXMLFromFileData(FileData file)
        {
            XElement element = XML_Local.Descendants("Game")
                .FirstOrDefault(el => (string)el.Attribute("TitleID") == file.TitleID);
            if (element != null)
            {
                element.Element("ROMSizeBytes").Value = Convert.ToString(file.UsedSpaceBytes);
                element.Element("ROMSize").Value = file.UsedSpace;
                element.Element("IsTrimmed").Value = Convert.ToString(file.IsTrimmed).ToLower();
                element.Element("FilePath").Value = file.FilePath;
                element.Element("FileNameWithExt").Value = file.FileNameWithExt;
                element.Element("FileName").Value = file.FileName;
                element.Element("GameName").Value = file.GameName;
                element.Element("Developer").Value = file.Developer;
                element.Element("Developer").Value = file.Developer;
                element.Element("GameRevision").Value = file.GameRevision;
                element.Element("ProductCode").Value = file.ProductCode;
                element.Element("SDKVersion").Value = file.SDKVersion;
                element.Element("CartSize").Value = file.CartSize;
                element.Element("CardType").Value = file.Cardtype;
                element.Element("MasterKeyRevision").Value = file.MasterKeyRevision;
            }
        }

        public static void RemoveMissingFilesFromXML(XDocument xml)
        {
            XDocument xml_ = XDocument.Load(@LOCAL_FILES_DB);

            foreach (XElement xe in xml_.Descendants("Game"))
            {
                if (!File.Exists(xe.Element("FilePath").Value))
                {
                    RemoveTitleIDFromXML(xe.Attribute("TitleID").Value);
                }                
            }

            XML_Local.Save(@LOCAL_FILES_DB);
        }

        public static bool IsTitleIDOnXML(string titleID)
        {
            bool result = false;

            XElement element = XML_Local.Descendants("Game")
               .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);

            if (element != null)
            {
                result = true;
            }

            return result;
        }

        public static string GetCardTypeFromScene(string titleID)
        {
            string result = "";

            XElement element = XML_NSWDB.Descendants("release")
               .FirstOrDefault(el => (string)el.Element("titleid") == titleID);

            if (element != null)
            {
                result = element.Element("card").Value;
            }

            return result;
        }

        public static bool WriteFileDataToXML(FileData data)
        {
            bool result = true;

            //Try to find the game. If exists, do nothing. If not, Append
            if (!IsTitleIDOnXML(data.TitleID))
            {
                string languages = "";
                foreach (string language in data.Languagues)
                {
                    languages += language + ","; 
                }
                languages = languages.Remove(languages.Length-1);

                XElement element = new XElement("Game", new XAttribute("TitleID", data.TitleID),
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
                           new XElement("Languagues", languages),
                           new XElement("IsTrimmed", data.IsTrimmed)
                   );
                XML_Local.Root.Add(element);
                XML_Local.Save(@LOCAL_FILES_DB);
            }
            else
            {
                //Nothing to do?
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

        public static void LoadSettings()
        {
            ini = new IniFile((AppDomain.CurrentDomain.BaseDirectory) + INI_FILE);
            string keys_file = ini.IniReadValue("Config", "keys_file");
            if (keys_file.Trim() == "")
            {
                keys_file = KEYS_FILE;
                ini.IniWriteValue("Config", "keys_file", keys_file);
            } else
            {
                KEYS_FILE = keys_file;
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

            //TODO: Download hactool.zip and extract files
            //Searches for hactool.exe. 
            if (!File.Exists(HACTOOL_FILE))
            {
                MessageBox.Show(HACTOOL_FILE+" is missing. Please, download it at "+HACTOOL_DOWNLOAD_SITE);
                Environment.Exit(0);
            }

            //Searches for db.xml
            if (!File.Exists(NSWDB_FILE))
            {
                UpdateNSWDB();
            } else
            {
                string autoUpdateNSWDB_File = ini.IniReadValue("Config", "autoUpdateNSWDB");
                if (autoUpdateNSWDB_File.Trim() != "")
                {
                    if (autoUpdateNSWDB_File.Trim() == "true")
                    {
                        UpdateNSWDB();
                    }
                } else
                {
                    ini.IniWriteValue("Config", "autoUpdateNSWDB", "false");
                }
            }
            
            XML_NSWDB = XDocument.Load(@NSWDB_FILE);

            //Searches for local dabases (xml) and loads it
            if (!File.Exists(LOCAL_FILES_DB))
            {
                XML_Local = new XDocument(new XComment("List of local games"),
                    new XElement("Games", new XAttribute("Date", DateTime.Now.ToString())));
                XML_Local.Declaration = new XDeclaration("1.0", "utf-8", "true");
                XML_Local.Save(@LOCAL_FILES_DB);
            } else
            {
                XML_Local = XDocument.Load(@LOCAL_FILES_DB);
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
        public static Dictionary<string, FileData> AddFilesFromFolder(string path)
        {
            Dictionary<string, FileData> dictionary = new Dictionary<string, FileData>();
            if (Directory.Exists(path) && path.Trim() != "")
            {
                List<string> files = Util.GetXCIsInFolder(path);
                int filesCount = files.Count();
                int i = 0;
                foreach (string file in files)
                {
                    FileData data = Util.GetFileData(file);
                    FrmMain.progressCurrentfile = data.FilePath;
                    dictionary.Add(data.TitleID, data);

                    i++;
                    FrmMain.progressPercent = (int)(i * 100) / filesCount;
                }               
            }
            return dictionary;
        }

        /// <summary>
        /// Add all XCI files on a given list to a Dictionary of FileData <TitleID, FileData>
        /// </summary>
        /// <param name="files string[]"></param>
        public static Dictionary<string, FileData> AddFiles(string[] files)
        {
            Dictionary<string, FileData> dictionary = new Dictionary<string, FileData>();

            int filesCount = files.Count();
            int i = 0;
            foreach (string file in files)
            {
                FileData data = Util.GetFileData(file);
                FrmMain.progressCurrentfile = data.FilePath;
                dictionary.Add(data.TitleID, data);

                i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return dictionary;
        }

        public static void AppendFileDataDictionaryToXML(Dictionary<string, FileData> dictionary, string xml)
        {
            foreach (KeyValuePair<string, FileData> entry in dictionary)
            {
                WriteFileDataToXML(entry.Value);
            }
        }

        public static void AppendFileDataDictionaryToXML(Dictionary<string, FileData> dictionary)
        {
            AppendFileDataDictionaryToXML(dictionary, LOCAL_FILES_DB);
        }

        public static void RemoveFileDataDictionaryFromXML(Dictionary<string, FileData> dictionary)
        {
            foreach (KeyValuePair<string, FileData> entry in dictionary)
            {
                RemoveTitleIDFromXML(entry.Key);
            }
            XML_Local.Save(@LOCAL_FILES_DB);
        }

        public static void RemoveTitleIDFromXML(string titleID)
        {
            XElement element = XML_Local.Descendants("Game")
               .FirstOrDefault(el => (string)el.Attribute("TitleID") == titleID);

            if (element != null)
            {
                element.Remove();
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
                FrmMain.progressCurrentfile = data.FilePath;
                if (operation == "copy")
                {
                    FileSystem.CopyFile(data.FilePath, destiny + data.FileNameWithExt, UIOption.AllDialogs);
                } else if (operation == "move")
                {
                    FileSystem.MoveFile(data.FilePath, destiny + data.FileNameWithExt, UIOption.AllDialogs);
                }

                i++;
                FrmMain.progressPercent = (int)(i * 100) / filesCount;
            }

            return result;
        }

        public static bool CheckXCI(string file)
        {
            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
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

        public static FileData GetFileData(string filepath)
        {
            FileData result = new FileData();
            //Basic Info
            result.FilePath = filepath;
            result.FileName = Path.GetFileNameWithoutExtension(filepath);
            result.FileNameWithExt = Path.GetFileName(filepath);

            if (CheckXCI(filepath))
            {
                //Get File Size
                string[] array_fs = new string[5] { "B", "KB", "MB", "GB", "TB" };
                double num_fs = (double)new FileInfo(filepath).Length;
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

                FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
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
                }
                fileStream.Close();


                NCA.NCA_Headers[0] = new NCA.NCA_Header(DecryptNCAHeader(filepath, gameNcaOffset));
                result.TitleID = "0" + NCA.NCA_Headers[0].TitleID.ToString("X");
                result.SDKVersion = $"{NCA.NCA_Headers[0].SDKVersion4}.{NCA.NCA_Headers[0].SDKVersion3}.{NCA.NCA_Headers[0].SDKVersion2}.{NCA.NCA_Headers[0].SDKVersion1}";
                result.MasterKeyRevision = Util.GetMkey(NCA.NCA_Headers[0].MasterKeyRev);

                //Extra Info Is Got Here
                if (getMKey())
                {

                    using (fileStream = File.OpenRead(filepath))
                    {
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
                                result.Languagues = new List<string>();
                                for (int i = 0; i < NACP.NACP_Strings.Length; i++)
                                {
                                    NACP.NACP_Strings[i] = new NACP.NACP_String(source.Skip(i * 0x300).Take(0x300).ToArray());

                                    if (NACP.NACP_Strings[i].Check != 0)
                                    {
                                        
                                        //CB_RegionName.Items.Add(Language[i]);
                                        string icon_filename = "data\\icon_" + Language[i].Replace(" ", "") + ".dat";
                                        string icon_titleID_filename = CACHE_FOLDER+"\\icon_" + result.TitleID + "_" + Language[i].Replace(" ", "") + ".bmp";
                                        
                                        if (File.Exists(icon_filename))
                                        {
                                            try
                                            {
                                                File.Copy(icon_filename, icon_titleID_filename, true);
                                            } catch (System.IO.IOException)
                                            {
                                                //File in use?
                                            }
                                            result.Region_Icon.Add(Language[i], icon_titleID_filename);
                                            result.Languagues.Add(Language[i]);
                                        }

                                        /* 
                                         * This Saves every image into the XML as String. Too much memory, huge XML file.
                                        if (File.Exists(icon_filename))
                                        {
                                            using (Bitmap original = new Bitmap(icon_filename))
                                            {                                                
                                                result.Region_Icon.Add(Language[i], BitMapToString(original)); //This Saves every image into the XML as String. Too much memory, huge XML file.
                                                Icons[i] = new Bitmap(original);
                                                //PB_GameIcon.BackgroundImage = Icons[i];
                                            }
                                        }
                                        */
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

                                //CB_RegionName.SelectedIndex = 0;
                                break;
                            }
                        }
                        fileStream.Close();
                    }
                }
                result.Cardtype = GetCardTypeFromScene(result.TitleID);
            }            
            return result;
        }

        public static FileData GetFileData(XElement xe)
        {
            FileData result = new FileData();
            result.TitleID = xe.Attribute("TitleID").Value;
            result.CartSize = xe.Element("CartSize").Value;
            result.Cardtype = xe.Element("CardType").Value;
            result.Developer = xe.Element("Developer").Value;
            result.FileName = xe.Element("FileName").Value;
            result.FileNameWithExt = xe.Element("FileNameWithExt").Value;
            result.FilePath = xe.Element("FilePath").Value;
            result.GameName = xe.Element("GameName").Value;
            result.GameRevision = xe.Element("GameRevision").Value;
            result.IsTrimmed = (xe.Element("IsTrimmed").Value == "true") ? true : false;

            string languages_ = xe.Element("Languagues").Value;
            string[] languages_array = languages_.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            List<string> languages = new List<string>();
            for (int i = 0; i < languages_array.Length; i++)
            {
                languages.Add(languages_array[i]);
            }
            result.Languagues = languages;

            result.MasterKeyRevision = xe.Element("MasterKeyRevision").Value;
            result.ProductCode = xe.Element("ProductCode").Value;
            result.ROMSize = xe.Element("ROMSize").Value;
            result.ROMSizeBytes = Convert.ToInt64(xe.Element("ROMSizeBytes").Value);
            result.SDKVersion = xe.Element("SDKVersion").Value;
            result.UsedSpace = xe.Element("UsedSpace").Value;
            result.UsedSpaceBytes = Convert.ToInt64(xe.Element("UsedSpaceBytes").Value);

            //result.Region_Icon = xe.Element("Region_Icon").Value;
            Dictionary<string, string> Region_Icon = new Dictionary<string, string>();
            //string regionIcons = xe.Element("Region_Icon").Value;
            string[] regionIcons = xe.Element("Region_Icon").Value.Split("[".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < regionIcons.Length; i++)
            {
                int ind_e = regionIcons[i].IndexOf(",");
                string region = regionIcons[i].Substring(0, ind_e);
                string icon = regionIcons[i].Substring(ind_e+2, (regionIcons[i].Length - ind_e - 3)).Trim();
                Region_Icon.Add(region, icon);
            }
            result.Region_Icon = Region_Icon;

            return result;
        }

        public static FileData GetFileData(XElement xe, bool isSceneXML)
        {
            FileData result = new FileData();

            result.TitleID = xe.Element("titleid").Value;
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

            List<string> languages = new List<string>();
            string[] languages_ = xe.Element("languages").Value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i=0; i < languages_.Length; i++)
            {
                languages.Add(languages_[i]);
            }

            result.Languagues = languages;
            return result;
        }

        public static FileData GetFileData(string titleID, Dictionary<string, FileData> dictionary)
        {
            FileData  result = new FileData();

            dictionary.TryGetValue(titleID, out result);

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
                result.Add(data.TitleID, data);
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
                FileStream fileStream = new FileStream(selectedFile, FileMode.Open, FileAccess.Read);
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
            //double num_fs = (double)new FileInfo(filepath).Length;
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
