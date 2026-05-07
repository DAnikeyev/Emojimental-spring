using BreakInfinity;

namespace Emojimental.Models;

public static class UpgradeLookup
{
    private const int MaxLevel = 100;
    private static readonly Dictionary<int, BigDouble> _itemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _timeUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _snowmanCosts = new();
    private static readonly Dictionary<int, BigDouble> _saplingCosts = new();
    private static readonly Dictionary<int, BigDouble> _fieldNodeCosts = new();
    private static readonly Dictionary<int, BigDouble> _temperatureMaxCosts = new();
    private static readonly Dictionary<int, BigDouble> _starUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _energyProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _energyItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _energyTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _snowProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _snowItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _snowTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _researcherProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _researcherItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _researcherTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _smelterProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _smelterItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _smelterTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _dustBreakerProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _dustBreakerItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _dustBreakerTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _farmProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _farmItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _farmTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _woodcutterProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _woodcutterItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _woodcutterTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _builderProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _builderItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _builderTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _gardenProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _gardenItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _gardenTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _bonfireProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _bonfireItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _bonfireTimeUpgradeCosts = new();

    private static readonly Dictionary<int, BigDouble> _recyclerProductivityValues = new();
    private static readonly Dictionary<int, BigDouble> _recyclerItemUpgradeCosts = new();
    private static readonly Dictionary<int, BigDouble> _recyclerTimeUpgradeCosts = new();

    static UpgradeLookup()
    {
        BigDouble energyProductivity = new BigDouble(10);
        BigDouble energyItemCost = new BigDouble(2000);
        BigDouble energyTimeCost = BigDouble.One;

        BigDouble snowProductivity = BigDouble.One;
        BigDouble snowItemCost = new BigDouble(20);
        BigDouble snowTimeCost = BigDouble.One;

        BigDouble resProductivity = BigDouble.One;
        BigDouble resItemCost = new BigDouble(100);
        BigDouble resTimeCost = new BigDouble(2);

        BigDouble smelterProductivity = BigDouble.One;
        BigDouble smelterItemCost = new BigDouble(500);
        BigDouble smelterTimeCost = new BigDouble(2);

        BigDouble dustBreakerProductivity = BigDouble.One;
        BigDouble dustBreakerItemCost = new BigDouble(20000);
        BigDouble dustBreakerTimeCost = new BigDouble(5);

        BigDouble farmProductivity = BigDouble.One;
        BigDouble farmItemCost = new BigDouble(250000); // Lvl 1 upgrade cost
        BigDouble farmTimeCost = new BigDouble(10);

        BigDouble woodcutterProductivity = new BigDouble(10);
        BigDouble woodcutterItemCost = new BigDouble(500000); // Lvl 1 upgrade cost
        BigDouble woodcutterTimeCost = new BigDouble(50);

        BigDouble builderProductivity = BigDouble.One;
        BigDouble builderItemCost = new BigDouble(1000000);
        BigDouble builderTimeCost = new BigDouble(100);

        BigDouble gardenProductivity = BigDouble.One;
        BigDouble gardenItemCost = new BigDouble(50000);
        BigDouble gardenTimeCost = new BigDouble(20);

        BigDouble bonfireProductivity = BigDouble.One;
        BigDouble bonfireItemCost = new BigDouble(10000);
        BigDouble bonfireTimeCost = new BigDouble(500);

        BigDouble recyclerProductivity = BigDouble.One;
        BigDouble recyclerItemCost = new BigDouble(500000); // Lvl 1 upgrade cost
        BigDouble recyclerTimeCost = new BigDouble(10);

        BigDouble temperatureMaxCost = new BigDouble(1000);
        BigDouble starUpgradeCost = new BigDouble(10);

        for (int i = 1; i <= MaxLevel; i++)
        {
            // Snowman cost: 10 * 1.5^(i-1)
            _snowmanCosts[i] = new BigDouble(10 * Math.Pow(1.45, i - 1));

            // Sapling cost: 10000 * 2^(i-1) (100x more expensive)
            _saplingCosts[i] = new BigDouble(10000 * Math.Pow(1.8, i - 1));

            // Temperature cost: lvl1=1000, f(n) = f(n-1)*1.9
            _temperatureMaxCosts[i] = temperatureMaxCost;
            temperatureMaxCost = Floor(temperatureMaxCost * (1.9+Math.Max(i-20, 0)*0.1));

            // Stars cost: lvl1=10, f(n) = f(n-1)*100
            _starUpgradeCosts[i] = starUpgradeCost;
            starUpgradeCost = Floor(starUpgradeCost * 100);

            // Energy (Solar Panel)
            _energyProductivityValues[i] = energyProductivity;
            energyProductivity = Floor((energyProductivity + 1) * 1.2);
            _energyItemUpgradeCosts[i] = energyItemCost;
            energyItemCost = Floor(energyItemCost * Math.Pow(1.3, 1 + 0.05 * (i + 1)));
            _energyTimeUpgradeCosts[i] = energyTimeCost;
            energyTimeCost *= 1.7;

            // Snow (Shovel)
            _snowProductivityValues[i] = snowProductivity;
            snowProductivity = Floor((snowProductivity + 1) * 1.2);
            _snowItemUpgradeCosts[i] = snowItemCost;
            snowItemCost = Floor(snowItemCost * Math.Pow(1.3, 1 + 0.05 * (i + 1)));
            _snowTimeUpgradeCosts[i] = snowTimeCost;
            snowTimeCost *= 1.7;

            // Researcher Productivity: f(n+1)=FLOOR((f(n)+1)*1.3)
            _researcherProductivityValues[i] = resProductivity;
            resProductivity = Floor((resProductivity + 1) * 1.3);

            // Smelter Productivity: f(n)=FLOOR((f(n-1)+1)*1.2) starting from 1
            _smelterProductivityValues[i] = smelterProductivity;
            smelterProductivity = Floor((smelterProductivity + 1) * 1.2);

            // Dust Breaker Productivity: f(n)=f(n-1)*1.2 starting from 1
            _dustBreakerProductivityValues[i] = dustBreakerProductivity;
            dustBreakerProductivity *= 1.2;

            // Researcher Value Upgrade cost (Gold): f(n+1)=f(n)(3*1.9^(1+0.1*n))
            _researcherItemUpgradeCosts[i] = resItemCost;
            resItemCost = Floor(resItemCost * (3 * Math.Pow(1.3, 1 + 0.1 * (i - 1))));

            // Smelter Value Upgrade cost (Gold): lvl1=500, f(n+1)=FLOOR(f(n)(1.7^(1+0.1(n+1))))
            _smelterItemUpgradeCosts[i] = smelterItemCost;
            smelterItemCost = Floor(smelterItemCost * Math.Pow(1.3, 1 + 0.05 * (i + 1)));

            // Dust Breaker Value Upgrade cost (Gold): lvl1=20000, f(n+1)=FLOOR(f(n)(1.9^(1+0.1(n+1))))
            _dustBreakerItemUpgradeCosts[i] = dustBreakerItemCost;
            dustBreakerItemCost = Floor(dustBreakerItemCost * Math.Pow(1.4, 1 + 0.05 * (i + 1)));

            // Researcher Cooldown Upgrade cost (Dust): 2 * 1.9^n
            _researcherTimeUpgradeCosts[i] = 2 * Math.Pow(1.9, i);

            // Smelter Cooldown Upgrade cost (Stardust): lvl1=2, f(n) = f(n-1)*1.7
            _smelterTimeUpgradeCosts[i] = smelterTimeCost;
            smelterTimeCost *= 1.7;

            // Dust Breaker Cooldown Upgrade cost (Stardust): lvl1=5, f(n) = f(n-1)*1.9
            _dustBreakerTimeUpgradeCosts[i] = dustBreakerTimeCost;
            dustBreakerTimeCost *= 1.9;

            // Farm Productivity: f(n)=f(n-1)*1.1 starting from 1
            _farmProductivityValues[i] = farmProductivity;
            farmProductivity *= 1.1;

            // Farm Value Upgrade cost (Gold): lvl1=250000, f(n+1)=FLOOR(f(n)(1.9^(1+0.1(n+1))))
            _farmItemUpgradeCosts[i] = farmItemCost;
            farmItemCost = Floor(farmItemCost * Math.Pow(1.4, 1 + 0.05 * (i + 1)));

            // Farm Cooldown Upgrade cost (Stardust): lvl1=10, f(n) = f(n-1)*1.9
            _farmTimeUpgradeCosts[i] = farmTimeCost;
            farmTimeCost *= 1.9;

            // Woodcutter Productivity: f(n)=f(n-1)*1.2 starting from 10
            _woodcutterProductivityValues[i] = woodcutterProductivity;
            woodcutterProductivity *= 1.2;

            // Woodcutter Value Upgrade cost (Gold): lvl1=500000, f(n+1)=FLOOR(f(n)*(1.3))
            _woodcutterItemUpgradeCosts[i] = woodcutterItemCost;
            woodcutterItemCost = Floor(woodcutterItemCost * 1.3);

            // Woodcutter Cooldown Upgrade cost (Stardust): lvl1=50, f(n) = f(n-1)*1.7
            _woodcutterTimeUpgradeCosts[i] = woodcutterTimeCost;
            woodcutterTimeCost *= 1.7;

            // Builder Productivity: f(n)=f(n-1)*1.2 starting from 1
            _builderProductivityValues[i] = builderProductivity;
            builderProductivity *= 1.2;

            // Builder Value Upgrade cost (Gold): lvl1=1000000, f(n+1)=FLOOR(f(n)*2.1^(1+0.1*(n+1)))
            _builderItemUpgradeCosts[i] = builderItemCost;
            builderItemCost = Floor(builderItemCost * Math.Pow(1.5, 1 + 0.1 * (i + 1)));

            // Builder Cooldown Upgrade cost (Stardust): lvl1=100, f(n) = f(n-1)*1.6
            _builderTimeUpgradeCosts[i] = builderTimeCost;
            builderTimeCost *= 1.6;

            // Garden
            _gardenProductivityValues[i] = gardenProductivity;
            gardenProductivity *= 1.2;
            _gardenItemUpgradeCosts[i] = gardenItemCost;
            gardenItemCost = Floor(gardenItemCost * Math.Pow(1.3, 1 + 0.05 * (i + 1)));
            _gardenTimeUpgradeCosts[i] = gardenTimeCost;
            gardenTimeCost *= 1.8;

            // Bonfire
            _bonfireProductivityValues[i] = bonfireProductivity;
            bonfireProductivity *= 1.25;
            _bonfireItemUpgradeCosts[i] = bonfireItemCost;
            bonfireItemCost = Floor(bonfireItemCost * 1.4);
            _bonfireTimeUpgradeCosts[i] = bonfireTimeCost;
            bonfireTimeCost = Floor(bonfireTimeCost * 1.8);

            // Recycler
            _recyclerProductivityValues[i] = recyclerProductivity;
            recyclerProductivity *= 1.15;
            _recyclerItemUpgradeCosts[i] = recyclerItemCost;
            recyclerItemCost = Floor(recyclerItemCost * Math.Pow(2.0, 1 + 0.1 * (i + 1)));
            _recyclerTimeUpgradeCosts[i] = recyclerTimeCost;
            recyclerTimeCost *= 1.9;

            // Upgrade cost for items = x^(2.1) for level x, rounded down.
            _itemUpgradeCosts[i] = new BigDouble(Math.Floor(Math.Pow(i, 2.1)));

            // cost for level x is 2^x for time
            _timeUpgradeCosts[i] = Pow2(i);

            // Field node cost: 10000 * 1.7^(i-1)
            _fieldNodeCosts[i] = new BigDouble(10000 * Math.Pow(1.7, i - 1));
        }
    }

    public static BigDouble GetItemUpgradeCost(FieldNodeType type, int level)
    {
        type = type.Normalize();
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) level = MaxLevel;

        return type switch
        {
            FieldNodeType.Energy => _energyItemUpgradeCosts[level],
            FieldNodeType.Snow => _snowItemUpgradeCosts[level],
            FieldNodeType.Researcher => _researcherItemUpgradeCosts[level],
            FieldNodeType.Smelter => _smelterItemUpgradeCosts[level],
            FieldNodeType.DustBreaker => _dustBreakerItemUpgradeCosts[level],
            FieldNodeType.Farm => _farmItemUpgradeCosts[level],
            FieldNodeType.Woodcutter => _woodcutterItemUpgradeCosts[level],
            FieldNodeType.Builder => _builderItemUpgradeCosts[level],
            FieldNodeType.Bonfire => _bonfireItemUpgradeCosts[level],
            FieldNodeType.Recycler => _recyclerItemUpgradeCosts[level],
            _ => GetItemUpgradeCost(level)
        };
    }

    public static BigDouble GetTimeUpgradeCost(FieldNodeType type, int level)
    {
        type = type.Normalize();
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) level = MaxLevel;

        return type switch
        {
            FieldNodeType.Energy => _energyTimeUpgradeCosts[level],
            FieldNodeType.Snow => _snowTimeUpgradeCosts[level],
            FieldNodeType.Researcher => _researcherTimeUpgradeCosts[level],
            FieldNodeType.Smelter => _smelterTimeUpgradeCosts[level],
            FieldNodeType.DustBreaker => _dustBreakerTimeUpgradeCosts[level],
            FieldNodeType.Farm => _farmTimeUpgradeCosts[level],
            FieldNodeType.Woodcutter => _woodcutterTimeUpgradeCosts[level],
            FieldNodeType.Builder => _builderTimeUpgradeCosts[level],
            FieldNodeType.Bonfire => _bonfireTimeUpgradeCosts[level],
            FieldNodeType.Recycler => _recyclerTimeUpgradeCosts[level],
            _ => GetTimeUpgradeCost(level)
        };
    }

    public static BigDouble GetProductivity(FieldNodeType type, int level)
    {
        type = type.Normalize();
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) level = MaxLevel;

        return type switch
        {
            FieldNodeType.Energy => _energyProductivityValues[level],
            FieldNodeType.Snow => _snowProductivityValues[level],
            FieldNodeType.Researcher => _researcherProductivityValues[level],
            FieldNodeType.Smelter => _smelterProductivityValues[level],
            FieldNodeType.DustBreaker => _dustBreakerProductivityValues[level],
            FieldNodeType.Farm => _farmProductivityValues[level],
            FieldNodeType.Woodcutter => _woodcutterProductivityValues[level],
            FieldNodeType.Builder => _builderProductivityValues[level],
            FieldNodeType.Bonfire => _bonfireProductivityValues[level],
            FieldNodeType.Recycler => _recyclerProductivityValues[level],
            _ => new BigDouble(level)
        };
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

    public static BigDouble GetTemperatureMaxCost(int count)
    {
        var level = count + 1;
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _temperatureMaxCosts[MaxLevel];
        return _temperatureMaxCosts[level];
    }

    public static BigDouble GetStarUpgradeCost(int count)
    {
        var level = count + 1;
        if (level < 1) return BigDouble.Zero;
        if (level > MaxLevel) return _starUpgradeCosts[MaxLevel];
        return _starUpgradeCosts[level];
    }

    public static BigDouble GetFieldNodeCost(FieldNodeType type, int count)
    {
        if (type == FieldNodeType.Energy)
        {
            return count switch
            {
                0 => 2500,
                1 => 1000000,
                2 => 2E12,
                _ => 2E12
            };
        }
        if (type == FieldNodeType.Snow)
        {
            return count switch
            {
                0 => 5,
                1 => 5000,
                2 => 10E9,
                _ => 10E9
            };
        }
        if (type == FieldNodeType.Researcher)
        {
            return count switch
            {
                0 => 100,
                _ => BigDouble.Zero // Max 1 will be handled in GameState
            };
        }
        if (type == FieldNodeType.Smelter)
        {
            return count switch
            {
                0 => 5000,
                1 => 50000000,
                _ => BigDouble.Zero // Max 2 will be handled in GameState
            };
        }
        if (type == FieldNodeType.Woodcutter)
        {
            return count switch
            {
                0 => 1000000,
                _ => BigDouble.Zero // Max 1 will be handled in GameState
            };
        }
        if (type == FieldNodeType.Farm)
        {
            return count switch
            {
                0 => 100000,
                1 => 50000000,
                _ => BigDouble.Zero
            };
        }
        if (type == FieldNodeType.DustBreaker)
        {
            return count switch
            {
                0 => 10000,
                1 => 1000000,
                _ => BigDouble.Zero
            };
        }
        if (type == FieldNodeType.Builder)
        {
            return count switch
            {
                0 => 500000,
                1 => 33000000,
                _ => BigDouble.Zero
            };
        }
        if (type == FieldNodeType.Recycler)
        {
            return count switch
            {
                0 => 500000,
                1 => 100000000,
                _ => BigDouble.Zero
            };
        }
        if (type == FieldNodeType.Bonfire)
        {
            return count switch
            {
                0 => 10000000,
                _ => BigDouble.Zero
            };
        }
        return GetFieldNodeCost(count);
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
        if (count <= 0) return BigDouble.Zero;
        return 7 * new BigDouble(Math.Pow(1.33, count - 1)) - 6;
    }

    public static BigDouble GetSnowmanCarrotBonus(int count)
    {
        if (count <= 0) return BigDouble.Zero;
        return 7 * new BigDouble(Math.Pow(1.33, count - 1)) - 6;
    }

    private static BigDouble Floor(BigDouble value)
    {
        return BreakInfinity.BigDouble.Floor(value);
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
