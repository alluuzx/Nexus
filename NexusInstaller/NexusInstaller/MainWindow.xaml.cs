using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace NexusInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// <see cref="WebClient"/> used to download Nexus
        /// </summary>
        private readonly WebClient client = new WebClient();

        /// <summary>
        /// Where Nexus will be downloaded from
        /// </summary>
        private const string DownloadLink = "https://cdn-141.anonfiles.com/j2z8s9e1z5/7ac241c7-1678815532/Nexus.zip";

        public MainWindow()
        {
            InitializeComponent();

            //get unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception exception = (Exception)args.ExceptionObject;
                Console.WriteLine("Unhandled exception caught.");
                Console.WriteLine(exception.Message);
                MessageBox.Show($"Nexus has encountered an unhandled exception. It will be restarted now. Message: {exception.Message}", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "NexusInstaller.exe",
                    UseShellExecute = true
                });
            };

            //show the progress to the user
            client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
            {
                InstallBtn.Content = "Downloading... " + e.ProgressPercentage + "%";
            };

            //when the file is downloaded, extract it
            client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
            {
                //check if the file exists before doing anything
                if (File.Exists(PathBox.Text + "\\Nexus.zip"))
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(PathBox.Text + "\\Nexus.zip", PathBox.Text + "\\Nexus");
                        DeleteDirectories(true, false);
                        InstallBtn.Content = "Installed!";
                        MessageBox.Show("Nexus has been installed succesfully!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        MessageBox.Show("Error while extracting Nexus. Please disable your antivirus and try again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                        InstallBtn.Content = "Install";
                        InstallBtn.IsEnabled = true;
                    }
                }
                else
                {
                    //if the file doesnt exist
                    MessageBox.Show("Nexus not found, please disable your antivirus and try again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    InstallBtn.Content = "Install";
                    InstallBtn.IsEnabled = true;
                }
            };
        }

        /// <summary>
        /// Check if Nexus is running, and if it is, kill it
        /// </summary>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        private bool CheckNexus()
        {
            bool result;
            try
            {
                Process[] processes = Process.GetProcessesByName("Nexus");
                if (processes != null)
                {
                    foreach (Process _ in processes)
                    {
                        MessageBox.Show("Nexus is already running. It will be closed for the installation.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
                    foreach (Process proc in processes)
                    {
                        proc.Kill();
                    }
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                result = false;
            }
            return result;
        }

        private void InstallStartBtn_Click(object sender, RoutedEventArgs e)
        {
            Blur.Radius = 20;
            InstallOverlay.Visibility = Visibility.Visible;
            CheckNexus();
        }

        private void SelectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true
                };
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    PathBox.Text = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(PathBox.Text))
            {
                MessageBox.Show("Please select a valid path.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //dont allow the user to install again while installing
            InstallBtn.IsEnabled = false;
            DeleteDirectories();
            try
            {
                //start downloading
                client.DownloadFileAsync(new Uri(DownloadLink), PathBox.Text + "\\Nexus.zip");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading Nexus. Please restart the installer.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine(ex.Message);
                client.CancelAsync();
                InstallBtn.Content = "Error";
            }
        }

        /// <summary>
        /// Delete existing Nexus files
        /// </summary>
        /// <param name="DeleteZip"></param>
        /// <param name="DeleteDirectory"></param>
        private void DeleteDirectories(bool DeleteZip = true, bool DeleteDirectory = true)
        {
            try
            {
                if (DeleteZip && File.Exists(PathBox.Text + "\\Nexus.zip"))
                {
                    File.Delete(PathBox.Text + "\\Nexus.zip");
                }

                if (DeleteDirectory && Directory.Exists(PathBox.Text + "\\Nexus"))
                {
                    Directory.Delete(PathBox.Text + "\\Nexus", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //check if Nexus is downloading
            if (client.IsBusy)
            {
                if (MessageBox.Show("Nexus is being installed. Are you sure you want to exit?", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            client.Dispose();
        }
    }
}