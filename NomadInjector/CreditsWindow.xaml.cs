using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace NomadInjector
{
    public partial class CreditsWindow : Window
    {
        public CreditsWindow()
        {
            InitializeComponent();
        }

        // Method to handle the hyperlink click
        private void Hyperlink_Click(object sender, MouseButtonEventArgs e)
        {
            // Opens the specified URL in the default browser
            Process.Start(new ProcessStartInfo("https://github.com/Deportando/NomadInjector") { UseShellExecute = true });
        }
    }
}