using Nexus.Debugging;
using System;
using System.IO;
using System.Windows;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for NewFolderDialog.xaml
    /// </summary>
    public partial class NewFolderDialog : ModernWpf.Controls.ContentDialog
    {
        public NewFolderDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            try
            {
                Directory.CreateDirectory("Scripts\\" + NameBox.Text);
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Error while creating folder!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}