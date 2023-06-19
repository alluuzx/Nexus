using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static Nexus.Kernel32.Imports;

namespace Nexus.Debugging
{
    /// <summary>
    /// Console for debugging
    /// </summary>
    public static class DebugConsole
    {
        /// <summary>
        /// Is the console initialized or not
        /// </summary>
        internal static bool IsConsoleInitialized { get; private set; } = false;

        /// <summary>
        /// Console output as a string
        /// </summary>
        internal static string ConsoleOutput { get; private set; } = string.Empty;

        /// <summary>
        /// Create a console and set the output and input
        /// </summary>
        /// <param name="alwaysCreateNewConsole">Create a new console even if one exists</param>
        internal static void Initialize(bool alwaysCreateNewConsole = true)
        {
            if (IsConsoleInitialized && !alwaysCreateNewConsole)
            {
                WriteLine("Debug console already initialized.");
                return;
            }

            bool consoleAttached = true;

            if (alwaysCreateNewConsole
                || (AttachConsole((uint)Attributes.ATTACH_PARRENT) == 0
                && Marshal.GetLastWin32Error() != (uint)Attributes.ERROR_ACCESS_DENIED))
            {
                consoleAttached = AllocConsole() != 0;
            }

            if (consoleAttached)
            {
                InitializeOutStream();
                InitializeInStream();
                DisableConsoleClose();
                IsConsoleInitialized = true;
            }
        }

        /// <summary>
        /// Disable the console close button
        /// </summary>
        private static void DisableConsoleClose()
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), (int)Attributes.SC_CLOSE, (int)Attributes.MF_BYCOMMAND);
        }

        /// <summary>
        /// Set the output stream of the console
        /// </summary>
        private static void InitializeOutStream()
        {
            FileStream? fs = CreateFileStream("CONOUT$", (uint)Attributes.GENERIC_WRITE, (uint)Attributes.FILE_SHARE_WRITE, FileAccess.Write);
            if (fs != null)
            {
                StreamWriter writer = new(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
        }

        /// <summary>
        /// Set the input stream of the console
        /// </summary>
        private static void InitializeInStream()
        {
            FileStream? fs = CreateFileStream("CONIN$", (uint)Attributes.GENERIC_READ, (uint)Attributes.FILE_SHARE_READ, FileAccess.Read);
            if (fs != null)
            {
                Console.SetIn(new StreamReader(fs));
            }
        }

        /// <summary>
        /// Create a filestream using CreateFileW
        /// </summary>
        /// <param name="name"></param>
        /// <param name="win32DesiredAccess"></param>
        /// <param name="win32ShareMode"></param>
        /// <param name="dotNetFileAccess"></param>
        private static FileStream? CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
                                FileAccess dotNetFileAccess)
        {
            SafeFileHandle file = new(CreateFileW(name, win32DesiredAccess, win32ShareMode, nint.Zero, (uint)Attributes.OPEN_EXISTING, (uint)Attributes.FILE_ATTRIBUTE_NORMAL, nint.Zero), true);
            if (!file.IsInvalid)
            {
                FileStream fs = new(file, dotNetFileAccess);
                return fs;
            }
            return null;
        }

        /// <summary>
        /// Write a line to the console and add it to the output log
        /// </summary>
        /// <param name="text">The output to print</param>
        public static void WriteLine(string? text)
        {
            if (text != null)
            {
                ConsoleOutput += text + "\n";
            }
            Console.WriteLine(text);
        }

        [Flags]
        private enum Attributes : uint
        {
            GENERIC_WRITE = 0x40000000,
            GENERIC_READ = 0x80000000,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            OPEN_EXISTING = 0x00000003,
            FILE_ATTRIBUTE_NORMAL = 0x80,
            ERROR_ACCESS_DENIED = 5,
            ATTACH_PARRENT = 0xFFFFFFFF,
            SC_CLOSE = 0xF060,
            MF_BYCOMMAND = 0x00000000
        }
    }
}