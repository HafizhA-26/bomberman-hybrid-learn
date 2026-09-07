using BombermanRL.Character;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BombermanRL
{
    public static class Util
    {
        [DllImport("__Internal")]
        public static extern int DetectPlatform();

        /// <summary>
        /// Get static enemy name for HUD counter name text
        /// </summary>
        /// <param name="type">Selected Playmode</param>
        /// <returns>String Enemy Name</returns>
        public static string GetEnemyStaticName(PlayMode type)
        {
            switch (type)
            {
                case PlayMode.None:
                    return "-";
                case PlayMode.ManualRuleBased:
                    return "Rule-Based Agent";
                case PlayMode.ManualMLAgent:
                    return "Machine Learning Agent";
                case PlayMode.OfflineTraining:
                    return "Offline ML Agent";
                case PlayMode.OnlineTraining:
                    return "Online ML Agent";
            }
            return "-";
        }

        /// <summary>
        /// Get time format for time counter in game
        /// </summary>
        /// <param name="elapsedTime">Time elapsed since match started</param>
        /// <returns>String formatted time fot UI time text</returns>
        public static string GetTimeFormatGameplay(float elapsedTime)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Get time format for time counter for result screen purpose
        /// </summary>
        /// <param name="elapsedTime">Time elapsed since match started</param>
        /// <returns>String formatted time fot UI time text</returns>
        public static string GetTimeFormatResult(float elapsedTime)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            int miliseconds = Mathf.FloorToInt((elapsedTime % 1f) * 1000f);
            return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, miliseconds);
        }

        public static ActionType GetActionFromDirection(Vector2 direction)
        {
            if (direction == Vector2.up)
                return ActionType.MoveUp;
            else if (direction == Vector2.down)
                return ActionType.MoveDown;
            else if (direction == Vector2.left)
                return ActionType.MoveLeft;
            else if (direction == Vector2.right)
                return ActionType.MoveRight;
            else 
                return ActionType.Idle;
        }
    }
}
