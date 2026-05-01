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
    StarDust
}

public enum SymbolType
{
    Multiply,
    Plus,
    Arrow,
    PerSecond,
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
    ResearchPoint
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
            ResourceType.ResearchPoint => "🧪",
            ResourceType.Wood => "🪵",
            ResourceType.StarDust => "✨",
            _ => "❓"
        };

    public static string Symbol(SymbolType type)
        => type switch
        {
            SymbolType.Multiply => "✖️",
            SymbolType.Plus => "➕",
            SymbolType.Arrow => "➡️",
            SymbolType.PerSecond => "⏱️",
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
            SymbolType.ResearchPoint => "🧪",
            _ => "✨"
        };
}