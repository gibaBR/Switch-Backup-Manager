using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Switch_Backup_Manager
{
    class FileData
    {
        public FileData () //Default constructor
        {
            this.FilePath = "";
            this.FileName = "";
            this.FileNameWithExt = "";
            this.ROMSize = "";
            this.ROMSizeBytes = 0;
            this.UsedSpace = "";
            this.UsedSpaceBytes = 0;
            this.TitleID = "";
            this.TitleIDBaseGame = "";
            this.GameName = "";
            this.Developer = "";
            this.GameRevision = "";
            this.ProductCode = "";
            this.SDKVersion = "";
            this.CartSize = "";
            this.MasterKeyRevision = "";
            this.Region_Icon = new Dictionary<string, string>();
            this.Languages = new List<string>();
            this.Languages_resumed = "";
            this.IsTrimmed = false;
            this.Group = "";
            this.Serial = "";
            this.Firmware = "";
            this.Cardtype = "";
            this.Region = "";
            this.IsSplit = false;
            this.DistributionType = "";
            this.IdScene = 0;
            this.ContentType = "";
            this.Version = "";
            this.HasExtendedInfo = false;
            this.Description = "";
            this.Publisher = "";
            this.ReleaseDate = "";
            this.NumberOfPlayers = "";
            this.Categories = new List<string>();
            this.ESRB = 0;
        }

        public FileData(string FilePath, string FileName, string FileNameWithExt, string ROMSize, long ROMSizeBytes, 
            string UsedSpace, long UsedSpaceBytes, string TitleID, string TitleIDBaseGame, string GameName, 
            string Developer, string GameRevision, string ProductCode, string SDKVersion, string CartSize, 
            string MasterKeyRevision, Dictionary<string, string> Region_Icon, List<string> Languages, string Languages_resumed, 
            bool IsTrimmed, string Group, string Serial, string Firmware, string Cardtype, string Region, bool IsSplit, 
            string DistributionType, int IdScene, string ContentType, string Version, bool HasExtendedInfo, string Description, 
            string Publisher, string ReleaseDate, string NumberOfPlayers, List<string> Categories, int ESRB)
        {
            this.FilePath = FilePath;
            this.FileName = FileName;
            this.FileNameWithExt = FileNameWithExt;
            this.ROMSize = ROMSize;
            this.ROMSizeBytes = ROMSizeBytes;
            this.UsedSpace = UsedSpace;
            this.UsedSpaceBytes = UsedSpaceBytes;
            this.TitleID = TitleID;
            this.TitleIDBaseGame = TitleIDBaseGame;
            this.GameName = GameName;
            this.Developer = Developer;
            this.GameRevision = GameRevision;
            this.ProductCode = ProductCode;
            this.SDKVersion = SDKVersion;
            this.CartSize = CartSize;
            this.MasterKeyRevision = MasterKeyRevision;
            this.Region_Icon = Region_Icon;
            this.Languages = Languages;
            this.Languages_resumed = Languages_resumed;
            this.IsTrimmed = IsTrimmed;
            this.Group = Group;
            this.Serial = Serial;
            this.Firmware = Firmware;
            this.Cardtype = Cardtype;
            this.Region = Region;
            this.IsSplit = IsSplit;
            this.DistributionType = DistributionType;
            this.IdScene = IdScene;
            this.ContentType = ContentType;
            this.Version = Version;
            this.HasExtendedInfo = HasExtendedInfo;
            this.Description = Description;
            this.Publisher = Publisher;
            this.ReleaseDate = ReleaseDate;
            this.NumberOfPlayers = NumberOfPlayers;
            this.Categories = Categories;
            this.ESRB = ESRB;
        }

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileNameWithExt { get; set; }
        public string ROMSize { get; set; } //The Size of the file
        public long ROMSizeBytes { get; set; }
        public string UsedSpace { get; set; } //The Size of the Game inside de file
        public long UsedSpaceBytes { get; set; }
        public string TitleID { get; set; }
        public string TitleIDBaseGame { get; set; } //Used for DLC NSP Files, they have their own Title ID so we neeg the base game Title ID to display pictures
        public string GameName { get; set; }
        public string Developer { get; set; }
        public string GameRevision { get; set; }
        public string ProductCode { get; set; }
        public string SDKVersion { get; set; }
        public string CartSize { get; set; }
        public string MasterKeyRevision { get; set; }
        public Dictionary<string, string> Region_Icon { get; set; }
        public List<string> Languages { get; set; }
        public string Languages_resumed { get; set; }
        public bool IsTrimmed { get; set; }
        public string Group { get; set; }
        public string Serial { get; set; }
        public string Firmware { get; set; }
        public string Cardtype { get; set; }
        public string Region { get; set; }
        public bool IsSplit { get; set; }
        public string DistributionType { get; set; }
        public int IdScene { get; set; }
        public string ContentType { get; set; } //Patch, AddOnContent, Application
        public string Version { get; set; } //Used by NSP Only

        //Info from web
        public bool HasExtendedInfo { get; set; }
        public string Description { get; set; }
        public string Publisher { get; set; }
        public string ReleaseDate { get; set; }
        public string NumberOfPlayers { get; set; }
        public List<string> Categories { get; set; }
        public int ESRB { get; set; }        

        //Available at runtime only
        public string sceneFound { get; set; }
    }
}
