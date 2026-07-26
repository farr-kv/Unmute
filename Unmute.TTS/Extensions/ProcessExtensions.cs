using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmute.TTS.Extensions
{
    // Clanker code to use job objects to auto close any child processes
    internal static class ProcessExtensions
    {
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        private static readonly IntPtr Handle;
        enum JobObjectInfoType
        {
            ExtendedLimitInformation = 9
        }

        static ProcessExtensions() {
            Handle = CreateJobObject(IntPtr.Zero, null);
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            var length = Marshal.SizeOf(info);
            var ptr = Marshal.AllocHGlobal(length);

            try
            {
                Marshal.StructureToPtr(info, ptr, false);

                if (!SetInformationJobObject(Handle, JobObjectInfoType.ExtendedLimitInformation, ptr, (uint)length))
                    throw new System.ComponentModel.Win32Exception();
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static void AttachToParent(this Process process)
        {
            if (!AssignProcessToJobObject(Handle, process.Handle))
                throw new System.ComponentModel.Win32Exception();
        }        

        [StructLayout(LayoutKind.Sequential)]
        struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetInformationJobObject(
            IntPtr hJob,
            JobObjectInfoType infoType,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(
            IntPtr job,
            IntPtr process);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);
    }
}
