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
    private BigDouble _beamCooldownMultiplier = BigDouble.One;
    private BigDouble _beamCooldownReduction = BigDouble.Zero;
    private bool _isHeld;

    public FieldNode(int id, FieldNodeType type, double x, double y)
    {
        Id = id;
        Type = type.Normalize();
        X = x;
        Y = y;

        CooldownSeconds.BaseValue = Type switch
        {
            FieldNodeType.Energy => 3d,
            FieldNodeType.Snow => 3d,
            FieldNodeType.Smelter => 6d,
            FieldNodeType.Woodcutter => 5d,
            FieldNodeType.Builder => 15d,
            FieldNodeType.Bonfire => 12d,
            FieldNodeType.Recycler => 10d,
            FieldNodeType.Researcher => 15d,
            FieldNodeType.DustBreaker => 10d,
            FieldNodeType.Farm => 5d,
            _ => 6d
        };
        CooldownSeconds.AddClampMin(_ => MinCooldownSeconds);
        CooldownSeconds.AddMul(_ => _temperatureCooldownMultiplier);
        CooldownSeconds.AddMul(_ => _beamCooldownMultiplier);
        CooldownSeconds.AddAdd(_ => -_beamCooldownReduction);
        CooldownSeconds.AddMul(_ => _isHeld ? 0.67d : 1d);
        OutputValue.BaseValue = UpgradeLookup.GetProductivity(Type, ItemLevel);
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
    
    public int RecycleQueue { get; private set; }

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

    public bool UsesFlowerUpgrade => Type == FieldNodeType.Farm;

    public BigDouble EvaluateFlowerProbability()
        => UsesFlowerUpgrade ? new BigDouble((TimeLevel - 1) / 100d) : BigDouble.Zero;

    public string FlowerProbabilityDisplay()
        => $"{EvaluateFlowerProbability().ToDouble():P0}";

    public string BaseFlowerProbabilityDisplay()
        => $"{0:P0}";

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

    public BigDouble GetBeamInputMultiplier() => _beamValueMultiplier;

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
    
    public void SetBeamCooldownMultiplier(BigDouble multiplier)
    {
        _beamCooldownMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetBeamCooldownReduction(BigDouble reduction)
    {
        _beamCooldownReduction = reduction;
        InvalidateCache();
    }

    public void SetHeld(bool isHeld)
    {
        if (_isHeld == isHeld) return;
        _isHeld = isHeld;
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
        => UpgradeLookup.GetItemUpgradeCost(Type, ItemLevel);

    public BigDouble GetTimeUpgradeCost()
        => UpgradeLookup.GetTimeUpgradeCost(Type, TimeLevel);

    public string ItemUpgradeCostDisplay()
        => GetItemUpgradeCost().Display();

    public string TimeUpgradeCostDisplay()
        => GetTimeUpgradeCost().Display();

    public bool IsPaused { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal int Advance(double deltaSeconds, bool canProduce = true)
    {
        if (IsPaused)
        {
            IsActive = false;
            return 0;
        }

        IsActive = canProduce;
        if (!canProduce)
            return 0;

        if (Type == FieldNodeType.Recycler && RecycleQueue <= 0)
        {
            IsActive = false;
            return 0;
        }

        var cooldownSeconds = EvaluateCooldown().ToDouble();
        if (double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0d)
            cooldownSeconds = MinCooldownSeconds;

        Progress += deltaSeconds / cooldownSeconds;

        var completedCycles = 0;
        if (Progress >= 1d)
        {
            completedCycles = (int)Math.Floor(Progress);
            Progress -= completedCycles;

            if (Type == FieldNodeType.Recycler)
            {
                var cyclesToConsume = Math.Min(completedCycles, RecycleQueue);
                RecycleQueue -= cyclesToConsume;
                completedCycles = cyclesToConsume;

                if (RecycleQueue <= 0)
                {
                    Progress = 0;
                }
            }
        }

        return completedCycles;
    }

    public void EnqueueRecycle()
    {
        if (Type != FieldNodeType.Recycler) return;
        RecycleQueue++;
    }

    public BigDouble GetGeneratedValue(int completedCycles)
    {
        if (completedCycles <= 0) return BigDouble.Zero;

        return EvaluateValue() * completedCycles;
    }

    internal void ToggleBuilderLever()
    {
        BuilderLever = (BuilderLever + 1) % 2;
    }

    internal void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void UpgradeItem()
    {
        ItemLevel += 1;
        OutputValue.BaseValue = UpgradeLookup.GetProductivity(Type, ItemLevel);
        InvalidateCache();
    }

    public void UpgradeTime()
    {
        TimeLevel += 1;
        if (Type == FieldNodeType.Farm)
        {
            InvalidateCache();
            return;
        }

        if (Type == FieldNodeType.DustBreaker || Type == FieldNodeType.Woodcutter)
        {
            CooldownSeconds.BaseValue *= 0.9;
        }
        else if (Type == FieldNodeType.Researcher)
        {
            CooldownSeconds.BaseValue -= 0.3d;
        }
        else if (Type == FieldNodeType.Smelter)
        {
            CooldownSeconds.BaseValue -= 0.15d;
        }
        else
        {
            CooldownSeconds.BaseValue -= 0.1d;
        }

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


