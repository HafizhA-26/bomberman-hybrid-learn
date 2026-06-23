using System.Runtime.InteropServices;
using UnityEngine;

namespace BombermanRL
{
    public static class Util
    {
        [DllImport("__Internal")]
        public static extern int DetectPlatform();

        public static string GetEnemyStaticName(PlayMode type)
        {
            switch (type)
            {
                case PlayMode.None:
                    return "-";
                case PlayMode.ManualRuleBased:
                    return "Rule-Based Agent";
                case PlayMode.ManualMLAgent:
                    return "ML Agent";
                case PlayMode.OfflineTraining:
                    return "Offline ML Agent";
                case PlayMode.OnlineTraining:
                    return "Online ML Agent";
            }
            return "-";
        }
    }
}
