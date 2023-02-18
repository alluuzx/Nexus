using Nexus.Classes;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nexus.API
{
    /// <summary>
    /// Used to inject KRNL to Roblox.
    /// </summary>
    internal partial class Injector
    {
        [LibraryImport("injector.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial InjectionStatus inject(string dll_path);

        /// <summary>
        /// Inject KRNL to Roblox from the path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns><see cref="InjectionStatus"/></returns>
        internal static InjectionStatus Inject(string path)
        {
            if (!File.Exists("injector.dll"))
            {
                try
                {
                    Utils.LoadUnmanagedLibraryFromResource(Assembly.GetExecutingAssembly(), "Nexus.injector.dll", "injector.dll");
                }
                catch (Exception)
                {
                    return InjectionStatus.failure;
                }
            }
            try
            {
                return inject(path);
            }
            catch (Exception)
            {
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
            no_rbx_proc
        }
    }
}