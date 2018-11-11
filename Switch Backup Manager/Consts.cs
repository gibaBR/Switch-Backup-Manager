using System.Collections.Generic;

namespace Switch_Backup_Manager
{
    class Consts
    {
        public static Dictionary<string, string> UPDATE_FILES = new Dictionary<string, string>
        {
            //https://gist.github.com/garoxas/b6f2db2542250b2b34aeecc3f6fe273e
            { "1.0.0", "73af776d8a1abb72507aa0c51a53adec.cnmt.nca" },
            { "2.0.0", "2d4c96214d911cd1a2ed7a1875827a86.cnmt.nca" },
            { "2.1.0", "129eb530699c67b6dbb3346d9e826221.cnmt.nca" },
            { "2.2.0", "c1d883faf878afd1f6d4afd3151aae8f.cnmt.nca" },
            { "2.3.0", "52b1effc9834a51339e1d9916e9cca8c.cnmt.nca" },
            { "3.0.0", "70ec927bc7815c4f424bb3799618d66e.cnmt.nca" },
            { "3.0.1", "9b74dbe36b836e2ee67d25a47c3bdee3.cnmt.nca" },
            { "3.0.2", "47d4947c74d36d48d2f2c36afd150275.cnmt.nca" },
            { "4.0.1", "8e31518c0b0561e4cba9baed9b9f805d.cnmt.nca" },
            { "4.1.0", "51fa355f76905f389b2b31181a309fd3.cnmt.nca" },
            { "5.0.0", "233d465ca810536af835fb175b60c231.cnmt.nca" },
            { "5.0.2", "e9f9f9bb6087e68e6dd9ce0e56eb70df.cnmt.nca" },
            { "5.1.0", "f5637c1023b9909c5026e0e8d0e30d2d.cnmt.nca" },
            { "5.x.x", "e9f9f9bb6087e68e6dd9ce0e56eb70df.cnmt.nca" }, //5.0.2
            { "6.0.0", "9965b2a2ea6b66723c4a7c25cdd888c9.cnmt.nca" },
            { "6.0.1", "7a242c9c6feb5686c8d0a19fec5c8ab9.cnmt.nca" },
            { "6.1.0", "a28317f1b2e0a35149dea5f7a85685ef.cnmt.nca" },
        };

        public static Dictionary<string, int> UPDATE_NUMBER_OF_FILES = new Dictionary<string, int>
        {
            { "1.0.0", 167 },
            { "2.0.0", 183 },
            { "2.0.1", 183 }, //2.1.0
            { "2.0.3", 183 }, //2.3.0
            { "2.1.0", 183 },
            { "2.2.0", 183 },
            { "2.3.0", 183 },
            { "3.0.0", 191 },
            { "3.0.1", 191 },
            { "3.0.2", 191 },
            { "4.0.1", 201 },
            { "4.1.0", 201 },
            { "5.0.0", 207 },
            { "5.0.2", 207 },
            { "5.1.0", 209 },
            { "5.x.x", 207 }, //5.0.2
            { "6.0.0", 211 },
            { "6.0.1", 211 },
            { "6.1.0", 211 },
        };

        public enum NSPSource
        {
            CNMT_XML = 1 << 0,
            CERT = 1 << 1,
            TIK = 1 << 2,
            LEGALINFO_XML = 1 << 3,
            NACP_XML = 1 << 4,
            PROGRAMINFO_XML = 1 << 5,
            CARDSPEC_XML = 1 << 6,
            AUTHORINGTOOLINFO_XML = 1 << 7,
        }
    }
}
