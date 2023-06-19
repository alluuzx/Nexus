using Microsoft.Win32;
using Nexus.Classes;
using Nexus.Debugging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Nexus.API
{
    /// <summary>
    /// Class to interact with the KRNL Dll.
    /// </summary>
    internal static class KrnlApi
    {
        /// <summary>
        /// The user data directory.
        /// </summary>
        private static string? DataDir { get; set; }

        /// <summary>
        /// The config file directory.
        /// </summary>
        private static string? ConfigFile { get; set; }

        /// <summary>
        /// The DLL version.
        /// </summary>
        private static string? DllVersion { get; set; }

        /// <summary>
        /// The KRNL registrykey.
        /// </summary>
        private static RegistryKey? KrnlRegistry { get; set; }

        /// <summary>
        /// Download required files of KRNL.
        /// </summary>
        internal static void Initialize()
        {
            Task.Run(delegate ()
            {
                DataDir = Environment.CurrentDirectory + "\\Data";
                ConfigFile = Environment.CurrentDirectory + "\\Data\\krnl.config";
                DllVersion = Utils.DownloadString("https://cdn.krnl.place/version.txt");

                if (!Directory.Exists(DataDir))
                {
                    Directory.CreateDirectory(DataDir);
                }

                if (!File.Exists(ConfigFile))
                {
                    File.WriteAllLines(ConfigFile, new string[]
                    {
                        "",
                        "0"
                    });
                }

                if (Utils.ReadLine(DataDir + "\\krnl.config", 0) != DllVersion || !File.Exists(Environment.CurrentDirectory + "\\krnl.dll"))
                {
                    if (File.Exists("injector.dll"))
                    {
                        File.Delete("injector.dll");
                    }

                    if (File.Exists("krnl.dll"))
                    {
                        File.Delete("krnl.dll");
                    }

                    DownloadKrnlDLL();

                    File.WriteAllLines(ConfigFile, new string[]
                    {
                        DllVersion,
                        "0"
                    });

                    DebugConsole.WriteLine("Krnl Updated.");
                    Globals.KrnlUpdated = true;
                }

                Utils.LoadInjector();

                RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE");
                if (registryKey == null) return;

                try
                {
                    KrnlRegistry = registryKey.OpenSubKey("Krnl", true);
                }
                catch
                {
                    KrnlRegistry = registryKey.CreateSubKey("Krnl", true);
                }

                Initialized = true;
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Download the Krnl DLL
        /// </summary>
        internal static async void DownloadKrnlDLL()
        {
            await Utils.DownloadFileTaskAsync("https://k-storage.com/bootstrapper/files/krnl.dll", Environment.CurrentDirectory + "\\krnl.dll");
        }

        /// <summary>
        /// Inject to Roblox
        /// </summary>
        /// <returns><see cref="Injector.InjectionStatus"/></returns>
        internal static Injector.InjectionStatus Inject()
        {
            return Injector.Inject(Environment.CurrentDirectory + "\\krnl.dll");
        }

        /// <summary>
        /// Execute the script in Roblox
        /// </summary>
        /// <param name="Script">Lua script to execute</param>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        internal static bool Execute(string Script)
        {
            if (IsInjected())
            {
                return Pipes.PassString(Script);
            }
            return false;
        }

        /// <summary>
        /// Set the frame rate limit in Roblox
        /// </summary>
        /// <remarks>
        /// Note: KRNL needs to be injected
        /// </remarks>
        /// <param name="frameRate">framerate to set</param>
        internal static void SetFrameRate(int frameRate)
        {
            Execute(string.Format("setfpscap({0})", frameRate));
        }

        /// <summary>
        /// Is KRNL is injected to Roblox
        /// </summary>
        /// <returns><see langword="true"/> if injected, else <see langword="false"/></returns>
        internal static bool IsInjected()
        {
            return Pipes.PipeActive();
        }

        /// <summary>
        /// Is KRNL injected
        /// </summary>
        /// <returns><see langword="true"/> if initialized, else <see langword="false"/></returns>
        internal static bool IsInitialized()
        {
            return Initialized;
        }

        /// <summary>
        /// Get the API version
        /// </summary>
        internal static string GetAPIVersion()
        {
            return Version;
        }

        /// <summary>
        /// Get the Krnl DLL version
        /// </summary>
        internal static string GetDLLVersion()
        {
            if (DllVersion == null)
            {
                return "Not found!";
            }
            return DllVersion;
        }

        /// <summary>
        /// The API version.
        /// </summary>
        private static readonly string Version = "v0.2x";

        /// <summary>
        /// Is KRNL initialized
        /// </summary>
        private static bool Initialized;
    }
}