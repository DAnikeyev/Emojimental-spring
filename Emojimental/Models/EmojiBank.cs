namespace Emojimental.Models;

public enum ResourceType
{
    Energy,
    Ice,
    Water,
    Coin,
    Sapling,
    Tree,
    Happiness,
    ResearchPoint,
    Wood,
    StarDust,
    Flower,
    Carrot
}

public enum SymbolType
{
    Multiply,
    Plus,
    Arrow,
    Thousand,
    Million,
    Second,
    Done,
    Locked,
    Nothing,
    Exchange,
    Deploy,
    Factory,
    Research,
    Stars,
    Snowman,
    Minus,
    Toolbox,
    Close,
    Upgrade,
    Level,
    Temperature,
    Sun,
    Moon,
    FastProgress,
    StarModifier,
    ResearchPoint,
    Music,
    Calendar,
    Goal
}

public static class EmojiBank
{
    public static string Resource(ResourceType type)
        => type switch
        {
            ResourceType.Energy => "⚡",
            ResourceType.Ice => "❄️",
            ResourceType.Water => "💧",
            ResourceType.Coin => "🪙",
            ResourceType.Sapling => "🌱",
            ResourceType.Tree => "🌲",
            ResourceType.Happiness => "😊",
            ResourceType.ResearchPoint => "🔬",
            ResourceType.Wood => "🪵",
            ResourceType.StarDust => "✨",
            ResourceType.Flower => "🌸",
            ResourceType.Carrot => "🥕",
            _ => "❓"
        };

    public static string Symbol(SymbolType type)
        => type switch
        {
            SymbolType.Multiply => "✖️",
            SymbolType.Plus => "➕",
            SymbolType.Arrow => "➡️",
            SymbolType.Thousand => "🧮",
            SymbolType.Million => "🌌",
            SymbolType.Second => "⏱️",
            SymbolType.Done => "✅",
            SymbolType.Locked => "🔒",
            SymbolType.Nothing => "∅",
            SymbolType.Exchange => "⚖️",
            SymbolType.Deploy => "📦",
            SymbolType.Factory => "🏭",
            SymbolType.Research => "🔬",
            SymbolType.Stars => "⭐",
            SymbolType.Snowman => "☃️",
            SymbolType.Minus => "➖",
            SymbolType.Toolbox => "🧰",
            SymbolType.Close => "✖️",
            SymbolType.Upgrade => "⬆️",
            SymbolType.Level => "⭐",
            SymbolType.Temperature => "🌡️",
            SymbolType.Sun => "☀️",
            SymbolType.Moon => "🌙",
            SymbolType.FastProgress => "🚀🕛",
            SymbolType.StarModifier => "✳️",
            SymbolType.ResearchPoint => "🧪",
            SymbolType.Music => "🎵",
            SymbolType.Calendar => "📅",
            SymbolType.Goal => "🎯",
            _ => "✨"
        };

    public static string Happiness(double value)
        => value switch
        {
            < 20 => "😭",
            < 40 => "😟",
            < 60 => "😐",
            < 80 => "🙂",
            < 100 => "😊",
            _ => "🤩"
        };
}