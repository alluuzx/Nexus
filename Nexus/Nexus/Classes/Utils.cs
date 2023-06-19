using Nexus.Debugging;
using Nexus.Kernel32;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using static Nexus.Kernel32.Misc;

namespace Nexus.Classes
{
    public static class Utils
    {
        /// <summary>
        /// used for functions in utils
        /// </summary>
        internal static HttpClient Client { get; } = new();

        /// <summary>
        /// Reads a line from the specified file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="lineNumber">Number of the line, starts from 0</param>
        /// <returns>The text from the line</returns>
        public static string ReadLine(string path, int lineNumber)
        {
            try
            {
                string line = File.ReadAllLines(path)[lineNumber];
                return line;
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while reading line from file. Message:");
                DebugConsole.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Load the DLL injector
        /// </summary>
        internal static async void LoadInjector()
        {
            if (!File.Exists("injector.dll"))
            {
                try
                {
                    await DownloadFileTaskAsync("https://k-storage.com/bootstrapper/injector.dll", "injector.dll");
                    DebugConsole.WriteLine("Injector loaded.");
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while downloading the injector. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Download a file with HttpClient
        /// </summary>
        /// <param name="uri">Where to download</param>
        /// <param name="FileName">Filename to save</param>
        /// <returns></returns>
        public static async Task DownloadFileTaskAsync(string uri, string FileName)
        {
            try
            {
                //replace
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }

                using Stream s = await Client.GetStreamAsync(new Uri(uri));
                using FileStream fs = new(FileName, FileMode.CreateNew);
                await s.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine($"Error while downloading a file from {uri}. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Download a string with HttpClient
        /// </summary>
        /// <param name="url">Where to download</param>
        /// <returns>The downloaded <see cref="string"/>.</returns>
        public static string DownloadString(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            try
            {
                using HttpResponseMessage response = Client.GetAsync(url).Result;
                using HttpContent content = response.Content;
                return content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine($"Error while downloading a string from {url}. Message:");
                DebugConsole.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the current cursor position as a point
        /// </summary>
        /// <returns>Current cursor position</returns>
        public static Point GetCursorPosition()
        {
            try
            {
                Imports.GetCursorPos(out POINT lpPoint);
                return lpPoint;
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine(ex.Message);
                return new(0, 0);
            }
        }
    }
}