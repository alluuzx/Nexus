using CefSharp;
using CefSharp.Wpf;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Nexus.API;
using Nexus.Classes;
using Nexus.Controls;
using Nexus.Debugging;
using Nexus.Dialogs;
using Nexus.Injector;
using Nexus.Kernel32;
using Nexus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private List<ToggleSwitch> switches { get; } = new();

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
        private ProcessWatcher watcher { get; } = new("RobloxPlayerBeta");

        /// <summary>
        /// Default border thickness
        /// </summary>
        private Thickness DefaultBorder { get; } = new(0);

        /// <summary>
        /// Maximized border thickness
        /// </summary>
        private Thickness MaxBorder { get; } = new(7);

        /// <summary>
        /// Is the window original
        /// </summary>
        private readonly bool IsOriginal = false;

        /// <summary>
        /// Script hub items
        /// </summary>
        private List<HubCard> scripts { get; } = new();

        /// <summary>
        /// System menu instance (add hub items)
        /// </summary>
        private readonly SystemMenu menu;

        /// <summary>
        /// is the window closed
        /// </summary>
        private bool HasClosed = false;

        /// <summary>
        /// shell context menu for hub
        /// </summary>
        private static ShellContextMenu ShellMenu { get; } = new();

        #endregion

        public MainWindow(bool IsOriginal)
        {
            this.IsOriginal = IsOriginal;
            InitializeComponent();

            //create menu instance
            menu = new(this);

            //add all settings switches
            switches.Add(AutoInjectSwitch);
            switches.Add(UnlockFpsSwitch);
            switches.Add(TopMostSwitch);
            switches.Add(MultiInstanceSwitch);
            switches.Add(EditorMinimapSwitch);
            switches.Add(CustomInjectorSwitch);

            if (IsOriginal)
            {
                //get unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    Exception exception = (Exception)args.ExceptionObject;

                    DebugConsole.WriteLine("Unhandled exception caught. Message and ST:");
                    DebugConsole.WriteLine(exception.Message);
                    DebugConsole.WriteLine(exception.StackTrace);

                    if (!DebugConsole.IsConsoleInitialized)
                    {
                        File.WriteAllText("error.log", $"Message: {exception.Message}\nInner Exception: {exception.InnerException}\nStacktrace: {exception.StackTrace}\nConsole Output:\n{DebugConsole.ConsoleOutput}");
                        if (MessageBox.Show($"Nexus has encountered an unhandled exception. It will be closed now. Would you like to restart? Error Message: {exception.Message}", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes && File.Exists("Nexus.exe"))
                        {
                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = "Nexus.exe",
                                UseShellExecute = true
                            });
                        }
                        Environment.Exit(1);
                    }
                };
            }

            //tab system event handlers
            EditTabs.Loaded += delegate (object source, RoutedEventArgs e)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        //add tab click handler
                        EditTabs.GetTemplateItem<Button>("AddTabButton").Click += delegate (object s, RoutedEventArgs f)
                        {
                            //save resources because 15 chrome tabs is a lot of ram
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
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.WriteLine("Error while loading tab system. Message:");
                        DebugConsole.WriteLine(ex.Message);
                    }
                });
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
                DebugConsole.WriteLine("Executed script");
            }
        }

        public void InjectBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (!KrnlApi.IsInjected())
            {
                DebugConsole.WriteLine("Injecting KRNL");
                if (CustomInjectorSwitch.IsOn)
                {
                    switch (CustomInjector.Inject(Environment.CurrentDirectory + "\\krnl.dll"))
                    {
                        case CustomInjector.DllInjectionResult.DllNotFound:
                            DebugConsole.WriteLine("Krnl DLL not found");
                            KrnlApi.DownloadKrnlDLL();
                            MessageBox.Show("Krnl DLL not found. Please disable your antivirus and inject again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Warning);
                            break;

                        case CustomInjector.DllInjectionResult.RobloxNotFound:
                            MessageBox.Show("Start Roblox before injecting!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;

                        case CustomInjector.DllInjectionResult.InjectionFailed:
                            MessageBox.Show("Error while injecting to Roblox.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
                else
                {
                    if (KrnlApi.Inject() == API.Injector.InjectionStatus.path_doesnt_exist)
                    {
                        DebugConsole.WriteLine("Krnl DLL not found");
                        KrnlApi.DownloadKrnlDLL();
                        MessageBox.Show("Krnl DLL not found. Please disable your antivirus and inject again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
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
                    DebugConsole.WriteLine($"Opened {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while opening file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                DebugConsole.WriteLine("Error while opening a file. Message:");
                DebugConsole.WriteLine(ex.Message);
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
                        DebugConsole.WriteLine($"Created {saveFileDialog.FileName}");
                    }
                    else
                    {
                        File.WriteAllText(saveFileDialog.FileName, ReadScript(GetCurrent()));
                        DebugConsole.WriteLine($"Saved {saveFileDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while saving/creating file", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                DebugConsole.WriteLine("Error while saving a file. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        #endregion

        #region window handlers

        private void ChangeThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            new ThemeDialog().Show();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Dispatcher.Invoke(() =>
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
                    DebugConsole.WriteLine("Exception while dropping file. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        public void KillRbxBtn_Click(object? sender, RoutedEventArgs e)
        {
            foreach (Process process in Process.GetProcessesByName("RobloxPlayerBeta"))
            {
                try
                {
                    process.Kill();
                    DebugConsole.WriteLine($"Killed '{process.ProcessName}'");
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine($"Error while killing '{process.ProcessName}'. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            }
        }

        private void OnProcessCreated(object sender, Process proc)
        {
            Dispatcher.Invoke(async () =>
            {
                if (!KrnlApi.IsInjected())
                {
                    DebugConsole.WriteLine("Injecting with Autoinject");
                    if (CustomInjectorSwitch.IsOn)
                    {
                        await Task.Delay(2300); //wait for the roblox engine to start
                        CustomInjector.Inject(Environment.CurrentDirectory + "\\krnl.dll");
                    }
                    else
                    {
                        KrnlApi.Inject();
                    }
                }
            });
        }

        private void AutoInjectSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
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
            });
        }

        private void ChangeIntervalItem_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                await new ChangeIntervalDialog().ShowAsync();

                //set the interval of the timer because the timer value may be high and not update
                watcher.SetInterval(Globals.AutoInjectInterval);
            });
        }

        private void MultiInstanceSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.MultiInstance = MultiInstanceSwitch.IsOn;

                if (MultiInstanceSwitch.IsOn)
                {
                    Globals.MultiInstanceHandle = Imports.CreateMutexW(nint.Zero, true, "ROBLOX_singletonMutex");
                    DebugConsole.WriteLine("Mutex address: " + Globals.MultiInstanceHandle);
                }
                else
                {
                    if (Globals.MultiInstanceHandle != nint.Zero)
                    {
                        Imports.ReleaseMutex(Globals.MultiInstanceHandle);
                        Globals.MultiInstanceHandle = nint.Zero;
                    }
                }
            });
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
                DebugConsole.WriteLine("Error while enabling minimap. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        private void CustomInjectorSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Settings.Default.CustomInjector = CustomInjectorSwitch.IsOn;
            });
        }

        private void NewBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow window = new(false);
                window.NewBtn.Visibility = Visibility.Hidden;
                window.Show();
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while starting another Nexus instance. Message:");
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Error while starting another Nexus instance.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure? This will reset all settings!", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        foreach (ToggleSwitch _switch in switches)
                        {
                            _switch.IsOn = false;
                        }
                        Settings.Default.Reset();
                        DebugConsole.WriteLine("All settings reset");
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.WriteLine("Error while reseting settings. Message:");
                        DebugConsole.WriteLine(ex.Message);
                    }
                });
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    //maximize with titlebar double click
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
                    DebugConsole.WriteLine("Drag move exception. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        private void ResizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Normal)
                {
                    SystemCommands.MaximizeWindow(this);
                    MaximizeRect.Visibility = Visibility.Hidden;
                    RestoreIcon.Visibility = Visibility.Visible;
                    ResizeTip.Content = "Restore";
                }
                else
                {
                    SystemCommands.RestoreWindow(this);
                    MaximizeRect.Visibility = Visibility.Visible;
                    RestoreIcon.Visibility = Visibility.Hidden;
                    ResizeTip.Content = "Maximize";
                }
            });
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Maximized)
                {
                    BorderThickness = MaxBorder;
                    MaximizeRect.Visibility = Visibility.Hidden;
                    RestoreIcon.Visibility = Visibility.Visible;
                    ResizeTip.Content = "Restore";
                }
                else
                {
                    BorderThickness = DefaultBorder;
                    MaximizeRect.Visibility = Visibility.Visible;
                    RestoreIcon.Visibility = Visibility.Hidden;
                    ResizeTip.Content = "Maximize";
                }
            });
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (IsOriginal)
                {
                    CloseWindow();
                }
                else
                {
                    SystemCommands.CloseWindow(this);
                }
            });
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SystemCommands.MinimizeWindow(this);
            });
        }

        private void TitleBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //handle titlebar right click
            Dispatcher.Invoke(() =>
            {
                SystemCommands.ShowSystemMenu(this, Utils.GetCursorPosition());
            });
        }

        private void EnableBackBtn()
        {
            Dispatcher.Invoke(() =>
            {
                TitleText.Margin = new(35, 0, 0, 0);
                BackBtn.Visibility = Visibility.Visible;
            });
        }

        private void DisableBackBtn()
        {
            Dispatcher.Invoke(() =>
            {
                TitleText.Margin = new(0, 0, 0, 0);
                BackBtn.Visibility = Visibility.Hidden;
            });
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
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
            });
        }

        private void HubBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
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
            });
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ContentArea.Visibility = Visibility.Visible;
                ScriptHubArea.Visibility = Visibility.Hidden;
                SettingsArea.Visibility = Visibility.Hidden;
                DisableBackBtn();
                SetTitle("Executor - Nexus");
            });
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
                KrnlApi.SetFrameRate(Globals.UnlockedFPS);
            }
            else
            {
                KrnlApi.SetFrameRate(60);
            }
        }

        private void ChangeFPSItem_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                await new ChangeFPSDialog().ShowAsync();

                //update the framerate without needing to toggle
                if (UnlockFpsSwitch.IsOn)
                {
                    KrnlApi.SetFrameRate(Globals.UnlockedFPS);
                }
            });
        }

        #endregion

        #region Tab System

        private void ScrollTabs(object sender, MouseWheelEventArgs e)
        {
            if (tabScroller == null) return;

            tabScroller.ScrollToHorizontalOffset(tabScroller.HorizontalOffset + (e.Delta / 10));
        }

        private void MoveTab(object sender, MouseEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
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
                });
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Exception while moving tabs. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        private ChromiumWebBrowser GetCurrent()
        {
            return (ChromiumWebBrowser)EditTabs.SelectedContent;
        }

        private static ChromiumWebBrowser MakeEditor()
        {
            if (!File.Exists(Environment.CurrentDirectory + "\\Monaco\\Monaco.html"))
            {
                MessageBox.Show("Monaco not found. Please reinstall Nexus or add the missing file.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
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
            try
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
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while creating tab. Message:");
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Error while creating tab.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                return new();
            }
        }

        //allow drag and drop with tabs
        private void DropTab(object sender, DragEventArgs e)
        {
            Dispatcher.Invoke(() =>
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Dropping tab exception, Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
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
                DebugConsole.WriteLine("Error while renaming tab. Message:");
                DebugConsole.WriteLine(ex.Message);
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
                DebugConsole.WriteLine("Error while closing tab. Message:");
                DebugConsole.WriteLine(ex.Message);
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
                DebugConsole.WriteLine("Exception while reading script. Message:");
                DebugConsole.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private static void WriteScript(string script, ChromiumWebBrowser browser)
        {
            try
            {
                if (browser.IsLoaded)
                {
                    WebBrowserExtensions.EvaluateScriptAsync(browser, "SetText(`" + script.Replace("`", "\\`").Replace("\\", "\\\\") + "`)", null, false);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Exception while writing script. Message:");
                DebugConsole.WriteLine(ex.Message);
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
                Dispatcher.Invoke(() =>
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
                    if (Settings.Default.CustomInjector)
                    {
                        CustomInjectorSwitch.IsOn = true;
                    }

                    Globals.UnlockedFPS = Settings.Default.UnlockedFPS;
                    Globals.AutoInjectInterval = Settings.Default.AutoInjectCheckInterval;

                    SetWindowSize(Settings.Default.WindowHeight, Settings.Default.WindowWidth);

                    if (IsOriginal)
                    {
                        //load the theme save
                        ThemeDialog color = new();
                        switch (Settings.Default.ThemeColorId)
                        {
                            case 0:
                                color.SetDefaultBtn_Click(this, new());
                                break;

                            case 1:
                                color.SetDarkBtn_Click(this, new());
                                break;

                            case 2:
                                color.SetOldBtn_Click(this, new());
                                break;

                            case 3:
                                color.SetSpaceBtn_Click(this, new());
                                break;

                            case 4:
                                color.SetSeaBtn_Click(this, new());
                                break;

                            case 5:
                                color.SetNatureBtn_Click(this, new());
                                break;

                            case 6:
                                color.SetRedBtn_Click(this, new());
                                break;

                            case 7:
                                color.SetPastelBtn_Click(this, new());
                                break;

                            case 8:
                                color.SetAmberBtn_Click(this, new());
                                break;

                            case 9:
                                color.SetPurpleBtn_Click(this, new());
                                break;

                            case 10:
                                color.SetNightDayBtn_Click(this, new());
                                break;

                            case 11:
                                color.SetRainbowBtn_Click(this, new());
                                break;

                            case 12:
                                color.SetWinterBtn_Click(this, new());
                                break;

                            case 13:
                                color.SetDarkNightBtn_Click(this, new());
                                break;

                            case 14:
                                color.SetIceBtn_Click(this, new());
                                break;

                            case 15:
                                Color customColor = new()
                                {
                                    R = Settings.Default.CustomThemeColor.R,
                                    G = Settings.Default.CustomThemeColor.G,
                                    B = Settings.Default.CustomThemeColor.B,
                                    A = Settings.Default.CustomThemeColor.A
                                };
                                color.SetColors(new SolidColorBrush(customColor), customColor);
                                break;

                            default:
                                DebugConsole.WriteLine("Invalid theme save! Using default color.");
                                color.SetDefaultBtn_Click(this, new());
                                break;
                        }
                        color.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while loading settings. Message:");
                DebugConsole.WriteLine(ex.Message);
            }

            SetTitle("Executor - Nexus");

            //add custom items to the system menu
            menu.Add();

            if (IsOriginal)
            {
                //handle notifications
                string? NotifText;
                if (Globals.KrnlUpdated)
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
                            InjectBtn_Click(sender, new());
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
                    DebugConsole.WriteLine("Error while showing a notification. User may be admin. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            }

            //script hub
            InitHubs();

            if (IsOriginal)
            {
                string ver = Utils.DownloadString("https://raw.githubusercontent.com/alluuzx/l/main/ver.txt");
                DebugConsole.WriteLine("Newest version of Nexus: " + ver);

                //check for updates finally
                if (!ver.Trim().Equals(Globals.NexusVERSION))
                {
                    DebugConsole.WriteLine("Update available!");
                    MessageBox.Show("A new version of Nexus is available! Use the installer to update.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsOriginal && !HasClosed)
            {
                e.Cancel = true;
                CloseWindow();
            }
        }

        private async void CloseWindow()
        {
            try
            {
                //needed for the dialog to show
                Activate();
                if (EditTabs.Items.Count > 1)
                {
                    if (await new CloseDialog().ShowAsync() != ContentDialogResult.Primary)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine(ex.Message);
            }

            //exit code
            int code = 0;

            DebugConsole.WriteLine("Closing Nexus");

            try
            {
                Utils.Client.Dispose();
                Cef.Shutdown();
                ShellMenu.DestroyHandle();
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while cleaning up. Message:");
                DebugConsole.WriteLine(ex.Message);
                code = 1;
            }

            //clear notifications
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while clearing notifications. User may be admin. Message:");
                DebugConsole.WriteLine(ex.Message);
                code = 1;
            }

            //release the multi-instance mutex
            if (Globals.MultiInstanceHandle != nint.Zero && MultiInstanceSwitch.IsOn)
            {
                Imports.ReleaseMutex(Globals.MultiInstanceHandle);
            }

            //save settings data
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (WindowState == WindowState.Normal)
                    {
                        Settings.Default.WindowHeight = (int)Height;
                        Settings.Default.WindowWidth = (int)Width;
                    }

                    Settings.Default.UnlockedFPS = Globals.UnlockedFPS;
                    Settings.Default.AutoInjectCheckInterval = Globals.AutoInjectInterval;

                    Settings.Default.Save();
                    DebugConsole.WriteLine("Saved settings!");
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while saving settings. Message:");
                    DebugConsole.WriteLine(ex.Message);
                    code = 1;
                }
            });

            try
            {
                DebugConsole.WriteLine("Saving output log...");

                //write output
                File.WriteAllText("output.log", DebugConsole.ConsoleOutput);
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while saving output. Message:");
                DebugConsole.WriteLine(ex.Message);
                code = 1;
            }

            HasClosed = true;
            Application.Current.Shutdown(code);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TitleItems.Opacity = DefaultOpacity;
                MainBorder.BorderBrush = (Brush)TryFindResource("ThemeMainBrush");
            });
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TitleItems.Opacity = TitleOpacity;
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            });
        }

        private void ScriptListBtn_Checked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScriptHubBtn.IsChecked = false;
                ScriptHubItems.Visibility = Visibility.Hidden;
                SearchBar.Visibility = Visibility.Hidden;
                ScriptList.Visibility = Visibility.Visible;
            });
        }

        private void ScriptHubBtn_Checked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScriptListBtn.IsChecked = false;
                ScriptHubItems.Visibility = Visibility.Visible;
                SearchBar.Visibility = Visibility.Visible;
                ScriptList.Visibility = Visibility.Hidden;
            });
        }

        private void ScriptHubBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScriptHubItems.Visibility = Visibility.Hidden;
                SearchBar.Visibility = Visibility.Hidden;
            });
        }

        private void ScriptListBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ScriptList.Visibility = Visibility.Hidden;
            });
        }

        private void ScriptList_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Scripts.Items.Clear();

                    if (!Directory.Exists("./Scripts"))
                    {
                        Directory.CreateDirectory("./Scripts");
                    }

                    foreach (DirectoryInfo directoryInfo in new DirectoryInfo("./Scripts").GetDirectories())
                    {
                        try
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

                            item.ToolTip = $"Scripts located in {Environment.CurrentDirectory}\\Scripts\\{directoryInfo.Name}";

                            ContextMenu menuu = new();
                            MenuItem foldItem = new()
                            {
                                Header = "Remove",
                                Icon = new SymbolIcon(Symbol.Remove)
                            };
                            MenuItem menuItemlol = new()
                            {
                                Header = "Open in Explorer",
                                Icon = new SymbolIcon(Symbol.Folder)
                            };

                            MenuItem shellItem = new()
                            {
                                Header = "Windows Shell Menu",
                                Icon = new SymbolIcon(Symbol.More)
                            };
                            shellItem.Click += (s, e) =>
                            {
                                DirectoryInfo[] info = new DirectoryInfo[1];
                                info[0] = directoryInfo;
                                ShellMenu.ShowContextMenu(info, Utils.GetCursorPosition());
                            };

                            foldItem.Click += (s, e) =>
                            {
                                try
                                {
                                    Directory.Delete($"./Scripts/{directoryInfo.Name}");
                                    ScriptList_Loaded(sender, new());
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.WriteLine(ex.Message);
                                }
                            };
                            menuItemlol.Click += (s, e) =>
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo()
                                    {
                                        FileName = Environment.CurrentDirectory + "\\Scripts\\" + directoryInfo.Name,
                                        UseShellExecute = true
                                    });
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.WriteLine("Error while opening explorer. Message:");
                                    DebugConsole.WriteLine(ex.Message);
                                    MessageBox.Show("Error while opening explorer, is the folder deleted?", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            };
                            menuu.Items.Add(foldItem);
                            menuu.Items.Add(menuItemlol);
                            menuu.Items.Add(shellItem);
                            item.ContextMenu = menuu;

                            foreach (FileInfo fileInfo in new DirectoryInfo($"./Scripts/{directoryInfo.Name}").GetFiles("*.txt"))
                            {
                                TreeViewItem titem = new()
                                {
                                    Header = fileInfo.Name
                                };

                                ContextMenu menu = new();
                                MenuItem mitem = new()
                                {
                                    Header = "Remove",
                                    Icon = new SymbolIcon(Symbol.Remove)
                                };

                                MenuItem shell1Item = new()
                                {
                                    Header = "Windows Shell Menu",
                                    Icon = new SymbolIcon(Symbol.More)
                                };
                                shell1Item.Click += (s, e) =>
                                {
                                    FileInfo[] info = new FileInfo[1];
                                    info[0] = fileInfo;
                                    ShellMenu.ShowContextMenu(info, Utils.GetCursorPosition());
                                };

                                mitem.Click += (s, e) =>
                                {
                                    try
                                    {
                                        File.Delete($"./Scripts/{directoryInfo.Name}/{titem.Header}");
                                        ScriptList_Loaded(sender, new());
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.WriteLine(ex.Message);
                                    }
                                };

                                menu.Items.Add(mitem);
                                menu.Items.Add(shell1Item);
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
                                    Header = "Remove",
                                    Icon = new SymbolIcon(Symbol.Remove)
                                };
                                mitem.Click += (s, e) =>
                                {
                                    try
                                    {
                                        File.Delete($"./Scripts/{directoryInfo.Name}/{titem.Header}");
                                        ScriptList_Loaded(sender, new());
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.WriteLine(ex.Message);
                                    }
                                };

                                MenuItem shell1Item = new()
                                {
                                    Header = "Windows Shell Menu",
                                    Icon = new SymbolIcon(Symbol.More)
                                };
                                shell1Item.Click += (s, e) =>
                                {
                                    FileInfo[] info = new FileInfo[1];
                                    info[0] = fileInfo2;
                                    ShellMenu.ShowContextMenu(info, Utils.GetCursorPosition());
                                };

                                menu.Items.Add(mitem);
                                menu.Items.Add(shell1Item);
                                titem.ContextMenu = menu;
                                item.Items.Add(titem);
                            }
                            Scripts.Items.Add(item);
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.WriteLine(ex.Message);
                        }
                    }

                    foreach (FileInfo fileInfo in new DirectoryInfo("./Scripts").GetFiles("*.txt"))
                    {
                        try
                        {
                            TreeViewItem titem = new()
                            {
                                Header = fileInfo.Name
                            };

                            ContextMenu menu = new();
                            MenuItem mitem = new()
                            {
                                Header = "Remove",
                                Icon = new SymbolIcon(Symbol.Remove)
                            };
                            mitem.Click += (s, e) =>
                            {
                                try
                                {
                                    File.Delete($"./Scripts/{titem.Header}");
                                    ScriptList_Loaded(sender, new());
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.WriteLine(ex.Message);
                                }
                            };

                            MenuItem shell1Item = new()
                            {
                                Header = "Windows Shell Menu",
                                Icon = new SymbolIcon(Symbol.More)
                            };
                            shell1Item.Click += (s, e) =>
                            {
                                FileInfo[] info = new FileInfo[1];
                                info[0] = fileInfo;
                                ShellMenu.ShowContextMenu(info, Utils.GetCursorPosition());
                            };

                            menu.Items.Add(mitem);
                            menu.Items.Add(shell1Item);
                            titem.ContextMenu = menu;
                            Scripts.Items.Add(titem);
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.WriteLine(ex.Message);
                        }
                    }
                    foreach (FileInfo fileInfo2 in new DirectoryInfo("./Scripts").GetFiles("*.lua"))
                    {
                        try
                        {
                            TreeViewItem titem = new()
                            {
                                Header = fileInfo2.Name
                            };

                            ContextMenu menu = new();
                            MenuItem mitem = new()
                            {
                                Header = "Remove",
                                Icon = new SymbolIcon(Symbol.Remove)
                            };
                            mitem.Click += (s, e) =>
                            {
                                try
                                {
                                    File.Delete($"./Scripts/{titem.Header}");
                                    ScriptList_Loaded(sender, new());
                                }
                                catch (Exception ex)
                                {
                                    DebugConsole.WriteLine(ex.Message);
                                }
                            };

                            MenuItem shell1Item = new()
                            {
                                Header = "Windows Shell Menu",
                                Icon = new SymbolIcon(Symbol.More)
                            };
                            shell1Item.Click += (s, e) =>
                            {
                                FileInfo[] info = new FileInfo[1];
                                info[0] = fileInfo2;
                                ShellMenu.ShowContextMenu(info, Utils.GetCursorPosition());
                            };

                            menu.Items.Add(mitem);
                            menu.Items.Add(shell1Item);
                            titem.ContextMenu = menu;
                            Scripts.Items.Add(titem);
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.WriteLine(ex.Message);
                        }
                    }
                    Scripts.ToolTip = $"Scripts located in {Environment.CurrentDirectory}\\Scripts";
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Exception while adding scripts. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        private void ScriptList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Dispatcher.Invoke(() =>
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
                        DebugConsole.WriteLine("Exception while opening script. Message:");
                        DebugConsole.WriteLine(ex.Message);
                    }
                }
            });
        }

        //reload script list
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
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
                DebugConsole.WriteLine("Error while opening explorer. Message:");
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Error while opening scripts folder, is the folder deleted?", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //add script
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    OpenFileDialog ofd = new()
                    {
                        Title = "Add Script",
                        Filter = "Text (*.txt)|*.txt|Lua (*.lua)|*.lua|All Files (*.*)|*.*",
                        DefaultExt = ".txt",
                        CheckFileExists = true,
                        CheckPathExists = true
                    };
                    if (ofd.ShowDialog() == true)
                    {
                        foreach (string file in ofd.FileNames)
                        {
                            if (Path.GetExtension(file) != ".txt" && Path.GetExtension(file) != ".lua")
                            {
                                if (MessageBox.Show("This file format is not supported by the script list. Are you sure you want to add this file?", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                                {
                                    continue;
                                }
                            }
                            if (File.Exists("Scripts\\" + Path.GetFileName(file)))
                            {
                                MessageBox.Show("File already exists!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Stop);
                                continue;
                            }
                            else
                            {
                                File.Copy(file, "Scripts\\" + Path.GetFileName(file));
                            }
                            Scripts.Items.Clear();
                            ScriptList_Loaded(sender, new());
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while adding file. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        //new folder menu
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                try
                {
                    await new NewFolderDialog().ShowAsync();
                    ScriptList_Loaded(sender, new());
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while creating folder. Message:");
                    DebugConsole.WriteLine(ex.Message);
                    MessageBox.Show("Error while creating folder.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void ScriptList_Drop(object sender, DragEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            if (Path.GetExtension(file) != ".txt" && Path.GetExtension(file) != ".lua")
                            {
                                if (MessageBox.Show("This file format is not supported by the script list. Are you sure you want to add this file?", "Nexus", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                                {
                                    continue;
                                }
                            }
                            if (File.Exists("Scripts\\" + Path.GetFileName(file)))
                            {
                                MessageBox.Show("File already exists!", "Nexus", MessageBoxButton.OK, MessageBoxImage.Stop);
                                continue;
                            }
                            else
                            {
                                File.Copy(file, "Scripts\\" + Path.GetFileName(file));
                            }
                            ScriptList_Loaded(sender, new());
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Exception while dropping file. Message:");
                    DebugConsole.WriteLine(ex.Message);
                    MessageBox.Show("Exception while dropping file.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        #endregion

        #region script hub

        /// <summary>
        /// Execute a script hub card script
        /// </summary>
        /// <param name="script"></param>
        /// <param name="sender"></param>
        private void ExecuteScript(string script, HubCard sender)
        {
            Dispatcher.Invoke(async () =>
            {
                if (sender == null) return;
                Grid defaultContent = sender.ButtonContent;
                try
                {
                    DebugConsole.WriteLine("Executing script from Script Hub");
                    sender.Execute.Content = "Executing...";
                    await Task.Delay(1000);
                    if (!KrnlApi.IsInjected())
                    {
                        sender.Execute.Content = "Not Injected!";
                        await Task.Delay(4500);
                        sender.Execute.Content = defaultContent;
                        return;
                    }
                    KrnlApi.Execute(script);
                    sender.Execute.Content = "Executed!";
                    await Task.Delay(3000);
                    sender.Execute.Content = defaultContent;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while downloading/executing script", "Nexus", MessageBoxButton.OK, MessageBoxImage.Error);
                    DebugConsole.WriteLine("Failed to execute, Message:");
                    DebugConsole.WriteLine(ex.Message);
                    sender.Execute.Content = "Error";
                    await Task.Delay(7000);
                    sender.Execute.Content = defaultContent;
                    return;
                }
            });
        }

        /// <summary>
        /// Initialize / refresh the script hub and custom hub
        /// </summary>
        public void InitHubs()
        {
            scripts.Clear();
            ItemList.Children.Clear();
            InitHub();
            InitCustomHub();
        }

        /// <summary>
        /// Initialize the cloud hub
        /// </summary>
        private void InitHub()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    dynamic? json = JsonConvert.DeserializeObject(Utils.DownloadString(Globals.ScripthubLink));
                    if (json != null)
                    {
                        foreach (dynamic item in json)
                        {
                            try
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
                                    card.CreatorText.FontSize = Convert.ToInt32(item.CreatorSize.ToString());
                                }
                                card.Execute.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card); };
                                card.Execute2.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card); };
                                card.EditBtn.Click += (sender, args) =>
                                {
                                    WriteScript(card.Script, GetCurrent());
                                    BackBtn_Click(sender, new());
                                };
                                ItemList.Children.Add(card);
                                scripts.Add(card);
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.WriteLine("Error while adding a script hub item. Message:");
                                DebugConsole.WriteLine(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while adding script hub items. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        /// <summary>
        /// Initialize the file hub
        /// </summary>
        private void InitCustomHub()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!Directory.Exists("./ScriptHub"))
                    {
                        Directory.CreateDirectory("./ScriptHub");
                    }

                    foreach (FileInfo fileInfo in new DirectoryInfo("./ScriptHub").GetFiles("*.txt"))
                    {
                        try
                        {
                            int length = File.ReadAllLines($"./ScriptHub/{fileInfo.Name}").Length;
                            if (length >= 1)
                            {
                                HubCard card = new()
                                {
                                    Script = Utils.ReadLine($"./ScriptHub/{fileInfo.Name}", 0)
                                };
                                if (length >= 2)
                                {
                                    card.TitleText.Content = Utils.ReadLine($"./ScriptHub/{fileInfo.Name}", 1);
                                }
                                else
                                {
                                    card.TitleText.Content = "No Title";
                                }
                                if (length >= 3)
                                {
                                    card.CreatorText.Content = Utils.ReadLine($"./ScriptHub/{fileInfo.Name}", 2);
                                }
                                else
                                {
                                    card.CreatorText.Content = "No Creator";
                                }
                                if (length >= 4)
                                {
                                    card.CreatorText.FontSize = Convert.ToInt32(Utils.ReadLine($"./ScriptHub/{fileInfo.Name}", 3));
                                }
                                if (length >= 5)
                                {
                                    card.ScriptImage.Source = new BitmapImage(new(Utils.ReadLine($"./ScriptHub/{fileInfo.Name}", 4)));
                                }
                                card.Execute.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card); };
                                card.Execute2.Click += delegate (object sender, RoutedEventArgs e) { ExecuteScript(card.Script, card); };
                                card.EditBtn.Click += (sender, args) =>
                                {
                                    WriteScript(card.Script, GetCurrent());
                                    BackBtn_Click(sender, new());
                                };
                                ItemList.Children.Add(card);
                                scripts.Add(card);
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.WriteLine("Error while adding a custom script hub item. Message:");
                            DebugConsole.WriteLine(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while initializing script hub files. Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        private void SearchHub_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (scripts.Count == 0 || SearchHub.Text == "Search") return;

                    ItemList.Children.Clear();

                    foreach (HubCard item in scripts)
                    {
                        string? text = item.TitleText.Content.ToString();
                        if (text != null && text.ToLower().Contains(SearchHub.Text.ToLower()))
                        {
                            ItemList.Children.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.WriteLine("Error while finding script, Message:");
                    DebugConsole.WriteLine(ex.Message);
                }
            });
        }

        private void SearchHub_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                SearchHub.Clear();
                SearchHub.Opacity = 1;
            });
        }

        private void SearchHub_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (SearchHub.Text == string.Empty || string.IsNullOrWhiteSpace(SearchHub.Text))
                {
                    SearchHub.Text = "Search";
                    SearchHub.Opacity = .6;
                }
            });
        }

        private void RefreshHubItem_Click(object sender, RoutedEventArgs e)
        {
            InitHubs();
        }

        #endregion
    }
}