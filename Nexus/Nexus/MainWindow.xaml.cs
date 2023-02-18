using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using Nexus.Classes;
using Nexus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Nexus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region vars

        private ScrollViewer? tabScroller;

        private readonly List<ModernWpf.Controls.ToggleSwitch> switches = new();

        private readonly API.KrnlApi api;

        private const double TitleOpacity = 0.6;

        private const double DefaultOpacity = 1;

        private readonly ProcessWatcher watcher = new("RobloxPlayerBeta");

        #endregion

        public MainWindow(API.KrnlApi _api)
        {
            InitializeComponent();

            api = _api;

            //web browser settings (monaco)
            CefSettings cefSettings = new();
            cefSettings.SetOffScreenRenderingBestPerformanceArgs();
            cefSettings.MultiThreadedMessageLoop = true;
            cefSettings.DisableGpuAcceleration();
            Cef.Initialize(cefSettings);

            Console.WriteLine("Initialized Cef");

            //add all switches
            switches.Add(AutoInjectSwitch);
            switches.Add(UnlockFpsSwitch);
            switches.Add(TopMostSwitch);

            //tab system event handlers
            EditTabs.Loaded += delegate (object source, RoutedEventArgs e)
            {
                //add tab click handler
                EditTabs.GetTemplateItem<Button>("AddTabButton").Click += delegate (object s, RoutedEventArgs f)
                {
                    MakeTab("New Tab");
                };

                //main tab properties
                if (EditTabs.SelectedItem is TabItem ti)
                {
                    ti.GetTemplateItem<Button>("CloseButton").Visibility = Visibility.Hidden;
                    ti.GetTemplateItem<Button>("CloseButton").Width = 0;
                    ti.Header = "Main Tab";
                    ti.Content = MakeEditor();
                    tabScroller = EditTabs.GetTemplateItem<ScrollViewer>("TabScrollViewer");
                }
            };
        }

        #region executor handlers

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!api.IsInjected())
            {
                MessageBox.Show("Inject before executing!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                api.Execute(ReadScript());
                Console.WriteLine("Executed script");
            }
        }

        private void InjectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!api.IsInjected())
            {
                api.Inject();
                Console.WriteLine("Injecting KRNL");
            }
            else
            {
                MessageBox.Show("Already Injected!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            WriteScript(string.Empty);
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open",
                    Filter = "Text (*.txt)|*.txt|Lua (*.lua)|*.lua|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    CheckFileExists = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    WriteScript(File.ReadAllText(openFileDialog.FileName));
                    Console.WriteLine($"Opened {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while opening file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Error while opening a file. ST:");
                Console.WriteLine(ex.Message);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Title = "Save as",
                    Filter = "Text (*.txt)|*.txt|Lua (*.lua)|*.lua|All Files (*.*)|*.*",
                    DefaultExt = ".txt"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    if (!File.Exists(saveFileDialog.FileName))
                    {
                        File.Create(saveFileDialog.FileName).Close();
                        File.WriteAllText(saveFileDialog.FileName, ReadScript());
                        Console.WriteLine($"Created {saveFileDialog.FileName}");
                    }
                    else
                    {
                        File.WriteAllText(saveFileDialog.FileName, ReadScript());
                        Console.WriteLine($"Saved {saveFileDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Error while saving a file. ST:");
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region window handlers

        private void KillRbxBtn_Click(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill(true);
                    Console.WriteLine($"Killed '{process.ProcessName}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while killing Roblox. ST:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void OnProcessCreated(object sender, Process proc)
        {
            if (!api.IsInjected())
            {
                api.Inject();
                Console.WriteLine("Injected with Autoinject");
            }
        }

        private void AutoInjectSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoInject = AutoInjectSwitch.IsOn;

            if (AutoInjectSwitch.IsOn)
            {
                watcher.Created += OnProcessCreated;
            }
            else
            {
                watcher.Created -= OnProcessCreated;
            }
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (ModernWpf.Controls.ToggleSwitch _switch in switches)
                {
                    _switch.IsOn = false;
                }
                try
                {
                    Settings.Default.Reset();
                    Console.WriteLine("All settings reset");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while reseting settings. ST:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch (Exception ex)
            {
                Console.WriteLine("DragMove exception. ST:");
                Console.WriteLine(ex.Message);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void EnableBackBtn()
        {
            TitleText.Margin = new(55, 0, 0, 0);
            BackBtn.Visibility = Visibility.Visible;
        }

        private void DisableBackBtn()
        {
            TitleText.Margin = new(0, 0, 0, 0);
            BackBtn.Visibility = Visibility.Hidden;
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsArea.Visibility == Visibility.Hidden)
            {
                ContentArea.Visibility = Visibility.Hidden;
                ScriptHubArea.Visibility = Visibility.Hidden;
                SettingsArea.Visibility = Visibility.Visible;
                EnableBackBtn();
            }
            else
            {
                ContentArea.Visibility = Visibility.Visible;
                ScriptHubArea.Visibility = Visibility.Hidden;
                SettingsArea.Visibility = Visibility.Hidden;
                DisableBackBtn();
            }
        }

        private void HubBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptHubArea.Visibility == Visibility.Hidden)
            {
                ContentArea.Visibility = Visibility.Hidden;
                SettingsArea.Visibility = Visibility.Hidden;
                ScriptHubArea.Visibility = Visibility.Visible;
                EnableBackBtn();
            }
            else
            {
                ContentArea.Visibility = Visibility.Visible;
                SettingsArea.Visibility = Visibility.Hidden;
                ScriptHubArea.Visibility = Visibility.Hidden;
                DisableBackBtn();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Visibility = Visibility.Visible;
            ScriptHubArea.Visibility = Visibility.Hidden;
            SettingsArea.Visibility = Visibility.Hidden;
            DisableBackBtn();
        }

        private void TopMostSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.Topmost = TopMostSwitch.IsOn;

            Topmost = TopMostSwitch.IsOn;
        }

        private void UnlockFpsSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.UnlockFPS = UnlockFpsSwitch.IsOn;

            if (UnlockFpsSwitch.IsOn)
            {
                api.SetFrameRate(0);
            }
            else
            {
                api.SetFrameRate(60);
            }
        }

        #endregion

        #region Tab System

        private void ScrollTabs(object sender, MouseWheelEventArgs e)
        {
            if (tabScroller == null) return;

            tabScroller.ScrollToHorizontalOffset(tabScroller.HorizontalOffset + e.Delta / 10);
        }

        private void MoveTab(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Source is not TabItem tabItem)
                {
                    return;
                }
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (VisualTreeHelper.HitTest(tabItem, Mouse.GetPosition(tabItem)).VisualHit is Button)
                    {
                        return;
                    }
                    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
                }
            }
            catch { }
        }

        private ChromiumWebBrowser GetCurrent()
        {
            return EditTabs.SelectedContent as ChromiumWebBrowser;
        }

        private ChromiumWebBrowser MakeEditor()
        {
            ChromiumWebBrowser textEditor = new(Environment.CurrentDirectory + "\\Monaco\\Monaco.html");
            textEditor.BrowserSettings.WindowlessFrameRate = 144;
            textEditor.BrowserSettings.JavascriptAccessClipboard = CefState.Enabled;
            return textEditor;
        }

        private TabItem MakeTab(string title = "Tab")
        {
            bool loaded = false;
            ChromiumWebBrowser textEditor = MakeEditor();
            TabItem tab = new()
            {
                Content = textEditor,
                Style = TryFindResource("Tab") as Style,
                AllowDrop = true,
                Header = title
            };
            tab.MouseWheel += ScrollTabs;
            tab.Loaded += delegate (object source, RoutedEventArgs e)
            {
                if (loaded)
                {
                    return;
                }
                tabScroller?.ScrollToRightEnd();
                loaded = true;
            };
            tab.MouseDown += delegate (object sender, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is Border)
                {
                    if (e.MiddleButton == MouseButtonState.Pressed)
                    {
                        textEditor.Dispose();
                        EditTabs.Items.Remove(tab);
                        return;
                    }
                }
            };
            tab.Loaded += delegate (object s, RoutedEventArgs e)
            {
                tab.GetTemplateItem<Button>("CloseButton").Click += delegate (object r, RoutedEventArgs f)
                {
                    textEditor.Dispose();
                    EditTabs.Items.Remove(tab);
                };

                tabScroller?.ScrollToRightEnd();
                loaded = true;
            };

            tab.MouseMove += MoveTab;
            tab.Drop += DropTab;
            string oldHeader = title;
            EditTabs.SelectedIndex = EditTabs.Items.Add(tab);
            return tab;
        }

        //allow drag and drop with tabs
        private void DropTab(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Source is TabItem tabItem)
                {
                    if (e.Data.GetData(typeof(TabItem)) is TabItem tabItem2)
                    {
                        if (!tabItem.Equals(tabItem2))
                        {
                            if (tabItem != null)
                            {
                                if (tabItem.Parent is TabControl tabControl)
                                {
                                    int insertIndex = tabControl.Items.IndexOf(tabItem2);
                                    int num = tabControl.Items.IndexOf(tabItem);
                                    tabControl.Items.Remove(tabItem2);
                                    tabControl.Items.Insert(num, tabItem2);
                                    tabControl.Items.Remove(tabItem);
                                    tabControl.Items.Insert(insertIndex, tabItem);
                                    tabControl.SelectedIndex = num;
                                }
                            }
                        }
                        return;
                    }
                }
            }
            catch { }
        }

        #endregion

        #region misc handlers

        private string ReadScript()
        {
            if (!GetCurrent().IsLoaded)
            {
                return "";
            }
            string? text = WebBrowserExtensions.EvaluateScriptAsync(GetCurrent(), "(function() { return GetText() })();", null, false).GetAwaiter().GetResult().Result.ToString();
            if (text != null)
            {
                return text;
            }
            return "";
        }

        private void WriteScript(string script)
        {
            if (GetCurrent().IsLoaded)
            {
                WebBrowserExtensions.EvaluateScriptAsync(GetCurrent(), "SetText(`" + script.Replace("`", "\\`").Replace("\\", "\\\\") + "`)", null, false);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //load settings data
            if (Settings.Default.AutoInject)
            {
                AutoInjectSwitch.IsOn = true;
            }
            if (Settings.Default.UnlockFPS)
            {
                UnlockFpsSwitch.IsOn = true;
            }
            if (Settings.Default.Topmost)
            {
                TopMostSwitch.IsOn = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save settings data
            Console.WriteLine("Closing Nexus");
            watcher.Dispose();
            try
            {
                Settings.Default.Save();
                Console.WriteLine("Saving settings");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while saving settings. ST:");
                Console.WriteLine(ex.Message);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TitleBar.Opacity = DefaultOpacity;
            MainBorder.BorderBrush = (LinearGradientBrush)TryFindResource("Gradient");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            TitleBar.Opacity = TitleOpacity;
            MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 66, 66));
        }

        private void ScriptListBtn_Checked(object sender, RoutedEventArgs e)
        {
            ScriptHubBtn.IsChecked = false;
            ScriptHubItems.Visibility = Visibility.Hidden;
            ScriptList.Visibility = Visibility.Visible;
        }

        private void ScriptHubBtn_Checked(object sender, RoutedEventArgs e)
        {
            ScriptListBtn.IsChecked = false;
            ScriptHubItems.Visibility = Visibility.Visible;
            ScriptList.Visibility = Visibility.Hidden;
        }

        private void ScriptHubBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            ScriptHubItems.Visibility = Visibility.Hidden;
        }

        private void ScriptListBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            ScriptList.Visibility = Visibility.Hidden;
        }

        private void ScriptList_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (DirectoryInfo directoryInfo in new DirectoryInfo("./Scripts").GetDirectories())
                {
                    TreeViewItem item = new()
                    {
                        Header = "📁 " + directoryInfo.Name,
                        Name = directoryInfo.Name
                    };

                    if (item.Name == "Scripts")
                    {
                        continue;
                    }

                    foreach (FileInfo fileInfo in new DirectoryInfo($"./Scripts/{directoryInfo.Name}").GetFiles("*.txt"))
                    {
                        item.Items.Add(new TreeViewItem()
                        {
                            Header = fileInfo.Name
                        });
                    }
                    foreach (FileInfo fileInfo2 in new DirectoryInfo($"./Scripts/{directoryInfo.Name}").GetFiles("*.lua"))
                    {
                        item.Items.Add(new TreeViewItem()
                        {
                            Header = fileInfo2.Name
                        });
                    }
                    item.ToolTip = $"Scripts located in {Environment.CurrentDirectory}\\Scripts\\{item.Name}";
                    Scripts.Items.Add(item);
                }

                foreach (FileInfo fileInfo in new DirectoryInfo("./Scripts").GetFiles("*.txt"))
                {
                    Scripts.Items.Add(new TreeViewItem()
                    {
                        Header = fileInfo.Name
                    });
                }
                foreach (FileInfo fileInfo2 in new DirectoryInfo("./Scripts").GetFiles("*.lua"))
                {
                    Scripts.Items.Add(new TreeViewItem()
                    {
                        Header = fileInfo2.Name
                    });
                }

                Scripts.ToolTip = $"Scripts located in {Environment.CurrentDirectory}\\Scripts";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while adding scripts. ST:");
                Console.WriteLine(ex.Message);
            }
        }

        private void ScriptList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //get the current item
            bool flag = ScriptList.SelectedItem is not null; //check if anything is selected

            if (flag)
            {
                try
                {
                    if (ScriptList.SelectedItem is TreeViewItem item)
                    {
                        string? header = item.Header.ToString();
                        if (header == null) return;

                        if (header.Contains("📁"))
                        {
                            return;
                        }

                        TreeViewItem parent = (TreeViewItem)item.Parent;
                        if (parent == null) return;

                        if (parent.Name == "Scripts")
                        {
                            WriteScript(File.ReadAllText("Scripts\\" + header));
                        }
                        else
                        {
                            WriteScript(File.ReadAllText("Scripts\\" + parent.Name + "\\" + header));
                        }

                        ContentArea.Visibility = Visibility.Visible;
                        ScriptHubArea.Visibility = Visibility.Hidden;
                        SettingsArea.Visibility = Visibility.Hidden;
                        DisableBackBtn();
                        item.IsSelected = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while opening script. ST:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        #endregion

        #region script hub

        private async void ExecuteCMDX_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing CMDX from ScriptHub");
            ExecuteCMDX.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteCMDX.Content = "Not Injected!";
                await Task.Delay(4500);
                ExecuteCMDX.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = wb.DownloadString("https://raw.githubusercontent.com/CMD-X/CMD-X/master/Source");
                api.Execute(script);
                wb.Dispose();
                ExecuteCMDX.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteCMDX.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteCMDX.Content = "Error";
                await Task.Delay(7000);
                ExecuteCMDX.Content = "Execute";
                return;
            }
        }

        private async void ExecuteIY_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing IY from ScriptHub");
            ExecuteIY.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteIY.Content = "Not Injected!";
                await Task.Delay(4500);
                ExecuteIY.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = wb.DownloadString("https://raw.githubusercontent.com/EdgeIY/infiniteyield/master/source");
                api.Execute(script);
                wb.Dispose();
                ExecuteIY.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteIY.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteIY.Content = "Error";
                await Task.Delay(7000);
                ExecuteIY.Content = "Execute";
                return;
            }
        }

        private async void ExecuteDex_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing Dex from ScriptHub");
            ExecuteDex.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteDex.Content = "Not Injected!";
                await Task.Delay(5000);
                ExecuteDex.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = "loadstring(game:HttpGet(\"https://raw.githubusercontent.com/peyton2465/Dex/master/out.lua\"))()"; //wb limitation
                api.Execute(script);
                wb.Dispose();
                ExecuteDex.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteDex.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteDex.Content = "Error";
                await Task.Delay(7000);
                ExecuteDex.Content = "Execute";
                return;
            }
        }

        private async void ExecuteOwlhub_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing Owl Hub from ScriptHub");
            ExecuteOwlhub.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteOwlhub.Content = "Not Injected!";
                await Task.Delay(5000);
                ExecuteOwlhub.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = wb.DownloadString("https://raw.githubusercontent.com/CriShoux/OwlHub/master/OwlHub.txt");
                api.Execute(script);
                wb.Dispose();
                ExecuteOwlhub.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteOwlhub.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteOwlhub.Content = "Error";
                await Task.Delay(7000);
                ExecuteOwlhub.Content = "Execute";
                return;
            }
        }

        private async void ExecuteRemoteSpy_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing RemoteSpy from ScriptHub");
            ExecuteRemoteSpy.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteRemoteSpy.Content = "Not Injected!";
                await Task.Delay(5000);
                ExecuteRemoteSpy.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = wb.DownloadString("https://pastebin.com/raw/qhqbNa2m");
                api.Execute(script);
                wb.Dispose();
                ExecuteRemoteSpy.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteRemoteSpy.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading/executing script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteRemoteSpy.Content = "Error";
                await Task.Delay(7000);
                ExecuteRemoteSpy.Content = "Execute";
                return;
            }
        }

        private async void ExecuteDomainX_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Executing DomainX from ScriptHub");
            ExecuteDomainX.Content = "Executing...";
            WebClient wb = new();
            await Task.Delay(1000);
            if (!api.IsInjected())
            {
                ExecuteDomainX.Content = "Not Injected!";
                await Task.Delay(5000);
                ExecuteDomainX.Content = "Execute";
                wb.Dispose();
                return;
            }
            try
            {
                string script = wb.DownloadString("https://raw.githubusercontent.com/shlexware/DomainX/main/source");
                api.Execute(script);
                wb.Dispose();
                ExecuteDomainX.Content = "Executed!";
                await Task.Delay(3000);
                ExecuteDomainX.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading/executing script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, ST:");
                Console.WriteLine(ex.Message);
                wb.Dispose();
                ExecuteDomainX.Content = "Error";
                await Task.Delay(7000);
                ExecuteDomainX.Content = "Execute";
                return;
            }
        }

        #endregion
    }
}