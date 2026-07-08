using Newtonsoft.Json;
using System;

namespace BombermanRL
{
    public class LeaderboardModel
    {
        public int Rank;
        public int BestRank;
        public string Username;
        public int ActionCount;
        public float PlayTime;
        public DateTime CreatedAt;
        public DateTime ModifiedAt;

        [JsonConstructor]
        public LeaderboardModel(int rank, string username, int actionCount, float playTime, int bestRank, DateTime createdAt, DateTime modifiedAt)
        {
            Rank = rank;
            BestRank = bestRank;
            Username = username;
            ActionCount = actionCount;
            PlayTime = playTime;
            CreatedAt = createdAt;
            ModifiedAt = modifiedAt;
        }
    }

    public class LeaderboardResponse : BaseResponse<LeaderboardModel> { }

}
