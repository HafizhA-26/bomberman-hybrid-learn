using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace BombermanRL.API
{
    public static class APIManager
    {
        private static string GetURL() => GameInstance.Instance.BASE_URL;

        public static async Awaitable GetPlayerData(string deviceId, Action<PlayerResponse> callback = null)
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);
            try
            {
                string url = $"{GetURL()}/players/check/{deviceId}";
                PlayerResponse response = await APIHelper.GetRequest<PlayerResponse, PlayerModel>(url);
                GameInstance.Instance.ShowLoading(false);

                Debug.Log("[Get player data] " + JsonConvert.SerializeObject(response));

                if (response.WebRequestStatus == UnityWebRequest.Result.ConnectionError || response.WebRequestStatus == UnityWebRequest.Result.ProtocolError)
                    GameInstance.Instance.AlertHandler.ShowErrorPopup(response, () => GetPlayerData(deviceId, callback));
                
                callback?.Invoke(response);
            }
            catch(Exception e)
            {
                GameInstance.Instance.ShowLoading(false);
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => GetPlayerData(deviceId, callback));
            }
        }

        public static async Awaitable UpdatePlayerInfo(Dictionary<string, string> data, Action<PlayerResponse> callback = null)
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);
            try
            {
                string url = $"{GetURL()}/players/update";
                PlayerResponse response = await APIHelper.PutRequest<PlayerResponse, PlayerModel>(url, data);
                GameInstance.Instance.ShowLoading(false);

                Debug.Log("[Update player info] " + JsonConvert.SerializeObject(response));

                if (response.WebRequestStatus == UnityWebRequest.Result.ConnectionError || response.WebRequestStatus == UnityWebRequest.Result.ProtocolError)
                    GameInstance.Instance.AlertHandler.ShowErrorPopup(response, () => UpdatePlayerInfo(data, callback));

                callback?.Invoke(response);
            }
            catch (Exception e)
            {
                GameInstance.Instance.ShowLoading(false);
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => UpdatePlayerInfo(data, callback));
            }
        }

        public static async Awaitable PostLeaderboard(Dictionary<string, string> data, Action<LeaderboardResponse> callback = null)
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);
            try
            {
                string url = $"{GetURL()}/leaderboard";
                LeaderboardResponse response = await APIHelper.PostRequest<LeaderboardResponse, LeaderboardResult>(url, data);
                GameInstance.Instance.ShowLoading(false);

                Debug.Log("[Post Leaderboard] " + JsonConvert.SerializeObject(response));

                if (response.WebRequestStatus == UnityWebRequest.Result.ConnectionError || response.WebRequestStatus == UnityWebRequest.Result.ProtocolError)
                    GameInstance.Instance.AlertHandler.ShowErrorPopup(response, () => PostLeaderboard(data, callback));

                callback?.Invoke(response);
            }
            catch (Exception e)
            {
                GameInstance.Instance.ShowLoading(false);
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => PostLeaderboard(data, callback));
            }
        }
    }
}
