using BreakInfinity;

namespace Emojimental.Models;

public enum ResearchType
{
    UnlockStars,
    UnlockStarSlots,
    UnlockTemperatureBar,
    UnlockEnergy,
    UnlockSmelter,
    UnlockSaplings,
    UnlockHappiness,
    UnlockTreeGrow,
    UnlockTemperatureExchange,
    UnlockStarRecycler,
    UnlockStarImprover,
    UnlockWoodcutter,
    UnlockBuilder,
    UnlockFlowers,
    UnlockPassiveGemSlots,
    UnlockBurner,
    UnlockGoal
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
            ResearchType.UnlockStars,
            ResourceType.ResearchPoint,
            new BigDouble(10),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Stars)}🪄",
            "stars"),
        new(
            ResearchType.UnlockStarSlots,
            ResourceType.ResearchPoint,
            new BigDouble(20),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Stars)}🏭",
            "star-slots"),
        new(
            ResearchType.UnlockTemperatureBar,
            ResourceType.ResearchPoint,
            new BigDouble(30),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Temperature)}",
            "temperature"),
        new(
            ResearchType.UnlockEnergy,
            ResourceType.ResearchPoint,
            new BigDouble(50),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Resource(ResourceType.Energy)}",
            "energy"),
        new(
            ResearchType.UnlockSmelter,
            ResourceType.ResearchPoint,
            new BigDouble(100),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Smelter.Icon()}{EmojiBank.Resource(ResourceType.Water)}",
            "smelter"),
        new(
            ResearchType.UnlockSaplings,
            ResourceType.ResearchPoint,
            new BigDouble(200),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Resource(ResourceType.Sapling)}",
            "saplings"),
        new(
            ResearchType.UnlockHappiness,
            ResourceType.ResearchPoint,
            new BigDouble(300),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Resource(ResourceType.Happiness)}",
            "happiness"),
        new(
            ResearchType.UnlockTreeGrow,
            ResourceType.ResearchPoint,
            new BigDouble(500),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Farm.Icon()}{EmojiBank.Resource(ResourceType.Tree)}",
            "tree-grow"),
        new(
            ResearchType.UnlockTemperatureExchange,
            ResourceType.ResearchPoint,
            new BigDouble(1000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Exchange)}{EmojiBank.Symbol(SymbolType.Temperature)}",
            "temperature-exchange"),
        new(
            ResearchType.UnlockStarRecycler,
            ResourceType.ResearchPoint,
            new BigDouble(2000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Recycler.Icon()}{EmojiBank.Resource(ResourceType.StarDust)}",
            "recycler"),
        new(
            ResearchType.UnlockStarImprover,
            ResourceType.ResearchPoint,
            new BigDouble(4000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Exchange)}🪄",
            "star-improver"),
        new(
            ResearchType.UnlockWoodcutter,
            ResourceType.ResearchPoint,
            new BigDouble(5000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Woodcutter.Icon()}{EmojiBank.Resource(ResourceType.Wood)}",
            "woodcutter"),
        new(
            ResearchType.UnlockBuilder,
            ResourceType.ResearchPoint,
            new BigDouble(10000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Builder.Icon()}🏠⚖️",
            "builder"),
        new(
            ResearchType.UnlockFlowers,
            ResourceType.ResearchPoint,
            new BigDouble(20000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Resource(ResourceType.Flower)}",
            "flowers"),
        new(
            ResearchType.UnlockPassiveGemSlots,
            ResourceType.ResearchPoint,
            new BigDouble(100000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}💎",
            "passive-gems"),
        new(
            ResearchType.UnlockBurner,
            ResourceType.ResearchPoint,
            new BigDouble(500000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{FieldNodeType.Bonfire.Icon()}",
            "burner"),
        new(
            ResearchType.UnlockGoal,
            ResourceType.ResearchPoint,
            new BigDouble(1000000),
            $"{EmojiBank.Symbol(SymbolType.Plus)}{EmojiBank.Symbol(SymbolType.Goal)}",
            "goal")
    ];

    public static ResearchDefinition Get(ResearchType type)
        => All.First(research => research.Type == type);
}