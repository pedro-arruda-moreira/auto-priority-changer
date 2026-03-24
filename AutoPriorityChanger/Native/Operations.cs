using System;
using System.Runtime.InteropServices;

namespace AutoPriorityChanger.Native
{
    public class Operations
    {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(uint access, bool inherit, int id);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);

            [DllImport("ntdll.dll", PreserveSig = false)]
            public static extern void NtSuspendProcess(IntPtr processHandle);

            [DllImport("ntdll.dll", PreserveSig = false)]
            public static extern void NtResumeProcess(IntPtr processHandle);
    }
}