using BreakInfinity;

namespace Emojimental.GameEngine.Values;

public class Stat : IValueSource
{
    private BigDouble _baseValue;

    private readonly List<ModifierEntry> _modifiers = new();
    private ModifierEntry[]? _cachedSortedModifiers;
    private BigDouble? _cachedValue;
    private string? _cachedDisplay;
    private int _nextInsertionOrder;
    private IReadOnlyList<ModifierStage> _stageOrder =
        new[] { ModifierStage.Add, ModifierStage.Multiply, ModifierStage.Clamp, ModifierStage.Post };

    public BigDouble BaseValue
    {
        get => _baseValue;
        set
        {
            if (_baseValue == value)
                return;

            _baseValue = value;
            InvalidateValue();
        }
    }

    public long Version { get; private set; }

    /// <summary>
    /// Controls how modifier stages are applied. Stages not present here will be applied after all listed stages.
    /// </summary>
    public IReadOnlyList<ModifierStage> StageOrder
    {
        get => _stageOrder;
        set
        {
            _stageOrder = value;
            _cachedSortedModifiers = null;
            InvalidateValue();
        }
    }

    public void AddModifier(IModifier modifier)
    {
        _modifiers.Add(new ModifierEntry(modifier, _nextInsertionOrder++));
        _cachedSortedModifiers = null;
        InvalidateValue();
    }

    public void RemoveModifier(IModifier modifier)
    {
        var removed = _modifiers.RemoveAll(e => e.Modifier == modifier);
        if (removed == 0)
            return;

        _cachedSortedModifiers = null;
        InvalidateValue();
    }

    public void AddAdd(Func<EvaluationContext, BigDouble> value, int priority = 0)
        => AddModifier(new AddModifier(value, priority));

    public void AddMul(Func<EvaluationContext, BigDouble> factor, int priority = 0)
        => AddModifier(new MulModifier(factor, priority));

    public void AddClampMax(Func<EvaluationContext, BigDouble> max, int priority = 0)
        => AddModifier(new ClampMaxModifier(max, priority));

    public void AddClampMin(Func<EvaluationContext, BigDouble> min, int priority = 0)
        => AddModifier(new ClampMinModifier(min, priority));

    public BigDouble Evaluate(EvaluationContext ctx)
    {
        BigDouble value = BaseValue;

        _cachedSortedModifiers ??= _modifiers.OrderBy(GetSortKey).ToArray();

        foreach (var entry in _cachedSortedModifiers)
            value = entry.Modifier.Apply(value, ctx);

        return value;
    }

    public BigDouble EvaluateCached()
    {
        if (_cachedValue.HasValue)
            return _cachedValue.Value;

        _cachedValue = Evaluate(new EvaluationContext());
        return _cachedValue.Value;
    }

    public string DisplayCached()
    {
        if (_cachedDisplay is not null)
            return _cachedDisplay;

        _cachedDisplay = EvaluateCached().Display();
        return _cachedDisplay;
    }

    public void InvalidateValue()
    {
        _cachedValue = null;
        _cachedDisplay = null;
        Version++;
    }

    private ModifierSortKey GetSortKey(ModifierEntry entry)
    {
        var stage = entry.Modifier is IStagedModifier staged
            ? staged.Stage
            : ModifierStage.Post;

        var stageIndex = GetStageIndex(stage);

        var priority = entry.Modifier is IStagedModifier staged2
            ? staged2.Priority
            : 0;

        return new ModifierSortKey(stageIndex, priority, entry.InsertionOrder);
    }

    private int GetStageIndex(ModifierStage stage)
    {
        for (var i = 0; i < StageOrder.Count; i++)
        {
            if (StageOrder[i] == stage)
                return i;
        }

        return int.MaxValue;
    }

    private readonly record struct ModifierEntry(IModifier Modifier, int InsertionOrder);

    private readonly record struct ModifierSortKey(int StageIndex, int Priority, int InsertionOrder) :
        IComparable<ModifierSortKey>
    {
        public int CompareTo(ModifierSortKey other)
        {
            var stageCompare = StageIndex.CompareTo(other.StageIndex);
            if (stageCompare != 0)
                return stageCompare;

            var priorityCompare = Priority.CompareTo(other.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            return InsertionOrder.CompareTo(other.InsertionOrder);
        }
    }

    public BigDouble Value => throw new InvalidOperationException(
        "Use EvaluationContext to resolve values.");
}