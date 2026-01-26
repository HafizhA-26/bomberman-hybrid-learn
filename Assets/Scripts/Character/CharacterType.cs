namespace BombermanRL.Character
{
    [System.Flags]
    public enum CharacterType
    {
        None = 0,
        Player  = 1 << 0,
        Bandit  = 1 << 1,
        // TODO : Add other enemy type if game polished
    }
}
