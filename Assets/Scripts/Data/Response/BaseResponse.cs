using System;

namespace BombermanRL
{
    [Serializable]
    public class BaseResponse<T>
    {
        public T Data;
        public string Message;
        public string Status;
    }

}