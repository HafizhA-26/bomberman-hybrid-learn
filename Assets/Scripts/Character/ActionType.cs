namespace BombermanRL.Character
{
    public enum ActionType
    {
        Idle = 0,
        MoveUp = 1,
        MoveDown = 2,
        MoveLeft = 3,
        MoveRight = 4,
        PlaceBomb = 5
    }

    public enum KillType
    {
        NormalKill,
        FriendlyFire,
        Suicide
    }

    public enum AIType
    {
        RuleBased = 0,
        MLAgent = 1,
    }

}
