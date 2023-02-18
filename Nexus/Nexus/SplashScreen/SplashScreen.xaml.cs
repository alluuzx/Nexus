using Nexus.Classes;
using System;
using System.ComponentModel;
using System.Reflection;
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

        private readonly API.KrnlApi api = new();

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

            await Task.Delay(100);

            try
            {
                api.Initialize();
                Console.WriteLine("KRNL Initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to initialize KRNL. ST:");
                Console.WriteLine(ex.Message);
            }

            try
            {
                Utils.LoadUnmanagedLibraryFromResource(Assembly.GetExecutingAssembly(), "Nexus.injector.dll", "injector.dll");
                Console.WriteLine("Injector loaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load the injector. ST:");
                Console.WriteLine(ex.Message);
            }

            for (int i = 0; i <= 200; i++)
            {
                if (i == 200)
                {
                    try
                    {
                        MainWindow main = new(api);
                        Application.Current.MainWindow = main;

                        await Task.Delay(800);

                        Hide();

                        loaded = true;
                        Console.WriteLine("Loading complete.");

                        await Task.Delay(400);

                        main.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error while loading Nexus", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                        Console.WriteLine("Fatal error while loading. ST:");
                        Console.WriteLine(ex.Message);

                        Application.Current.Shutdown(1);
                    }

                    Close();

                    break;
                }

                ProgressBar.Width = i;
                await Task.Delay(1);
            }
        }
    }
}