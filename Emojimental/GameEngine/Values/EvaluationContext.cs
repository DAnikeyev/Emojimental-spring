using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public class EvaluationContext
{
    private readonly Dictionary<Stat, BigDouble> _cache = new();
    private readonly HashSet<Stat> _visiting = new();

    public BigDouble Get(Stat stat)
    {
        if (_cache.TryGetValue(stat, out var val))
            return val;

        if (_visiting.Contains(stat))
            throw new InvalidOperationException("Cycle detected");

        _visiting.Add(stat);

        var result = stat.Evaluate(this);

        _visiting.Remove(stat);
        _cache[stat] = result;

        return result;
    }
}