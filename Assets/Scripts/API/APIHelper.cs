using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace BombermanRL.API
{
    public static class APIHelper
    {
        public static async Awaitable<TResponse> GetRequest<TResponse, TData>(string endpoint)
            where TResponse : BaseResponse<TData>, new()
        {
            TResponse response = DefaultResponse<TResponse, TData>();

            using UnityWebRequest request = new UnityWebRequest(endpoint);
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();
            response.WebRequestStatus = request.result;
            if(request.result == UnityWebRequest.Result.Success)
            {
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
            }
            return response;

        }
        public static async Awaitable<TResponse> PostRequest<TResponse, TData>
            (string endpoint, Dictionary<string, string> formData) where TResponse : BaseResponse<TData>, new()
        {
            TResponse response = DefaultResponse<TResponse, TData>();
            using UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            WWWForm form = new();
            foreach (KeyValuePair<string, string> item in formData)
            {
                form.AddField(item.Key, item.Value);
            }
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);

            await request.SendWebRequest();
            response.WebRequestStatus = request.result;
            if (request.result == UnityWebRequest.Result.Success)
            {
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
            }
            return response;

        }

        public static async Awaitable<TResponse> PutRequest<TResponse, TData>
            (string endpoint, Dictionary<string, string> formData) where TResponse : BaseResponse<TData>, new()
        {
            TResponse response = DefaultResponse<TResponse, TData>();
            WWWForm form = new();
            foreach (KeyValuePair<string, string> item in formData)
            {
                form.AddField(item.Key, item.Value);
            }
            using UnityWebRequest request = UnityWebRequest.Put(endpoint, form.data);
            request.timeout = 5;
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);

            await request.SendWebRequest();
            response.WebRequestStatus = request.result;
            if (request.result == UnityWebRequest.Result.Success)
            {
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
            }
            return response;
        }

        private static TResponse DefaultResponse<TResponse, TData>() where TResponse : BaseResponse<TData>, new()
        {
            return new TResponse { WebRequestStatus = UnityWebRequest.Result.ConnectionError, Message = "", Data = default(TData), ResponseCode = 404 };
        }
    }
}
