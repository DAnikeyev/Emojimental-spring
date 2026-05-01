namespace Emojimental.Models;

public sealed class StarInventorySlot
{
    public StarInventorySlot(int index, bool hasStar)
    {
        Index = index;
        HasStar = hasStar;
    }

    public int Index { get; }

    public bool HasStar { get; set; }

    public bool ConsumeStar()
    {
        if (!HasStar)
            return false;

        HasStar = false;
        return true;
    }

    public void AddStar()
    {
        HasStar = true;
    }
}
