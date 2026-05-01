using BreakInfinity;
using Emojimental.GameEngine.Values;
using Emojimental.Models;

namespace Emojimental.Services;

public sealed class GameState
{
    private const double Log10Of2 = 0.3010299956639812d;
    private const int StarInventorySize = 80;
    private const int StartingStarCount = 10;
    private const double DayCycleDurationSeconds = 15; // 15 seconds for testing

    private readonly ResourceState _resources = new();
    private readonly ResearchState _research = new();
    private readonly FieldState _field = new();
    private readonly List<StarInventorySlot> _starInventory = Enumerable.Range(0, StarInventorySize)
        .Select(index => new StarInventorySlot(index, index < StartingStarCount))
        .ToList();
    private readonly List<PassiveStatStarSlot> _passiveStatStarSlots = Enum.GetValues<PassiveStatType>()
        .Select(type => new PassiveStatStarSlot(type))
        .ToList();
    private readonly Dictionary<PassiveStatType, MulModifier> _passiveStatStarModifiers = new();
    private readonly Dictionary<PassiveStatType, BigDouble> _passiveStatStarMultipliers = Enum.GetValues<PassiveStatType>()
        .ToDictionary(t => t, _ => BigDouble.One);
    private const double PassiveStarMultiplierPerStar = 1.2d;
    private static readonly string[] StarModifierLines = ["✳️x1.2"];

    public GameState()
    {
        Stars.BaseValue = BigDouble.One;
        RecycledStars.BaseValue = BigDouble.Zero;
        _field.AddFieldObject(FieldNodeType.Energy);
        _field.AddFieldObject(FieldNodeType.Snow);
    }

    public Stat Energy => _resources.Energy;
    public Stat Ice => _resources.Ice;
    public Stat Coin => _resources.Coin;
    public Stat Sapling => _resources.Sapling;
    public Stat Tree => _resources.Tree;
    public Stat Water => _resources.Water;
    public Stat Temperature { get; } = new() { BaseValue = BigDouble.Zero };

    public Stat Happiness => _resources.Happiness;
    public Stat ResearchPoint => _resources.ResearchPoint;

    public int SnowmanCount => _resources.SnowmanCount;
    public int SaplingCount => _resources.SaplingCount;
    public Stat Wood => _resources.Wood;
    public Stat StarDust => _resources.StarDust;
    public Stat RecycledStars { get; } = new();

    public Stat Stars { get; } = new();

    public Stat Markets { get; } = new();
    public Stat Houses { get; } = new();

    public BigDouble EnergyMultiplier => _research.HasResearch(ResearchType.SunChorus) ? new BigDouble(2) : BigDouble.One;
    public BigDouble IceMultiplier => _research.HasResearch(ResearchType.FrostBloom) ? new BigDouble(2) : BigDouble.One;
    public BigDouble CoinMultiplier => _research.HasResearch(ResearchType.CoinFlow) ? new BigDouble(2) : BigDouble.One;

    public BigDouble SnowmanCoinIncomePerSecond => UpgradeLookup.GetSnowmanCoinBonus(SnowmanCount) * CoinMultiplier;
    public BigDouble SnowmanHappinessIncomePerSecond => UpgradeLookup.GetSnowmanHappinessBonus(SnowmanCount);

    public IReadOnlyList<FieldNode> FieldObjects => _field.FieldObjects;
    public IReadOnlyList<AlignmentBeam> AlignmentBeams => _field.AlignmentBeams;
    public IReadOnlyList<BeamStarSlot> BeamStarSlots => _field.BeamStarSlots;
    public IReadOnlyList<StarInventorySlot> StarInventory => _starInventory;
    public IReadOnlyCollection<ResearchType> OwnedResearch => _research.OwnedResearch;
    public IReadOnlyList<ResearchDefinition> Researches => ResearchCatalog.All;
    public IReadOnlyList<string> StarModifiers => StarModifierLines;
    public IReadOnlyList<PassiveStatStarSlot> PassiveStatStarSlots => _passiveStatStarSlots;

    public FieldNode? SelectedFieldObject => _field.SelectedFieldObject;
    public int? DraggingStarSlotIndex { get; private set; }

    public double FieldWidth => _field.FieldWidth;
    public double FieldHeight => _field.FieldHeight;

    public event Action? Changed;
    public event Action? RequestUiUpdate;

    public TimeSpan PlayTime { get; private set; }

    public int TemperatureCelsius => (int)Math.Round(EvaluateStat(Temperature).ToDouble(), MidpointRounding.AwayFromZero);

    public BigDouble SnowTemperatureMultiplier => BonusPercentToMultiplier(GetSnowTemperatureBonusPercent());
    public BigDouble WaterTemperatureMultiplier => BonusPercentToMultiplier(GetWaterTemperatureBonusPercent());
    public BigDouble EnergyTemperatureMultiplier => BonusPercentToMultiplier(GetEnergyTemperatureBonusPercent());

    public double DayCyclePosition => (PlayTime.TotalSeconds % DayCycleDurationSeconds) / DayCycleDurationSeconds;

    public void AddStarToInventory()
    {
        var emptySlot = _starInventory.FirstOrDefault(s => !s.HasStar);
        if (emptySlot != null)
        {
            emptySlot.AddStar();
            Changed?.Invoke();
        }
    }

    public void Advance(double deltaSeconds)
    {
        PlayTime += TimeSpan.FromSeconds(deltaSeconds);

        if ((FieldObjects.Count == 0 && SnowmanCount == 0) || deltaSeconds <= 0d)
            return;

        var generatedResources = new Dictionary<ResourceType, BigDouble>();
        var consumedResources = new Dictionary<ResourceType, BigDouble>();

        var structureBuilt = new List<(int Type, BigDouble Amount)>();

        foreach (var fieldObject in FieldObjects)
        {
            var consumption = GetDynamicConsumption(fieldObject);
            var canProduce = true;
            foreach (var (resType, amount) in consumption)
            {
                var currentAmount = GetResource(resType);
                if (currentAmount < amount)
                {
                    canProduce = false;
                    break;
                }
            }

            var generatedValue = fieldObject.Advance(deltaSeconds, canProduce);
            if (generatedValue <= BigDouble.Zero)
                continue;

            // Consume resources if produced
            foreach (var (resType, amount) in consumption)
            {
                consumedResources.TryAdd(resType, BigDouble.Zero);
                consumedResources[resType] += amount;
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
                FieldNodeType.Garden => ResourceType.Tree,
                FieldNodeType.Woodcutter => ResourceType.Wood,
                FieldNodeType.Bonfire => ResourceType.Energy,
                FieldNodeType.Recycler => ResourceType.StarDust,
                _ => ResourceType.Energy
            };

            var multiplier = fieldObject.Type switch
            {
                FieldNodeType.Snow => IceMultiplier,
                FieldNodeType.Energy => EnergyMultiplier,
                FieldNodeType.Recycler => EvaluateStat(RecycledStars),
                _ => BigDouble.One
            };

            if (fieldObject.Type == FieldNodeType.Garden)
            {
                var saplingConsumed = consumption.FirstOrDefault(c => c.Type == ResourceType.Sapling).Amount;
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * saplingConsumed;
            }
            else if (fieldObject.Type == FieldNodeType.Woodcutter)
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * 10;
            }
            else if (fieldObject.Type == FieldNodeType.Bonfire)
            {
                generatedResources.TryAdd(producedType, BigDouble.Zero);
                generatedResources[producedType] += generatedValue * 20;
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
            _resources.AddResource(ResourceType.Happiness, SnowmanHappinessIncomePerSecond * deltaSeconds);
        }

        RequestUiUpdate?.Invoke();
    }

    public IEnumerable<(ResourceType Type, BigDouble Amount)> GetDynamicConsumption(FieldNode node)
    {
        var itemLevelMultiplier = node.ItemLevel;
        foreach (var c in node.Type.Consumption())
            yield return (c.Type, c.Amount * itemLevelMultiplier);

        switch (node.Type)
        {
            case FieldNodeType.Garden:
                var sapling = EvaluateStat(Sapling) * itemLevelMultiplier;
                yield return (ResourceType.Water, sapling);
                yield return (ResourceType.Sapling, sapling);
                break;
            case FieldNodeType.Woodcutter:
                yield return (ResourceType.Tree, 10 * itemLevelMultiplier);
                break;
            case FieldNodeType.Builder:
                yield return (ResourceType.Wood, 100 * itemLevelMultiplier);
                break;
            case FieldNodeType.Bonfire:
                yield return (ResourceType.Wood, 1 * itemLevelMultiplier);
                yield return (ResourceType.Energy, 10 * itemLevelMultiplier);
                break;
        }
    }

    public void ToggleFieldObjectLever(int fieldObjectId)
    {
        var fieldObject = FieldObjects.FirstOrDefault(f => f.Id == fieldObjectId);
        fieldObject?.ToggleBuilderLever();
        RequestUiUpdate?.Invoke();
    }

    public void ToggleFieldObjectPause(int fieldObjectId)
    {
        var fieldObject = FieldObjects.FirstOrDefault(f => f.Id == fieldObjectId);
        fieldObject?.TogglePause();
        RequestUiUpdate?.Invoke();
    }

    public void AddFieldObject(FieldNodeType type = FieldNodeType.Energy)
    {
        var cost = GetFieldNodeCost(type);
        if (EvaluateStat(Energy) < cost) return;

        _resources.AddResource(ResourceType.Energy, -cost);
        _field.AddFieldObject(type);
        ApplyTemperatureModifiers();
        ApplyBeamStarModifiers();
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
        _field.RemoveFieldObject(type);
        ApplyBeamStarModifiers();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void RemoveFieldObject()
    {
        if (FieldObjects.Count == 0) return;
        RemoveFieldObject(FieldObjects[^1].Type);
    }

    public bool CanRemoveFieldObject(FieldNodeType type) => FieldObjects.Any(f => f.Type == type);
    public int CountFieldObjects(FieldNodeType type) => FieldObjects.Count(f => f.Type == type);

    public void SelectFieldObject(int? fieldObjectId)
    {
        _field.SelectFieldObject(fieldObjectId);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public bool CanUpgradeSelectedFieldObjectItem()
        => SelectedFieldObject is not null && EvaluateStat(Energy) >= SelectedFieldObject.GetItemUpgradeCost();

    public void UpgradeSelectedFieldObjectItem()
    {
        if (SelectedFieldObject is null) return;
        var cost = SelectedFieldObject.GetItemUpgradeCost();
        if (EvaluateStat(Energy) < cost) return;

        _resources.AddResource(ResourceType.Energy, -cost);
        SelectedFieldObject.UpgradeItem();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public bool CanUpgradeSelectedFieldObjectTime()
        => SelectedFieldObject is not null && EvaluateStat(Energy) >= SelectedFieldObject.GetTimeUpgradeCost();

    public void UpgradeSelectedFieldObjectTime()
    {
        if (SelectedFieldObject is null) return;
        var cost = SelectedFieldObject.GetTimeUpgradeCost();
        if (EvaluateStat(Energy) < cost) return;

        _resources.AddResource(ResourceType.Energy, -cost);
        SelectedFieldObject.UpgradeTime();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public BigDouble GetSnowmanCost() => UpgradeLookup.GetSnowmanCost(SnowmanCount);
    public bool CanBuySnowman() => EvaluateStat(Ice) >= GetSnowmanCost();

    public void BuySnowman()
    {
        var cost = GetSnowmanCost();
        if (EvaluateStat(Ice) < cost) return;

        _resources.AddResource(ResourceType.Ice, -cost);
        _resources.SnowmanCount++;

        // buying snowman should increase baseValue of output by 1
        Coin.BaseValue += 1;
        Happiness.BaseValue += 1;

        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public BigDouble GetSaplingCost() => UpgradeLookup.GetSaplingCost(SaplingCount);

    public BigDouble GetFieldNodeCost(FieldNodeType type) => UpgradeLookup.GetFieldNodeCost(CountFieldObjects(type));

    public bool CanBuyFieldNode(FieldNodeType type) => EvaluateStat(Energy) >= GetFieldNodeCost(type);

    public bool CanBuySapling() => EvaluateStat(Coin) >= GetSaplingCost();

    public void BuySapling()
    {
        var cost = GetSaplingCost();
        if (EvaluateStat(Coin) < cost) return;

        _resources.AddResource(ResourceType.Coin, -cost);
        _resources.SaplingCount++;
        _resources.AddResource(ResourceType.Sapling, 1);

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

    public void CheatSeeds()
    {
        _resources.AddResource(ResourceType.Sapling, 100);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void CheatWood()
    {
        _resources.AddResource(ResourceType.Wood, 1000);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public bool HasResearch(ResearchType type) => _research.HasResearch(type);

    public bool CanBuyResearch(ResearchType type)
    {
        if (HasResearch(type)) return false;
        var definition = ResearchCatalog.Get(type);
        return GetResource(definition.PriceResource) >= definition.Price;
    }

    public void BuyResearch(ResearchType type)
    {
        if (HasResearch(type)) return;
        var definition = ResearchCatalog.Get(type);
        if (GetResource(definition.PriceResource) < definition.Price) return;

        _resources.AddResource(definition.PriceResource, -definition.Price);
        _research.AddResearch(type);
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetFieldSize(double width, double height)
    {
        _field.SetFieldSize(width, height);
        ApplyBeamStarModifiers();
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

        _starInventory[DraggingStarSlotIndex!.Value].ConsumeStar();
        _starInventory[targetSlotIndex].AddStar();
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

    public bool CanPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide)
        => DraggingStarSlotIndex is not null && BeamStarSlots.Any(slot =>
            slot.SourceFieldNodeId == sourceFieldNodeId &&
            slot.SourceSide == sourceSide &&
            !slot.HasStar);

    public bool TryPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide)
    {
        if (DraggingStarSlotIndex is null)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        if (!_field.TryPlaceStar(sourceFieldNodeId, sourceSide))
            return false;

        inventorySlot.ConsumeStar();
        DraggingStarSlotIndex = null;
        ApplyBeamStarModifiers();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    public bool TryPlaceStarInRecycler(int recyclerFieldNodeId)
    {
        if (DraggingStarSlotIndex is null)
            return false;

        var fieldNode = FieldObjects.FirstOrDefault(n => n.Id == recyclerFieldNodeId);
        if (fieldNode == null || fieldNode.Type != FieldNodeType.Recycler)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        inventorySlot.ConsumeStar();
        RecycledStars.BaseValue += BigDouble.One;
        DraggingStarSlotIndex = null;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    public void BeginFieldObjectDrag(int fieldObjectId) { }
    public void EndFieldObjectDrag(int fieldObjectId) { }
    public void MoveFieldObject(int fieldObjectId, double x, double y)
    {
        _field.MoveFieldObject(fieldObjectId, x, y);
        ApplyBeamStarModifiers();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public void SetTemperature(int temperatureCelsius)
    {
        var clampedTemperature = Math.Clamp(temperatureCelsius, -10, 20);
        var nextTemperature = new BigDouble(clampedTemperature);
        if (Temperature.BaseValue == nextTemperature)
            return;

        Temperature.BaseValue = nextTemperature;
        ApplyTemperatureModifiers();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }

    public int GetSnowTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        return (int)Math.Round(InterpolateLinear(temperature, (-10d, 100d), (10d, -100d)), MidpointRounding.AwayFromZero);
    }

    public int GetWaterTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        return (int)Math.Round(InterpolateLinear(temperature, (-10d, -50d), (5d, 50d), (20d, -50d)), MidpointRounding.AwayFromZero);
    }

    public int GetEnergyTemperatureBonusPercent()
    {
        var temperature = EvaluateStat(Temperature).ToDouble();
        if (temperature <= 0d)
            return 0;

        return (int)Math.Round(InterpolateLinear(temperature, (0d, 0d), (5d, -50d), (10d, 0d), (20d, 100d)), MidpointRounding.AwayFromZero);
    }

    public BigDouble EvaluateStat(Stat stat) => new EvaluationContext().Get(stat);

    private BigDouble GetResource(ResourceType type)
        => _resources.GetResource(type, new EvaluationContext());

    private void ApplyTemperatureModifiers()
    {
        foreach (var fieldObject in FieldObjects)
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
    }

    private void ApplyBeamStarModifiers()
    {
        foreach (var fieldObject in FieldObjects)
        {
            var starCount = BeamStarSlots.Count(slot => slot.TargetFieldNodeId == fieldObject.Id && slot.HasStar);
            fieldObject.SetBeamValueMultiplier(Pow(new BigDouble(1.2d), starCount));
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
        => DraggingStarSlotIndex is not null
           && _passiveStatStarSlots.Any(s => s.Type == type && !s.HasStar);

    public bool TryPlacePassiveStatStar(PassiveStatType type)
    {
        if (DraggingStarSlotIndex is null)
            return false;

        var inventorySlot = _starInventory[DraggingStarSlotIndex.Value];
        if (!inventorySlot.HasStar)
        {
            EndStarDrag();
            return false;
        }

        var slotIndex = _passiveStatStarSlots.FindIndex(s => s.Type == type && !s.HasStar);
        if (slotIndex < 0)
            return false;

        _passiveStatStarSlots[slotIndex] = _passiveStatStarSlots[slotIndex] with { HasStar = true };
        inventorySlot.ConsumeStar();
        DraggingStarSlotIndex = null;
        ApplyPassiveStatStarModifiers();
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
        return true;
    }

    private void ApplyPassiveStatStarModifiers()
    {
        foreach (var type in Enum.GetValues<PassiveStatType>())
        {
            var starCount = _passiveStatStarSlots.Count(s => s.Type == type && s.HasStar);
            var multiplier = Pow(new BigDouble(PassiveStarMultiplierPerStar), starCount);
            _passiveStatStarMultipliers[type] = multiplier;

            // Remove old modifier if it exists
            if (_passiveStatStarModifiers.TryGetValue(type, out var oldModifier))
            {
                GetStatForPassive(type).RemoveModifier(oldModifier);
                _passiveStatStarModifiers.Remove(type);
            }

            if (starCount > 0 && multiplier > BigDouble.One)
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    
    public int StarUpgradeLevel { get; private set; }
    public BigDouble GetStarUpgradeCost()
    {
        var cost = new BigDouble(10);
        for (int i = 0; i < StarUpgradeLevel; i++)
        {
            cost *= 100;
        }
        return cost;
    }
    public bool CanUpgradeStars() => GetResource(ResourceType.ResearchPoint) >= GetStarUpgradeCost();

    public void UpgradeStars()
    {
        var cost = GetStarUpgradeCost();
        if (GetResource(ResourceType.ResearchPoint) < cost) return;

        _resources.AddResource(ResourceType.ResearchPoint, -cost);
        StarUpgradeLevel++;
        Stars.BaseValue += 1;
        Changed?.Invoke();
        RequestUiUpdate?.Invoke();
    }
}
