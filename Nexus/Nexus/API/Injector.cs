using Nexus.Classes;
using Nexus.Debugging;
using Nexus.Kernel32;
using System;
using System.IO;
using System.Windows;

namespace Nexus.API
{
    /// <summary>
    /// Used to inject KRNL to Roblox.
    /// </summary>
    internal static class Injector
    {
        /// <summary>
        /// Inject KRNL to Roblox from the path.
        /// </summary>
        /// <param name="path">Path to the DLL</param>
        /// <returns><see cref="InjectionStatus"/></returns>
        internal static InjectionStatus Inject(string path)
        {
            if (!Path.Exists(path)) return InjectionStatus.path_doesnt_exist;

            Utils.LoadInjector();

            try
            {
                return Imports.inject(path);
            }
            catch (Exception ex)
            {
                DebugConsole.WriteLine("Error while injecting. Message:");
                DebugConsole.WriteLine(ex.Message);
                MessageBox.Show("Injector reloaded. Please disable your antivirus and inject again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
                return InjectionStatus.failure;
            }
        }

        /// <summary>
        /// Values returned by the injector.
        /// </summary>
        internal enum InjectionStatus
        {
            failure = -1,
            success,
            loadimage_fail,
            no_rbx_proc,
            path_doesnt_exist
        }
    }
}