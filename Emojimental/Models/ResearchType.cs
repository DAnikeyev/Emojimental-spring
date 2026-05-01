using BreakInfinity;

namespace Emojimental.Models;

public enum ResearchType
{
    CoinFlow,
    FrostBloom,
    SunChorus
}

public sealed record ResearchDefinition(
    ResearchType Type,
    ResourceType PriceResource,
    BigDouble Price,
    string EffectDisplay,
    string CardModifier);

public static class ResearchCatalog
{
    public static IReadOnlyList<ResearchDefinition> All { get; } =
    [
        new(
            ResearchType.CoinFlow,
            ResourceType.Coin,
            new BigDouble(10),
            $"{EmojiBank.Resource(ResourceType.Coin)}:{EmojiBank.Symbol(SymbolType.Multiply)}2",
            "coin"),
        new(
            ResearchType.FrostBloom,
            ResourceType.Ice,
            new BigDouble(12),
            $"{EmojiBank.Resource(ResourceType.Ice)}:{EmojiBank.Symbol(SymbolType.Multiply)}2",
            "ice"),
        new(
            ResearchType.SunChorus,
            ResourceType.Energy,
            new BigDouble(15),
            $"{EmojiBank.Resource(ResourceType.Energy)}:{EmojiBank.Symbol(SymbolType.Multiply)}2",
            "energy")
    ];

    public static ResearchDefinition Get(ResearchType type)
        => All.First(research => research.Type == type);
}