using Nexus.API;
using Nexus.Classes;
using Nexus.Debugging;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void CheckUpdatesBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                try
                {
                    CheckUpdatesBtn.Content = "Checking...";
                    CheckStatus.IsActive = true;

                    KrnlApi.Initialize();

                    await Task.Delay(4000);

                    DllVer.Content = "KRNL Version: " + KrnlApi.GetDLLVersion();
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while updating. Message:");
                    DebugConsole.WriteLine(ex.Message);
                    MessageBox.Show("Error while updating!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    CheckUpdatesBtn.Content = "Check for KRNL updates";
                    CheckStatus.IsActive = false;
                }
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                NexusVer.Content = "Nexus Version: " + Globals.NexusVERSION;
                DllVer.Content = "KRNL Version: " + KrnlApi.GetDLLVersion();
                ApiVer.Content = "API Version: " + KrnlApi.GetAPIVersion();
            });
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void TitleBar_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SystemCommands.ShowSystemMenu(this, Utils.GetCursorPosition());
        }
    }
}