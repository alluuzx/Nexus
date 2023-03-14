using CefSharp;
using CefSharp.Wpf;
using Nexus.API;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace Nexus.SplashScreen
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        #region vars

        /// <summary>
        /// Is Nexus loaded or not
        /// </summary>
        private bool loaded = false;

        #endregion

        public SplashScreen()
        {
            InitializeComponent();

            //prevent the splash from closing
            Closing += delegate (object? sender, CancelEventArgs e)
            {
                if (!loaded)
                {
                    e.Cancel = true;
                }
            };
        }

        private async void MainBorder_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Loading started.");

            try
            {
                KrnlApi.Initialize();
                Console.WriteLine("KRNL Initialized.");
                Console.WriteLine("Krnl API Version: " + KrnlApi.GetAPIVersion());
                Console.WriteLine("Krnl DLL Version: " + KrnlApi.GetDLLVersion());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to initialize KRNL. Message:");
                Console.WriteLine(ex.Message);
            }

            Progress.IsActive = true;

            await Task.Delay(100);

            if (MainWindow.KrnlUpdated)
            {
                Update.Visibility = Visibility.Visible;
                await Task.Delay(2000);
            }

            for (int i = 0; i <= 100; i++)
            {
                if (i == 100)
                {
                    try
                    {
                        MainWindow main = new();
                        Application.Current.MainWindow = main;

                        //web browser settings (monaco)
                        CefSettings cefSettings = new();
                        cefSettings.SetOffScreenRenderingBestPerformanceArgs();
                        cefSettings.MultiThreadedMessageLoop = true;
                        cefSettings.DisableGpuAcceleration();
                        Cef.EnableHighDPISupport();
                        Cef.Initialize(cefSettings);

                        Console.WriteLine("Initialized Cef");

                        await Task.Delay(650);

                        Hide();

                        loaded = true;
                        Console.WriteLine("Loading complete.");

                        await Task.Delay(400);

                        main.Show();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Fatal error while loading. Message and ST:");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        MessageBox.Show("Error while loading Nexus", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);

                        Application.Current.Shutdown(1);
                    }

                    Close();

                    break;
                }
                await Task.Delay(30);
            }
        }
    }
}