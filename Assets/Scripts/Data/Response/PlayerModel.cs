using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

namespace BombermanRL
{
    public class PlayerModel
    {

        public string Username { get; set; }
        public string DeviceId { get; set; }
        public WinRecordModel[] WinRecords { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonConstructor]
        public PlayerModel(string username, string deviceId, WinRecordModel[] winRecords, DateTime createdAt)
        {
            Username = username;
            DeviceId = deviceId;
            WinRecords = winRecords;
            CreatedAt = createdAt;
        }
    }

    public class PlayerResponse : BaseResponse<PlayerModel>
    {
        public PlayerResponse() { }

        [JsonConstructor]
        public PlayerResponse(PlayerModel data, string message, string error, string status, UnityWebRequest.Result webRequestStatus, long responseCode) : base(data, message, error, status, webRequestStatus, responseCode)
        {

        }
    }
}
