using Newtonsoft.Json;
using System;

namespace BombermanRL
{
    public class LeaderboardModel
    {
        public int Rank;
        public string Username;
        public int ActionCount;
        public float PlayTime;
        public bool IsLastRank;
        public DateTime CreatedAt;
        public DateTime ModifiedAt;

        [JsonConstructor]
        public LeaderboardModel(int rank, string username, int actionCount, float playTime, bool isLastRank, DateTime createdAt, DateTime modifiedAt)
        {
            Rank = rank;
            Username = username;
            ActionCount = actionCount;
            PlayTime = playTime;
            IsLastRank = isLastRank;
            CreatedAt = createdAt;
            ModifiedAt = modifiedAt;
        }
    }

    public class LeaderboardResponse : BaseResponse<LeaderboardModel> { }

}
