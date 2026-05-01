using BreakInfinity;
using Emojimental.GameEngine.Values;

namespace Emojimental.Models;

public sealed class FieldNode
{
    public const double Size = 160d;
    private const double MinCooldownSeconds = 0.1d;
    private BigDouble _temperatureValueMultiplier = BigDouble.One;
    private BigDouble _temperatureCooldownMultiplier = BigDouble.One;
    private BigDouble _beamValueMultiplier = BigDouble.One;

    public FieldNode(int id, FieldNodeType type, double x, double y)
    {
        Id = id;
        Type = type;
        X = x;
        Y = y;

        CooldownSeconds.BaseValue = type switch
        {
            FieldNodeType.Snow => 7d,
            FieldNodeType.Smelter => 10d,
            FieldNodeType.Garden => 5d,
            FieldNodeType.Woodcutter => 8d,
            FieldNodeType.Builder => 15d,
            FieldNodeType.Bonfire => 12d,
            FieldNodeType.Recycler => 10d,
            _ => 6d
        };
;
        CooldownSeconds.AddAdd(_ => -0.1d * (TimeLevel - 1));
        CooldownSeconds.AddClampMin(_ => MinCooldownSeconds);
        CooldownSeconds.AddMul(_ => _temperatureCooldownMultiplier);
        OutputValue.AddAdd(_ => ItemLevel - 1);
        OutputValue.AddMul(_ => _beamValueMultiplier);
        OutputValue.AddMul(_ => _temperatureValueMultiplier);
    }

    public int Id { get; }

    public FieldNodeType Type { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    public double Progress { get; private set; }

    public int Level => ItemLevel + TimeLevel - 1;

    public int ItemLevel { get; private set; } = 1;

    public int TimeLevel { get; private set; } = 1;

    public int ConnectionCount { get; private set; }

    public int BuilderLever { get; private set; } // 0: Market, 1: House

    public string TypeDisplayName => Type.DisplayName();

    public string Icon => Type.Icon();

    public string ResourceDisplayName => Type.ProducedResourceName();

    public string ResourceIcon => Type.ProducedResourceIcon();

    public Stat CooldownSeconds { get; } = new() { BaseValue = 6 };

    public Stat OutputValue { get; } = new() { BaseValue = 1 };

    public double ProgressPercent => Math.Clamp(Progress * 100d, 0d, 100d);

    public BigDouble EvaluateCooldown()
    {
        _cachedCooldown ??= new EvaluationContext().Get(CooldownSeconds);
        return _cachedCooldown.Value;
    }

    public string CooldownDisplay()
    {
        _cachedCooldownDisplay ??= EvaluateCooldown().Display();
        return _cachedCooldownDisplay;
    }

    public string BaseCooldownDisplay() => CooldownSeconds.BaseValue.Display();

    public BigDouble EvaluateValue()
    {
        _cachedValue ??= new EvaluationContext().Get(OutputValue);
        return _cachedValue.Value;
    }

    public string ValueDisplay()
    {
        _cachedValueDisplay ??= EvaluateValue().Display();
        return _cachedValueDisplay;
    }

    public string BaseValueDisplay() => OutputValue.BaseValue.Display();

    public void SetTemperatureValueMultiplier(BigDouble multiplier)
    {
        _temperatureValueMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetTemperatureCooldownMultiplier(BigDouble multiplier)
    {
        _temperatureCooldownMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetBeamValueMultiplier(BigDouble multiplier)
    {
        _beamValueMultiplier = multiplier;
        InvalidateCache();
    }

    private BigDouble? _cachedCooldown;
    private string? _cachedCooldownDisplay;
    private BigDouble? _cachedValue;
    private string? _cachedValueDisplay;

    private void InvalidateCache()
    {
        _cachedCooldown = null;
        _cachedCooldownDisplay = null;
        _cachedValue = null;
        _cachedValueDisplay = null;
    }

    public BigDouble GetItemUpgradeCost()
        => UpgradeLookup.GetItemUpgradeCost(ItemLevel);

    public BigDouble GetTimeUpgradeCost()
        => UpgradeLookup.GetTimeUpgradeCost(TimeLevel);

    public string ItemUpgradeCostDisplay()
        => GetItemUpgradeCost().Display();

    public string TimeUpgradeCostDisplay()
        => GetTimeUpgradeCost().Display();

    public bool IsPaused { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal BigDouble Advance(double deltaSeconds, bool canProduce = true)
    {
        if (IsPaused)
        {
            IsActive = false;
            return BigDouble.Zero;
        }

        IsActive = canProduce;
        if (!canProduce)
            return BigDouble.Zero;

        var cooldownMultiplier = _temperatureCooldownMultiplier;
        if (Type == FieldNodeType.Bonfire)
        {
            cooldownMultiplier = BigDouble.One;
        }

        var cooldownSeconds = (EvaluateCooldown() * cooldownMultiplier).ToDouble();
        if (double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0d)
            cooldownSeconds = MinCooldownSeconds;

        Progress += deltaSeconds / cooldownSeconds;

        var completedCycles = 0;
        if (Progress >= 1d)
        {
            completedCycles = (int)Math.Floor(Progress);
            Progress -= completedCycles;
        }

        if (completedCycles == 0)
            return BigDouble.Zero;

        var valueMultiplier = _temperatureValueMultiplier;
        if (Type == FieldNodeType.Bonfire)
        {
            valueMultiplier = BigDouble.One;
        }

        return EvaluateValue() * valueMultiplier * completedCycles;
    }

    internal void ToggleBuilderLever()
    {
        BuilderLever = (BuilderLever + 1) % 2;
    }

    internal void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    internal void UpgradeItem()
    {
        ItemLevel += 1;
        InvalidateCache();
    }

    internal void UpgradeTime()
    {
        TimeLevel += 1;
        InvalidateCache();
    }

    internal void SetConnectionCount(int count)
    {
        ConnectionCount = Math.Max(0, count);
        InvalidateCache();
    }

    internal void SetPosition(double x, double y)
    {
        X = x;
        Y = y;
    }
}


