using Nexus.Classes;
using Nexus.Debugging;
using System;
using System.Windows;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for ChangeFPSDialog.xaml
    /// </summary>
    public partial class ChangeFPSDialog : ModernWpf.Controls.ContentDialog
    {
        public ChangeFPSDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            try
            {
                int fps = Convert.ToInt32(FPSBox.Text);
                Globals.UnlockedFPS = fps;
                DebugConsole.WriteLine("FPS set to: " + fps);
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Value must be a valid number!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            FPSBox.Text = Globals.UnlockedFPS.ToString();
        }
    }
}