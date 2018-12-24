using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using static NspUsbInstall.NativeMethods;

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
        DeviceHandle dh;

        public NspUsbInstaller()
        {}

        [HandleProcessCorruptedStateExceptions]
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SendFile(string files, INspUsbInstallListener listener = null)
        {
            this.files = files;
            this.listener = listener;
            bool retValue = true;
            int re = 0;

            IntPtr ctx = new IntPtr();
            Init(ref ctx);
            Context context = Context.DangerousCreate(ctx);
            dh = OpenDeviceWithVidPid(context, 0x057E, 0x3000);
            if (((Int64)dh.DangerousGetHandle()) == 0)
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
                ClaimInterface(dh, 0);

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
            }

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
            if (((Int64)dh.DangerousGetHandle()) != 0)
                ReleaseInterface(dh, 0);
            dh.Close();
            return retValue;
        }

        unsafe int Write(byte[] data, int b)
        {
            fixed (byte* p = data)
            {
                int len = 0;
                Error ret = BulkTransfer(dh, SWITCH_OUT_ENDPOINT, p, b, ref len, 0);
                if ((ret == 0) && (len == b))
                {
                    return len;
                }
                else
                {
                    return (int)ret;
                }
            }
        }

        unsafe Error Read(byte[] data, int b)
        {
            fixed (byte* p = data)
            {
                int len = 0;
                Error ret = BulkTransfer(dh, SWITCH_IN_ENDPOINT, p, b, ref len, 0);

                if ((ret == 0) && (len == b))
                {
                    return 0;
                }
                else
                {
                    return ret;
                }
            }
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
            catch (IOException)
            {
                if (listener != null) listener.Error("Can't open the file");
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



    //From this point almost all extract from LibUsbDotNet

    //------------------------------------------------------------------------------
    // <auto-generated>
    //     This code was generated by a tool.
    //
    //     Changes to this file may cause incorrect behavior and will be lost if
    //     the code is regenerated.
    // </auto-generated>
    //------------------------------------------------------------------------------

    // Copyright © 2006-2010 Travis Robinson. All rights reserved.
    // Copyright © 2011-2018 LibUsbDotNet contributors. All rights reserved.
    // 
    // website: http://github.com/libusbdotnet/libusbdotnet
    // license: https://github.com/LibUsbDotNet/LibUsbDotNet/blob/master/LICENSE
    // 
    // This program is free software; you can redistribute it and/or modify it
    // under the terms of the GNU General Public License as published by the
    // Free Software Foundation; either version 2 of the License, or 
    // (at your option) any later version.
    // 
    // This program is distributed in the hope that it will be useful, but 
    // WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
    // or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
    // for more details.
    // 
    // You should have received a copy of the GNU General Public License along
    // with this program; if not, write to the Free Software Foundation, Inc.,
    // 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA. or 
    // visit www.gnu.org.
    // 
    //


    public unsafe static class NativeMethods
    {
        /// <summary>
        /// Use the default struct alignment for this platform.
        /// </summary>
        internal const int Pack = 0;
        static bool is64BitProcess = (IntPtr.Size == 8);

        internal const CallingConvention LibUsbCallingConvention = CallingConvention.StdCall;
        internal const string LibUsbNativeLibrary = "libusb-1.0.dll";


        #region DLL CALLS
        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_init")]
        public static extern Error Init(ref IntPtr ctx);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_exit")]
        public static extern void Exit(IntPtr ctx);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_free_device_list")]
        public static extern void FreeDeviceList(ref IntPtr list, int unrefDevices);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_ref_device")]
        public static extern Device RefDevice(Device dev);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_unref_device")]
        public static extern void UnrefDevice(IntPtr dev);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_open")]
        public static extern Error Open(Device dev, ref IntPtr devHandle);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_close")]
        public static extern void Close(IntPtr devHandle);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_claim_interface")]
        public static extern Error ClaimInterface(DeviceHandle devHandle, int interfaceNumber);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_release_interface")]
        public static extern Error ReleaseInterface(DeviceHandle devHandle, int interfaceNumber);

        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_open_device_with_vid_pid")]
        public static extern DeviceHandle OpenDeviceWithVidPid(Context ctx, ushort vendorId, ushort productId);


        [DllImport(LibUsbNativeLibrary, CallingConvention = LibUsbCallingConvention, EntryPoint = "libusb_bulk_transfer")]
        public static extern Error BulkTransfer(DeviceHandle devHandle, byte endpoint, byte* data, int length, ref int actualLength, uint timeout);

        #endregion

        #region Error enum
        [Flags]
        public enum Error : int
        {
            /// <summary>
            ///  Success (no error) 
            /// </summary>
            Success = 0,

            /// <summary>
            ///  Input/output error 
            /// </summary>
            Io = -1,

            /// <summary>
            ///  Invalid parameter 
            /// </summary>
            InvalidParam = -2,

            /// <summary>
            ///  Access denied (insufficient permissions) 
            /// </summary>
            Access = -3,

            /// <summary>
            ///  No such device (it may have been disconnected) 
            /// </summary>
            NoDevice = -4,

            /// <summary>
            ///  Entity not found 
            /// </summary>
            NotFound = -5,

            /// <summary>
            ///  Resource busy 
            /// </summary>
            Busy = -6,

            /// <summary>
            ///  Operation timed out 
            /// </summary>
            Timeout = -7,

            /// <summary>
            ///  Overflow 
            /// </summary>
            Overflow = -8,

            /// <summary>
            ///  Pipe error 
            /// </summary>
            Pipe = -9,

            /// <summary>
            ///  System call interrupted (perhaps due to signal) 
            /// </summary>
            Interrupted = -10,

            /// <summary>
            ///  Insufficient memory 
            /// </summary>
            NoMem = -11,

            /// <summary>
            ///  Operation not supported or unimplemented on this platform 
            /// </summary>
            NotSupported = -12,

            /// <summary>
            ///  Other error 
            /// </summary>
            Other = -99,

        }

        #endregion


        #region UnixNativeTimeVal
        [StructLayout(LayoutKind.Sequential)]
        public struct UnixNativeTimeval
        {
            private IntPtr mTvSecInternal;
            private IntPtr mTvUSecInternal;

            /// <summary>
            /// Default <see cref="UnixNativeTimeval"/>.
            /// </summary>
            public static UnixNativeTimeval Default
            {
                get { return new UnixNativeTimeval(2, 0); }
            }

            /// <summary>
            /// Timeval seconds property.
            /// </summary>
            public long tv_sec
            {
                get { return this.mTvSecInternal.ToInt64(); }
                set { this.mTvSecInternal = new IntPtr(value); }
            }

            /// <summary>
            /// Timeval milliseconds property.
            /// </summary>
            public long tv_usec
            {
                get { return this.mTvUSecInternal.ToInt64(); }
                set { this.mTvUSecInternal = new IntPtr(value); }
            }

            /// <summary>
            /// Timeval constructor.
            /// </summary>
            /// <param name="tvSec">seconds</param>
            /// <param name="tvUsec">milliseconds</param>
            public UnixNativeTimeval(long tvSec, long tvUsec)
            {
                this.mTvSecInternal = new IntPtr(tvSec);
                this.mTvUSecInternal = new IntPtr(tvUsec);
            }
        }
        #endregion

        #region Contex
        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public class Context : SafeHandleZeroOrMinusOneIsInvalid
        {
            private string creationStackTrace;

            /// <summary>
            /// Initializes a new instance of the <see cref="Context"/> class.
            /// </summary>
            public Context() :
                    base(true)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Context"/> class, specifying whether the handle is to be reliably released.
            /// </summary>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            public Context(bool ownsHandle) :
                    base(ownsHandle)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Gets a value which represents a pointer or handle that has been initialized to zero.
            /// </summary>
            public static Context Zero
            {
                get
                {
                    return Context.DangerousCreate(IntPtr.Zero);
                }
            }

            /// <summary>
            /// Creates a new <see cref="Context"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            /// <returns>
            /// </returns>
            public static Context DangerousCreate(IntPtr unsafeHandle, bool ownsHandle)
            {
                Context safeHandle = new Context(ownsHandle);
                safeHandle.SetHandle(unsafeHandle);
                return safeHandle;
            }

            /// <summary>
            /// Creates a new <see cref="Context"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <returns>
            /// </returns>
            public static Context DangerousCreate(IntPtr unsafeHandle)
            {
                return Context.DangerousCreate(unsafeHandle, true);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return string.Format("{0} ({1})", this.handle, "Context");
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj != null && obj.GetType() == typeof(Context))
                {
                    return ((Context)obj).handle.Equals(this.handle);
                }
                else
                {
                    return false;
                }
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return this.handle.GetHashCode();
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="Context"/> are equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> equals <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator ==(Context value1, Context value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return true;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return false;
                }

                return value1.handle == value2.handle;
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="Context"/> are not equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> does not equal <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator !=(Context value1, Context value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return false;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return true;
                }

                return value1.handle != value2.handle;
            }

            /// <inheritdoc/>
            protected override bool ReleaseHandle()
            {

                NativeMethods.Exit(this.handle);

                return true;
            }
        }
        #endregion

        #region Device
        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public partial class Device : SafeHandleZeroOrMinusOneIsInvalid
        {
            private string creationStackTrace;

            /// <summary>
            /// Initializes a new instance of the <see cref="Device"/> class.
            /// </summary>
            protected Device() :
                    base(true)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Device"/> class, specifying whether the handle is to be reliably released.
            /// </summary>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            protected Device(bool ownsHandle) :
                    base(ownsHandle)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Gets a value which represents a pointer or handle that has been initialized to zero.
            /// </summary>
            public static Device Zero
            {
                get
                {
                    return Device.DangerousCreate(IntPtr.Zero);
                }
            }

            /// <summary>
            /// Creates a new <see cref="Device"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            /// <returns>
            /// </returns>
            public static Device DangerousCreate(IntPtr unsafeHandle, bool ownsHandle)
            {
                Device safeHandle = new Device(ownsHandle);
                safeHandle.SetHandle(unsafeHandle);
                return safeHandle;
            }

            /// <summary>
            /// Creates a new <see cref="Device"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <returns>
            /// </returns>
            public static Device DangerousCreate(IntPtr unsafeHandle)
            {
                return Device.DangerousCreate(unsafeHandle, true);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return string.Format("{0} ({1})", this.handle, "Device");
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj != null && obj.GetType() == typeof(Device))
                {
                    return ((Device)obj).handle.Equals(this.handle);
                }
                else
                {
                    return false;
                }
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return this.handle.GetHashCode();
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="Device"/> are equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> equals <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator ==(Device value1, Device value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return true;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return false;
                }

                return value1.handle == value2.handle;
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="Device"/> are not equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> does not equal <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator !=(Device value1, Device value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return false;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return true;
                }

                return value1.handle != value2.handle;
            }
            protected override bool ReleaseHandle()
            {
                NativeMethods.UnrefDevice(this.handle);
                return true;
            }
        }
        #endregion

        #region DeviceHandle
        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public partial class DeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private string creationStackTrace;

            /// <summary>
            /// Initializes a new instance of the <see cref="DeviceHandle"/> class.
            /// </summary>
            protected DeviceHandle() :
                    base(true)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DeviceHandle"/> class, specifying whether the handle is to be reliably released.
            /// </summary>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            protected DeviceHandle(bool ownsHandle) :
                    base(ownsHandle)
            {
                this.creationStackTrace = Environment.StackTrace;
            }

            /// <summary>
            /// Gets a value which represents a pointer or handle that has been initialized to zero.
            /// </summary>
            public static DeviceHandle Zero
            {
                get
                {
                    return DeviceHandle.DangerousCreate(IntPtr.Zero);
                }
            }

            /// <summary>
            /// Creates a new <see cref="DeviceHandle"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <param name="ownsHandle">
            /// <see langword="true"/> to reliably release the handle during the finalization phase; <see langword="false"/> to prevent reliable release (not recommended).
            /// </param>
            /// <returns>
            /// </returns>
            public static DeviceHandle DangerousCreate(IntPtr unsafeHandle, bool ownsHandle)
            {
                DeviceHandle safeHandle = new DeviceHandle(ownsHandle);
                safeHandle.SetHandle(unsafeHandle);
                return safeHandle;
            }

            /// <summary>
            /// Creates a new <see cref="DeviceHandle"/> from a <see cref="IntPtr"/>.
            /// </summary>
            /// <param name="unsafeHandle">
            /// The underlying <see cref="IntPtr"/>
            /// </param>
            /// <returns>
            /// </returns>
            public static DeviceHandle DangerousCreate(IntPtr unsafeHandle)
            {
                return DeviceHandle.DangerousCreate(unsafeHandle, true);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return string.Format("{0} ({1})", this.handle, "DeviceHandle");
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                if (obj != null && obj.GetType() == typeof(DeviceHandle))
                {
                    return ((DeviceHandle)obj).handle.Equals(this.handle);
                }
                else
                {
                    return false;
                }
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return this.handle.GetHashCode();
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="DeviceHandle"/> are equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> equals <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator ==(DeviceHandle value1, DeviceHandle value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return true;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return false;
                }

                return value1.handle == value2.handle;
            }

            /// <summary>
            /// Determines whether two specified instances of <see cref="DeviceHandle"/> are not equal.
            /// </summary>
            /// <param name="value1">
            /// The first pointer or handle to compare.
            /// </param>
            /// <param name="value2">
            /// The second pointer or handle to compare.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="value1"/> does not equal <paramref name="value2"/>; otherwise, <see langword="false"/>.
            /// </returns>
            public static bool operator !=(DeviceHandle value1, DeviceHandle value2)
            {
                if (object.Equals(value1, null) && object.Equals(value2, null))
                {
                    return false;
                }

                if (object.Equals(value1, null) || object.Equals(value2, null))
                {
                    return true;
                }

                return value1.handle != value2.handle;
            }
            protected override bool ReleaseHandle()
            {
                NativeMethods.Close(this.handle);
                return true;
            }
        }
        #endregion
    }

}


