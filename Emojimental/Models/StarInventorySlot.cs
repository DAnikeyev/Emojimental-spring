namespace Emojimental.Models;

public sealed class StarInventorySlot
{
    public StarInventorySlot(int index, Star? star)
    {
        Index = index;
        Star = star;
    }

    public int Index { get; }

    public Star? Star { get; set; }

    public bool HasStar => Star != null;

    public bool ConsumeStar()
    {
        if (Star == null)
            return false;

        Star = null;
        return true;
    }

    public void AddStar(Star star)
    {
        Star = star;
    }
}
