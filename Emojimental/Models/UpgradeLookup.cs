using BreakInfinity;

namespace Emojimental.Models;

public static class UpgradeLookup
{
    private const int MaxLevel = 1000;
    private static readonly Dictionary<int, BigDouble> _itemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _timeUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _snowmanCosts = new();
    private static readonly Dictionary<int, BigDouble> _saplingCosts = new();
    private static readonly Dictionary<int, BigDouble> _fieldNodeCosts = new();

    static UpgradeLookup()
    {
        for (int i = 1; i <= MaxLevel; i++)
        {
            // Upgrade cost for items = x^(2.1) for level x, rounded down.
            _itemUpgradeCosts[i] = new BigDouble(Math.Floor(Math.Pow(i, 2.1)));

            // cost for level x is 2^x for time
            _timeUpgradeCosts[i] = Pow2(i);

            // Snowman cost: 10 * 1.5^(i-1)
            _snowmanCosts[i] = new BigDouble(10 * Math.Pow(1.5, i - 1));

            // Sapling cost: 100 * 2^(i-1)
            _saplingCosts[i] = new BigDouble(100 * Math.Pow(2, i - 1));

            // Field node cost: 10 * 2^(i-1)
            _fieldNodeCosts[i] = new BigDouble(10 * Math.Pow(2, i - 1));
        }
    }

    public static BigDouble GetItemUpgradeCost(int level)
    {
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _itemUpgradeCosts[MaxLevel];
        return _itemUpgradeCosts[level];
    }

    public static BigDouble GetTimeUpgradeCost(int level)
    {
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _timeUpgradeCosts[MaxLevel];
        return _timeUpgradeCosts[level];
    }

    public static BigDouble GetSnowmanCost(int count)
    {
        var level = count + 1;
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _snowmanCosts[MaxLevel];
        return _snowmanCosts[level];
    }

    public static BigDouble GetSaplingCost(int count)
    {
        var level = count + 1;
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _saplingCosts[MaxLevel];
        return _saplingCosts[level];
    }

    public static BigDouble GetFieldNodeCost(int count)
    {
        var level = count + 1;
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _fieldNodeCosts[MaxLevel];
        return _fieldNodeCosts[level];
    }

    public static BigDouble GetSnowmanCoinBonus(int count)
    {
        return new BigDouble(count);
    }

    public static BigDouble GetSnowmanHappinessBonus(int count)
    {
        return new BigDouble(count);
    }

    private static BigDouble Pow2(int exponent)
    {
        if (exponent <= 0) return BigDouble.One;
        const double log10Of2 = 0.3010299956639812d;
        var base10Exponent = exponent * log10Of2;
        var wholeExponent = (int)Math.Floor(base10Exponent);
        var mantissa = Math.Pow(10d, base10Exponent - wholeExponent);
        return new BigDouble(mantissa, wholeExponent);
    }
}
