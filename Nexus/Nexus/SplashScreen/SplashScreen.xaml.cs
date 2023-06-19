using CefSharp;
using CefSharp.Wpf;
using Nexus.API;
using Nexus.Classes;
using Nexus.Debugging;
using System;
using System.ComponentModel;
using System.IO;
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
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while loading the splash. Message and ST:");
                DebugConsole.WriteLine(ex.Message);
                DebugConsole.WriteLine(ex.StackTrace);

                if (!DebugConsole.IsConsoleInitialized)
                {
                    File.WriteAllText("error.log", $"Message: {ex.Message}\nInner Exception: {ex.InnerException}\nStacktrace: {ex.StackTrace}\nConsole Output:\n{DebugConsole.ConsoleOutput}");
                    MessageBox.Show("Error while loading Nexus! Please reinstall. Closing...", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                }
            }

            //prevent the splash from closing
            Closing += delegate (object? sender, CancelEventArgs e)
            {
                if (!loaded)
                {
                    e.Cancel = true;
                }
            };
        }

        private void MainBorder_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                DebugConsole.WriteLine("Loading started.");

                try
                {
                    KrnlApi.Initialize();
                    DebugConsole.WriteLine("KRNL Initialized.");
                    DebugConsole.WriteLine("Krnl API Version: " + KrnlApi.GetAPIVersion());
                    DebugConsole.WriteLine("Krnl DLL Version: " + KrnlApi.GetDLLVersion());
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Failed to initialize KRNL. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }

                Progress.IsActive = true;

                await Task.Delay(100);

                if (Globals.KrnlUpdated)
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
                            MainWindow main = new(true);
                            Application.Current.MainWindow = main;

                            //web browser settings (monaco)
                            CefSettings cefSettings = new();
                            cefSettings.SetOffScreenRenderingBestPerformanceArgs();
                            cefSettings.MultiThreadedMessageLoop = true;
                            cefSettings.DisableGpuAcceleration();
                            Cef.Initialize(cefSettings);

                            DebugConsole.WriteLine("Initialized Cef");

                            await Task.Delay(650);

                            Hide();

                            loaded = true;
                            DebugConsole.WriteLine("Loading complete.");

                            await Task.Delay(400);

                            main.Show();
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.WriteLine("Fatal error while loading. Message and ST:");
                            DebugConsole.WriteLine(ex.Message);
                            DebugConsole.WriteLine(ex.StackTrace);
                            MessageBox.Show("Error while loading Nexus. Closing!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(1);
                        }

                        SystemCommands.CloseWindow(this);

                        break;
                    }
                    await Task.Delay(27);
                }
            });
        }
    }
}