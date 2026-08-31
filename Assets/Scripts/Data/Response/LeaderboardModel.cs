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

        [JsonConstructor]
        public LeaderboardModel(int rank, string username, int actionCount, float playTime, int bestRank, DateTime createdAt, DateTime modifiedAt)
        {
            Rank = rank;
            BestRank = bestRank;
            Username = username;
            ActionCount = actionCount;
            PlayTime = playTime;
        }
    }

    public class LeaderboardResponse : BaseResponse<LeaderboardModel> { }

}
