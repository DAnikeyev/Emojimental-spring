using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public sealed class ClampMinModifier : IStagedModifier
{
    private readonly Func<EvaluationContext, BigDouble> _min;

    public ClampMinModifier(Func<EvaluationContext, BigDouble> min, int priority = 0)
    {
        _min = min;
        Priority = priority;
    }

    public ModifierStage Stage => ModifierStage.Clamp;

    public int Priority { get; }

    public BigDouble Apply(BigDouble current, EvaluationContext ctx)
    {
        var min = _min(ctx);
        return current < min ? min : current;
    }
}
