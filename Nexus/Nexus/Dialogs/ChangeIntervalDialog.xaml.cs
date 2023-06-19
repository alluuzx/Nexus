using ModernWpf.Controls;
using Nexus.Classes;
using Nexus.Debugging;
using System;
using System.Windows;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for ChangeIntervalDialog.xaml
    /// </summary>
    public partial class ChangeIntervalDialog : ContentDialog
    {
        public ChangeIntervalDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                int value = Convert.ToInt32(IntervalBox.Text);

                if (value == 0)
                {
                    MessageBox.Show("Value can't be 0!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    args.Cancel = true;
                    return;
                }

                if (value > 50000)
                {
                    MessageBox.Show("Value is too big!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    args.Cancel = true;
                    return;
                }

                if (value < 50)
                {
                    MessageBoxResult result = MessageBox.Show("Values lower than 50 may cause lag to your computer! Are you sure you want to continue?", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes)
                    {
                        args.Cancel = true;
                        return;
                    }
                }

                Globals.AutoInjectInterval = value;
                DebugConsole.WriteLine("AutoInject Check Interval set to: " + value);
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Invalid value!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            IntervalBox.Text = Globals.AutoInjectInterval.ToString();
        }
    }
}