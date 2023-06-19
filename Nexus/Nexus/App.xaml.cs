using Nexus.Classes;
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
            try
            {
                //get command line args
                string[] args = Environment.GetCommandLineArgs();

                foreach (string arg1 in args)
                {
                    if (arg1.ToLower() == "/debug")
                    {
                        //enable debug mode
                        DebugConsole.Initialize();

                        Console.Title = "Nexus Debug Console";

                        string version = Environment.Is64BitProcess ? "x64" : "x86";
                        DebugConsole.WriteLine($"Nexus {Globals.NexusVERSION} {version} launched in debug mode");

                        DebugConsole.WriteLine($"OS Version: {Environment.OSVersion}");
                        DebugConsole.WriteLine($"CLR Version: {Environment.Version}");

                        if (Environment.Is64BitOperatingSystem)
                        {
                            DebugConsole.WriteLine("OS is 64-bit");
                        }
                        else
                        {
                            DebugConsole.WriteLine("OS is 32-bit");
                        }

                        DebugConsole.WriteLine("-----------------------------------");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Exception while loading. Message and ST:");
                DebugConsole.WriteLine(ex.Message);
                DebugConsole.WriteLine(ex.StackTrace);
            }
        }
    }
}