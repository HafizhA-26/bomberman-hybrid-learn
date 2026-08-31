using System;

namespace BombermanRL
{
    public class PlayerModel
    {
        public string Username { get; set; }
        public string DeviceId { get; set; }
        public DateTime CreatedAt { get; set; }

    }

    public class PlayerResponse : BaseResponse<PlayerModel> { }
}
