using System.Windows;
using System.Windows.Controls; // Necesario para ComboBox
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input; // Para eventos de mouse, si se usan

namespace NomadInjector
{
    // Asumimos que GlobalSettings y InjectionMethod están definidos una única vez en InjectionSettings.cs

    public partial class SettingsWindow : Window
    {
        // Campo privado que guarda la selección temporal del usuario.
        private InjectionMethod _selectedMethod = GlobalSettings.CurrentMethod;

        // El constructor ÚNICO
        public SettingsWindow()
        {
            InitializeComponent();

            // Establece el ComboBox al valor actual guardado al cargar la ventana.
            if (CmbInjectionMethod != null)
            {
                CmbInjectionMethod.SelectedIndex = (int)GlobalSettings.CurrentMethod;
            }

            // Inicializar CheckBoxes y otros controles aquí
            // Ejemplo: ChkEraseHeaders.IsChecked = GlobalSettings.ErasePEHeaders;
        }

        // --- MANEJADORES DE EVENTOS ---

        private void CmbInjectionMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _selectedMethod = (InjectionMethod)comboBox.SelectedIndex;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // 1. Persistencia: Guarda la selección final del ComboBox en la configuración global
            GlobalSettings.CurrentMethod = _selectedMethod;

            // 2. Aquí iría la lógica para guardar el estado de los CheckBoxes y el TextBox TxtTimeout.

            // 3. Cierra la ventana y señala el éxito
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Cierra la ventana sin guardar cambios
            this.DialogResult = false;
            this.Close();
        }

        private void BtnOpenModuleViewer_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir el Visor de Módulos
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