using System.Runtime.InteropServices;

namespace BombermanRL
{
    public static class Util
    {
        [DllImport("__Internal")]
        public static extern int DetectPlatform();
    }
}
