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
            string[]? args = Environment.GetCommandLineArgs();

            //check the args
            if (args.Length >= 2 && !string.IsNullOrEmpty(args[1]) && args[1].ToLower() == "/debug")
            {
                //enable debug mode
                DebugConsole.Initialize();

                Console.Title = "Nexus Debug Console";

                Console.WriteLine("Nexus launched in debug mode");
                Console.WriteLine($"OS Version: {Environment.OSVersion}");
                Console.WriteLine($"CLR Version: {Environment.Version}");
            }
        }
    }
}