using Nexus.Debugging;
using Nexus.Dialogs;
using System;
using System.Windows.Interop;
using static Nexus.Kernel32.Imports;

namespace Nexus.Classes
{
    internal sealed class SystemMenu
    {
        #region constants

        [Flags]
        private enum CONSTANTS : int
        {
            WM_SYSCOMMAND = 0x112,
            MF_SEPARATOR = 0x800,
            MF_BYPOSITION = 0x400,
            MF_STRING = 0x0
        }

        private const int _InjectID = 1000;
        private const int _KillRobloxID = 1001;
        private const int _AboutID = 1002;

        #endregion

        //sender window
        private readonly MainWindow Window;

        //is the menu added
        public bool IsAdded { get; private set; } = false;

        public SystemMenu(MainWindow window)
        {
            Window = window;
        }

        /// <summary>
        /// MainWindow handle
        /// </summary>
        public nint Handle
        {
            get
            {
                return new WindowInteropHelper(Window).Handle;
            }
        }

        /// <summary>
        /// Add items to the system menu
        /// </summary>
        public void Add()
        {
            try
            {
                if (!IsAdded)
                {
                    //get handle
                    nint systemMenuHandle = GetSystemMenu(Handle, false);

                    //create items
                    InsertMenu(systemMenuHandle, 5, (int)CONSTANTS.MF_BYPOSITION | (int)CONSTANTS.MF_SEPARATOR, 0, string.Empty);
                    InsertMenu(systemMenuHandle, 6, (int)CONSTANTS.MF_BYPOSITION, _InjectID, "Inject to Roblox");
                    InsertMenu(systemMenuHandle, 7, (int)CONSTANTS.MF_BYPOSITION, _KillRobloxID, "Kill Roblox");
                    InsertMenu(systemMenuHandle, 8, (int)CONSTANTS.MF_BYPOSITION, _AboutID, "About...");

                    //attach our wndproc handler to this
                    HwndSource source = HwndSource.FromHwnd(Handle);
                    source.AddHook(new HwndSourceHook(WndProc));

                    IsAdded = true;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while adding menu items. Message:");
                DebugConsole.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// WndProc handler
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        private nint WndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == (int)CONSTANTS.WM_SYSCOMMAND)
            {
                switch (wParam.ToInt32())
                {
                    case _InjectID:
                        Window.InjectBtn_Click(null, new());
                        handled = true;
                        break;

                    case _KillRobloxID:
                        Window.KillRbxBtn_Click(null, new());
                        handled = true;
                        break;

                    case _AboutID:
                        new AboutDialog().Show();
                        handled = true;
                        break;
                }
            }

            return nint.Zero;
        }
    }
}