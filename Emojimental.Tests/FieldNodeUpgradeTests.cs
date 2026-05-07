using Xunit;
using Emojimental.Models;
using BreakInfinity;

namespace Emojimental.Tests;

public class FieldNodeUpgradeTests
{
    [Fact]
    public void UpgradeItem_UpdatesBaseValue()
    {
        var node = new FieldNode(1, FieldNodeType.Smelter, 0, 0);
        var initialBaseValue = node.OutputValue.BaseValue;
        
        node.UpgradeItem();
        
        var newBaseValue = node.OutputValue.BaseValue;
        Assert.NotEqual(initialBaseValue, newBaseValue);
        Assert.Equal(UpgradeLookup.GetProductivity(FieldNodeType.Smelter, 2), newBaseValue);
    }

    [Fact]
    public void UpgradeTime_UpdatesBaseValue()
    {
        var node = new FieldNode(1, FieldNodeType.Researcher, 0, 0);
        var initialBaseValue = node.CooldownSeconds.BaseValue.ToDouble();
        
        node.UpgradeTime();
        
        var newBaseValue = node.CooldownSeconds.BaseValue.ToDouble();
        Assert.Equal(initialBaseValue - 0.3d, newBaseValue, 5);
    }
    
    [Fact]
    public void UpgradeTime_DustBreaker_UsesMultiplier()
    {
        var node = new FieldNode(1, FieldNodeType.DustBreaker, 0, 0);
        var initialBaseValue = node.CooldownSeconds.BaseValue.ToDouble();
        
        node.UpgradeTime();
        
        var newBaseValue = node.CooldownSeconds.BaseValue.ToDouble();
        Assert.Equal(initialBaseValue * 0.9d, newBaseValue, 5);
    }

    [Fact]
    public void TimeUpgradeCosts_ScaleMoreSteeply()
    {
        var smelterLevelOneCost = UpgradeLookup.GetTimeUpgradeCost(FieldNodeType.Smelter, 1).ToDouble();
        var smelterLevelTwoCost = UpgradeLookup.GetTimeUpgradeCost(FieldNodeType.Smelter, 2).ToDouble();
        var recyclerLevelOneCost = UpgradeLookup.GetTimeUpgradeCost(FieldNodeType.Recycler, 1).ToDouble();
        var recyclerLevelTwoCost = UpgradeLookup.GetTimeUpgradeCost(FieldNodeType.Recycler, 2).ToDouble();

        Assert.Equal(1.7d, smelterLevelTwoCost / smelterLevelOneCost, 5);
        Assert.Equal(1.9d, recyclerLevelTwoCost / recyclerLevelOneCost, 5);
    }

    [Fact]
    public void BaseCooldownDisplay_ClampsAtMinimum()
    {
        var node = new FieldNode(1, FieldNodeType.Snow, 0, 0);
        node.CooldownSeconds.BaseValue = 0.09d;

        Assert.Equal("0.1", node.BaseCooldownDisplay());
    }

    [Fact]
    public void UpgradeTime_CooldownAtMinimum_DoesNotUpgrade()
    {
        var node = new FieldNode(1, FieldNodeType.Snow, 0, 0);
        node.CooldownSeconds.BaseValue = 0.1d;

        node.UpgradeTime();

        Assert.False(node.CanUpgradeTime);
        Assert.Equal(1, node.TimeLevel);
        Assert.Equal(0.1d, node.CooldownSeconds.BaseValue.ToDouble(), 5);
    }

    [Fact]
    public void UpgradeTime_CooldownBelowNextReduction_ClampsToMinimum()
    {
        var node = new FieldNode(1, FieldNodeType.Snow, 0, 0);
        node.CooldownSeconds.BaseValue = 0.11d;

        node.UpgradeTime();

        Assert.False(node.CanUpgradeTime);
        Assert.Equal(0.1d, node.CooldownSeconds.BaseValue.ToDouble(), 5);
    }
}
