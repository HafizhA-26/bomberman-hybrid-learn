using System;
using System.Collections.Generic;
using UnityEngine;

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
                callback?.Invoke(response);
            }catch(Exception e)
            {
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => GetPlayerData(deviceId, callback));
            }
        }

        public static async Awaitable UpdatePlayerName(Dictionary<string, string> data, Action<PlayerResponse> callback = null)
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);
            try
            {
                string url = $"{GetURL()}/players/update-name";
                PlayerResponse response = await APIHelper.PutRequest<PlayerResponse, PlayerModel>(url, data);
                GameInstance.Instance.ShowLoading(false);
                callback?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => UpdatePlayerName(data, callback));
            }
        }

        public static async Awaitable PostLeaderboard(Dictionary<string, string> data, Action<LeaderboardResponse> callback = null)
        {
            GameInstance.Instance.ShowLoading(true, 0.3f);
            try
            {
                string url = $"{GetURL()}/leaderboard";
                LeaderboardResponse response = await APIHelper.PutRequest<LeaderboardResponse, LeaderboardModel>(url, data);
                GameInstance.Instance.ShowLoading(false);
                callback?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                GameInstance.Instance.AlertHandler.ShowErrorPopup(e.Message, () => PostLeaderboard(data, callback));
            }
        }
    }
}
