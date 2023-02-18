using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.API
{
    internal static partial class Pipes
    {
        [LibraryImport("kernel32.dll", EntryPoint = "WaitNamedPipeW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool WaitNamedPipe(string name, int timeout);

        /// <summary>
        /// Is the KRNL pipe active
        /// </summary>
        /// <returns><see langword="true"/> if active, else <see langword="false"/></returns>
        internal static bool PipeActive()
        {
            bool result;
            try
            {
                if (!WaitNamedPipe(Path.GetFullPath("\\\\.\\pipe\\" + PipeName), 0))
                {
                    result = false;
                }
                else
                {
                    result = true;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Send a script to KRNL inside Roblox
        /// </summary>
        /// <param name="input"></param>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        internal static bool PassString(string input)
        {
            bool result = false;
            Task.Run(delegate ()
            {
                if (PipeActive())
                {
                    try
                    {
                        using NamedPipeClientStream namedPipeClientStream = new(".", PipeName, PipeDirection.Out);
                        namedPipeClientStream.Connect();
                        using (StreamWriter streamWriter = new(namedPipeClientStream, Encoding.ASCII, input.Length))
                        {
                            streamWriter.Write(input);
                            streamWriter.Dispose();
                        }
                        namedPipeClientStream.Dispose();
                        return;
                    }
                    catch (Exception)
                    {
                        result = false;
                        return;
                    }
                }
                result = false;
            }).GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// The KRNL pipe name
        /// </summary>
        private const string PipeName = "krnlpipe";
    }
}