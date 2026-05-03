using BreakInfinity;

namespace Emojimental.Models;

public enum StarType
{
    Yellow,
    Blue,
    Legendary
}

public sealed class Star
{
    public StarType Type { get; set; }
    public BigDouble ProductionMultiplier { get; set; } = 1;
    public double CooldownReduction { get; set; } = 0;
    public int RecycleValue { get; set; }

    public Star(StarType type, BigDouble productionMultiplier, double cooldownReduction, int recycleValue)
    {
        Type = type;
        ProductionMultiplier = productionMultiplier;
        CooldownReduction = cooldownReduction;
        RecycleValue = recycleValue;
    }
}
