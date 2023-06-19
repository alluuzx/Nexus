namespace Nexus.Classes
{
    internal static class Globals
    {
        internal static string NexusVERSION { get; } = "3.2";

        internal static bool KrnlUpdated { get; set; } = false;

        internal static string ScripthubLink { get; } = "https://raw.githubusercontent.com/alluuzx/l/main/scripts.json";

        internal static nint MultiInstanceHandle { get; set; } = nint.Zero;

        internal static int UnlockedFPS { get; set; } = 0;

        internal static int AutoInjectInterval { get; set; } = 200;
    }
}