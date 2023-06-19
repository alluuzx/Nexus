using Nexus.Debugging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using static Nexus.Kernel32.Imports;

namespace Nexus.Injector
{
    /// <summary>
    /// Dll injector for krnl
    /// </summary>
    internal static class CustomInjector
    {
        #region flags

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x00008000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        #endregion

        /// <summary>
        /// Results returned by the injector
        /// </summary>
        internal enum DllInjectionResult
        {
            DllNotFound,
            RobloxNotFound,
            InjectionFailed,
            Success
        }

        /// <summary>
        /// Inject to roblox
        /// </summary>
        /// <param name="sDllPath">The path to the dll</param>
        /// <param name="sProcName">Name of process to inject to</param>
        /// <returns>The result as <see cref="DllInjectionResult"/></returns>
        internal static DllInjectionResult Inject(string sDllPath, string sProcName = "RobloxPlayerBeta")
        {
            try
            {
                if (!File.Exists(sDllPath))
                {
                    return DllInjectionResult.DllNotFound;
                }

                uint _procId = 0;
                Process[] _procs = Process.GetProcesses();
                for (int i = 0; i < _procs.Length; i++)
                {
                    if (_procs[i].ProcessName == sProcName)
                    {
                        _procId = (uint)_procs[i].Id;
                        break;
                    }
                }

                if (_procId == 0)
                {
                    return DllInjectionResult.RobloxNotFound;
                }

                if (!StartInject(_procId, sDllPath))
                {
                    return DllInjectionResult.InjectionFailed;
                }

                return DllInjectionResult.Success;
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Exception while injecting. Message:");
                DebugConsole.WriteLine(ex.Message);
                return DllInjectionResult.InjectionFailed;
            }
        }

        /// <summary>
        /// Do the injection part
        /// </summary>
        /// <param name="pToBeInjected">Process ID to inject to</param>
        /// <param name="sDllPath">Path of the dll</param>
        /// <returns>true if success, else it will return false</returns>
        private static bool StartInject(uint pToBeInjected, string sDllPath)
        {
            //get the process handle
            nint handle = OpenProcess((uint)ProcessAccessFlags.All, 1, pToBeInjected);

            if (handle == nint.Zero)
            {
                return false;
            }

            //allocate memory for the dll path
            nint DLLMemory = VirtualAllocEx(handle, nint.Zero, sDllPath.Length, (uint)AllocationType.Reserve | (uint)AllocationType.Commit,
                (uint)MemoryProtection.ExecuteReadWrite);

            if (DLLMemory == nint.Zero)
            {
                return false;
            }

            //get the dll path as bytes
            byte[] bytes = Encoding.ASCII.GetBytes(sDllPath);

            //write the dll path to the allocated memory
            if (!WriteProcessMemory(handle, DLLMemory, bytes, (uint)bytes.Length, 0))
            {
                return false;
            }

            //get the loadlibraryA address from kernel32.dll
            nint loadLibraryAAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (loadLibraryAAddress == nint.Zero)
            {
                return false;
            }

            //call loadlibraryA inside the application and pass the text in the memory as a parameter so the process will load the dll from the path
            nint threadHandle = CreateRemoteThread(handle, nint.Zero, 0, loadLibraryAAddress, DLLMemory, 0, nint.Zero);

            if (threadHandle == nint.Zero)
            {
                return false;
            }

            //close the handles to clean up
            CloseHandle(handle);
            CloseHandle(threadHandle);

            return true;
        }
    }
}