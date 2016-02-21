// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Defines the KeyboardWriter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KeyboardMinigames
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;

    public class KeyboardWriter
    {
        #region pInvoke Imports

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetFeature(IntPtr HidDeviceObject, ref Byte lpReportBuffer, int ReportBufferLength);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetupDiGetClassDevs(           // 1st form using a ClassGUID only, with null Enumerator
           ref Guid ClassGuid,
           IntPtr Enumerator,
           IntPtr hwndParent,
           int Flags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern int CM_Get_Device_ID(
           UInt32 dnDevInst,
           IntPtr buffer,
           int bufferLen,
           int flags
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Boolean SetupDiEnumDeviceInterfaces(
           IntPtr hDevInfo,
           ref SP_DEVINFO_DATA devInfo,
           ref Guid interfaceClassGuid,
           UInt32 memberIndex,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr hDevInfo,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
           ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
           UInt32 deviceInterfaceDetailDataSize,
           out UInt32 requiredSize,
           ref SP_DEVINFO_DATA deviceInfoData
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr hDevInfo,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
           ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
           UInt32 deviceInterfaceDetailDataSize,
           IntPtr requiredSize,                     // Allow null
           IntPtr deviceInfoData                    // Allow null
        );

        [DllImport(@"setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Boolean SetupDiGetDeviceInterfaceDetail(
           IntPtr hDevInfo,
           ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
           IntPtr deviceInterfaceDetailData,        // Allow null
           UInt32 deviceInterfaceDetailDataSize,
           out UInt32 requiredSize,
           IntPtr deviceInfoData                    // Allow null
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList
        (
             IntPtr DeviceInfoSet
        );

        #endregion

        #region Types to support pInvoke methods

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid interfaceClassGuid;
            public uint flags;
            private UIntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BUFFER_SIZE)]
            public string DevicePath;
        }

        #endregion

        #region Flags to support pInvoke methods

        const Int64 INVALID_HANDLE_VALUE = -1;

        const int DIGCF_DEFAULT = 0x1;
        const int DIGCF_PRESENT = 0x2;
        const int DIGCF_ALLCLASSES = 0x4;
        const int DIGCF_PROFILE = 0x8;
        const int DIGCF_DEVICEINTERFACE = 0x10;

        // Used for CreateFile
        public const short FILE_ATTRIBUTE_NORMAL = 0x80;
        //public const short INVALID_HANDLE_VALUE = -1;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint CREATE_NEW = 1;
        public const uint CREATE_ALWAYS = 2;
        public const uint OPEN_EXISTING = 3;

        // Used for CreateFile
        public const uint FILE_SHARE_NONE = 0x00;
        public const uint FILE_SHARE_READ = 0x01;
        public const uint FILE_SHARE_WRITE = 0x02;
        public const uint FILE_SHARE_DELETE = 0x04;

        // Used for CreateFile
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        static Guid GUID_DEVINTERFACE_HID = new Guid(0x4D1E55B2, 0xF16F, 0x11CF, 0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);

        const int BUFFER_SIZE = 128;
        const int MAX_DEVICE_ID_LEN = 200;

        #endregion

        private IntPtr _keyboardUsbDevice;

        private byte[,] _ledMatrix = new byte[7, 92];

        private byte[] _redValues = new byte[144];
        private byte[] _greenValues = new byte[144];
        private byte[] _blueValues = new byte[144];

        private byte[][] _dataPacket = new byte[5][]; // 2nd dimension initialized to size 64

        private Random _random;

        public KeyboardWriter()
        {
            InitKeyboard();

            _random = new Random();

            for (int i = 0; i < _dataPacket.Length; i++)
            {
                _dataPacket[i] = new byte[64];
            }
        }

        public void Clear()
        {
            for (int i = 0; i < 144; i++)
            {
                SetLed(i, 0, 255, 255);
            }

            UpdateKeyboard();
        }

        private void SetLed(int x, int y, int r, int g, int b)
        {
            int led = _ledMatrix[y, x];

            if (led >= 144)
            {
                return;
            }

            if (r > 7) r = 7;
            if (g > 7) g = 7;
            if (b > 7) b = 7;

            r = 7 - r;
            g = 7 - g;
            b = 7 - b;

            _redValues[led] = (byte)r;
            _greenValues[led] = (byte)g;
            _blueValues[led] = (byte)b;
        }

        public void SetLed(int pos, int r, int g, int b)
        {
            if (pos > 143) return;

            if (r > 7) r = 7;
            if (g > 7) g = 7;
            if (b > 7) b = 7;

            r = 7 - r;
            g = 7 - g;
            b = 7 - b;

            _redValues[pos] = (byte)r;
            _greenValues[pos] = (byte)g;
            _blueValues[pos] = (byte)b;
        }

        private int InitKeyboard()
        {
            var keyboards = new Dictionary<string, uint[]>{
                {"K95 RGB", new uint[]{0x1B1C, 0x1B11, 0x3}},
                {"K70 RGB", new uint[]{0x1B1C, 0x1B13, 0x3}},
            };

            var names = keyboards.Keys.ToArray();
            Console.WriteLine("Searching for Corsair {0} keyboard...", names[0]);
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                _keyboardUsbDevice = GetDeviceHandle(keyboards[name][0], keyboards[name][1], keyboards[name][2]);
                if (_keyboardUsbDevice != IntPtr.Zero)
                {
                    Console.WriteLine("Corsair {0} detected successfully :)", name);
                    return 0;
                }
                if (i + 1 < names.Length)
                {
                    Console.WriteLine("Couldn't find Corsair {0}... looking for {1} instead", name, names[i + 1]);
                }
            }
            Console.WriteLine("Couldn't find any compatible keyboard, it seems you're out of luck, sorry :(");

            return 1;
        }

        /// <summary>
        /// C Code by http://www.reddit.com/user/chrisgzy
        /// Converted to C# by http://www.reddit.com/user/billism
        /// </summary>
        private IntPtr GetDeviceHandle(uint uiVID, uint uiPID, uint uiMI)
        {
            IntPtr deviceInfo = SetupDiGetClassDevs(ref GUID_DEVINTERFACE_HID, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (deviceInfo.ToInt64() == INVALID_HANDLE_VALUE)
            {
                return IntPtr.Zero;
            }

            IntPtr returnPointer = IntPtr.Zero;

            SP_DEVINFO_DATA deviceData = new SP_DEVINFO_DATA();
            deviceData.cbSize = (uint)Marshal.SizeOf(deviceData);

            for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfo, i, ref deviceData); ++i)
            {
                IntPtr deviceId = Marshal.AllocHGlobal(MAX_DEVICE_ID_LEN); // was wchar_t[] type
                // CM_Get_Device_ID was CM_Get_Device_IDW in C++ code
                if (CM_Get_Device_ID(deviceData.devInst, deviceId, MAX_DEVICE_ID_LEN, 0) != 0)
                {
                    continue;
                }

                if (!IsMatchingDevice(deviceId, uiVID, uiPID, uiMI))
                    continue;

                SP_DEVICE_INTERFACE_DATA interfaceData = new SP_DEVICE_INTERFACE_DATA(); // C code used SP_INTERFACE_DEVICE_DATA
                interfaceData.cbSize = (uint)Marshal.SizeOf(interfaceData);

                if (!SetupDiEnumDeviceInterfaces(deviceInfo, ref deviceData, ref GUID_DEVINTERFACE_HID, 0, ref interfaceData))
                {
                    break;
                }

                uint requiredSize = 0;
                SetupDiGetDeviceInterfaceDetail(deviceInfo, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);
                // var lastError = Marshal.GetLastWin32Error();

                SP_DEVICE_INTERFACE_DETAIL_DATA interfaceDetailData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                if (IntPtr.Size == 8) // for 64 bit operating systems
                {
                    interfaceDetailData.cbSize = 8;
                }
                else
                {
                    interfaceDetailData.cbSize = 4 + (uint)Marshal.SystemDefaultCharSize; // for 32 bit systems
                }

                if (!SetupDiGetDeviceInterfaceDetail(deviceInfo, ref interfaceData, ref interfaceDetailData, requiredSize, IntPtr.Zero, IntPtr.Zero))
                {
                    break;
                }

                var deviceHandle = CreateFile(interfaceDetailData.DevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                if (deviceHandle.ToInt64() == INVALID_HANDLE_VALUE)
                {
                    break;
                }

                returnPointer = deviceHandle;
                break;
            }

            SetupDiDestroyDeviceInfoList(deviceInfo);
            return returnPointer;
        }

        /// <summary>
        /// C Code by http://www.reddit.com/user/chrisgzy
        /// Converted to C# by http://www.reddit.com/user/billism
        /// </summary>
        private bool IsMatchingDevice(IntPtr pDeviceID, uint uiVID, uint uiPID, uint uiMI)
        {
            var deviceString = Marshal.PtrToStringAuto(pDeviceID);
            if (deviceString == null)
            {
                return false;
            }

            bool isMatch = deviceString.Contains(string.Format("VID_{0:X4}", uiVID));
            isMatch &= deviceString.Contains(string.Format("PID_{0:X4}", uiPID));
            isMatch &= deviceString.Contains(string.Format("MI_{0:X2}", uiMI));

            return isMatch;
        }

        public void UpdateKeyboard()
        {
            // Perform USB control message to keyboard
            //
            // Request Type:  0x21
            // Request:       0x09
            // Value          0x0300
            // Index:         0x03
            // Size:          64

            _dataPacket[0][0] = 0x7F;
            _dataPacket[0][1] = 0x01;
            _dataPacket[0][2] = 0x3C;

            _dataPacket[1][0] = 0x7F;
            _dataPacket[1][1] = 0x02;
            _dataPacket[1][2] = 0x3C;

            _dataPacket[2][0] = 0x7F;
            _dataPacket[2][1] = 0x03;
            _dataPacket[2][2] = 0x3C;
            
            _dataPacket[3][0] = 0x7F;
            _dataPacket[3][1] = 0x04;
            _dataPacket[3][2] = 0x24;
            
            _dataPacket[4][0] = 0x07;
            _dataPacket[4][1] = 0x27;
            _dataPacket[4][4] = 0xD8;

            for (int i = 0; i < 60; i++)
            {
                _dataPacket[0][i + 4] = (byte)(_redValues[i * 2 + 1] << 4 | _redValues[i * 2]);
            }

            for (int i = 0; i < 12; i++)
            {
                _dataPacket[1][i + 4] = (byte)(_redValues[i * 2 + 121] << 4 | _redValues[i * 2 + 120]);
            }

            for (int i = 0; i < 48; i++)
            {
                _dataPacket[1][i + 16] = (byte)(_greenValues[i * 2 + 1] << 4 | _greenValues[i * 2]);
            }

            for (int i = 0; i < 24; i++)
            {
                _dataPacket[2][i + 4] = (byte)(_greenValues[i * 2 + 97] << 4 | _greenValues[i * 2 + 96]);
            }

            for (int i = 0; i < 36; i++)
            {
                _dataPacket[2][i + 28] = (byte)(_blueValues[i * 2 + 1] << 4 | _blueValues[i * 2]);
            }

            for (int i = 0; i < 36; i++)
            {
                _dataPacket[3][i + 4] = (byte)(_blueValues[i * 2 + 73] << 4 | _blueValues[i * 2 + 72]);
            }

            SendUsbMessage(_dataPacket[0]);
            System.Threading.Thread.Sleep(10);
            SendUsbMessage(_dataPacket[1]);
            System.Threading.Thread.Sleep(10);
            SendUsbMessage(_dataPacket[2]);
            System.Threading.Thread.Sleep(10);
            SendUsbMessage(_dataPacket[3]);
            System.Threading.Thread.Sleep(10);
            SendUsbMessage(_dataPacket[4]);
            System.Threading.Thread.Sleep(10);
        }

        private void SendUsbMessage(byte[] data_pkt)
        {
            byte[] usb_pkt = new byte[65];
            for (int i = 1; i < 65; i++)
            {
                usb_pkt[i] = data_pkt[i - 1];
            }

            HidD_SetFeature(_keyboardUsbDevice, ref usb_pkt[0], 65);
        }
    }
}
