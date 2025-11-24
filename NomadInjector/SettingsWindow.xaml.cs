using System.Windows;
using System.Windows.Controls; 
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input; 

namespace NomadInjector
{

    public partial class SettingsWindow : Window
    {
        private InjectionMethod _selectedMethod = GlobalSettings.CurrentMethod;

        public SettingsWindow()
        {
            InitializeComponent();

            if (CmbInjectionMethod != null)
            {
                CmbInjectionMethod.SelectedIndex = (int)GlobalSettings.CurrentMethod;
            }
        }

        private void CmbInjectionMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _selectedMethod = (InjectionMethod)comboBox.SelectedIndex;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            GlobalSettings.CurrentMethod = _selectedMethod;

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnOpenModuleViewer_Click(object sender, RoutedEventArgs e)
        {
            string targetProcessName = "explorer";
            Process targetProcess = Process.GetProcessesByName(targetProcessName).FirstOrDefault();

            if (targetProcess == null)
            {
                MessageBox.Show($"Target process '{targetProcessName}' is not running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var moduleViewer = new ModuleViewer(targetProcess.Id, targetProcess.ProcessName);
            moduleViewer.Show();
        }
    }
}