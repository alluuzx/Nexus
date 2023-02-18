using System.IO;
using System.Reflection;

namespace Nexus.Classes
{
    public static class Utils
    {
        /// <summary>
        /// Load an unmanaged DLL from the application resources
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="libraryResourceName"></param>
        /// <param name="libraryName"></param>
        /// <returns>The DLL path as <see langword="string"/></returns>
        public static string? LoadUnmanagedLibraryFromResource(Assembly assembly, string libraryResourceName, string libraryName)
        {
            string? tempDllPath = string.Empty;
            using (Stream? s = assembly.GetManifestResourceStream(libraryResourceName))
            {
                if (s == null) return null;

                byte[]? data = new BinaryReader(s).ReadBytes((int)s.Length);

                string? assemblyPath = Path.GetDirectoryName(assembly.Location);
                if (assemblyPath == null) return null;

                tempDllPath = Path.Combine(assemblyPath, libraryName);

                File.WriteAllBytes(tempDllPath, data);
            }
            return tempDllPath;
        }
    }
}