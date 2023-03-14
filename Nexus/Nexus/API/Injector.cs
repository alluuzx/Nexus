using Nexus.Classes;
using Nexus.Kernel32;
using System;
using System.IO;
using System.Windows;

namespace Nexus.API
{
    /// <summary>
    /// Used to inject KRNL to Roblox.
    /// </summary>
    internal static partial class Injector
    {
        /// <summary>
        /// Inject KRNL to Roblox from the path.
        /// </summary>
        /// <param name="path"></param>
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
                Console.WriteLine("Error while injecting. Message:");
                Console.WriteLine(ex.Message);
                MessageBox.Show("Injector reloaded. Please inject again.", "Nexus", MessageBoxButton.OK, MessageBoxImage.Information);
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