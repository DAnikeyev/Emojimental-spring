using BreakInfinity;
using Emojimental.GameEngine.Values;

namespace Emojimental.Models;

public sealed class FieldNode
{
    public const double Size = 184d;
    private const double MinCooldownSeconds = 0.25d;

    public FieldNode(int id, FieldNodeType type, double x, double y)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;

        CooldownSeconds.BaseValue = type switch
        {
            FieldNodeType.Snow => 7d,
            _ => 6d
        };

        CooldownSeconds.AddMul(_ => 1d / Math.Max(1, ConnectionCount));
        CooldownSeconds.AddClampMin(_ => MinCooldownSeconds);
        OutputValue.AddAdd(_ => Level - 1);
    }

    public int Id { get; }

    public FieldNodeType Type { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    public double Progress { get; private set; }

    public int Level { get; private set; } = 1;

    public int ConnectionCount { get; private set; }

    public string TypeDisplayName => Type.DisplayName();

    public string Icon => Type.Icon();

    public string ResourceDisplayName => Type.ProducedResourceName();

    public string ResourceIcon => Type.ProducedResourceIcon();

    public Stat CooldownSeconds { get; } = new() { BaseValue = 6 };

    public Stat OutputValue { get; } = new() { BaseValue = 1 };

    public double ProgressPercent => Math.Clamp(Progress * 100d, 0d, 100d);

    public BigDouble EvaluateCooldown()
        => new EvaluationContext().Get(CooldownSeconds);

    public string CooldownDisplay()
        => EvaluateCooldown().Display();

    public BigDouble EvaluateValue()
        => new EvaluationContext().Get(OutputValue);

    public string ValueDisplay()
        => EvaluateValue().Display();

    public BigDouble GetUpgradeCost()
        => new BigDouble(Level) * Level;

    public string UpgradeCostDisplay()
        => GetUpgradeCost().Display();

    internal BigDouble Advance(double deltaSeconds)
    {
        var cooldownSeconds = EvaluateCooldown().ToDouble();
        if (double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0d)
            cooldownSeconds = MinCooldownSeconds;

        Progress += deltaSeconds / cooldownSeconds;

        var completedCycles = 0;
        while (Progress >= 1d)
        {
            Progress -= 1d;
            completedCycles++;
        }

        if (completedCycles == 0)
            return BigDouble.Zero;

        return EvaluateValue() * completedCycles;
    }

    internal void Upgrade()
        => Level += 1;

    internal void SetConnectionCount(int count)
        => ConnectionCount = Math.Max(0, count);

    internal void SetPosition(double x, double y)
    {
        X = x;
        Y = y;
    }
}


