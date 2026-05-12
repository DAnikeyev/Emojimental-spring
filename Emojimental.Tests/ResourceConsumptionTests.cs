using BreakInfinity;
using Emojimental.Models;
using Emojimental.Services;

namespace Emojimental.Tests;

public class ResourceConsumptionTests
{
    [Fact]
    public void Advance_WoodcutterWithOnlyOneAffordableCycle_DoesNotConsumePastZero()
    {
        var gameState = new GameState();
        var field = GetFieldState(gameState);
        field.AddFieldObject(FieldNodeType.Woodcutter);
        var woodcutter = gameState.FieldObjects.Single(node => node.Type == FieldNodeType.Woodcutter);
        woodcutter.CooldownSeconds.BaseValue = 1d;

        gameState.Tree.BaseValue = woodcutter.OutputValue.BaseValue;

        gameState.Advance(2d);

        Assert.Equal(0d, gameState.Tree.BaseValue.ToDouble(), 6);
        Assert.Equal((woodcutter.EvaluateValue() * 10).ToDouble(), gameState.Wood.BaseValue.ToDouble(), 6);
    }

    [Fact]
    public void Advance_WoodcuttersShareRemainingTreesAcrossSameTick()
    {
        var gameState = new GameState();
        var field = GetFieldState(gameState);
        field.AddFieldObject(FieldNodeType.Woodcutter);
        field.AddFieldObject(FieldNodeType.Woodcutter);
        var woodcutters = gameState.FieldObjects.Where(node => node.Type == FieldNodeType.Woodcutter).ToList();
        foreach (var woodcutter in woodcutters)
            woodcutter.CooldownSeconds.BaseValue = 1d;

        gameState.Tree.BaseValue = woodcutters[0].OutputValue.BaseValue;

        gameState.Advance(1d);

        Assert.Equal(0d, gameState.Tree.BaseValue.ToDouble(), 6);
        Assert.Equal((woodcutters[0].EvaluateValue() * 10).ToDouble(), gameState.Wood.BaseValue.ToDouble(), 6);
    }

    [Fact]
    public void AddResource_ClampsAtZero()
    {
        var resources = new ResourceState();

        resources.AddResource(ResourceType.Tree, new BigDouble(5));
        resources.AddResource(ResourceType.Tree, new BigDouble(-10));

        Assert.Equal(0d, resources.Tree.BaseValue.ToDouble(), 6);
    }

    private static FieldState GetFieldState(GameState gameState)
        => (FieldState)typeof(GameState)
            .GetField("_field", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(gameState)!;
}

