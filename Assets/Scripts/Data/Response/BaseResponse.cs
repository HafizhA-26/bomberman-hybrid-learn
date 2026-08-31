using System;
using UnityEngine.Networking;

namespace BombermanRL
{
    [Serializable]
    public class BaseResponse<T>
    {
        public T Data;
        public string Message;
        public string Error;
        public string Status;
        public UnityWebRequest.Result WebRequestStatus;
        public long ResponseCode;
    }

}