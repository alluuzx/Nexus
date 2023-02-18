using Microsoft.Win32;
using System;
using System.IO;
using System.Net;

namespace Nexus.API
{
    /// <summary>
    /// Class to interact with the KRNL dll.
    /// </summary>
    public class KrnlApi
    {
        /// <summary>
        /// The KRNL directory.
        /// </summary>
        private string? KrnlDir { get; set; }

        /// <summary>
        /// The user data directory.
        /// </summary>
        private string? DataDir { get; set; }

        /// <summary>
        /// The config file directory.
        /// </summary>
        private string? ConfigFile { get; set; }

        /// <summary>
        /// The DLL version.
        /// </summary>
        private string? DllVersion { get; set; }

        /// <summary>
        /// The KRNL registrykey.
        /// </summary>
        private RegistryKey? KrnlRegistry { get; set; }

        /// <summary>
        /// Get the KRNL Configuration file
        /// </summary>
        /// <param name="Line"></param>
        /// <returns>The configuration file as <see cref="string"/></returns>
        private string ReadConfig(int Line)
        {
            return File.ReadAllLines(DataDir + "\\krnl.config")[Line];
        }

        /// <summary>
        /// Download required files of KRNL.
        /// </summary>
        internal void Initialize()
        {
            KrnlDir = AppData + "\\Krnl";
            DataDir = AppData + "\\Krnl\\Data";
            ConfigFile = AppData + "\\Krnl\\Data\\krnl.config";
            DllVersion = wc.DownloadString("https://cdn.krnl.place/version.txt");
            if (!Directory.Exists(KrnlDir))
            {
                Directory.CreateDirectory(KrnlDir);
                if (!Directory.Exists(DataDir))
                {
                    Directory.CreateDirectory(DataDir);
                }
            }
            if (!File.Exists(ConfigFile))
            {
                File.WriteAllLines(ConfigFile, new string[]
                {
                    DllVersion,
                    "0"
                });
            }
            if (ReadConfig(0) != DllVersion || !File.Exists(KrnlDir + "\\krnl.dll"))
            {
                wc.DownloadFile("https://k-storage.com/bootstrapper/files/krnl.dll", KrnlDir + "\\krnl.dll");
            }
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
        }

        /// <summary>
        /// Inject to Roblox
        /// </summary>
        /// <returns><see cref="Injector.InjectionStatus"/></returns>
        internal Injector.InjectionStatus Inject()
        {
            return Injector.Inject(KrnlDir + "\\krnl.dll");
        }

        /// <summary>
        /// Execute the script in Roblox
        /// </summary>
        /// <param name="Script"></param>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        internal bool Execute(string Script)
        {
            if (IsInjected())
            {
                Pipes.PassString(Script);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set the frame rate limit in Roblox
        /// </summary>
        /// <remarks>
        /// Note: KRNL needs to be injected
        /// </remarks>
        /// <param name="frameRate"></param>
        internal void SetFrameRate(int frameRate)
        {
            Execute(string.Format("setfpscap({0})", frameRate));
        }

        /// <summary>
        /// Is KRNL is injected to Roblox
        /// </summary>
        /// <returns><see langword="true"/> if injected, else <see langword="false"/></returns>
        internal bool IsInjected()
        {
            return Pipes.PipeActive();
        }

        /// <summary>
        /// Is KRNL injected
        /// </summary>
        /// <returns><see langword="true"/> if injected, else <see langword="false"/></returns>
        internal bool IsInitialized()
        {
            return Initialized;
        }

        /// <summary>
        /// The API version.
        /// </summary>
        internal const string Version = "v0.2x";

        /// <summary>
        /// User appdata folder.
        /// </summary>
        private readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Is KRNL initialized
        /// </summary>
        private bool Initialized;

        /// <summary>
        /// WebClient used to download the files.
        /// </summary>
        private readonly WebClient wc = new();
    }
}