namespace Emojimental.GameEngine.Values;

public interface IStagedModifier : IModifier
{
    ModifierStage Stage { get; }

    /// <summary>
    /// Lower runs earlier within the same Stage.
    /// </summary>
    int Priority { get; }
}

