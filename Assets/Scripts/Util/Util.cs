using System.Runtime.InteropServices;
using UnityEngine;

namespace BombermanRL
{
    public static class Util
    {
        [DllImport("__Internal")]
        public static extern int DetectPlatform();

        public static string GetEnemyStaticName(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.None:
                    return "-";
                case EnemyType.RuleBasedAgent:
                    return "Rule-Based Agent";
                case EnemyType.MlAgent:
                    return "ML Agent";
            }
            return "-";
        }
    }
}
