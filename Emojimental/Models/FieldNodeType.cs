using BreakInfinity;
namespace Emojimental.Models;

public enum FieldNodeType
{
    Energy,
    Snow,
    Smelter,
    Garden,
    Woodcutter,
    Builder,
    Bonfire,
    Recycler,
    Researcher,
    DustBreaker,
    Farm,
    Fireplace
}

public static class FieldNodeTypeExtensions
{
    public static FieldNodeType Normalize(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Garden => FieldNodeType.Farm,
            FieldNodeType.Fireplace => FieldNodeType.Bonfire,
            _ => type
        };

    public static string DisplayName(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => "Solar Panel",
            FieldNodeType.Snow => "Shovel",
            FieldNodeType.Smelter => "Smelter",
            FieldNodeType.Woodcutter => "Woodcutter",
            FieldNodeType.Builder => "Builder",
            FieldNodeType.Bonfire => "Bonfire",
            FieldNodeType.Recycler => "Recycler",
            FieldNodeType.Researcher => EmojiBank.Symbol(SymbolType.Research),
            FieldNodeType.DustBreaker => "Dust breaker",
            FieldNodeType.Farm => "Farm",
            _ => "Box"
        };

    public static string Icon(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => "☀️🪟",
            FieldNodeType.Snow => "🪏",
            FieldNodeType.Smelter => "♨️",
            FieldNodeType.Woodcutter => "🪓",
            FieldNodeType.Builder => "🔨",
            FieldNodeType.Bonfire => "🔥",
            FieldNodeType.Recycler => "♻️",
            FieldNodeType.Researcher => EmojiBank.Symbol(SymbolType.Research),
            FieldNodeType.DustBreaker => "🔨✨",
            FieldNodeType.Farm => "🚜",
            _ => "⬜"
        };

    public static string ProducedResourceName(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => "Energy",
            FieldNodeType.Snow => "Ice",
            FieldNodeType.Smelter => "Water",
            FieldNodeType.Woodcutter => "Wood",
            FieldNodeType.Builder => "Structure",
            FieldNodeType.Bonfire => "Energy",
            FieldNodeType.Recycler => "StarDust",
            FieldNodeType.Researcher => "ResearchPoint",
            FieldNodeType.DustBreaker => "StarDust",
            FieldNodeType.Farm => "Tree",
            _ => "Resource"
        };

    public static string ProducedResourceIcon(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => EmojiBank.Resource(ResourceType.Energy),
            FieldNodeType.Snow => EmojiBank.Resource(ResourceType.Ice),
            FieldNodeType.Smelter => EmojiBank.Resource(ResourceType.Water),
            FieldNodeType.Woodcutter => EmojiBank.Resource(ResourceType.Wood),
            FieldNodeType.Builder => "🏛️",
            FieldNodeType.Bonfire => EmojiBank.Resource(ResourceType.Energy),
            FieldNodeType.Recycler => EmojiBank.Resource(ResourceType.StarDust),
            FieldNodeType.Researcher => EmojiBank.Resource(ResourceType.ResearchPoint),
            FieldNodeType.DustBreaker => EmojiBank.Resource(ResourceType.StarDust),
            FieldNodeType.Farm => EmojiBank.Resource(ResourceType.Tree),
            _ => "✨"
        };

    public static string Infographic(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Snow => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Ice)}",
            FieldNodeType.Smelter => $"{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Resource(ResourceType.Ice)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Water)}",
            FieldNodeType.Woodcutter => $"{EmojiBank.Resource(ResourceType.Tree)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Wood)}",
            FieldNodeType.Builder => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Arrow)}🏛️",
            FieldNodeType.Bonfire => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Plus)}10{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Symbol(SymbolType.Arrow)}30{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Recycler => $"{EmojiBank.Symbol(SymbolType.Stars)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            FieldNodeType.Researcher => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.ResearchPoint)}",
            FieldNodeType.DustBreaker => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            FieldNodeType.Farm => $"{EmojiBank.Resource(ResourceType.Water)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Tree)}",
            _ => ""
        };

    public static string Effect(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Snow => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Ice)}",
            FieldNodeType.Smelter => $"{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Resource(ResourceType.Ice)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Water)}",
            FieldNodeType.Woodcutter => $"{EmojiBank.Resource(ResourceType.Tree)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Wood)}",
            FieldNodeType.Builder => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Arrow)}🏛️",
            FieldNodeType.Bonfire => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Plus)}10{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Symbol(SymbolType.Arrow)}30{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Recycler => $"{EmojiBank.Symbol(SymbolType.Stars)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            FieldNodeType.Researcher => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.ResearchPoint)}",
            FieldNodeType.DustBreaker => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            FieldNodeType.Farm => $"{EmojiBank.Resource(ResourceType.Water)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Tree)}",
            _ => ""
        };

    public static IEnumerable<(ResourceType Type, BigDouble Amount)> Consumption(this FieldNodeType type)
    {
        return type.Normalize() switch
        {
            FieldNodeType.Bonfire => new (ResourceType Type, BigDouble Amount)[]
            {
                (ResourceType.Wood, 1),
                (ResourceType.Energy, 1)
            },
            _ => Enumerable.Empty<(ResourceType Type, BigDouble Amount)>()
        };
    }

    public static string CssModifier(this FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Energy => "energy",
            FieldNodeType.Snow => "snow",
            FieldNodeType.Smelter => "smelter",
            FieldNodeType.Woodcutter => "woodcutter",
            FieldNodeType.Builder => "builder",
            FieldNodeType.Bonfire => "bonfire",
            FieldNodeType.Recycler => "recycler",
            FieldNodeType.Researcher => "researcher",
            FieldNodeType.DustBreaker => "dust-breaker",
            FieldNodeType.Farm => "farm",
            _ => "default"
        };
}
