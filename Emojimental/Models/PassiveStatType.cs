namespace Emojimental.Models;

public enum PassiveStatType
{
    Happiness,
    Seeds,
    Markets,
    Houses,
    Stars,
    RecycledStars,
    Flower,
    Snowman,
    Carrots
}

public static class PassiveStatTypeExtensions
{
    public static string Emoji(this PassiveStatType type)
        => type switch
        {
            PassiveStatType.Happiness => EmojiBank.Resource(ResourceType.Happiness),
            PassiveStatType.Seeds => EmojiBank.Resource(ResourceType.Sapling),
            PassiveStatType.Markets => "🏪",
            PassiveStatType.Houses => "🏠",
            PassiveStatType.Stars => "🪄",
            PassiveStatType.RecycledStars => "♻️",
            PassiveStatType.Flower => EmojiBank.Resource(ResourceType.Flower),
            PassiveStatType.Snowman => EmojiBank.Symbol(SymbolType.Snowman),
            PassiveStatType.Carrots => "🥕",
            _ => "✨"
        };

    public static string TargetEmoji(this PassiveStatType type)
        => type switch
        {
            PassiveStatType.Happiness => EmojiBank.Symbol(SymbolType.Stars),
            PassiveStatType.Seeds => EmojiBank.Resource(ResourceType.Tree),
            PassiveStatType.Markets => EmojiBank.Symbol(SymbolType.Exchange),
            PassiveStatType.Houses => EmojiBank.Resource(ResourceType.Happiness),
            PassiveStatType.Stars => EmojiBank.Symbol(SymbolType.Stars),
            PassiveStatType.RecycledStars => $"{EmojiBank.Resource(ResourceType.StarDust)} {EmojiBank.Symbol(SymbolType.Factory)}",
            PassiveStatType.Flower => EmojiBank.Resource(ResourceType.Happiness),
            PassiveStatType.Snowman => $"{EmojiBank.Resource(ResourceType.Carrot)} {EmojiBank.Resource(ResourceType.Coin)}",
            PassiveStatType.Carrots => EmojiBank.Resource(ResourceType.Happiness),
            _ => "✨"
        };
}
