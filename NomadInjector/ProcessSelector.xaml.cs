using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic; 

namespace NomadInjector
{
    public partial class ProcessSelector : Window
    {
        public string SelectedProcessName { get; private set; }

        public ProcessSelector()
        {
            InitializeComponent();
            LoadProcesses();
        }

        private void LoadProcesses()
        {
            try
            {
                var uniqueProcesses = Process.GetProcesses()
                                             .Where(p => p.Id > 0 && p.ProcessName.Length > 0)
                                             .GroupBy(p => p.ProcessName)
                                             .Select(g => g.First())
                                             .OrderBy(p => p.ProcessName)
                                             .ToList();

                LstProcesses.ItemsSource = uniqueProcesses;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Could not load process list: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (LstProcesses.SelectedItem is Process selectedProcess)
            {
                SelectedProcessName = selectedProcess.ProcessName;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a process from the list.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}