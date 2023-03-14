using System.Windows.Controls;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : ModernWpf.Controls.ContentDialog
    {
        private readonly TabItem? item;

        public RenameDialog(TabItem item)
        {
            InitializeComponent();
            this.item = item;
        }

        private void ContentDialog_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (item != null)
            {
                RenameBox.Text = item.Header as string;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ModernWpf.Controls.ContentDialog sender, ModernWpf.Controls.ContentDialogButtonClickEventArgs args)
        {
            if (item != null)
            {
                item.Header = RenameBox.Text;
            }
        }
    }
}