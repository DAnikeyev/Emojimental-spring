using BreakInfinity;
using Emojimental.GameEngine.Values;
using Emojimental.GameEngine.Numbers;
using Emojimental.Models;

namespace Emojimental.Services;

public enum HoverSide
{
    Center,
    Left,
    Right
}

public sealed class GameState
{
    public const double MaxMusicVolume = 1d;

    private const double Log10Of2 = 0.3010299956639812d;
    private const double UiUpdateIntervalSeconds = 1d / 60d;
    private const int StarInventorySize = 80;
    private const int StartingStarCount = 3;
    private static readonly StarType[] RecycleStarTypePriority = [StarType.Yellow, StarType.Blue, StarType.Legendary];
    public const double DayCycleDurationSeconds = 60; // testing
    public const int MinTemperatureCelsius = -10;
    public const int FirstVictoryTemperatureCelsius = 20;
    public const int FinalVictoryTemperatureCelsius = 40;
    private const int InitialTemperatureCelsius = -5;
    private const int InitialMaxTemperatureCelsius = -5;

    private readonly ResourceState _resources = new();
    private readonly ResearchState _research = new();
    private readonly FieldState _field = new();
    private readonly List<StarInventorySlot> _starInventory = Enumerable.Range(0, StarInventorySize)
        .Select(index => new StarInventorySlot(index, index < StartingStarCount ? new Star(StarType.Yellow, 1.2, 0, 1) : null))
        .ToList();
    private readonly List<PassiveStatStarSlot> _passiveStatStarSlots = Enum.GetValues<PassiveStatType>()
        .Select(type => new PassiveStatStarSlot(type))
        .ToList();
    private readonly Dictionary<PassiveStatType, MulModifier> _passiveStatStarModifiers = new();
    private readonly Dictionary<PassiveStatType, BigDouble> _passiveStatStarMultipliers = Enum.GetValues<PassiveStatType>()
        .ToDictionary(t => t, _ => BigDouble.One);
    private const double PassiveStarMultiplierPerStar = 2d;

    public static BigDouble Log2(BigDouble value)
    {
        if (value <= 0) return BigDouble.Zero;
        var log10 = Math.Log10(value.Mantissa) + value.Exponent;
        return log10 / Log10Of2;
    }
    private double _pendingUiUpdateSeconds;
    private double _pendingSlowPassiveRefreshSeconds;
    private const double MinUiUpdateInterval = UiUpdateIntervalSeconds;
    private const double SlowPassiveRefreshIntervalSeconds = 1d;

    public GameState()
    {
        Temperature.BaseValue = new BigDouble(InitialTemperatureCelsius);
        Stars.BaseValue = BigDouble.Zero;
        RecycledStars.BaseValue = BigDouble.Zero;
        Coin.BaseValue = 5; // testing
        ApplyPassiveStatStarModifiers();
    }

    public double TemperatureProgress
    {
        get
        {
            var temperature = EvaluateStat(Temperature).ToDouble();
            return Math.Clamp((temperature - MinTemperatureCelsius) / (double)(FirstVictoryTemperatureCelsius - MinTemperatureCelsius), 0d, 1d);
        }
    }

    public int GoalTemperatureCelsius => VictoryContinued ? FinalVictoryTemperatureCelsius : FirstVictoryTemperatureCelsius;
    public int ThermometerMaxCelsius
        => VictoryContinued && HasResearch(ResearchType.UnlockSecondActTemperatureRange)
            ? FinalVictoryTemperatureCelsius
            : FirstVictoryTemperatureCelsius;
    public int TemperatureExchangeLimitCelsius
        => VictoryContinued && !HasResearch(ResearchType.UnlockSecondActTemperatureRange)
            ? FirstVictoryTemperatureCelsius
            : GoalTemperatureCelsius;
    public int HappinessDisplayMax
        => VictoryContinued && HasResearch(ResearchType.UnlockSecondActHappinessRange) ? 140 : 100;

    public string MixColor(int startR, int startG, int startB, int endR, int endG, int endB, double progress, double alpha = 1d)
    {
        var amount = Math.Clamp(progress, 0d, 1d);
        var r = (int)Math.Round(startR + ((endR - startR) * amount), MidpointRounding.AwayFromZero);
        var g = (int)Math.Round(startG + ((endG - startG) * amount), MidpointRounding.AwayFromZero);
        var b = (int)Math.Round(startB + ((endB - startB) * amount), MidpointRounding.AwayFromZero);

        return alpha >= 0.999d
            ? $"rgb({r} {g} {b})"
            : FormattableString.Invariant($"rgba({r}, {g}, {b}, {alpha:F3})");
    }

    public string MixHeatColor(
        int coldR, int coldG, int coldB,
        int warmR, int warmG, int warmB,
        int hotR, int hotG, int hotB,
        double alpha = 1d)
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        if (temperature <= FirstVictoryTemperatureCelsius)
        {
            var warmProgress = Math.Clamp(
                (temperature - MinTemperatureCelsius) / (double)(FirstVictoryTemperatureCelsius - MinTemperatureCelsius),
                0d,
                1d);
            return MixColor(coldR, coldG, coldB, warmR, warmG, warmB, warmProgress, alpha);
        }

        var heatProgress = Math.Clamp(
            (temperature - FirstVictoryTemperatureCelsius) / (double)(FinalVictoryTemperatureCelsius - FirstVictoryTemperatureCelsius),
            0d,
            1d);
        return MixColor(warmR, warmG, warmB, hotR, hotG, hotB, heatProgress, alpha);
    }

    public void AddInitialNodes()
    {
        _field.AddFieldObject(FieldNodeType.Snow);
    }

    public Stat Energy => _resources.Energy;
    public Stat Ice => _resources.Ice;
    public Stat Coin => _resources.Coin;
    public Stat Sapling => _resources.Sapling;
    public Stat Tree => _resources.Tree;
    public Stat Water => _resources.Water;
    public Stat Flower => _resources.Flower;
    public Stat Carrot => _resources.Carrot;
    public Stat Temperature { get; } = new() { BaseValue = BigDouble.Zero };

    public Stat Happiness => _resources.Happiness;
    public Stat ResearchPoint => _resources.ResearchPoint;

    public int SnowmanCount => _resources.SnowmanCount;
    public int SaplingCount => _resources.SaplingCount;
    public Stat Wood => _resources.Wood;
    public Stat StarDust => _resources.StarDust;
    public Stat Snowman { get; } = new();
    public Stat RecycledStars { get; } = new();
    public double MusicVolume { get; private set; } = MaxMusicVolume * 0.05d;
    public bool Cheats { get; set; } = false; // testing
    public bool ParticlesEnabled { get; set; } = false;
    public double GameSpeed { get; set; } = 1.0;
    public Stat Stars { get; } = new();
    public bool IsMuted { get; private set; }
    public bool IsWeatherDisabled { get; private set; }
    public bool IsVictory { get; private set; }
    public bool VictoryContinued { get; private set; }

    public Stat Markets { get; } = new();
    public Stat Houses { get; } = new();

    public int MaxTemperatureCelsius { get; private set; } = InitialMaxTemperatureCelsius;

    public BigDouble EnergyMultiplier => BigDouble.One;
    public BigDouble FlowerMultiplier => BigDouble.One + EvaluatePassiveStat(PassiveStatType.Flower) * 0.1;
    public BigDouble IceMultiplier => BigDouble.One;
    public BigDouble CoinMultiplier => BigDouble.One;

    public bool IsResourceUnlocked(ResourceType type)
        => type switch
        {
            ResourceType.Coin => true,
            ResourceType.Ice => true,
            ResourceType.ResearchPoint => true,
            ResourceType.Energy => HasResearch(ResearchType.UnlockEnergy),
            ResourceType.Water => HasResearch(ResearchType.UnlockSmelter),
            ResourceType.StarDust => HasResearch(ResearchType.UnlockStarRecycler),
            ResourceType.Tree => HasResearch(ResearchType.UnlockTreeGrow),
            ResourceType.Wood => HasResearch(ResearchType.UnlockWoodcutter),
            ResourceType.Flower => HasResearch(ResearchType.UnlockFlowers),
            ResourceType.Happiness => HasResearch(ResearchType.UnlockHappiness),
            ResourceType.Carrot => HasResearch(ResearchType.UnlockHappiness),
            ResourceType.Sapling => HasResearch(ResearchType.UnlockSaplings),
            _ => true
        };

    public bool IsPassiveStatUnlocked(PassiveStatType type)
        => type switch
        {
            PassiveStatType.Happiness => HasResearch(ResearchType.UnlockHappiness),
            PassiveStatType.Seeds => HasResearch(ResearchType.UnlockSaplings),
            PassiveStatType.Markets => HasResearch(ResearchType.UnlockBuilder),
            PassiveStatType.Houses => HasResearch(ResearchType.UnlockBuilder),
            PassiveStatType.Stars => HasResearch(ResearchType.UnlockStars),
            PassiveStatType.RecycledStars => HasResearch(ResearchType.UnlockStarRecycler),
            PassiveStatType.Flower => HasResearch(ResearchType.UnlockFlowers),
            PassiveStatType.Carrots => HasResearch(ResearchType.UnlockHappiness),
            PassiveStatType.Snowman => true,
            _ => true
        };

    public bool CanAdjustTemperature() => HasResearch(ResearchType.UnlockTemperatureBar);

    public bool IsTemperatureGoalVisible()
        => VictoryContinued
            ? HasResearch(ResearchType.UnlockSecondActGoal)
            : HasResearch(ResearchType.UnlockGoal);

    public double GetFlowerProbability(FieldNode node)
        => HasResearch(ResearchType.UnlockFlowers) ? node.EvaluateFlowerProbability().ToDouble() : 0.0;

    public bool IsPassiveStatStarSlotsUnlocked() => HasResearch(ResearchType.UnlockPassiveGemSlots);

    public bool AreFactoryStarSlotsUnlocked() => HasResearch(ResearchType.UnlockStarSlots);

    public bool IsFactoryUnlocked(FieldNodeType type)
        => type.Normalize() switch
        {
            FieldNodeType.Snow => true,
            FieldNodeType.Researcher => true,
            FieldNodeType.Energy => HasResearch(ResearchType.UnlockEnergy),
            FieldNodeType.Smelter => HasResearch(ResearchType.UnlockSmelter),
            FieldNodeType.Farm => HasResearch(ResearchType.UnlockTreeGrow),
            FieldNodeType.Recycler => HasResearch(ResearchType.UnlockStarRecycler),
            FieldNodeType.Woodcutter => HasResearch(ResearchType.UnlockWoodcutter),
            FieldNodeType.Builder => HasResearch(ResearchType.UnlockBuilder),
            FieldNodeType.Bonfire => HasResearch(ResearchType.UnlockBurner),
            FieldNodeType.DustBreaker => false,
            _ => false
        };

    public BigDouble SnowmanCoinIncomePerSecond => UpgradeLookup.GetSnowmanCoinBonus(SnowmanCount) * CoinMultiplier;
    public BigDouble SnowmanCarrotIncomePerSecond => HasResearch(ResearchType.UnlockHappiness) ? UpgradeLookup.GetSnowmanCarrotBonus(SnowmanCount) * FlowerMultiplier : BigDouble.Zero;

    public IReadOnlyList<FieldNode> FieldObjects => _field.FieldObjects;
    public IReadOnlyList<AlignmentBeam> AlignmentBeams => _field.AlignmentBeams;
    public IReadOnlyList<BeamStarSlot> BeamStarSlots => _field.BeamStarSlots;
    public BeamStarSlot? GetBeamStarSlot(int sourceFieldNodeId, FieldNodeSide sourceSide) => _field.GetBeamStarSlot(sourceFieldNodeId, sourceSide);
    public IReadOnlyList<StarInventorySlot> StarInventory => _starInventory;
    public IReadOnlyCollection<ResearchType> OwnedResearch => _research.OwnedResearch;
    public IReadOnlyList<ResearchDefinition> Researches => ResearchCatalog.All;
    public IReadOnlyList<ResearchDefinition> VisibleResearches
        => ResearchCatalog.All.Where(research => IsResearchVisible(research.Type)).ToList();
    public IReadOnlyList<PassiveStatStarSlot> PassiveStatStarSlots => _passiveStatStarSlots;

    public FieldNode? SelectedFieldObject => _field.SelectedFieldObject;
    public int? DraggingStarSlotIndex { get; private set; }

    public double FieldWidth => _field.FieldWidth;
    public double FieldHeight => _field.FieldHeight;

    public event Action? Changed;
    public event Action<FieldNode, int>? OnResourceProduced;
    public event Action? RequestUiUpdate;
    public event Action? RequestResourceUiUpdate;
    public void NotifyResourceUiUpdate() => RequestResourceUiUpdate?.Invoke();

    public Star? HoveredStar { get; private set; }
    public PassiveStatType? HoveredPassiveStat { get; private set; }
    public HoverSide CurrentHoverSide { get; private set; }
    public (double X, double Y) HoveredInfoPosition { get; private set; }

    public void SetHoveredStar(Star? star, double x = 0, double y = 0, HoverSide side = HoverSide.Center)
    {
        HoveredStar = star;
        HoveredPassiveStat = null;
        CurrentHoverSide = side;
        HoveredInfoPosition = (x, y);
        RequestUiUpdate?.Invoke();
    }

    public void SetHoveredPassiveStat(PassiveStatType? type, double x = 0, double y = 0, HoverSide side = HoverSide.Center)
    {
        HoveredPassiveStat = type;
        HoveredStar = null;
        CurrentHoverSide = side;
        HoveredInfoPosition = (x, y);
        RequestUiUpdate?.Invoke();
    }

    public TimeSpan PlayTime { get; private set; }

    public int TemperatureCelsius => (int)Math.Round(EvaluateStat(Temperature).ToDouble(), MidpointRounding.AwayFromZero);

    public BigDouble SnowTemperatureMultiplier => BonusPercentToMultiplier(GetSnowTemperatureBonusPercent());
    public BigDouble WaterTemperatureMultiplier => BonusPercentToMultiplier(GetWaterTemperatureBonusPercent());
    public BigDouble EnergyTemperatureMultiplier => BonusPercentToMultiplier(GetEnergyTemperatureBonusPercent());

    public double DayCyclePosition => (PlayTime.TotalSeconds % DayCycleDurationSeconds) / DayCycleDurationSeconds;

    public int DaysElapsed => (int)(PlayTime.TotalSeconds / DayCycleDurationSeconds);

    public Star GenerateRandomStar()
    {
        var evalValue = EvaluatePassiveStat(PassiveStatType.Happiness).ToDouble();
        var h = Math.Min(Math.Log2(evalValue + 1), 100);

        StarType type;
        var legendaryProb = Math.Sqrt(Math.Max(h - 25, 0)) / 100.0;
        var rareProb = Math.Sqrt(h) / 100.0;

        var r = _random.NextDouble();
        if (r < legendaryProb) type = StarType.Legendary;
        else if (r < legendaryProb + rareProb) type = StarType.Blue;
        else type = StarType.Yellow;

        var statH = type == StarType.Legendary ? h * 1.5 : h;

        BigDouble prodMult = 1;
        if (type == StarType.Yellow || type == StarType.Legendary)
        {
            var w = MathHelper.NextGamma(2.0, Math.Max(0.001, statH / 15.0));
            var value = Math.Max(1.2, Math.Pow(1.15, w));
            prodMult = new BigDouble(value);
        }

        double cdReduction = 0;
        if (type == StarType.Blue || type == StarType.Legendary)
        {
            var w = MathHelper.NextGamma(2.0, Math.Max(0.001, statH / 15.0)) / 10.0;
            cdReduction = Math.Max(0.1, w);
        }

        // Console.WriteLine($"Generating star with Happiness stat {evalValue:F2} and derived h value {h:F2}: Type={type}, ProdMult={prodMult}, CdReduction={cdReduction}");
        int recycleValue = type switch
        {
            StarType.Yellow => 1,
            StarType.Blue => 3,
            StarType.Legendary => 20,
            _ => 1
        };

        return new Star(type, prodMult, cdReduction, recycleValue);
    }

    public void AddStarToInventory()
    {
        if (!HasResearch(ResearchType.UnlockStars)) return;

        var emptySlot = _starInventory.FirstOrDefault(s => !s.HasStar);
        if (emptySlot != null)
        {
            var star = GenerateRandomStar();
            emptySlot.AddStar(star);
            Changed?.Invoke();
        }
    }

    public void AddStarToInventory(Star star)
    {
        if (!HasResearch(ResearchType.UnlockStars)) return;

        var emptySlot = _starInventory.FirstOrDefault(s => !s.HasStar);
        if (emptySlot != null)
        {
            emptySlot.AddStar(star);
            Changed?.Invoke();
        }
    }

    private static readonly Random _random = new();
    public void Advance(double deltaSeconds)
    {
        if (IsVictory)
            return;

        deltaSeconds *= GameSpeed;
        PlayTime += TimeSpan.FromSeconds(deltaSeconds);

        if ((FieldObjects.Count == 0 && SnowmanCount == 0) || deltaSeconds <= 0d)
        {
            _pendingUiUpdateSeconds += deltaSeconds;
            if (_pendingUiUpdateSeconds >= MinUiUpdateInterval)
            {
                _pendingUiUpdateSeconds = 0d;
                RequestUiUpdate?.Invoke();
            }
            return;
        }

        var generatedResources = new Dictionary<ResourceType, BigDouble>();
        var consumedResources = new Dictionary<ResourceType, BigDouble>();
        var remainingResources = new Dictionary<ResourceType, BigDouble>();

        var structureBuilt = new List<(int Type, BigDouble Amount)>();

        // Cache context and commonly used stats
        var ctx = new EvaluationContext();
        var energyMult = EnergyMultiplier;
        var iceMult = IceMultiplier;
        var recycledStarsVal = ctx.Get(RecycledStars);
        
        BigDouble GetRemainingResource(ResourceType type)
        {
            if (remainingResources.TryGetValue(type, out var amount))
                return amount;

            amount = _resources.GetResource(type, ctx);
            remainingResources[type] = amount;
            return amount;
        }

        foreach (var fieldObject in FieldObjects)
        {
            var consumption = GetDynamicConsumption(fieldObject)
                .Where(entry => entry.Amount > BigDouble.Zero)
                .ToArray();
            var maxAffordableCycles = GetMaxAffordableCycles(consumption, GetRemainingResource);
            var completedCycles = fieldObject.Advance(deltaSeconds, maxAffordableCycles > 0, maxAffordableCycles, ctx);
            if (completedCycles <= 0)
                continue;

            OnResourceProduced?.Invoke(fieldObject, completedCycles);
            var generatedValue = fieldObject.EvaluateValue(ctx) * completedCycles;

            // Consume resources if produced
            foreach (var (resType, amount) in consumption)
            {
                remainingResources[resType] = BigDouble.Max(BigDouble.Zero, GetRemainingResource(resType) - (amount * completedCycles));
                consumedResources.TryAdd(resType, BigDouble.Zero);
                consumedResources[resType] += amount * completedCycles;
            }

            if (fieldObject.Type == FieldNodeType.Builder)
            {
                structureBuilt.Add((fieldObject.BuilderLever, generatedValue));
                continue;
            }

            var producedType = fieldObject.Type switch
            {
                FieldNodeType.Snow => ResourceType.Ice,
                FieldNodeType.Smelter => ResourceType.Water,
                FieldNodeType.Woodcutter => ResourceType.Wood,
                FieldNodeType.Bonfire => ResourceType.Energy,
                FieldNodeType.Recycler => ResourceType.StarDust,
                FieldNodeType.Researcher => ResourceType.ResearchPoint,
                FieldNodeType.DustBreaker => ResourceType.StarDust,
                FieldNodeType.Farm => _random.NextDouble() < GetFlowerProbability(fieldObject) ? ResourceType.Flower : ResourceType.Tree,
                _ => ResourceType.Energy
            };

            var multiplier = fieldObject.Type switch
            {
                FieldNodeType.Snow => iceMult,
                FieldNodeType.Energy => energyMult,
                FieldNodeType.Recycler => recycledStarsVal,
                FieldNodeType.DustBreaker => recycledStarsVal,
                _ => BigDouble.One
            };

            if (fieldObject.Type == FieldNodeType.Farm)
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                var saplings = ctx.Get(Sapling);
                var baseValue = fieldObject.OutputValue.BaseValue;
                var currentWater = _resources.GetResource(ResourceType.Water, ctx);
                
                var totalW = BigDouble.Min(currentWater, saplings * baseValue * completedCycles);
                
                // Override consumed water for Farm
                consumedResources[ResourceType.Water] = totalW;
                
                var outputValue = fieldObject.EvaluateValue(ctx);
                generatedResources[producedType] += totalW * outputValue / baseValue;
            }
            else if (fieldObject.Type == FieldNodeType.Woodcutter)
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * 10;
            }
            else if (fieldObject.Type == FieldNodeType.Bonfire)
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * 3000;
            }
            else
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * multiplier;
            }
        }

        foreach (var (type, amount) in consumedResources)
        {
            _resources.AddResource(type, -amount);
        }

        foreach (var (type, amount) in generatedResources)
        {
            _resources.AddResource(type, amount);
        }

        foreach (var (type, amount) in structureBuilt)
        {
            if (type == 0) Markets.BaseValue += amount;
            else Houses.BaseValue += amount;
        }

        if (SnowmanCount > 0)
        {
            _resources.AddResource(ResourceType.Coin, SnowmanCoinIncomePerSecond * deltaSeconds);
            _resources.AddResource(ResourceType.Carrot, SnowmanCarrotIncomePerSecond * deltaSeconds);
        }

        RefreshSlowPassiveStats(deltaSeconds);

        _pendingUiUpdateSeconds += deltaSeconds;
        if (_pendingUiUpdateSeconds >= MinUiUpdateInterval)
        {
            _pendingUiUpdateSeconds = 0;
            RequestUiUpdate?.Invoke();
        }
    }

    private void RefreshSlowPassiveStats(double deltaSeconds)
    {
        _pendingSlowPassiveRefreshSeconds += deltaSeconds;
        if (_pendingSlowPassiveRefreshSeconds < SlowPassiveRefreshIntervalSeconds)
            return;

        _pendingSlowPassiveRefreshSeconds = 0d;
        RefreshHappiness();
    }

    private static int GetMaxAffordableCycles(
        IReadOnlyList<(ResourceType Type, BigDouble Amount)> consumption,
        Func<ResourceType, BigDouble> getAvailableResource)
    {
        if (consumption.Count == 0)
            return int.MaxValue;

        var maxAffordableCycles = int.MaxValue;
        foreach (var (type, amount) in consumption)
        {
            if (amount <= BigDouble.Zero)
                continue;

            var affordableCycles = GetAffordableCycleLimit(getAvailableResource(type), amount);
            if (affordableCycles <= 0)
                return 0;

            maxAffordableCycles = Math.Min(maxAffordableCycles, affordableCycles);
        }

        return maxAffordableCycles;
    }

    private static int GetAffordableCycleLimit(BigDouble available, BigDouble amountPerCycle)
    {
        if (amountPerCycle <= BigDouble.Zero)
            return int.MaxValue;

        if (available < amountPerCycle)
            return 0;

        var low = 1;
        var high = 2;
        while (high < int.MaxValue / 2 && amountPerCycle * high <= available)
        {
            low = high;
            high *= 2;
        }

        if (amountPerCycle * high <= available)
            return int.MaxValue;

        while (low < high)
        {
            var mid = low + ((high - low + 1) / 2);
            if (amountPerCycle * mid <= available)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    private void RefreshHappiness()
    {
        var nextHappiness = EvaluateHappiness();
        if (Happiness.BaseValue != nextHappiness)
            Happiness.BaseValue = nextHappiness;
    }

    public IEnumerable<(ResourceType Type, BigDouble Amount)> GetDynamicConsumption(FieldNode node)
    {
        var beamInputMultiplier = node.GetBeamInputMultiplier();
        var itemLevelMultiplier = node.ItemLevel;
        if (node.Type == FieldNodeType.Smelter)
        {
            var baseValue = node.OutputValue.BaseValue;
            yield return (ResourceType.Energy, 10 * baseValue);
            yield return (ResourceType.Ice, 10 * baseValue);
        }
        else if (node.Type == FieldNodeType.Farm)
        {
            // Water consumption for Farm is handled dynamically in Advance loop to allow for partial production if water is low.
            // However, GetDynamicConsumption is used to check if ANY production can happen.
            // We'll return the 'ideal' consumption here.
            yield return (ResourceType.Water, EvaluateStat(Sapling) * node.OutputValue.BaseValue);
        }
        else
        {
            // Note: FieldNodeType.Consumption() currently returns empty for some types to avoid redundancy with this method.
            foreach (var c in node.Type.Consumption())
                yield return (c.Type, c.Amount * itemLevelMultiplier * beamInputMultiplier);
        }

        switch (node.Type)
        {
            case FieldNodeType.Woodcutter:
                yield return (ResourceType.Tree, node.OutputValue.BaseValue * beamInputMultiplier);
                break;
            case FieldNodeType.Builder:
                yield return (ResourceType.Wood, 100 * node.OutputValue.BaseValue * beamInputMultiplier);
                break;
            case FieldNodeType.Bonfire:
                yield return (ResourceType.Wood, 10 * node.OutputValue.BaseValue * beamInputMultiplier);
                yield return (ResourceType.Energy, 1000 * node.OutputValue.BaseValue * beamInputMultiplier);
                break;
        }
    }

    public BigDouble GetDisplayedFactoryValue(FieldNode node)
    {
        if (node.Type == FieldNodeType.Farm)
        {
            return BigDouble.Min(GetResource(ResourceType.Water), EvaluateStat(Sapling) * node.OutputValue.BaseValue) * node.EvaluateValue() / node.OutputValue.BaseValue;
        }

        var val = node.EvaluateValue();
        return node.Type switch
        {
            FieldNodeType.Woodcutter => val * 10,
            FieldNodeType.Bonfire => val * 3000,
            _ => val
        };
    }

    public BigDouble GetDisplayedFactoryBaseValue(FieldNode node)
    {
        var val = node.OutputValue.BaseValue;
        return node.Type switch
        {
            FieldNodeType.Woodcutter => val * 10,
            FieldNodeType.Bonfire => val * 3000,
            _ => val
        };
    }

    private BigDouble GetFarmSeedCount(FieldNode node)
        => EvaluateStat(Sapling);

    public void ToggleFieldObjectLever(int fieldObjectId)
    {
        var fieldObject = _field.GetFieldObject(fieldObjectId);
        fieldObject?.ToggleBuilderLever();
        RequestUiUpdate?.Invoke();
    }

    public void ToggleFieldObjectPause(int fieldObjectId)
    {
        var fieldObject = _field.GetFieldObject(fieldObjectId);
        fieldObject?.TogglePause();
        RequestUiUpdate?.Invoke();
    }

    public void AddFieldObject(FieldNodeType type = FieldNodeType.Energy)
    {
        type = type.Normalize();
        if (FieldObjects.Count >= 24) return;
        var count = CountFieldObjects(type);
        if ((type is FieldNodeType.Energy or FieldNodeType.Snow) && count >= 3) return;
        if (type == FieldNodeType.Researcher && count >= 1) return;
        if (type == FieldNodeType.Smelter && count >= 2) return;
        if (type == FieldNodeType.DustBreaker && count >= 2) return;
        if (type == FieldNodeType.Farm && count >= 2) return;
        if (type == FieldNodeType.Woodcutter && count >= 1) return;
        if (type == FieldNodeType.Builder && count >= 2) return;

        var cost = GetFieldNodeCost(type);
        var costResource = ResourceType.Coin;
        
        if (GetResource(costResource) < cost) return;

        _resources.AddResource(costResource, -cost);
        var affectedTargetFieldNodeIds = _field.AddFieldObject(type);
        if (SelectedFieldObject is not null)
            ApplyTemperatureModifiers(SelectedFieldObject);
        ApplyBeamStarModifiers(affectedTargetFieldNodeIds);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void CheatResources()
    {
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
        {
            var stat = _resources.GetStat(type);
            stat.BaseValue = (stat.BaseValue + 10) * 2;
        }
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void RemoveFieldObject(FieldNodeType type)
    {
        type = type.Normalize();
        ApplyBeamStarModifiers(_field.RemoveFieldObject(type));
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void RemoveFieldObject()
    {
        if (FieldObjects.Count == 0) return;
        RemoveFieldObject(FieldObjects[^1].Type);
    }

    public bool CanRemoveFieldObject(FieldNodeType type) => FieldObjects.Any(f => f.Type == type.Normalize());
    public int CountFieldObjects(FieldNodeType type) => FieldObjects.Count(f => f.Type == type.Normalize());

    public void SelectFieldObject(int? fieldObjectId)
    {
        _field.SelectFieldObject(fieldObjectId);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetFieldNodeHeld(int fieldObjectId, bool isHeld)
    {
        var node = _field.GetFieldObject(fieldObjectId);
        node?.SetHeld(isHeld);
        RequestUiUpdate?.Invoke();
    }

    public bool CanUpgradeSelectedFieldObjectItem()
    {
        if (SelectedFieldObject is null) return false;
        if (!SelectedFieldObject.CanUpgradeItem) return false;
        var cost = SelectedFieldObject.GetItemUpgradeCost();
        var costResource = ResourceType.Coin;
        return GetResource(costResource) >= cost;
    }

    public void UpgradeSelectedFieldObjectItem()
    {
        if (SelectedFieldObject is null) return;
        if (!SelectedFieldObject.CanUpgradeItem) return;
        var cost = SelectedFieldObject.GetItemUpgradeCost();
        
        var costResource = ResourceType.Coin;
        if (GetResource(costResource) < cost) return;

        _resources.AddResource(costResource, -cost);
        SelectedFieldObject.UpgradeItem();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public bool CanUpgradeSelectedFieldObjectTime()
    {
        if (SelectedFieldObject is null) return false;
        if (!SelectedFieldObject.CanUpgradeTime) return false;
        var cost = SelectedFieldObject.GetTimeUpgradeCost();
        var costResource = ResourceType.StarDust;
        return GetResource(costResource) >= cost;
    }

    public void UpgradeSelectedFieldObjectTime()
    {
        if (SelectedFieldObject is null) return;
        if (!SelectedFieldObject.CanUpgradeTime) return;
        var cost = SelectedFieldObject.GetTimeUpgradeCost();
        
        var costResource = ResourceType.StarDust;
        if (GetResource(costResource) < cost) return;

        _resources.AddResource(costResource, -cost);
        SelectedFieldObject.UpgradeTime();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    private BigDouble ApplyExchangeMarketModifier(BigDouble cost)
        => cost / (EvaluateStat(Markets) + 1);

    public BigDouble GetSnowmanCost()
    {
        var cost = UpgradeLookup.GetSnowmanCost(SnowmanCount);
        return ApplyExchangeMarketModifier(cost);
    }

    public BigDouble GetSaplingCost()
    {
        var cost = UpgradeLookup.GetSaplingCost(SaplingCount);
        return ApplyExchangeMarketModifier(cost);
    }

    public bool CanBuySnowman()
        => UpgradeLookup.CanBuyExchangeLevel(SnowmanCount)
           && EvaluateStat(Ice) >= GetSnowmanCost();

    public void BuySnowman()
    {
        if (!CanBuySnowman()) return;
        var cost = GetSnowmanCost();

        _resources.AddResource(ResourceType.Ice, -cost);
        _resources.SnowmanCount++;
        Snowman.BaseValue += 1;

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public BigDouble GetFieldNodeCost(FieldNodeType type)
    {
        type = type.Normalize();
        var cost = UpgradeLookup.GetFieldNodeCost(type, CountFieldObjects(type));
        if (type == FieldNodeType.Builder)
        {
            cost /= (EvaluateStat(Markets) + 1);
        }
        return cost;
    }

    public bool IsFieldNodeMaxed(FieldNodeType type)
    {
        type = type.Normalize();
        var count = CountFieldObjects(type);
        if (type == FieldNodeType.Researcher && count >= 1) return true;
        if (type == FieldNodeType.Smelter && count >= 2) return true;
        if (type == FieldNodeType.DustBreaker && count >= 2) return true;
        if (type == FieldNodeType.Farm && count >= 2) return true;
        if (type == FieldNodeType.Recycler && count >= 2) return true;
        if (type == FieldNodeType.Woodcutter && count >= 1) return true;
        if (type == FieldNodeType.Builder && count >= 2) return true;
        if (type == FieldNodeType.Bonfire && count >= 1) return true;
        if ((type is FieldNodeType.Energy or FieldNodeType.Snow) && count >= 3) return true;
        return false;
    }

    public bool CanBuyFieldNode(FieldNodeType type)
    {
        if (!IsFactoryUnlocked(type)) return false;
        if (IsFieldNodeMaxed(type)) return false;

        var cost = GetFieldNodeCost(type);
        var costResource = ResourceType.Coin;
        return GetResource(costResource) >= cost;
    }

    public bool CanBuySapling()
        => HasResearch(ResearchType.UnlockSaplings)
           && UpgradeLookup.CanBuyExchangeLevel(SaplingCount)
           && EvaluateStat(Coin) >= GetSaplingCost();

    public void BuySapling()
    {
        if (!CanBuySapling()) return;
        var cost = GetSaplingCost();

        _resources.AddResource(ResourceType.Coin, -cost);
        _resources.SaplingCount++;
        _resources.AddResource(ResourceType.Sapling, 1);

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public BigDouble GetTemperatureMaxCost()
    {
        var cost = UpgradeLookup.GetTemperatureMaxCost(MaxTemperatureCelsius - InitialMaxTemperatureCelsius);

        if (MaxTemperatureCelsius >= FirstVictoryTemperatureCelsius)
        {
            // The second act should feel meaningfully steeper once the cap pushes past 20C.
            var extraHeatLevels = MaxTemperatureCelsius - FirstVictoryTemperatureCelsius + 1;
            cost *= Pow(new BigDouble(2.5), extraHeatLevels);
        }

        return ApplyExchangeMarketModifier(cost);
    }

    public bool CanBuyTemperatureMax()
        => HasResearch(ResearchType.UnlockTemperatureBar)
           && HasResearch(ResearchType.UnlockTemperatureExchange)
           && UpgradeLookup.CanBuyExchangeLevel(MaxTemperatureCelsius - InitialMaxTemperatureCelsius)
           && MaxTemperatureCelsius < TemperatureExchangeLimitCelsius
           && EvaluateStat(Energy) >= GetTemperatureMaxCost();

    public void BuyTemperatureMax()
    {
        if (!CanBuyTemperatureMax()) return;

        _resources.AddResource(ResourceType.Energy, -GetTemperatureMaxCost());
        MaxTemperatureCelsius++;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public BigDouble GetResearchPointCost() => EvaluateStat(ResearchPoint) * 100 + 1000;
    public bool CanBuyResearchPoint() => EvaluateStat(Energy) >= GetResearchPointCost();

    public void BuyResearchPoint()
    {
        var cost = GetResearchPointCost();
        if (EvaluateStat(Energy) < cost) return;

        _resources.AddResource(ResourceType.Energy, -cost);
        _resources.AddResource(ResourceType.ResearchPoint, 1);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }


    public bool HasResearch(ResearchType type) => _research.HasResearch(type);

    private bool IsResearchVisible(ResearchType type)
        => type switch
        {
            ResearchType.UnlockGoal => !VictoryContinued,
            ResearchType.UnlockSecondActTemperatureRange => VictoryContinued,
            ResearchType.UnlockSecondActHappinessRange
                => VictoryContinued && HasResearch(ResearchType.UnlockSecondActTemperatureRange),
            ResearchType.UnlockSecondActGoal
                => VictoryContinued && HasResearch(ResearchType.UnlockSecondActHappinessRange),
            _ => true
        };

    public bool CanBuyResearch(ResearchType type)
    {
        if (HasResearch(type)) return false;
        if (!IsResearchVisible(type)) return false;
        var definition = ResearchCatalog.Get(type);
        return GetResource(definition.PriceResource) >= definition.Price;
    }

    public void BuyResearch(ResearchType type)
    {
        if (HasResearch(type)) return;
        if (!IsResearchVisible(type)) return;
        var definition = ResearchCatalog.Get(type);
        if (GetResource(definition.PriceResource) < definition.Price) return;

        _resources.AddResource(definition.PriceResource, -definition.Price);
        _research.AddResearch(type);

        if (type == ResearchType.UnlockStars)
        {
            Stars.BaseValue = BigDouble.One;
            _starInventory[0].AddStar(new Star(StarType.Yellow, 1.2, 0, 1));
        }

        if (type == ResearchType.UnlockTemperatureBar)
        {
            MaxTemperatureCelsius = InitialMaxTemperatureCelsius;
            var currentTemperature = TemperatureCelsius;
            var clampedTemperature = Math.Clamp(currentTemperature, -10, MaxTemperatureCelsius);
            Temperature.BaseValue = new BigDouble(clampedTemperature);
            ApplyTemperatureModifiers();
        }

        if (type == ResearchType.UnlockStarRecycler)
        {
            RecycledStars.BaseValue = BigDouble.Zero;
        }

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetFieldSize(double width, double height)
    {
        if (!_field.SetFieldSize(width, height))
            return;

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void BeginStarDrag(int inventorySlotIndex)
    {
        if (inventorySlotIndex < 0 || inventorySlotIndex >= _starInventory.Count)
            return;

        if (!_starInventory[inventorySlotIndex].HasStar)
            return;

        DraggingStarSlotIndex = inventorySlotIndex;
        RequestUiUpdate?.Invoke();
    }

    public bool CanDropStarIntoInventory(int targetSlotIndex)
        => DraggingStarSlotIndex is not null
           && DraggingStarSlotIndex != targetSlotIndex
           && targetSlotIndex >= 0
           && targetSlotIndex < _starInventory.Count
           && !_starInventory[targetSlotIndex].HasStar;

    public void MoveStarInInventory(int targetSlotIndex)
    {
        if (!CanDropStarIntoInventory(targetSlotIndex))
            return;

        var star = _starInventory[DraggingStarSlotIndex!.Value].Star;
        _starInventory[DraggingStarSlotIndex!.Value].ConsumeStar();
        _starInventory[targetSlotIndex].AddStar(star!);
        DraggingStarSlotIndex = null;
        RequestUiUpdate?.Invoke();
    }

    public void EndStarDrag()
    {
        if (DraggingStarSlotIndex is null)
            return;

        DraggingStarSlotIndex = null;
        RequestUiUpdate?.Invoke();
    }

    public void SendLowestYellowStarToRecycler()
    {
        var recycler = FieldObjects
            .Where(fieldObject => fieldObject.Type == FieldNodeType.Recycler)
            .OrderByDescending(EvaluateRecyclerResultValue)
            .ThenBy(fieldObject => fieldObject.Id)
            .FirstOrDefault();
        if (recycler is null)
            return;

        var starSlot = FindLowestPriorityStarSlotForRecycler();
        if (starSlot == null)
            return;

        var star = starSlot.Star;
        starSlot.ConsumeStar();
        recycler.EnqueueRecycle();
        RecycledStars.BaseValue += star?.RecycleValue ?? 1;

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    private StarInventorySlot? FindLowestPriorityStarSlotForRecycler()
    {
        foreach (var starType in RecycleStarTypePriority)
        {
            var slot = _starInventory
                .Where(s => s.HasStar && s.Star!.Type == starType)
                .OrderBy(s => s.Star!.ProductionMultiplier)
                .ThenBy(s => s.Star!.CooldownReduction)
                .FirstOrDefault();

            if (slot != null)
                return slot;
        }

        return null;
    }

    private BigDouble EvaluateRecyclerResultValue(FieldNode recycler)
        => GetDisplayedFactoryValue(recycler) * EvaluatePassiveStat(PassiveStatType.RecycledStars);

    public void SortStarInventoryByRarityAndMultiplier()
    {
        EndStarDrag();

        var sortedStars = _starInventory
            .Where(slot => slot.HasStar)
            .Select(slot => slot.Star!)
            .OrderByDescending(star => star.Type)
            .ThenByDescending(star => star.ProductionMultiplier)
            .ToList();

        foreach (var slot in _starInventory)
        {
            slot.ConsumeStar();
        }

        for (var i = 0; i < sortedStars.Count; i++)
        {
            _starInventory[i].AddStar(sortedStars[i]);
        }

        RequestUiUpdate?.Invoke();
    }

    public bool CanPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide)
        => DraggingStarSlotIndex is not null && _field.HasBeamStarSlot(sourceFieldNodeId, sourceSide);

    public bool TryPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide)
    {
        if (DraggingStarSlotIndex is null)
            return false;

        var beamStarSlot = _field.GetBeamStarSlot(sourceFieldNodeId, sourceSide);
        if (beamStarSlot is null)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        var star = inventorySlot.Star;
        if (!_field.TryPlaceStar(sourceFieldNodeId, sourceSide, star!))
            return false;

        inventorySlot.ConsumeStar();
        DraggingStarSlotIndex = null;
        ApplyBeamStarModifiers(beamStarSlot.TargetFieldNodeId);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    public bool TryPlaceStarInRecycler(int recyclerFieldNodeId)
    {
        if (DraggingStarSlotIndex is null)
            return false;

        var fieldNode = _field.GetFieldObject(recyclerFieldNodeId);
        if (fieldNode == null || fieldNode.Type != FieldNodeType.Recycler)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        var star = inventorySlot.Star;
        inventorySlot.ConsumeStar();
        fieldNode.EnqueueRecycle();
        RecycledStars.BaseValue += star?.RecycleValue ?? 1;
        DraggingStarSlotIndex = null;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    public void BeginFieldObjectDrag(int fieldObjectId) { }
    public void EndFieldObjectDrag(int fieldObjectId) { }
    public void MoveFieldObject(int fieldObjectId, double x, double y)
    {
        var fieldObject = _field.GetFieldObject(fieldObjectId);
        if (fieldObject is null)
            return;

        var previousX = fieldObject.X;
        var previousY = fieldObject.Y;
        var affectedTargetFieldNodeIds = _field.MoveFieldObject(fieldObjectId, x, y);
        if (Math.Abs(fieldObject.X - previousX) < 0.1d && Math.Abs(fieldObject.Y - previousY) < 0.1d)
            return;

        ApplyBeamStarModifiers(affectedTargetFieldNodeIds);
        RequestUiUpdate?.Invoke();
    }

    public void RefreshConnections()
    {
        ApplyBeamStarModifiers(_field.RefreshConnections());
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetTemperature(int temperatureCelsius)
    {
        if (!CanAdjustTemperature())
            return;

        var clampedTemperature = Math.Clamp(temperatureCelsius, MinTemperatureCelsius, MaxTemperatureCelsius);
        var nextTemperature = new BigDouble(clampedTemperature);
        if (Temperature.BaseValue == nextTemperature)
            return;

        Temperature.BaseValue = nextTemperature;
        ApplyTemperatureModifiers();

        if (TemperatureCelsius >= GoalTemperatureCelsius)
        {
            IsVictory = true;
        }

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void ContinueVictory()
    {
        IsVictory = false;
        VictoryContinued = true;
        MaxTemperatureCelsius = FirstVictoryTemperatureCelsius;

        var clampedTemperature = Math.Clamp(TemperatureCelsius, MinTemperatureCelsius, MaxTemperatureCelsius);
        var nextTemperature = new BigDouble(clampedTemperature);
        if (Temperature.BaseValue != nextTemperature)
        {
            Temperature.BaseValue = nextTemperature;
            ApplyTemperatureModifiers();
        }

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetMusicVolume(double volume)
    {
        MusicVolume = Math.Clamp(volume, 0d, MaxMusicVolume);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void ToggleWeather()
    {
        IsWeatherDisabled = !IsWeatherDisabled;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public int GetSnowTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        return (int)Math.Round(
            InterpolateLinear(
                temperature,
                (MinTemperatureCelsius, 100d),
                (10d, -100d),
                (FinalVictoryTemperatureCelsius, -100d)),
            MidpointRounding.AwayFromZero);
    }

    public int GetWaterTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        return (int)Math.Round(
            InterpolateLinear(
                temperature,
                (MinTemperatureCelsius, -50d),
                (5d, 50d),
                (FirstVictoryTemperatureCelsius, -50d),
                (FinalVictoryTemperatureCelsius, -100d)),
            MidpointRounding.AwayFromZero);
    }

    public int GetEnergyTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        if (temperature <= 0d)
            return 0;

        return (int)Math.Round(
            InterpolateLinear(
                temperature,
                (0d, 0d),
                (5d, -50d),
                (10d, 0d),
                (FirstVictoryTemperatureCelsius, 100d),
                (FinalVictoryTemperatureCelsius, 300d)),
            MidpointRounding.AwayFromZero);
    }

    public BigDouble EvaluateStat(Stat stat) => stat.EvaluateCached();

    private BigDouble GetResource(ResourceType type)
        => _resources.GetResource(type, new EvaluationContext());

    private void ApplyTemperatureModifiers()
    {
        foreach (var fieldObject in FieldObjects)
            ApplyTemperatureModifiers(fieldObject);
    }

    private void ApplyTemperatureModifiers(FieldNode fieldObject)
    {
        var valueMultiplier = fieldObject.Type switch
        {
            FieldNodeType.Snow => SnowTemperatureMultiplier,
            FieldNodeType.Energy => EnergyTemperatureMultiplier,
            FieldNodeType.Smelter => WaterTemperatureMultiplier,
            _ => BigDouble.One
        };

        fieldObject.SetTemperatureValueMultiplier(valueMultiplier);
        fieldObject.SetTemperatureCooldownMultiplier(BigDouble.One);
    }

    private void ApplyBeamStarModifiers()
    {
        foreach (var fieldObject in FieldObjects)
        {
            var productionMultiplier = BigDouble.One;
            var cooldownReduction = BigDouble.Zero;

            var incomingStars = BeamStarSlots.Where(slot => slot.TargetFieldNodeId == fieldObject.Id && slot.Star != null);
            foreach (var slot in incomingStars)
            {
                var star = slot.Star!;
                productionMultiplier *= star.ProductionMultiplier;
                cooldownReduction += star.CooldownReduction;
            }

            fieldObject.SetBeamValueMultiplier(productionMultiplier);
            fieldObject.SetBeamCooldownReduction(cooldownReduction);
        }
    }

    private void ApplyBeamStarModifiers(int? affectedTargetFieldNodeId)
    {
        if (affectedTargetFieldNodeId is not int fieldNodeId)
            return;

        ApplyBeamStarModifiers([fieldNodeId]);
    }

    private void ApplyBeamStarModifiers(IReadOnlyCollection<int> affectedTargetFieldNodeIds)
    {
        if (affectedTargetFieldNodeIds.Count == 0)
            return;

        foreach (var fieldNodeId in affectedTargetFieldNodeIds)
        {
            var fieldObject = _field.GetFieldObject(fieldNodeId);
            if (fieldObject is null)
                continue;

            var productionMultiplier = BigDouble.One;
            var cooldownReduction = BigDouble.Zero;

            foreach (var slot in BeamStarSlots)
            {
                if (slot.TargetFieldNodeId != fieldNodeId || slot.Star is null)
                    continue;

                productionMultiplier *= slot.Star.ProductionMultiplier;
                cooldownReduction += slot.Star.CooldownReduction;
            }

            fieldObject.SetBeamValueMultiplier(productionMultiplier);
            fieldObject.SetBeamCooldownReduction(cooldownReduction);
        }
    }

    private static BigDouble BonusPercentToMultiplier(int percent)
    {
        var multiplier = BigDouble.One + new BigDouble(percent) / 100d;
        return multiplier < BigDouble.Zero ? BigDouble.Zero : multiplier;
    }

    private static double InterpolateLinear(double x, params (double X, double Y)[] points)
    {
        if (points.Length == 0)
            return 0d;

        if (x <= points[0].X)
            return points[0].Y;

        for (var index = 1; index < points.Length; index++)
        {
            var previous = points[index - 1];
            var current = points[index];
            if (x <= current.X)
            {
                var progress = (x - previous.X) / (current.X - previous.X);
                return previous.Y + ((current.Y - previous.Y) * progress);
            }
        }

        return points[^1].Y;
    }

    private static BigDouble Pow2(int exponent)
    {
        if (exponent <= 0) return BigDouble.One;
        var base10Exponent = exponent * Log10Of2;
        var wholeExponent = (int)Math.Floor(base10Exponent);
        var mantissa = Math.Pow(10d, base10Exponent - wholeExponent);
        return new BigDouble(mantissa, wholeExponent);
    }

    private static BigDouble Pow(BigDouble value, int exponent)
    {
        if (exponent <= 0)
            return BigDouble.One;

        var result = BigDouble.One;
        for (var index = 0; index < exponent; index++)
            result *= value;

        return result;
    }

    public BigDouble EvaluatePassiveStat(PassiveStatType type)
    {
        return EvaluateStat(GetStatForPassive(type));
    }

    public bool CanPlacePassiveStatStar(PassiveStatType type)
        => IsPassiveStatStarSlotsUnlocked()
           && DraggingStarSlotIndex is not null
           && _passiveStatStarSlots.Any(s => s.Type == type && s.IsEnabled);

    public bool TryPlacePassiveStatStar(PassiveStatType type)
    {
        if (!IsPassiveStatStarSlotsUnlocked())
            return false;

        if (DraggingStarSlotIndex is null)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        var slotIndex = _passiveStatStarSlots.FindIndex(s => s.Type == type && s.IsEnabled);
        if (slotIndex < 0)
            return false;

        var star = inventorySlot.Star;
        _passiveStatStarSlots[slotIndex] = _passiveStatStarSlots[slotIndex] with { Star = star };
        inventorySlot.ConsumeStar();
        DraggingStarSlotIndex = null;
        ApplyPassiveStatStarModifiers();
        
        // If it's a happiness-related stat, we need to ensure UI knows about it
        if (type == PassiveStatType.Happiness || type == PassiveStatType.Carrots || type == PassiveStatType.Houses || type == PassiveStatType.Flower)
        {
             // These affect star odds, so refresh the cached passive value immediately.
             RefreshHappiness();
        }
        
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    private void ApplyPassiveStatStarModifiers()
    {
        foreach (var type in Enum.GetValues<PassiveStatType>())
        {
            var stars = _passiveStatStarSlots.Where(s => s.Type == type && s.IsEnabled && s.Star != null).ToList();
            var multiplier = BigDouble.One;
            foreach (var s in stars)
            {
                var star = s.Star!;
                var starBonus = star.ProductionMultiplier;
                
                // If it has cooldown reduction, give it a small bonus to production as well for passives
                if (star.CooldownReduction > 0)
                {
                    starBonus *= (1.0 + star.CooldownReduction / 10.0);
                }
                
                multiplier *= starBonus;
            }

            if (type == PassiveStatType.Stars)
            {
                multiplier = BigDouble.Max(BigDouble.One, Log2(multiplier + 1));
            }

            _passiveStatStarMultipliers[type] = multiplier;

            // Remove old modifier if it exists
            if (_passiveStatStarModifiers.TryGetValue(type, out var oldModifier))
            {
                GetStatForPassive(type).RemoveModifier(oldModifier);
                _passiveStatStarModifiers.Remove(type);
            }

            if (stars.Any() && multiplier > BigDouble.One)
            {
                var modifier = new MulModifier(_ => multiplier);
                GetStatForPassive(type).AddModifier(modifier);
                _passiveStatStarModifiers[type] = modifier;
            }
        }
    }

    public Stat GetStatForPassive(PassiveStatType type)
        => type switch
        {
            PassiveStatType.Happiness => Happiness,
            PassiveStatType.Seeds => Sapling,
            PassiveStatType.Markets => Markets,
            PassiveStatType.Houses => Houses,
            PassiveStatType.Stars => Stars,
            PassiveStatType.RecycledStars => RecycledStars,
            PassiveStatType.Flower => Flower,
            PassiveStatType.Snowman => Snowman,
            PassiveStatType.Carrots => Carrot,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public BigDouble EvaluateHappiness()
    {
        var carrots = EvaluateStat(Carrot);
        var flowers = EvaluateStat(Flower);
        var houses = EvaluateStat(Houses);
        
        return (carrots + 1) * (flowers + 1) * (houses + 1);
    }
    
    public int StarUpgradeLevel { get; private set; }
    public BigDouble GetStarUpgradeCost()
    {
        var cost = UpgradeLookup.GetStarUpgradeCost(StarUpgradeLevel);
        return ApplyExchangeMarketModifier(cost);
    }
    public bool CanUpgradeStars()
        => HasResearch(ResearchType.UnlockStars)
           && HasResearch(ResearchType.UnlockStarImprover)
           && HasResearch(ResearchType.UnlockStarRecycler)
           && HasResearch(ResearchType.UnlockWoodcutter)
           && UpgradeLookup.CanBuyExchangeLevel(StarUpgradeLevel)
           && GetResource(ResourceType.StarDust) >= GetStarUpgradeCost()
           && GetResource(ResourceType.Wood) >= GetStarUpgradeCost();

    public void UpgradeStars()
    {
        var cost = GetStarUpgradeCost();
        if (!CanUpgradeStars()) return;

        _resources.AddResource(ResourceType.StarDust, -cost);
        _resources.AddResource(ResourceType.Wood, -cost);
        StarUpgradeLevel++;
        Stars.BaseValue += 1;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }
}
