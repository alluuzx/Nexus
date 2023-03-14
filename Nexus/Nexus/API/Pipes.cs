using Nexus.Kernel32;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.API
{
    internal static partial class Pipes
    {
        /// <summary>
        /// Is the KRNL pipe active
        /// </summary>
        /// <returns><see langword="true"/> if active, else <see langword="false"/></returns>
        internal static bool PipeActive()
        {
            bool result;
            try
            {
                if (!Imports.WaitNamedPipe(Path.GetFullPath("\\\\.\\pipe\\" + PipeName), 0))
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
            if (PipeActive())
            {
                Task.Run(delegate ()
                {
                    NamedPipeClientStream? namedPipeClientStream = null;
                    try
                    {
                        namedPipeClientStream = new(".", PipeName, PipeDirection.Out);
                        namedPipeClientStream.Connect();
                        byte[] bytes = Encoding.UTF8.GetBytes(input);
                        namedPipeClientStream.Write(bytes, 0, bytes.Length);
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing. Message:");
                        Console.WriteLine(ex.Message);
                        result = false;
                    }
                    finally
                    {
                        namedPipeClientStream?.Dispose();
                    }
                }).GetAwaiter().GetResult();
            }
            return result;
        }

        /// <summary>
        /// The KRNL pipe name
        /// </summary>
        private const string PipeName = "krnlpipe";
    }
}