using Nexus.Classes;
using Nexus.Debugging;
using Nexus.Properties;
using System;
using System.Windows;
using System.Windows.Media;

namespace Nexus.Dialogs
{
    /// <summary>
    /// Interaction logic for ThemeDialog.xaml
    /// </summary>
    public partial class ThemeDialog : Window
    {
        public ThemeDialog()
        {
            InitializeComponent();
        }

        private void CloseTitleBar_Click(object sender, RoutedEventArgs e)
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

        public void SetColors(Brush darkBrush, Color darkColor)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Application.Current.Resources["ThemeMainBrush"] = darkBrush;
                    Application.Current.Resources["ThemeMainColor"] = darkColor;

                    ModernWpf.ThemeManager.Current.AccentColor = darkColor;
                    MainBorder.BorderBrush = darkBrush;
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine(ex.Message);
                    MessageBox.Show("Error while setting theme!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        public void SetDefaultBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(Brushes.DeepSkyBlue, Colors.DeepSkyBlue);
            Settings.Default.ThemeColorId = 0;
        }

        public void SetDarkBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(Brushes.Black, Colors.Black);
            Settings.Default.ThemeColorId = 1;
        }

        public void SetOldBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(OldBrush, Colors.Orange);
            Settings.Default.ThemeColorId = 2;
        }

        public void SetSpaceBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(SpaceBrush, Colors.Purple);
            Settings.Default.ThemeColorId = 3;
        }

        public void SetSeaBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(SeaBrush, Colors.DarkBlue);
            Settings.Default.ThemeColorId = 4;
        }

        public void SetNatureBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(NatureBrush, Colors.DarkGreen);
            Settings.Default.ThemeColorId = 5;
        }

        public void SetRedBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(Brushes.Red, Colors.Red);
            Settings.Default.ThemeColorId = 6;
        }

        public void SetPastelBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(PastelBrush, Colors.DarkTurquoise);
            Settings.Default.ThemeColorId = 7;
        }

        public void SetAmberBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(AmberBrush, Colors.DarkOrange);
            Settings.Default.ThemeColorId = 8;
        }

        public void SetPurpleBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(Brushes.Indigo, Colors.Indigo);
            Settings.Default.ThemeColorId = 9;
        }

        public void SetNightDayBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(NightDayBrush, Colors.LightCyan);
            Settings.Default.ThemeColorId = 10;
        }

        public void SetRainbowBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(RainbowBrush, Colors.Yellow);
            Settings.Default.ThemeColorId = 11;
        }

        public void SetWinterBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(WinterBrush, Colors.MediumPurple);
            Settings.Default.ThemeColorId = 12;
        }

        public void SetDarkNightBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(DarkNightBrush, Colors.Purple);
            Settings.Default.ThemeColorId = 13;
        }

        public void SetIceBtn_Click(object sender, RoutedEventArgs e)
        {
            SetColors(IceBrush, Colors.LightBlue);
            Settings.Default.ThemeColorId = 14;
        }

        public void SetCustomBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog dialog = new();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Color color = new()
                {
                    R = dialog.Color.R,
                    G = dialog.Color.G,
                    B = dialog.Color.B,
                    A = dialog.Color.A
                };
                CustomBG.Background = new SolidColorBrush(color);
                SetColors(new SolidColorBrush(color), color);
                Settings.Default.CustomThemeColor = dialog.Color;
                Settings.Default.ThemeColorId = 15;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Color color = new()
            {
                R = Settings.Default.CustomThemeColor.R,
                G = Settings.Default.CustomThemeColor.G,
                B = Settings.Default.CustomThemeColor.B,
                A = Settings.Default.CustomThemeColor.A
            };
            CustomBG.Background = new SolidColorBrush(color);
        }
    }
}