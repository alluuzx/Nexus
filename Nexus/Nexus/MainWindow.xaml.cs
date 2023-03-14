using CefSharp;
using CefSharp.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using Newtonsoft.Json;
using Nexus.API;
using Nexus.Classes;
using Nexus.Controls;
using Nexus.Debugging;
using Nexus.Dialogs;
using Nexus.Kernel32;
using Nexus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nexus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region vars

        /// <summary>
        /// ScrollViewer used to scroll tabs
        /// </summary>
        private ScrollViewer? tabScroller;

        /// <summary>
        /// List of switches in settings
        /// </summary>
        private readonly List<ModernWpf.Controls.ToggleSwitch> switches = new();

        /// <summary>
        /// Titlebar opacity when window is deactivated
        /// </summary>
        private const double TitleOpacity = 0.6;

        /// <summary>
        /// Titlebar opacity when window is activated
        /// </summary>
        private const double DefaultOpacity = 1;

        /// <summary>
        /// Tool to detect when Roblox is launched for autoinject
        /// </summary>
        private readonly ProcessWatcher watcher = new("RobloxPlayerBeta");

        /// <summary>
        /// The mutex handle for multi-instance
        /// </summary>
        protected nint MultiInstanceHandle = nint.Zero;

        /// <summary>
        /// Has Krnl been updated
        /// </summary>
        internal static bool KrnlUpdated = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //add all settings switches
            switches.Add(AutoInjectSwitch);
            switches.Add(UnlockFpsSwitch);
            switches.Add(TopMostSwitch);
            switches.Add(MultiInstanceSwitch);
            switches.Add(EditorMinimapSwitch);
            switches.Add(AutoLaunchSwitch);

            //get unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception exception = (Exception)args.ExceptionObject;
                Console.WriteLine("Unhandled exception caught.");
                Console.WriteLine(exception.Message);
                MessageBox.Show($"Nexus has encountered an unhandled exception. It will be restarted now. Message: {exception.Message}", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!DebugConsole.IsConsoleInitialized)
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "Nexus.exe",
                        UseShellExecute = true
                    });
                    Environment.Exit(1);
                }
            };

            //tab system event handlers
            EditTabs.Loaded += delegate (object source, RoutedEventArgs e)
            {
                //add tab click handler
                EditTabs.GetTemplateItem<Button>("AddTabButton").Click += delegate (object s, RoutedEventArgs f)
                {
                    //save resources because 15 chrome tabs is a lot of ram lol
                    if (EditTabs.Items.Count >= 15)
                    {
                        MessageBox.Show("Tab limit reached.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    else
                    {
                        MakeTab("New Tab");
                    }
                };

                //main tab properties
                if (EditTabs.SelectedItem is TabItem ti)
                {
                    ti.GetTemplateItem<Button>("CloseButton").Visibility = Visibility.Hidden;
                    ti.GetTemplateItem<Button>("CloseButton").Width = 0;
                    ti.Header = "Main Tab";
                    ti.Content = MakeEditor();
                    tabScroller = EditTabs.GetTemplateItem<ScrollViewer>("TabScrollViewer");
                    AddTabContext(ti, false);
                }
            };
        }

        #region executor handlers

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!KrnlApi.IsInjected())
            {
                MessageBox.Show("Inject before executing!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            else
            {
                if (!KrnlApi.Execute(ReadScript(GetCurrent())))
                {
                    MessageBox.Show("Error while executing script.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Console.WriteLine("Executed script");
            }
        }

        private void InjectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!KrnlApi.IsInjected())
            {
                if (KrnlApi.Inject() == Injector.InjectionStatus.path_doesnt_exist)
                {
                    Console.WriteLine("Krnl DLL not found");
                    KrnlApi.DownloadKrnlDLL();
                    MessageBox.Show("Krnl DLL not found. Please disable your antivirus and inject again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Console.WriteLine("Injecting KRNL");
            }
            else
            {
                MessageBox.Show("Already Injected!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            WriteScript(string.Empty, GetCurrent());
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
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    WriteScript(File.ReadAllText(openFileDialog.FileName), GetCurrent());
                    Console.WriteLine($"Opened {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while opening file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Error while opening a file. Message:");
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
                        File.WriteAllText(saveFileDialog.FileName, ReadScript(GetCurrent()));
                        Console.WriteLine($"Created {saveFileDialog.FileName}");
                    }
                    else
                    {
                        File.WriteAllText(saveFileDialog.FileName, ReadScript(GetCurrent()));
                        Console.WriteLine($"Saved {saveFileDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving/creating file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Error while saving a file. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region window handlers

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string file = files[0];
                    if (Path.GetExtension(file) == ".lua" || Path.GetExtension(file) == ".txt")
                    {
                        WriteScript(File.ReadAllText(file), GetCurrent());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while dropping file. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private void KillRbxBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Process process in Process.GetProcessesByName("RobloxPlayerBeta"))
            {
                try
                {
                    process.Kill();
                    Console.WriteLine($"Killed '{process.ProcessName}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while killing '{process.ProcessName}'. Message:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void OnProcessCreated(object sender, Process proc)
        {
            if (!KrnlApi.IsInjected())
            {
                KrnlApi.Inject();
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

        private void MultiInstanceSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.MultiInstance = MultiInstanceSwitch.IsOn;

            if (MultiInstanceSwitch.IsOn)
            {
                MultiInstanceHandle = Imports.CreateMutexW(nint.Zero, true, "ROBLOX_singletonMutex");
                Console.WriteLine(MultiInstanceHandle);
            }
            else
            {
                if (MultiInstanceHandle != nint.Zero)
                {
                    Imports.ReleaseMutex(MultiInstanceHandle);
                }
            }
        }

        private async void EditorMinimapSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.EditorMinimap = EditorMinimapSwitch.IsOn;

            await Task.Delay(100); //give monaco time to load on startup

            try
            {
                if (EditorMinimapSwitch.IsOn)
                {
                    GetCurrent().ExecuteScriptAsyncWhenPageLoaded("SwitchMinimap(" + "true" + ");", true);
                }
                else
                {
                    GetCurrent().ExecuteScriptAsyncWhenPageLoaded("SwitchMinimap(" + "false" + ");", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while enabling minimap. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private void AutoLaunchSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.AutoLaunch = AutoLaunchSwitch.IsOn;

            if (AutoLaunchSwitch.IsOn)
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key?.SetValue("Nexus", Assembly.GetExecutingAssembly().Location);
                key?.Close();
            }
            else
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                try
                {
                    key?.DeleteValue("Nexus");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    key?.Close();
                }
            }
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure? This will reset all settings!", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                    Console.WriteLine("Error while reseting settings. Message:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    ResizeBtn_Click(sender, new());
                }
                else
                {
                    DragMove();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Drag move exception. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private void ResizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Normal)
                {
                    BorderThickness = new(7);
                    WindowState = WindowState.Maximized;
                    ResizeSymbol.Symbol = ModernWpf.Controls.Symbol.BackToWindow;
                    ResizeIconMenu.Symbol = ModernWpf.Controls.Symbol.BackToWindow;
                    ResizeMenu.Header = "Restore";
                    ResizeTip.Content = "Restore";
                }
                else
                {
                    BorderThickness = new(0);
                    WindowState = WindowState.Normal;
                    ResizeSymbol.Symbol = ModernWpf.Controls.Symbol.FullScreen;
                    ResizeIconMenu.Symbol = ModernWpf.Controls.Symbol.FullScreen;
                    ResizeMenu.Header = "Maximize";
                    ResizeTip.Content = "Maximize";
                }
            });
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WindowState = WindowState.Minimized;
            });
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
                SetTitle("Settings - Nexus");
            }
            else
            {
                BackBtn_Click(sender, new());
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
                SetTitle("Script Hub - Nexus");
            }
            else
            {
                BackBtn_Click(sender, new());
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Visibility = Visibility.Visible;
            ScriptHubArea.Visibility = Visibility.Hidden;
            SettingsArea.Visibility = Visibility.Hidden;
            DisableBackBtn();
            SetTitle("Executor - Nexus");
        }

        private void TopMostSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.Topmost = TopMostSwitch.IsOn;

            Dispatcher.Invoke(() =>
            {
                Topmost = TopMostSwitch.IsOn;
            });
        }

        private void UnlockFpsSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Default.UnlockFPS = UnlockFpsSwitch.IsOn;

            if (UnlockFpsSwitch.IsOn)
            {
                KrnlApi.SetFrameRate(0);
            }
            else
            {
                KrnlApi.SetFrameRate(60);
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
            catch (Exception ex)
            {
                Console.WriteLine("Exception while moving tabs. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private ChromiumWebBrowser GetCurrent()
        {
            return EditTabs.SelectedContent as ChromiumWebBrowser;
        }

        private static ChromiumWebBrowser MakeEditor()
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\Monaco\\Monaco.html"))
            {
                MessageBox.Show("Monaco not found. Please reinstall.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                return new();
            }

            ChromiumWebBrowser textEditor = new(Environment.CurrentDirectory + "\\Monaco\\Monaco.html");
            textEditor.BrowserSettings.WindowlessFrameRate = 144;
            textEditor.BrowserSettings.JavascriptAccessClipboard = CefState.Enabled;
            textEditor.BrowserSettings.JavascriptDomPaste = CefState.Enabled;
            textEditor.AllowDrop = false;
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
            AddTabContext(tab, true);
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
                }
            }
        }

        #endregion

        #region misc handlers

        private void AddTabContext(TabItem tabItem, bool remove = true)
        {
            Dispatcher.Invoke(() =>
            {
                if (remove)
                {
                    ContextMenu menu = new();
                    MenuItem item = new()
                    {
                        Header = "Rename"
                    };
                    item.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        RenameHandler(tabItem);
                    };
                    MenuItem item2 = new()
                    {
                        Header = "Close"
                    };
                    item2.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        CloseTabHandler(tabItem);
                    };
                    menu.Items.Add(item);
                    menu.Items.Add(item2);
                    tabItem.ContextMenu = menu;
                }
                else
                {
                    ContextMenu menu = new();
                    MenuItem item = new()
                    {
                        Header = "Rename"
                    };
                    item.Click += delegate (object sender, RoutedEventArgs e)
                    {
                        RenameHandler(tabItem);
                    };
                    menu.Items.Add(item);
                    tabItem.ContextMenu = menu;
                }
            });
        }

        private void SetTitle(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Dispatcher.Invoke(() =>
                {
                    Title = title;
                });
            }
        }

        private static void RenameHandler(TabItem sender)
        {
            try
            {
                RenameDialog dialog = new(sender);
                dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while renaming tab.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Error while renaming tab. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private void CloseTabHandler(TabItem sender)
        {
            try
            {
                if (sender.Content is ChromiumWebBrowser browser)
                {
                    browser.Dispose();
                }
                EditTabs.Items.Remove(sender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while closing tab. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private static string ReadScript(ChromiumWebBrowser browser)
        {
            try
            {
                if (!browser.IsLoaded)
                {
                    return string.Empty;
                }
                string? text = WebBrowserExtensions.EvaluateScriptAsync(browser, "(function() { return GetText() })();", null, false).GetAwaiter().GetResult().Result.ToString();
                if (text != null)
                {
                    return text;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while reading script. Message:");
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private static void WriteScript(string script, ChromiumWebBrowser browser)
        {
            try
            {
                if (browser.IsLoaded)
                {
                    WebBrowserExtensions.EvaluateScriptAsync(browser, "SetText(`" + script.Replace("`", "\\`").Replace("\\", "\\\\") + "`)", null, false).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while writing script. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        private void SetWindowSize(int height, int width)
        {
            Dispatcher.Invoke(() =>
            {
                Height = height;
                Width = width;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
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
                if (Settings.Default.MultiInstance)
                {
                    MultiInstanceSwitch.IsOn = true;
                }
                if (Settings.Default.EditorMinimap)
                {
                    EditorMinimapSwitch.IsOn = true;
                }
                if (Settings.Default.AutoLaunch)
                {
                    AutoLaunchSwitch.IsOn = true;
                }
                SetWindowSize(Settings.Default.WindowHeight, Settings.Default.WindowWidth);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while loading settings. Message:");
                Console.WriteLine(ex.Message);
            }

            SetTitle("Executor - Nexus");

            //handle notifications
            string? NotifText;
            if (KrnlUpdated)
            {
                NotifText = "Krnl has been updated.";
            }
            else
            {
                NotifText = "Krnl is up to date.";
            }
            try
            {
                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                    if (args.ToString() == "inject")
                    {
                        KrnlApi.Inject();
                    }
                };

                new ToastContentBuilder()
                    .AddText("Nexus Loaded")

                    .AddText("Nexus loaded! Have fun exploiting!")

                    .AddText(NotifText)

                    .AddButton(new ToastButton()
                        .AddArgument("inject")
                        .SetContent("Inject to Roblox"))

                    .Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while showing a notification. User may be admin. Message:");
                Console.WriteLine(ex.Message);
            }
            InitHub();
            InitCustomHub();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            CloseWindow();
        }

        private async void CloseWindow()
        {
            Activate();
            try
            {
                if (EditTabs.Items.Count > 1)
                {
                    if (await new CloseDialog().ShowAsync() != ModernWpf.Controls.ContentDialogResult.Primary)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Closing Nexus");
            DebugConsole.Deinitialize();
            watcher.Dispose();
            Utils.Client.Dispose();
            Cef.Shutdown();

            //clear notifications
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while clearing notifications. User may be admin. Message:");
                Console.WriteLine(ex.Message);
            }

            //disable multi-instance
            if (MultiInstanceHandle != nint.Zero && MultiInstanceSwitch.IsOn)
            {
                Imports.ReleaseMutex(MultiInstanceHandle);
            }

            //save settings data
            try
            {
                if (WindowState == WindowState.Normal)
                {
                    Settings.Default.WindowHeight = (int)Height;
                    Settings.Default.WindowWidth = (int)Width;
                }
                Settings.Default.Save();
                Console.WriteLine("Saved settings!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while saving settings. Message:");
                Console.WriteLine(ex.Message);
            }
            Application.Current.Shutdown(0);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TitleBar.Opacity = DefaultOpacity;
                MainBorder.BorderBrush = (LinearGradientBrush)TryFindResource("Gradient");
            });
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TitleBar.Opacity = TitleOpacity;
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            });
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
                if (!Directory.Exists("./Scripts"))
                {
                    Directory.CreateDirectory("./Scripts");
                }

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
                        TreeViewItem titem = new()
                        {
                            Header = fileInfo.Name
                        };

                        ContextMenu menu = new();
                        MenuItem mitem = new()
                        {
                            Header = "Remove"
                        };
                        mitem.Click += (s, e) =>
                        {
                            try
                            {
                                File.Delete($"./Scripts/{directoryInfo.Name}/{titem.Header}");
                                Scripts.Items.Clear();
                                ScriptList_Loaded(sender, new());
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        };

                        menu.Items.Add(mitem);
                        titem.ContextMenu = menu;
                        item.Items.Add(titem);
                    }
                    foreach (FileInfo fileInfo2 in new DirectoryInfo($"./Scripts/{directoryInfo.Name}").GetFiles("*.lua"))
                    {
                        TreeViewItem titem = new()
                        {
                            Header = fileInfo2.Name
                        };

                        ContextMenu menu = new();
                        MenuItem mitem = new()
                        {
                            Header = "Remove"
                        };
                        mitem.Click += (s, e) =>
                        {
                            try
                            {
                                File.Delete($"./Scripts/{directoryInfo.Name}/{titem.Header}");
                                Scripts.Items.Clear();
                                ScriptList_Loaded(sender, new());
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        };

                        menu.Items.Add(mitem);
                        titem.ContextMenu = menu;
                        item.Items.Add(titem);
                    }
                    Scripts.Items.Add(item);
                }

                foreach (FileInfo fileInfo in new DirectoryInfo("./Scripts").GetFiles("*.txt"))
                {
                    TreeViewItem titem = new()
                    {
                        Header = fileInfo.Name
                    };

                    ContextMenu menu = new();
                    MenuItem mitem = new()
                    {
                        Header = "Remove"
                    };
                    mitem.Click += (s, e) =>
                    {
                        try
                        {
                            File.Delete($"./Scripts/{titem.Header}");
                            Scripts.Items.Clear();
                            ScriptList_Loaded(sender, new());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    };

                    menu.Items.Add(mitem);
                    titem.ContextMenu = menu;
                    Scripts.Items.Add(titem);
                }
                foreach (FileInfo fileInfo2 in new DirectoryInfo("./Scripts").GetFiles("*.lua"))
                {
                    TreeViewItem titem = new()
                    {
                        Header = fileInfo2.Name
                    };

                    ContextMenu menu = new();
                    MenuItem mitem = new()
                    {
                        Header = "Remove"
                    };
                    mitem.Click += (s, e) =>
                    {
                        try
                        {
                            File.Delete($"./Scripts/{titem.Header}");
                            Scripts.Items.Clear();
                            ScriptList_Loaded(sender, new());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    };

                    menu.Items.Add(mitem);
                    titem.ContextMenu = menu;
                    Scripts.Items.Add(titem);
                }
                Scripts.ToolTip = $"Scripts located in {Environment.CurrentDirectory}\\Scripts";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while adding scripts. Message:");
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
                            WriteScript(File.ReadAllText("Scripts\\" + header), GetCurrent());
                        }
                        else
                        {
                            WriteScript(File.ReadAllText("Scripts\\" + parent.Name + "\\" + header), GetCurrent());
                        }

                        BackBtn_Click(sender, new());
                        item.IsSelected = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while opening script. Message:");
                    Console.WriteLine(ex.Message);
                }
            }
        }

        //reload script list
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Scripts.Items.Clear();
            ScriptList_Loaded(sender, new());
        }

        //open in explorer
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = Environment.CurrentDirectory + "\\Scripts",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ScriptList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    string file = files[0];
                    if (File.Exists("Scripts\\" + Path.GetFileName(file)))
                    {
                        MessageBox.Show("File already exists!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }
                    else
                    {
                        File.Copy(file, "Scripts\\" + Path.GetFileName(file));
                    }
                    Scripts.Items.Clear();
                    ScriptList_Loaded(sender, new());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while dropping file. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region script hub

        private static async void ExecuteScript(string script, Button sender)
        {
            if (sender == null) return;

            Console.WriteLine("Executing script from Script Hub");
            sender.Content = "Executing...";
            await Task.Delay(1000);
            if (!KrnlApi.IsInjected())
            {
                sender.Content = "Not Injected!";
                await Task.Delay(5000);
                sender.Content = "Execute";
                return;
            }
            try
            {
                KrnlApi.Execute(script);
                sender.Content = "Executed!";
                await Task.Delay(3000);
                sender.Content = "Execute";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while downloading/executing script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Failed to execute, Message:");
                Console.WriteLine(ex.Message);
                sender.Content = "Error";
                await Task.Delay(7000);
                sender.Content = "Execute";
                return;
            }
        }

        private void InitHub()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    dynamic? json = JsonConvert.DeserializeObject(Utils.DownloadString("https://raw.githubusercontent.com/alluuzx/l/main/scripts.json"));
                    if (json != null)
                    {
                        foreach (dynamic item in json)
                        {
                            HubCard card = new()
                            {
                                Script = item.script.ToString()
                            };
                            card.TitleText.Content = item.title.ToString();
                            card.CreatorText.Content = item.CreatorText.ToString();
                            card.ScriptImage.Source = new BitmapImage(new(item.image.ToString()));
                            if (item.CreatorSize != null)
                            {
                                try
                                {
                                    card.CreatorText.FontSize = Convert.ToInt32(item.CreatorSize.ToString());
                                }
                                catch { continue; }
                            }
                            card.Execute.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card.Execute); };
                            card.Execute2.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card.Execute); };
                            card.EditBtn.Click += (sender, args) =>
                            {
                                WriteScript(card.Script, GetCurrent());
                                BackBtn_Click(sender, new());
                            };
                            ItemList.Children.Add(card);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while adding script hub items. Message:");
                    Console.WriteLine(ex.Message);
                }
            });
        }

        private static string ReadLine(string path, int lineNumber)
        {
            return File.ReadAllLines(path)[lineNumber];
        }

        private void InitCustomHub()
        {
            try
            {
                if (!Directory.Exists("./ScriptHub"))
                {
                    Directory.CreateDirectory("./ScriptHub");
                }

                foreach (FileInfo fileInfo in new DirectoryInfo("./ScriptHub").GetFiles("*.txt"))
                {
                    int length = File.ReadAllLines($"./ScriptHub/{fileInfo.Name}").Length;
                    if (length >= 1)
                    {
                        HubCard card = new()
                        {
                            Script = ReadLine($"./ScriptHub/{fileInfo.Name}", 0)
                        };
                        if (length >= 2)
                        {
                            card.TitleText.Content = ReadLine($"./ScriptHub/{fileInfo.Name}", 1);
                        }
                        else
                        {
                            card.TitleText.Content = "No Title";
                        }
                        if (length >= 3)
                        {
                            card.CreatorText.Content = ReadLine($"./ScriptHub/{fileInfo.Name}", 2);
                        }
                        else
                        {
                            card.CreatorText.Content = "No Creator";
                        }
                        if (length >= 4)
                        {
                            card.CreatorText.FontSize = Convert.ToInt32(ReadLine($"./ScriptHub/{fileInfo.Name}", 3));
                        }
                        if (length >= 5)
                        {
                            card.ScriptImage.Source = new BitmapImage(new(ReadLine($"./ScriptHub/{fileInfo.Name}", 4)));
                        }
                        card.Execute.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card.Execute); };
                        card.Execute2.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card.Execute); };
                        card.EditBtn.Click += (sender, args) =>
                        {
                            WriteScript(card.Script, GetCurrent());
                            BackBtn_Click(sender, new());
                        };
                        ItemList.Children.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while initializing Script Hub scripts. Message:");
                Console.WriteLine(ex.Message);
            }
        }

        #endregion
    }
}