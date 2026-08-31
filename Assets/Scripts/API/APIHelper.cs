using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
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
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();

                if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new System.Exception($"HTTP Error: {request.responseCode} - {request.error} | Response: {request.downloadHandler.text}");
                }

                response.WebRequestStatus = request.result;
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
                return response;
            }
            finally
            {
                Debug.Log("Finish Get Request");
            }

        }
        public static async Awaitable<TResponse> PostRequest<TResponse, TData>
            (string endpoint, Dictionary<string, string> formData) where TResponse : BaseResponse<TData>, new()
        {
            TResponse response = DefaultResponse<TResponse, TData>();
            using UnityWebRequest request = new UnityWebRequest(endpoint, "POST");
            request.downloadHandler = new DownloadHandlerBuffer();
            WWWForm form = new();
            foreach (KeyValuePair<string, string> item in formData)
            {
                form.AddField(item.Key, item.Value);
            }
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new System.Exception($"HTTP Error: {request.responseCode} - {request.error} | Response: {request.downloadHandler.text}");
                }

                response.WebRequestStatus = request.result;
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
                return response;
            }
            finally
            {
                Debug.Log("Finish Post Request");
            }

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
            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(form.data);
            request.SetRequestHeader("Content-Type", form.headers["Content-Type"]);

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new System.Exception($"HTTP Error: {request.responseCode} - {request.error} | Response: {request.downloadHandler.text}");
                }

                response.WebRequestStatus = request.result;
                response = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                response.ResponseCode = request.responseCode;
                return response;
            }
            finally
            {
                Debug.Log("Finish Put Request");
            }

        }

        private static TResponse DefaultResponse<TResponse, TData>() where TResponse : BaseResponse<TData>, new()
        {
            return new TResponse { WebRequestStatus = UnityWebRequest.Result.ProtocolError, Message = "", Data = default(TData), ResponseCode = 400 };
        }
    }
}
