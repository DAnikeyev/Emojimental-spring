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

    public long PositionVersion { get; private set; }

    public long StateVersion { get; private set; }

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

    public double ProgressPercent
    {
        get
        {
            return Math.Clamp(Progress * 100d, 0d, 100d);
        }
    }

    public string ProgressPercentDisplay() => _cachedProgressPercentDisplay;

    private string _cachedProgressPercentDisplay = "0.000%";
    private double _lastReportedProgress = -1;

    public BigDouble EvaluateCooldown(EvaluationContext? ctx = null)
    {
        if (_cachedCooldown.HasValue)
            return _cachedCooldown.Value;

        var result = (ctx ?? new EvaluationContext()).Get(CooldownSeconds);
        _cachedCooldown = result;
        return result;
    }

    public string CooldownDisplay()
    {
        if (_cachedCooldownDisplay != null)
            return _cachedCooldownDisplay;

        _cachedCooldownDisplay = EvaluateCooldown().Display();
        return _cachedCooldownDisplay;
    }

    public string BaseCooldownDisplay()
        => (CooldownSeconds.BaseValue <= MinCooldownSeconds ? MinCooldownSeconds : CooldownSeconds.BaseValue).Display();

    public bool UsesFlowerUpgrade => Type == FieldNodeType.Farm;

    public BigDouble EvaluateFlowerProbability()
        => UsesFlowerUpgrade ? new BigDouble((TimeLevel - 1) / 100d) : BigDouble.Zero;

    public string FlowerProbabilityDisplay()
        => $"{EvaluateFlowerProbability().ToDouble():P0}";

    public string BaseFlowerProbabilityDisplay()
        => $"{0:P0}";

    public BigDouble EvaluateValue(EvaluationContext? ctx = null)
    {
        if (_cachedValue.HasValue)
            return _cachedValue.Value;

        var result = (ctx ?? new EvaluationContext()).Get(OutputValue);
        _cachedValue = result;
        return result;
    }

    public string ValueDisplay()
    {
        if (_cachedValueDisplay != null)
            return _cachedValueDisplay;

        _cachedValueDisplay = EvaluateValue().Display();
        return _cachedValueDisplay;
    }

    public string BaseValueDisplay() => OutputValue.BaseValue.Display();

    public BigDouble GetBeamInputMultiplier() => _beamValueMultiplier;

    public void SetTemperatureValueMultiplier(BigDouble multiplier)
    {
        if (_temperatureValueMultiplier == multiplier) return;
        _temperatureValueMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetTemperatureCooldownMultiplier(BigDouble multiplier)
    {
        if (_temperatureCooldownMultiplier == multiplier) return;
        _temperatureCooldownMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetBeamValueMultiplier(BigDouble multiplier)
    {
        if (_beamValueMultiplier == multiplier) return;
        _beamValueMultiplier = multiplier;
        InvalidateCache();
    }
    
    public void SetBeamCooldownMultiplier(BigDouble multiplier)
    {
        if (_beamCooldownMultiplier == multiplier) return;
        _beamCooldownMultiplier = multiplier;
        InvalidateCache();
    }

    public void SetBeamCooldownReduction(BigDouble reduction)
    {
        if (_beamCooldownReduction == reduction) return;
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
        _cachedProgressPercentDisplay = ProgressPercent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "%";
        _lastReportedProgress = Progress;
        StateVersion++;
    }

    public BigDouble GetItemUpgradeCost()
        => UpgradeLookup.GetItemUpgradeCost(Type, ItemLevel);

    public BigDouble GetTimeUpgradeCost()
        => UpgradeLookup.GetTimeUpgradeCost(Type, TimeLevel);

    public string ItemUpgradeCostDisplay()
        => GetItemUpgradeCost().Display();

    public string TimeUpgradeCostDisplay()
        => GetTimeUpgradeCost().Display();

    public bool CanUpgradeTime
        => UsesFlowerUpgrade || CooldownSeconds.BaseValue > MinCooldownSeconds;

    public bool IsPaused { get; private set; }
    public bool IsActive { get; private set; } = true;

    internal int Advance(double deltaSeconds, bool canProduce = true, EvaluationContext? ctx = null)
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

        var cooldownSeconds = EvaluateCooldown(ctx).ToDouble();
        if (double.IsNaN(cooldownSeconds) || double.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0d)
            cooldownSeconds = MinCooldownSeconds;

        Progress += deltaSeconds / cooldownSeconds;

        if (Math.Abs(Progress - _lastReportedProgress) > 0.00001)
        {
            _cachedProgressPercentDisplay = ProgressPercent.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "%";
            _lastReportedProgress = Progress;
        }

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
        StateVersion++;
    }

    public BigDouble GetGeneratedValue(int completedCycles)
    {
        if (completedCycles <= 0) return BigDouble.Zero;

        return EvaluateValue() * completedCycles;
    }

    internal void ToggleBuilderLever()
    {
        BuilderLever = (BuilderLever + 1) % 2;
        StateVersion++;
    }

    internal void TogglePause()
    {
        IsPaused = !IsPaused;
        StateVersion++;
    }

    public void UpgradeItem()
    {
        ItemLevel += 1;
        OutputValue.BaseValue = UpgradeLookup.GetProductivity(Type, ItemLevel);
        InvalidateCache();
    }

    public void UpgradeTime()
    {
        if (!CanUpgradeTime) return;

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

        if (CooldownSeconds.BaseValue <= MinCooldownSeconds)
            CooldownSeconds.BaseValue = MinCooldownSeconds;

        InvalidateCache();
    }

    internal void SetConnectionCount(int count)
    {
        var nextCount = Math.Max(0, count);
        if (ConnectionCount == nextCount) return;
        ConnectionCount = nextCount;
        InvalidateCache();
    }

    internal void SetPosition(double x, double y)
    {
        if (Math.Abs(X - x) < 0.1d && Math.Abs(Y - y) < 0.1d)
            return;

        X = x;
        Y = y;
        PositionVersion++;
    }
}


