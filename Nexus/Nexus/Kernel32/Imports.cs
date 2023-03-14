using System;
using System.Runtime.InteropServices;
using static Nexus.API.Injector;

namespace Nexus.Kernel32
{
    /// <summary>
    /// Kernel32 methods imported
    /// </summary>
    internal static partial class Imports
    {
        [LibraryImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        internal static partial int AllocConsole();

        [LibraryImport("kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        internal static partial uint AttachConsole(uint dwProcessId);

        [LibraryImport("kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FreeConsole();

        [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
        internal static partial nint CreateFileW(
              string lpFileName,
              uint dwDesiredAccess,
              uint dwShareMode,
              nint lpSecurityAttributes,
              uint dwCreationDisposition,
              uint dwFlagsAndAttributes,
              nint hTemplateFile
        );

        //not kernel32 but idc lol
        [LibraryImport("injector.dll", EntryPoint = "inject", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial InjectionStatus inject(string lpDllPath);

        [LibraryImport("kernel32.dll", EntryPoint = "WaitNamedPipeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool WaitNamedPipe(string lpNamedPipeName, int nTimeOut);

        [LibraryImport("kernel32.dll", EntryPoint = "CreateMutexW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint CreateMutexW(nint lpMutexAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInitialOwner, string lpName);

        [LibraryImport("kernel32.dll", EntryPoint = "ReleaseMutex", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReleaseMutex(nint hMutex);

        [LibraryImport("user32.dll", EntryPoint = "DeleteMenu", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [LibraryImport("user32.dll", EntryPoint = "GetSystemMenu", SetLastError = true)]
        internal static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

        [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        internal static partial IntPtr GetConsoleWindow();
    }
}