using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public interface IModifier
{
    BigDouble Apply(BigDouble current, EvaluationContext ctx);
}