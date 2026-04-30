using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public sealed class ClampMaxModifier : IStagedModifier
{
    private readonly Func<EvaluationContext, BigDouble> _max;

    public ClampMaxModifier(Func<EvaluationContext, BigDouble> max, int priority = 0)
    {
        _max = max;
        Priority = priority;
    }

    public ModifierStage Stage => ModifierStage.Clamp;

    public int Priority { get; }

    public BigDouble Apply(BigDouble current, EvaluationContext ctx)
    {
        var max = _max(ctx);
        return current > max ? max : current;
    }
}

