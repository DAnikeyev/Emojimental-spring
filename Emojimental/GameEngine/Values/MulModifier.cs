using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public sealed class MulModifier : IStagedModifier
{
    private readonly Func<EvaluationContext, BigDouble> _factor;

    public MulModifier(Func<EvaluationContext, BigDouble> factor, int priority = 0)
    {
        _factor = factor;
        Priority = priority;
    }

    public ModifierStage Stage => ModifierStage.Multiply;

    public int Priority { get; }

    public BigDouble Apply(BigDouble current, EvaluationContext ctx)
        => current * _factor(ctx);
}