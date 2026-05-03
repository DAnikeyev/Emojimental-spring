namespace Emojimental.Models;

public sealed record PassiveStatStarSlot(PassiveStatType Type, Star? Star = null)
{
    public bool HasStar => Star != null;
}
