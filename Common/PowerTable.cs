using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ZenStates;

namespace ZenTimings.Utils
{
    public class PowerTable : INotifyPropertyChanged
    {
        public const uint tableSize = 0x7E4;
        private uint[] table = new uint[tableSize];

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, PropertyChangedEventArgs args)
        {
            if (Equals(storage, value) || value == null) return false;
            storage = value;
            OnPropertyChanged(args);
            return true;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableAPU0
        {
            [FieldOffset(0x104)] public uint VddcrSoc;
            [FieldOffset(0x298)] public uint Fclk;
            [FieldOffset(0x2C8)] public uint Uclk;
            [FieldOffset(0x2CC)] public uint Mclk;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableAPU1
        {
            [FieldOffset(0x144)] public uint Fclk;
            [FieldOffset(0x154)] public uint Uclk;
            [FieldOffset(0x164)] public uint Mclk;
            [FieldOffset(0x198)] public uint VddcrSoc;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableCPU0
        {
            [FieldOffset(0x044)] public uint CldoVddp;
            [FieldOffset(0x068)] public uint VddcrSoc;
            [FieldOffset(0x084)] public uint Fclk;
            [FieldOffset(0x084)] public uint Uclk;
            [FieldOffset(0x084)] public uint Mclk;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableCPU1
        {
            [FieldOffset(0x03C)] public uint CldoVddp;
            [FieldOffset(0x060)] public uint VddcrSoc;
            [FieldOffset(0x084)] public uint Fclk;
            [FieldOffset(0x084)] public uint Uclk;
            [FieldOffset(0x084)] public uint Mclk;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableCPU2
        {
            [FieldOffset(0x0B4)] public uint VddcrSoc;
            [FieldOffset(0x0C0)] public uint Fclk;
            [FieldOffset(0x0C8)] public uint Uclk;
            [FieldOffset(0x0CC)] public uint Mclk;
            [FieldOffset(0x1F4)] public uint CldoVddp;
            [FieldOffset(0x1F8)] public uint CldoVddgIod;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        private struct PowerTableCPU3
        {
            [FieldOffset(0x0B4)] public uint VddcrSoc;
            [FieldOffset(0x0C0)] public uint Fclk;
            [FieldOffset(0x0C8)] public uint Uclk;
            [FieldOffset(0x0CC)] public uint Mclk;
            [FieldOffset(0x224)] public uint CldoVddp;
            [FieldOffset(0x228)] public uint CldoVddgIod;
            [FieldOffset(0x22C)] public uint CldoVddgCcd;
        };

        public PowerTable(SMU.SmuType smutype) => SmuType = smutype;

        private void ParseTable(uint[] pt)
        {
            if (pt == null)
                return;

            GCHandle handle = GCHandle.Alloc(pt, GCHandleType.Pinned);
            try
            {
                dynamic powerTable = null;
                byte[] bytes = new byte[4];

                switch (SmuType)
                {
                    case SMU.SmuType.TYPE_CPU0:
                        powerTable = (PowerTableCPU0)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableCPU0));
                        break;

                    case SMU.SmuType.TYPE_CPU1:
                        powerTable = (PowerTableCPU1)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableCPU1));
                        break;

                    case SMU.SmuType.TYPE_CPU2:
                        powerTable = (PowerTableCPU2)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableCPU2));
                        break;

                    case SMU.SmuType.TYPE_CPU3:
                        powerTable = (PowerTableCPU3)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableCPU3));
                        break;

                    case SMU.SmuType.TYPE_APU0:
                        powerTable = (PowerTableAPU0)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableAPU0));
                        break;

                    case SMU.SmuType.TYPE_APU1:
                        powerTable = (PowerTableAPU1)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PowerTableAPU1));
                        break;

                    default:
                        return;
                }

                float bclkCorrection = 1.00f;

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.Mclk);
                    float mclkFreq = BitConverter.ToSingle(bytes, 0);

                    // Compensate for lack of BCLK detection, based on configuredClockSpeed
                    float dramFreq = ConfiguredClockSpeed / 2;
                    //if ((dramFreq + 1) / mclkFreq > 1 && dramFreq % mclkFreq > 1)
                    bclkCorrection = dramFreq / mclkFreq;

                    MCLK = mclkFreq * bclkCorrection;
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.Fclk);
                    float fclkFreq = BitConverter.ToSingle(bytes, 0);
                    FCLK = fclkFreq * bclkCorrection;
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.Uclk);
                    float uclkFreq = BitConverter.ToSingle(bytes, 0);
                    UCLK = uclkFreq * bclkCorrection;
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.VddcrSoc);
                    VDDCR_SOC = BitConverter.ToSingle(bytes, 0);
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.CldoVddp);
                    CLDO_VDDP = BitConverter.ToSingle(bytes, 0);
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.CldoVddgIod);
                    CLDO_VDDG_IOD = BitConverter.ToSingle(bytes, 0);
                }
                catch { }

                try
                {
                    bytes = BitConverter.GetBytes(powerTable.CldoVddgCcd);
                    CLDO_VDDG_CCD = BitConverter.ToSingle(bytes, 0);
                }
                catch { }
            }
            finally
            {
                handle.Free();
            }
        }

        public SMU.SmuType SmuType { get; protected set; }
        public uint[] Table
        {
            get => table;
            set
            {
                if (value != null)
                {
                    table = value;
                    ParseTable(value);
                }
            }
        }

        float fclk;
        public float FCLK
        {
            get => fclk;
            set => SetProperty(ref fclk, value, InternalEventArgsCache.FCLK);
        }

        float mclk;
        public float MCLK
        {
            get => mclk;
            set => SetProperty(ref mclk, value, InternalEventArgsCache.MCLK);
        }

        float uclk;
        public float UCLK
        {
            get => uclk;
            set => SetProperty(ref uclk, value, InternalEventArgsCache.UCLK);
        }

        float vddcr_soc;
        public float VDDCR_SOC
        {
            get => vddcr_soc;
            set => SetProperty(ref vddcr_soc, value, InternalEventArgsCache.VDDCR_SOC);
        }

        float cldo_vddp;
        public float CLDO_VDDP
        {
            get => cldo_vddp;
            set => SetProperty(ref cldo_vddp, value, InternalEventArgsCache.CLDO_VDDP);
        }

        float cldo_vddg_iod;
        public float CLDO_VDDG_IOD
        {
            get => cldo_vddg_iod;
            set => SetProperty(ref cldo_vddg_iod, value, InternalEventArgsCache.CLDO_VDDG_IOD);
        }

        float cldo_vddg_ccd;
        public float CLDO_VDDG_CCD
        {
            get => cldo_vddg_ccd;
            set => SetProperty(ref cldo_vddg_ccd, value, InternalEventArgsCache.CLDO_VDDG_CCD);
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            PropertyChanged?.Invoke(this, eventArgs);
        }

        public float ConfiguredClockSpeed { get; set; }
    }

    internal static class InternalEventArgsCache
    {
        internal static PropertyChangedEventArgs FCLK = new PropertyChangedEventArgs("FCLK");
        internal static PropertyChangedEventArgs MCLK = new PropertyChangedEventArgs("MCLK");
        internal static PropertyChangedEventArgs UCLK = new PropertyChangedEventArgs("UCLK");

        internal static PropertyChangedEventArgs VDDCR_SOC = new PropertyChangedEventArgs("VDDCR_SOC");
        internal static PropertyChangedEventArgs CLDO_VDDP = new PropertyChangedEventArgs("CLDO_VDDP");
        internal static PropertyChangedEventArgs CLDO_VDDG_IOD = new PropertyChangedEventArgs("CLDO_VDDG_IOD");
        internal static PropertyChangedEventArgs CLDO_VDDG_CCD = new PropertyChangedEventArgs("CLDO_VDDG_CCD");
    }
}
