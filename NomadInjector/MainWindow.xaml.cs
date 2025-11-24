using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using System.Text.Json;

namespace NomadInjector
{
    public partial class MainWindow : Window
    {
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint WAIT_OBJECT_0 = 0x00000000;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        private const string DllListFileName = "NomadDllList.txt"; 

        private ObservableCollection<DLLItem> _dlls = new ObservableCollection<DLLItem>();

        public MainWindow()
        {
            InitializeComponent();
            LstDLLs.ItemsSource = _dlls;
            TxtProcessName.Text = "explorer";

            LoadDllList();

            this.Closing += MainWindow_Closing;
        }

        private const string SettingsFileName = "NomadSettings.json";

        private void SaveSettings()
        {
            try
            {
                // 1. Create a dictionary to easily serialize settings
                var settings = new Dictionary<string, string>
        {
            // Convert the enum value to a string for saving
            { "Method", GlobalSettings.CurrentMethod.ToString() }, 
            // Add other settings here (e.g., CheckBox states)
            { "Timeout", GlobalSettings.ThreadTimeout.ToString() }
        };

                // 2. Serialize the dictionary to a pretty JSON string
                string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

                // 3. Write the JSON string to the file
                System.IO.File.WriteAllText(SettingsFileName, jsonString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save global settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            if (!System.IO.File.Exists(SettingsFileName))
                return;

            try
            {
                string jsonString = System.IO.File.ReadAllText(SettingsFileName);

                // Deserialize using a simple type structure to avoid 'dynamic' issues
                var savedSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                if (savedSettings != null && savedSettings.TryGetValue("Method", out string methodString))
                {
                    // Parse the string back into the InjectionMethod enum and update the global setting
                    if (Enum.TryParse(methodString, out InjectionMethod loadedMethod))
                    {
                        GlobalSettings.CurrentMethod = loadedMethod;
                    }
                }
            }
            catch (Exception ex)
            {
                // Warn the user if the settings file is corrupt but don't crash
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error Loading Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadDllList()
        {
            if (!File.Exists(DllListFileName))
                return;

            try
            {
                var savedPaths = File.ReadAllLines(DllListFileName);

                _dlls.Clear();
                foreach (var path in savedPaths)
                {
                    if (File.Exists(path))
                    {
                        _dlls.Add(new DLLItem(path, isSelected: true));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load DLL list file: {ex.Message}", "Error Loading List", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveDllList()
        {
            var pathsToSave = _dlls.Select(d => d.FullPath).ToList();

            try
            {
                File.WriteAllLines(DllListFileName, pathsToSave);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save DLL list: {ex.Message}");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDllList();
        }

        private void BtnSelectProcess_Click(object sender, RoutedEventArgs e)
        {
            var selector = new ProcessSelector();
            if (selector.ShowDialog() == true)
            {
                TxtProcessName.Text = selector.SelectedProcessName;
            }
        }

        private void BtnAddDLL_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dynamic Link Library (*.dll)|*.dll";

            if (openFileDialog.ShowDialog() == true)
            {
                if (!_dlls.Any(d => d.FullPath == openFileDialog.FileName))
                {
                    _dlls.Add(new DLLItem(openFileDialog.FileName));
                }
            }
        }

        private void BtnRemoveDLL_Click(object sender, RoutedEventArgs e)
        {
            if (LstDLLs.SelectedItem is DLLItem selectedItem)
            {
                _dlls.Remove(selectedItem);
            }
        }

        private void BtnClearDLLs_Click(object sender, RoutedEventArgs e)
        {
            _dlls.Clear();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error opening Settings Window: {ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnInject_Click(object sender, RoutedEventArgs e)
        {
            string processName = TxtProcessName.Text.Trim();

            var dllsToInject = _dlls.Where(d => d.IsSelected).ToList();

            if (!dllsToInject.Any())
            {
                MessageBox.Show("No DLLs selected for injection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process[] targetProcesses = Process.GetProcessesByName(processName);
            if (targetProcesses.Length == 0)
            {
                MessageBox.Show($"Process '{processName}' not found. Is it running?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int processId = targetProcesses[0].Id;

            IntPtr kernel32 = GetModuleHandle("kernel32.dll");
            if (kernel32 == IntPtr.Zero)
            {
                MessageBox.Show("Failed to get handle for kernel32.dll.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IntPtr loadLibraryAddr = GetProcAddress(kernel32, "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                MessageBox.Show("Failed to get address for LoadLibraryA.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int successfulInjections = 0;

            foreach (var dllItem in dllsToInject)
            {
                if (InjectDLL(processId, dllItem.FullPath, loadLibraryAddr))
                {
                    successfulInjections++;
                }
                else
                {
                    MessageBox.Show($"Failed to inject {dllItem.DisplayName}. Last error code: {Marshal.GetLastWin32Error()}", "Injection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            MessageBox.Show($"Injection complete. {successfulInjections}/{dllsToInject.Count} DLLs injected successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var creditsWindow = new CreditsWindow();
            creditsWindow.Owner = this;
            creditsWindow.ShowDialog();
        }

        private bool InjectDLL(int processId, string dllPath, IntPtr loadLibraryAddr)
        {
            IntPtr hProcess = IntPtr.Zero;
            IntPtr remoteMemory = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            bool success = false;

            try
            {
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                if (hProcess == IntPtr.Zero) return false;

                byte[] pathBytes = Encoding.ASCII.GetBytes(dllPath + "\0");
                uint pathSize = (uint)pathBytes.Length;

                remoteMemory = VirtualAllocEx(hProcess, IntPtr.Zero, pathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (remoteMemory == IntPtr.Zero) return false;

                IntPtr bytesWritten;
                if (!WriteProcessMemory(hProcess, remoteMemory, pathBytes, pathSize, out bytesWritten)) return false;

                IntPtr threadId;
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, remoteMemory, 0, out threadId);
                if (hThread == IntPtr.Zero) return false;

                WaitForSingleObject(hThread, 5000);

                success = true;
            }
            finally
            {
                if (hThread != IntPtr.Zero) CloseHandle(hThread);
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
            }

            return success;
        }
    }
}