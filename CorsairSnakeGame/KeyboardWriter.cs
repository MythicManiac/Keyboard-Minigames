// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   Defines the KeyboardWriter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace KeybaordAudio
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;

    public class KeyboardWriter : IWriter
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

        private IntPtr keyboardUsbDevice;

        private byte[] positionMap = new byte[]
        {
            137, 8, 20, 255,
            0, 12, 24, 36, 48, 60, 72, 84, 96, 108, 120, 132,  6, 18, 30, 42, 32, 44, 56, 68, 255,
            1, 13, 25, 37, 49, 61, 73, 85, 97, 109, 121, 133,  7, 31, 54, 66, 78, 80, 92,104, 116, 255,
            2, 14, 26, 38, 50, 62, 74, 86, 98, 110, 122, 134, 90, 102, 43, 55, 67, 9, 21, 33, 128, 255,
            3, 15, 27, 39, 51, 63, 75, 87, 99, 111, 123, 135, 126, 57, 69, 81, 128, 255,
            4, 28, 40, 52, 64, 76, 88, 100, 112, 124, 136, 79,103, 93, 105, 117, 140, 255,
            5, 17, 29, 53, 89, 101, 113, 91,115, 127, 139, 129, 141, 140, 255,
        };

        private float[] sizeMap = new[]
        {
            -15.5f, 1f, 1f, -2.5f, 1f, -2f, 0f,
            1f, -.5f, 1f, 1f, 1f, 1f, -.75f, 1f, 1f, 1f, 1f, -.75f, 1f, 1f, 1f, 1f, -.5f, 1f, 1f, 1f, -.5f, 1f, 1f, 1f, 1f, 0f,
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2f, -.5f, 1f, 1f, 1f, -.5f, 1f, 1f, 1f, 1f, 0f,
            1.5f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.5f, -.5f, 1f, 1f, 1f, -.5f, 1f, 1f, 1f, 1f, 0f,
            1.75f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2.25f, -4f, 1f, 1f, 1f, 1f, 0f,
            2.25f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 2.75f, -1.5f, 1f, -1.5f, 1f, 1f, 1f, 1f, 0f,
            1.5f, 1f, 1.25f, 6.5f, 1.25f, 1f, 1f, 1.5f, -.5f, 1f, 1f, 1f, -.5f, 2f, 1f, 1f, 0f,
        };

        private byte[,] ledMatrix = new byte[7, 92];

        private byte[] redValues = new byte[144];
        private byte[] greenValues = new byte[144];
        private byte[] blueValues = new byte[144];

        private byte[][] dataPacket = new byte[5][]; // 2nd dimension initialized to size 64

        byte red, grn, blu;

        private Random rand;

        public KeyboardWriter()
        {
            InitKeyboard();

            rand = new Random();

            for (int i = 0; i < dataPacket.Length; i++)
            {
                this.dataPacket[i] = new byte[64];
            }
        }

        public void Write()
        {
            var colors = new int[7][];
            colors[0] = new int[] { 255, 0, 0 };
            colors[1] = new int[] { 0, 255, 0 };
            colors[2] = new int[] { 0, 0, 255 };
            colors[3] = new int[] { 255, 255, 0 };
            colors[4] = new int[] { 0, 255, 255 };
            colors[5] = new int[] { 255, 0, 255 };
            colors[6] = new int[] { 255, 255, 255 };

            for (int i = 0; i < 91; i++)
                for (int j = 0; j < 7; j++)
                {
                    var index = rand.Next(7);
                    SetLed(i, j, colors[index][0], colors[index][1], colors[index][2]);
                }

            UpdateKeyboard();
        }

        public void Clear()
        {
            for (int i = 0; i < 144; i++)
            {
                SetLed(i, 0, 255, 255);
            }

            UpdateKeyboard();
        }

        public void Write(int iter, byte[] fftData)
        {
            // Rainbow to key lights
            for (int x = 0; x < 91; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    this.red = (byte)(1.5f * (Math.Sin((x / 92.0f) * 2 * 3.14f) + 1));
                    this.grn = (byte)(1.5f * (Math.Sin(((x / 92.0f) * 2 * 3.14f) - (6.28f / 3)) + 1));
                    this.blu = (byte)(1.5f * (Math.Sin(((x / 92.0f) * 2 * 3.14f) + (6.28f / 3)) + 1));

                    this.SetLed((x + iter) % 92, y, red, grn, blu);
                }
            }

            //// FFT Data to key lights
            //for (int i = 0; i < 91; i++)
            //{
            //    for (int k = 0; k < 7; k++)
            //    {
            //        if (fftData[(int)(i * 2.1 + 1)] > (255 / (15 + (i * 0.8))) * (7 - k))
            //        {
            //            this.SetLed(i, k, 0x07, 0x07, 0x07);
            //        }
            //    }
            //}

            UpdateKeyboard();
        }

        private void SetLed(int x, int y, int r, int g, int b)
        {
            int led = this.ledMatrix[y, x];

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

            this.redValues[led] = (byte)r;
            this.greenValues[led] = (byte)g;
            this.blueValues[led] = (byte)b;
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

            this.redValues[pos] = (byte)r;
            this.greenValues[pos] = (byte)g;
            this.blueValues[pos] = (byte)b;
        }

        private int InitKeyboard()
        {
            Console.WriteLine("Searching for Corsair K70 RGB keyboard...");

            this.keyboardUsbDevice = this.GetDeviceHandle(0x1B1C, 0x1B13, 0x3);

            if (this.keyboardUsbDevice == IntPtr.Zero)
            {
                Console.WriteLine("Corsair K70 RGB keyboard not detected...but it is ok, maybe you have a K95?");

                this.keyboardUsbDevice = this.GetDeviceHandle(0x1B1C, 0x1B11, 0x3);
                if (this.keyboardUsbDevice == IntPtr.Zero)
                {
                    Console.WriteLine("Nope, no K95 either...sorry you live that way.");
                    return 1;
                }
            }

            Console.WriteLine("Corsair K70 or K95 RGB keyboard detected successfully :)");

            // Construct XY lookup table
            var keys = this.positionMap.GetEnumerator();
            keys.MoveNext();
            var sizes = this.sizeMap.GetEnumerator();
            sizes.MoveNext();

            for (int y = 0; y < 7; y++)
            {
                byte key = 0x00;
                int size = 0;

                for (int x = 0; x < 92; x++)
                {
                    if (size == 0)
                    {
                        float sizef = (float)sizes.Current;
                        sizes.MoveNext();
                        if (sizef < 0)
                        {
                            size = (int)(-sizef * 4);
                            key = 255;
                        }
                        else
                        {
                            key = (byte)keys.Current;
                            keys.MoveNext();
                            size = (int)(sizef * 4);
                        }
                    }

                    ledMatrix[y, x] = key;
                    size--;
                }

                if ((byte)keys.Current != 255 || (float)sizes.Current != 0f)
                {
                    Console.WriteLine("Bad line {0}", y);
                    return 1;
                }

                keys.MoveNext();
                sizes.MoveNext();
            }

            return 0;
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

            this.dataPacket[0][0] = 0x7F;
            this.dataPacket[0][1] = 0x01;
            this.dataPacket[0][2] = 0x3C;

            this.dataPacket[1][0] = 0x7F;
            this.dataPacket[1][1] = 0x02;
            this.dataPacket[1][2] = 0x3C;

            this.dataPacket[2][0] = 0x7F;
            this.dataPacket[2][1] = 0x03;
            this.dataPacket[2][2] = 0x3C;
            
            this.dataPacket[3][0] = 0x7F;
            this.dataPacket[3][1] = 0x04;
            this.dataPacket[3][2] = 0x24;
            
            this.dataPacket[4][0] = 0x07;
            this.dataPacket[4][1] = 0x27;
            this.dataPacket[4][4] = 0xD8;

            for (int i = 0; i < 60; i++)
            {
                this.dataPacket[0][i + 4] = (byte)(this.redValues[i * 2 + 1] << 4 | this.redValues[i * 2]);
            }

            for (int i = 0; i < 12; i++)
            {
                this.dataPacket[1][i + 4] = (byte)(this.redValues[i * 2 + 121] << 4 | this.redValues[i * 2 + 120]);
            }

            for (int i = 0; i < 48; i++)
            {
                this.dataPacket[1][i + 16] = (byte)(this.greenValues[i * 2 + 1] << 4 | this.greenValues[i * 2]);
            }

            for (int i = 0; i < 24; i++)
            {
                this.dataPacket[2][i + 4] = (byte)(this.greenValues[i * 2 + 97] << 4 | this.greenValues[i * 2 + 96]);
            }

            for (int i = 0; i < 36; i++)
            {
                this.dataPacket[2][i + 28] = (byte)(this.blueValues[i * 2 + 1] << 4 | this.blueValues[i * 2]);
            }

            for (int i = 0; i < 36; i++)
            {
                this.dataPacket[3][i + 4] = (byte)(this.blueValues[i * 2 + 73] << 4 | this.blueValues[i * 2 + 72]);
            }

            this.SendUsbMessage(dataPacket[0]);
            System.Threading.Thread.Sleep(10);
            this.SendUsbMessage(dataPacket[1]);
            System.Threading.Thread.Sleep(10);
            this.SendUsbMessage(dataPacket[2]);
            System.Threading.Thread.Sleep(10);
            this.SendUsbMessage(dataPacket[3]);
            System.Threading.Thread.Sleep(10);
            this.SendUsbMessage(dataPacket[4]);
            System.Threading.Thread.Sleep(10);
        }

        private void SendUsbMessage(byte[] data_pkt)
        {
            byte[] usb_pkt = new byte[65];
            for (int i = 1; i < 65; i++)
            {
                usb_pkt[i] = data_pkt[i - 1];
            }

            HidD_SetFeature(this.keyboardUsbDevice, ref usb_pkt[0], 65);
        }
    }
}
