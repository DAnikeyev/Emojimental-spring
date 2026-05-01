namespace Emojimental.Models;

public enum PassiveStatType
{
    Happiness,
    Seeds,
    Markets,
    Houses,
    Stars,
    RecycledStars
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
            PassiveStatType.Stars => EmojiBank.Symbol(SymbolType.Stars),
            PassiveStatType.RecycledStars => "♻️",
            _ => "✨"
        };
}
