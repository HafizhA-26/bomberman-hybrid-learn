using Newtonsoft.Json;

namespace BombermanRL
{
    public class WinRecordModel
    {

        public int EnemyType { get; set; }
        public int WinCount { get; set; }
        public int LoseCount { get; set; }

        [JsonConstructor]
        public WinRecordModel(int enemyType, int winCount, int loseCount)
        {
            EnemyType = enemyType;
            WinCount = winCount;
            LoseCount = loseCount;
        }

    }
}
