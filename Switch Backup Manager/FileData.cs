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
            this.IdScene = 0;
        }

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileNameWithExt { get; set; }
        public string ROMSize { get; set; } //The Size of the file
        public long ROMSizeBytes { get; set; }
        public string UsedSpace { get; set; } //The Size of the Game inside de file
        public long UsedSpaceBytes { get; set; }
        public string TitleID { get; set; }
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
    }
}
