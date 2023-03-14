using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nexus.Classes
{
    public static class Utils
    {
        /// <summary>
        /// used for functions in utils
        /// </summary>
        public static HttpClient Client { get; } = new();

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
                    Console.WriteLine("Injector loaded.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while downloading the injector. Message:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Download a file with HttpClient
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static async Task DownloadFileTaskAsync(string uri, string FileName)
        {
            if (!File.Exists(FileName))
            {
                try
                {
                    using Stream s = await Client.GetStreamAsync(new Uri(uri));
                    using FileStream fs = new(FileName, FileMode.CreateNew);
                    await s.CopyToAsync(fs);
                    s.Close();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while downloading a file from {uri}. Message:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Download a string with HttpClient
        /// </summary>
        /// <param name="url"></param>
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
                Console.WriteLine($"Error while downloading a string from {url}. Message:");
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}