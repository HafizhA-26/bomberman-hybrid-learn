using UnityEngine;

namespace BombermanRL.Character
{
    public class ActionCooldown
    {
        private float cooldown;
        private float lasttime;

        public ActionCooldown(float cooldown)
        {
            this.cooldown = cooldown;
            lasttime = -cooldown;
        }

        public bool CanAction()
        {
            if(Time.time < lasttime + cooldown)
                return false;

            lasttime = Time.time;
            return true;
        }
    }
}
