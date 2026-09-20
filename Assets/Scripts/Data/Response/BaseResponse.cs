using Newtonsoft.Json;
using System;
using UnityEngine.Networking;

namespace BombermanRL
{
    [Serializable]
    public abstract class BaseResponse<T>
    {
        public T Data;
        public string Message;
        public string Error;
        public string Status;
        public UnityWebRequest.Result WebRequestStatus;
        public long ResponseCode;

        [JsonConstructor]
        protected BaseResponse() { }

        [JsonConstructor]
        protected BaseResponse(T data, string message, string error, string status, UnityWebRequest.Result webRequestStatus, long responseCode)
        {
            Data = data;
            Message = message;
            Error = error;
            Status = status;
            WebRequestStatus = webRequestStatus;
            ResponseCode = responseCode;
        }
    }

}