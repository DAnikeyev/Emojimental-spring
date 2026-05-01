using System.Linq;
using Emojimental.Models;
using Emojimental.Services;

namespace Emojimental.Tests;

public class UnitTest1
{
    [Fact]
    public void StarInventory_InitializesWithEightySlotsAndTenStars()
    {
        var gameState = new GameState();

        Assert.Equal(80, gameState.StarInventory.Count);
        Assert.Equal(10, gameState.StarInventory.Count(slot => slot.HasStar));
        Assert.Equal("✳️x1.2", Assert.Single(gameState.StarModifiers));
    }

    [Fact]
    public void TryPlaceStar_ConsumesInventoryAndBoostsTargetNode()
    {
        var gameState = new GameState();
        var source = gameState.FieldObjects[0];
        var target = gameState.FieldObjects[1];

        gameState.BeginStarDrag(0);

        Assert.True(gameState.CanPlaceStar(source.Id, FieldNodeSide.Right));
        Assert.True(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));

        Assert.False(gameState.StarInventory[0].HasStar);
        Assert.Null(gameState.DraggingStarSlotIndex);
        Assert.True(gameState.BeamStarSlots.Single(slot =>
            slot.SourceFieldNodeId == source.Id &&
            slot.SourceSide == FieldNodeSide.Right).HasStar);
        Assert.Equal(1d, source.EvaluateValue().ToDouble(), 6);
        Assert.Equal(1.2d, target.EvaluateValue().ToDouble(), 6);
    }

    [Fact]
    public void TryPlaceStar_RejectsFilledOrInvalidSlotsWithoutConsumingAnotherStar()
    {
        var gameState = new GameState();
        var source = gameState.FieldObjects[0];

        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));

        gameState.BeginStarDrag(1);

        Assert.False(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));
        Assert.True(gameState.StarInventory[1].HasStar);
        Assert.Equal(9, gameState.StarInventory.Count(slot => slot.HasStar));

        gameState.EndStarDrag();

        Assert.False(gameState.TryPlaceStar(source.Id, FieldNodeSide.Left));
        Assert.True(gameState.StarInventory[1].HasStar);
    }

    [Fact]
    public void MultipleStarredBeams_StackOnTargetIncome()
    {
        var gameState = new GameState();
        gameState.AddFieldObject(FieldNodeType.Energy);

        var leftSource = gameState.FieldObjects[0];
        var target = gameState.FieldObjects[1];
        var rightSource = gameState.FieldObjects[2];

        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(leftSource.Id, FieldNodeSide.Right));

        gameState.BeginStarDrag(1);
        Assert.True(gameState.TryPlaceStar(rightSource.Id, FieldNodeSide.Left));

        Assert.Equal(1.44d, target.EvaluateValue().ToDouble(), 6);
    }

    [Fact]
    public void BeamStarSlots_ExistOnAllSidesEvenWithoutActiveBeams()
    {
        var gameState = new GameState();
        var source = gameState.FieldObjects[0];

        var sourceSlots = gameState.BeamStarSlots.Where(slot => slot.SourceFieldNodeId == source.Id).ToList();

        Assert.Equal(4, sourceSlots.Count);
        Assert.Contains(sourceSlots, slot => slot.SourceSide == FieldNodeSide.Left && slot.TargetFieldNodeId is null);
        Assert.Contains(sourceSlots, slot => slot.SourceSide == FieldNodeSide.Top && slot.TargetFieldNodeId is null);
        Assert.Contains(sourceSlots, slot => slot.SourceSide == FieldNodeSide.Bottom && slot.TargetFieldNodeId is null);
    }

    [Fact]
    public void PlacedStar_RemainsInSlotAfterNodeMovesAwayFromBeam()
    {
        var gameState = new GameState();
        var source = gameState.FieldObjects[0];
        var originalTarget = gameState.FieldObjects[1];

        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));
        Assert.Equal(1.2d, originalTarget.EvaluateValue().ToDouble(), 6);

        gameState.MoveFieldObject(originalTarget.Id, originalTarget.X, originalTarget.Y + (FieldNode.Size * 2));

        var starredSlot = gameState.BeamStarSlots.Single(slot =>
            slot.SourceFieldNodeId == source.Id &&
            slot.SourceSide == FieldNodeSide.Right);

        Assert.True(starredSlot.HasStar);
        Assert.Null(starredSlot.TargetFieldNodeId);
        Assert.Equal(1d, originalTarget.EvaluateValue().ToDouble(), 6);
    }
}
