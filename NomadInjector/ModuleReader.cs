using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Linq;

public static class ModuleReader
{

    private const uint TH32CS_SNAPMODULE = 0x00000008;
    private const uint TH32CS_SNAPMODULE32 = 0x00000010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MODULEENTRY32
    {
        public uint dwSize;
        public uint th32ModuleID;
        public uint th32ProcessID;
        public uint GlblcntUsage;
        public uint ProccntUsage;
        public IntPtr modBaseAddr;
        public uint modBaseSize;
        public IntPtr hModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExePath;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, int th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int CloseHandle(IntPtr hSnapshot);

    public static List<ModuleInfo> GetModules(int processId)
    {
        List<ModuleInfo> modules = new List<ModuleInfo>();

        IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);

        if (hSnapshot == IntPtr.Zero || hSnapshot == (IntPtr)(-1))
        {
            return modules;
        }

        MODULEENTRY32 me32 = new MODULEENTRY32();
        me32.dwSize = (uint)Marshal.SizeOf(me32); 

        if (Module32First(hSnapshot, ref me32))
        {
            do
            {
                modules.Add(new ModuleInfo(
                    me32.szModule,
                    me32.modBaseAddr,
                    (int)me32.modBaseSize,
                    me32.szExePath
                ));
            } while (Module32Next(hSnapshot, ref me32)); 
        }

        CloseHandle(hSnapshot); 
        return modules.OrderBy(m => m.ModuleName).ToList(); 
    }
}