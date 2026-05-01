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
    Recycler
}

public static class FieldNodeTypeExtensions
{
    public static string DisplayName(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "Solar Panel",
            FieldNodeType.Snow => "Shovel",
            FieldNodeType.Smelter => "Smelter",
            FieldNodeType.Garden => "Garden",
            FieldNodeType.Woodcutter => "Woodcutter",
            FieldNodeType.Builder => "Builder",
            FieldNodeType.Bonfire => "Bonfire",
            FieldNodeType.Recycler => "Recycler",
            _ => "Box"
        };

    public static string Icon(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "☀️🪟",
            FieldNodeType.Snow => "🪏",
            FieldNodeType.Smelter => "♨️",
            FieldNodeType.Garden => "🪴🧑‍🌾",
            FieldNodeType.Woodcutter => "🪓",
            FieldNodeType.Builder => "🔨",
            FieldNodeType.Bonfire => "🔥",
            FieldNodeType.Recycler => "♻️",
            _ => "⬜"
        };

    public static string ProducedResourceName(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "Energy",
            FieldNodeType.Snow => "Ice",
            FieldNodeType.Smelter => "Water",
            FieldNodeType.Garden => "Tree",
            FieldNodeType.Woodcutter => "Wood",
            FieldNodeType.Builder => "Structure",
            FieldNodeType.Bonfire => "Energy",
            FieldNodeType.Recycler => "StarDust",
            _ => "Resource"
        };

    public static string ProducedResourceIcon(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => EmojiBank.Resource(ResourceType.Energy),
            FieldNodeType.Snow => EmojiBank.Resource(ResourceType.Ice),
            FieldNodeType.Smelter => EmojiBank.Resource(ResourceType.Water),
            FieldNodeType.Garden => EmojiBank.Resource(ResourceType.Tree),
            FieldNodeType.Woodcutter => EmojiBank.Resource(ResourceType.Wood),
            FieldNodeType.Builder => "🏛️",
            FieldNodeType.Bonfire => EmojiBank.Resource(ResourceType.Energy),
            FieldNodeType.Recycler => EmojiBank.Resource(ResourceType.StarDust),
            _ => "✨"
        };

    public static string Infographic(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Snow => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Ice)}",
            FieldNodeType.Smelter => $"{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Resource(ResourceType.Ice)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Water)}",
            FieldNodeType.Garden => $"{EmojiBank.Resource(ResourceType.Water)}{EmojiBank.Resource(ResourceType.Sapling)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Tree)}",
            FieldNodeType.Woodcutter => $"{EmojiBank.Resource(ResourceType.Tree)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Wood)}",
            FieldNodeType.Builder => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Arrow)}🏛️",
            FieldNodeType.Bonfire => $"({EmojiBank.Resource(ResourceType.Wood)}+10{EmojiBank.Resource(ResourceType.Energy)}){EmojiBank.Symbol(SymbolType.Arrow)}20{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Recycler => $"{EmojiBank.Symbol(SymbolType.Stars)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            _ => ""
        };

    public static string Effect(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Snow => $"{EmojiBank.Symbol(SymbolType.Nothing)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Ice)}",
            FieldNodeType.Smelter => $"{EmojiBank.Resource(ResourceType.Energy)}{EmojiBank.Resource(ResourceType.Ice)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Water)}",
            FieldNodeType.Garden => $"{EmojiBank.Resource(ResourceType.Water)}{EmojiBank.Resource(ResourceType.Sapling)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Tree)}",
            FieldNodeType.Woodcutter => $"{EmojiBank.Resource(ResourceType.Tree)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.Wood)}",
            FieldNodeType.Builder => $"{EmojiBank.Resource(ResourceType.Wood)}{EmojiBank.Symbol(SymbolType.Arrow)}🏛️",
            FieldNodeType.Bonfire => $"({EmojiBank.Resource(ResourceType.Wood)}+10{EmojiBank.Resource(ResourceType.Energy)}){EmojiBank.Symbol(SymbolType.Arrow)}20{EmojiBank.Resource(ResourceType.Energy)}",
            FieldNodeType.Recycler => $"{EmojiBank.Symbol(SymbolType.Stars)}{EmojiBank.Symbol(SymbolType.Arrow)}{EmojiBank.Resource(ResourceType.StarDust)}",
            _ => ""
        };

    public static IEnumerable<(ResourceType Type, BigDouble Amount)> Consumption(this FieldNodeType type)
    {
        if (type == FieldNodeType.Smelter)
        {
            yield return (ResourceType.Energy, BigDouble.One);
            yield return (ResourceType.Ice, BigDouble.One);
        }
    }

    public static string CssModifier(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "energy",
            FieldNodeType.Snow => "snow",
            FieldNodeType.Smelter => "smelter",
            FieldNodeType.Garden => "garden",
            FieldNodeType.Woodcutter => "woodcutter",
            FieldNodeType.Builder => "builder",
            FieldNodeType.Bonfire => "bonfire",
            FieldNodeType.Recycler => "recycler",
            _ => "default"
        };
}
