using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public sealed class AddModifier : IStagedModifier
{
    private readonly Func<EvaluationContext, BigDouble> _value;

    public AddModifier(Func<EvaluationContext, BigDouble> value, int priority = 0)
    {
        _value = value;
        Priority = priority;
    }

    public ModifierStage Stage => ModifierStage.Add;

    public int Priority { get; }

    public BigDouble Apply(BigDouble current, EvaluationContext ctx)
        => current + _value(ctx);
}