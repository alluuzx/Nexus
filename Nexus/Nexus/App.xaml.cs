using Nexus.Debugging;
using System;
using System.Windows;

namespace Nexus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //get command line args
            string[] args = Environment.GetCommandLineArgs();

            try
            {
                foreach (string arg1 in args)
                {
                    if (arg1.ToLower() == "/debug")
                    {
                        //enable debug mode
                        DebugConsole.Initialize();

                        Console.Title = "Nexus Debug Console";

                        Console.WriteLine("Nexus launched in debug mode");
                        Console.WriteLine($"OS Version: {Environment.OSVersion}");
                        Console.WriteLine($"CLR Version: {Environment.Version}");

                        if (Environment.Is64BitOperatingSystem)
                        {
                            Console.WriteLine("OS is 64-bit");
                        }
                        else
                        {
                            Console.WriteLine("OS is 32-bit");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while loading startup arguments. Message and ST:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show("Error while loading startup arguments.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}