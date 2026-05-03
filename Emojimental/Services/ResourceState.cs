using BreakInfinity;
using Emojimental.GameEngine.Values;
using Emojimental.Models;

namespace Emojimental.Services;

public sealed class ResourceState
{
    public Stat Energy { get; } = new();
    public Stat Ice { get; } = new();
    public Stat Coin { get; } = new();
    public Stat Sapling { get; } = new();
    public Stat Tree { get; } = new();
    public Stat Water { get; } = new();
    public Stat Happiness { get; } = new();
    public Stat ResearchPoint { get; } = new();
    public Stat Wood { get; } = new();
    public Stat StarDust { get; } = new();
    public Stat Flower { get; } = new();
    public Stat Carrot { get; } = new();

    public int SnowmanCount { get; set; }
    public int SaplingCount { get; set; }

    public BigDouble GetResource(ResourceType type, EvaluationContext ctx)
    {
        return ctx.Get(GetStat(type));
    }

    public Stat GetStat(ResourceType type) => type switch
    {
        ResourceType.Energy => Energy,
        ResourceType.Ice => Ice,
        ResourceType.Water => Water,
        ResourceType.Coin => Coin,
        ResourceType.Sapling => Sapling,
        ResourceType.Tree => Tree,
        ResourceType.Happiness => Happiness,
        ResourceType.ResearchPoint => ResearchPoint,
        ResourceType.Wood => Wood,
        ResourceType.StarDust => StarDust,
        ResourceType.Flower => Flower,
        ResourceType.Carrot => Carrot,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public void AddResource(ResourceType type, BigDouble delta)
    {
        var stat = GetStat(type);
        stat.BaseValue += delta;
    }
}
