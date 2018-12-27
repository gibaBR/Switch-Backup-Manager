using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using libusbK;

namespace NspUsbInstall
{

    public interface INspUsbInstallListener
    {
        void Start(string filename);
        void ProgressUpdate(int progress);
        void End(int encode);
        void Error(string descriptor);
    }

    public class NspUsbInstaller
    {
        //Tinfoil commands
        private const int CMD_ID_EXIT = 0;
        private const int CMD_ID_FILE_RANGE = 1;
        private const int CMD_ID_FILE_RANGE_PADDED = 2;
        private const int CMD_TYPE_RESPONSE = 1;

        //tinfoil vars
        private const int BUFFER_SEGMENT_DATA_SIZE = 0x100000;
        private const int PADDING_SIZE = 0x1000;
        private const int HEADER_SIZE = 0x20;

        //switch globals
        private const int SWITCH_VENDOR_ID = 0x057E;
        private const int SWITCH_PRODUCT_ID = 0x3000;
        private const int SWITCH_OUT_ENDPOINT = 0x01;
        private const int SWITCH_IN_ENDPOINT = 0x81;


        string files = "";

        INspUsbInstallListener listener;
        UsbK usb;


        public NspUsbInstaller()
        { }

        [HandleProcessCorruptedStateExceptions]
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SendFile(string files, INspUsbInstallListener listener = null)
        {
            this.files = files;
            this.listener = listener;
            bool retValue = true;
            int re = 0;

            var list = new LstK(0);
            bool stat = list.FindByVidPid(SWITCH_VENDOR_ID, SWITCH_PRODUCT_ID, out var devInfo);

            if (!stat)
            {
                if (listener != null)
                {
                    listener.Error("NX not Found");
                }
                else
                {
                    Console.Write("NX not Found");
                }
                retValue = false;
            }
            if (retValue)
            {
                usb = new UsbK(devInfo);
                usb.ClaimInterface(0, false);
                Write(GetBytes("TUL0"), 4);
                Write(PackInt(files.Length), 4);
                Write(new byte[8], 8);
                Write(GetBytes(files), files.Length);

                var readBuffer = new byte[0x20];
                bool run = true;
                while (run)
                {
                    Read(readBuffer, 0x20);
                    if (!((readBuffer[0] == 'T') && (readBuffer[1] == 'U') && (readBuffer[2] == 'C') && (readBuffer[3] == '0')))
                    {
                        re++;
                        if (re > 100)
                        {
                            retValue = false;
                            break;
                        }
                        continue;
                    }
                    byte cmd_type = readBuffer[4];
                    int cmd_id = UnpackInt(readBuffer, 8);
                    long data_size = UnpackLong(readBuffer, 12);

                    if (cmd_id == CMD_ID_EXIT)
                    {
                        if (listener != null) listener.End(0);
                        run = false;
                    }
                    else if (cmd_id == CMD_ID_FILE_RANGE)
                    {
                        if (!File_range_cmd(data_size, false)) break;
                    }
                    else if (cmd_id == CMD_ID_FILE_RANGE_PADDED)
                    {
                        if (!File_range_cmd(data_size, true)) break;
                    }
                }
                usb.ReleaseInterface(0, false);
            }
            list.Free();
            if (re > 10)
            {
                if (listener != null)
                {
                    listener.Error("Is device connected?");
                }
                else
                {
                    Console.Write("Error Reading, Is device connected?");
                }
            }
            return retValue;
        }

        int Write(byte[] data, int b)
        {
            usb.WritePipe(SWITCH_OUT_ENDPOINT, data, data.Length, out var transfered, IntPtr.Zero);
            return transfered;
        }

        int Read(byte[] data, int b)
        {
            usb.ReadPipe(SWITCH_IN_ENDPOINT, data, data.Length, out var transfered, IntPtr.Zero);
            return transfered;
        }

        public void Send_response_header(int cmd_id, long data_size)
        {
            byte[] rType = { CMD_TYPE_RESPONSE, 0, 0, 0 };
            Write(GetBytes("TUC0"), 4);
            Write(rType, 4);
            Write(PackInt(cmd_id), 4);
            Write(PackLong(data_size), 8);
            Write(new byte[0xC], 0xC);
        }

        public bool File_range_cmd(long data_size, bool padding)
        {
            long bytesSent = 0;
            byte[] file_range_header = new byte[HEADER_SIZE];
            Read(file_range_header, HEADER_SIZE);
            long range_size = UnpackLong(file_range_header);
            long range_offset = UnpackLong(file_range_header, 8);
            long nsp_name_len = UnpackLong(file_range_header, 16);
            byte[] nsp_name_bytes = new byte[nsp_name_len];
            Read(nsp_name_bytes, (int)nsp_name_len);
            string nsp_name = Encoding.UTF8.GetString(nsp_name_bytes);
            if (listener != null) listener.Start(nsp_name);
            else Console.Write("Range size: " + range_size + ", Range offset: " + range_offset + ", Name len: " + nsp_name_len + ", Name: " + nsp_name);
            int cmd_id = (padding ? CMD_ID_FILE_RANGE_PADDED : CMD_ID_FILE_RANGE);
            Send_response_header(cmd_id, range_size);
            try
            {
                using (BinaryReader b = new BinaryReader(File.Open(nsp_name, FileMode.Open)))
                {
                    b.BaseStream.Seek((long)range_offset, SeekOrigin.Begin);
                    int tBytes;
                    long curr_off = 0x0;
                    long end_off = (long)range_size;
                    long read_size = BUFFER_SEGMENT_DATA_SIZE;
                    if (padding)
                    {
                        read_size -= PADDING_SIZE;
                    }
                    while (curr_off < end_off)
                    {
                        if (curr_off + read_size >= end_off)
                        {
                            read_size = end_off - curr_off;
                        }
                        byte[] buff = b.ReadBytes((int)read_size);

                        if (padding)
                        {
                            Write(new byte[PADDING_SIZE], PADDING_SIZE);
                        }
                        tBytes = Write(buff, buff.Length);
                        curr_off += read_size;
                        bytesSent += buff.Length;
                        if (tBytes != buff.Length)
                        {
                            if (listener != null) listener.Error("Error sendind data. Closed connetion?");
                            return false;
                        }
                        if (listener != null) listener.ProgressUpdate((int)((bytesSent * 100) / range_size));
                    }
                    b.Close();
                }
            }
            catch (IOException e)
            {
                if (listener != null) listener.Error("Can't open the file: '" + e.Message + "'" );
                else Console.Write("Can't open the file");
                return false;
            }
            return true;
        }

        byte[] GetBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        long UnpackLong(byte[] bytes, int idx = 0)
        {
            return BitConverter.ToInt64(bytes, idx);
        }

        int UnpackInt(byte[] bytes, int idx = 0)
        {
            return BitConverter.ToInt32(bytes, idx);
        }

        byte[] PackInt(int n)
        {
            return BitConverter.GetBytes(n).ToArray(); ;
        }

        byte[] PackLong(long n)
        {
            return BitConverter.GetBytes(n).ToArray(); ;
        }
    }

}
