using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

namespace BombermanRL
{
    public class LeaderboardModel
    {
        public int Rank;
        public string Username;
        public int ActionCount;
        public float PlayTime;

        [JsonConstructor]
        public LeaderboardModel(int rank, string username, int actionCount, float playTime)
        {
            Rank = rank;
            Username = username;
            ActionCount = actionCount;
            PlayTime = playTime;
        }
    }

    public class PlayerLeaderboard : LeaderboardModel
    {
        public int BestRank;
        public bool IsNewRecord;

        [JsonConstructor]
        public PlayerLeaderboard(int rank, string username, int actionCount, float playTime, int bestRank, bool isNewRecord) : base(rank, username, actionCount, playTime)
        {
            BestRank = bestRank;
            IsNewRecord = isNewRecord;
        }
    }

    public class LeaderboardResult
    {
        public LeaderboardModel[] TopRanks;
        public PlayerLeaderboard MyRank;
        public WinRecordModel WinRecord;

        [JsonConstructor]
        public LeaderboardResult(LeaderboardModel[] topRanks, PlayerLeaderboard myRank, WinRecordModel winRecord)
        {
            TopRanks = topRanks;
            MyRank = myRank;
            WinRecord = winRecord;
        }
    }

    public class LeaderboardResponse : BaseResponse<LeaderboardResult>
    {
        public LeaderboardResponse() { }

        [JsonConstructor]
        public LeaderboardResponse(LeaderboardResult data, 
            string message, 
            string error, 
            string status, 
            UnityWebRequest.Result webRequestStatus, 
            long responseCode) : base(data, message, error, status, webRequestStatus, responseCode)
        {

        }
    }

}
