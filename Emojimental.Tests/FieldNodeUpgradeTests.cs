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
}
