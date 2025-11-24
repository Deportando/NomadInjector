using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Runtime.InteropServices; 

namespace NomadInjector
{
    public partial class ModuleViewer : Window
    {
        private readonly int _processId;
        private readonly string _processName;

        public ModuleViewer(int processId, string processName)
        {
            InitializeComponent();
            _processId = processId;
            _processName = processName;

            TxtTargetProcess.Text = $"{processName} (PID: {processId})";
            LoadModuleList();
        }

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

        private void LoadModuleList()
        {
            var modules = new List<ModuleInfo>();
            IntPtr hSnapshot = IntPtr.Zero;

            try
            {
                hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, _processId);

                if (hSnapshot == IntPtr.Zero || hSnapshot == (IntPtr)(-1))
                {
                    MessageBox.Show("Failed to get module snapshot. Check process integrity or permissions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
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
            }
            finally
            {
                if (hSnapshot != IntPtr.Zero && hSnapshot != (IntPtr)(-1))
                {
                    CloseHandle(hSnapshot); 
                }
            }

            LstModules.ItemsSource = new ObservableCollection<ModuleInfo>(modules.OrderBy(m => m.ModuleName));
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadModuleList();
        }
    }
}