using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static Nexus.API.Injector;
using static Nexus.Kernel32.Misc;

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

        //not winapi but idc lol
        [LibraryImport("injector.dll", EntryPoint = "inject", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
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
        internal static partial bool DeleteMenu(nint hMenu, int nPosition, int wFlags);

        [LibraryImport("user32.dll", EntryPoint = "GetSystemMenu", SetLastError = true)]
        internal static partial nint GetSystemMenu(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

        [LibraryImport("user32.dll", EntryPoint = "InsertMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InsertMenu(nint hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);

        [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        internal static partial nint GetConsoleWindow();

        [LibraryImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCursorPos(out POINT lpPoint);

        #region injector

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "OpenProcess")]
        internal static partial nint OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CloseHandle")]
        internal static partial int CloseHandle(nint hObject);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "GetProcAddress", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        internal static partial nint GetProcAddress(nint hModule, string lpProcName);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint GetModuleHandle(string lpModuleName);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "VirtualAllocEx")]
        internal static partial nint VirtualAllocEx(nint hProcess, nint lpAddress, nint dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesWritten);

        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateRemoteThread")]
        internal static partial nint CreateRemoteThread(nint hProcess, nint lpThreadAttribute, nint dwStackSize, nint lpStartAddress,
            nint lpParameter, uint dwCreationFlags, nint lpThreadId);

        #endregion
    }
}