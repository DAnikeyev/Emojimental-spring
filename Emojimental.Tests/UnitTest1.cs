using System.Linq;
using Emojimental.Models;
using Emojimental.Services;

namespace Emojimental.Tests;

public class UnitTest1
{
    [Fact]
    public void ResearchCatalog_RemadeUnlocksUseRequestedOrderAndPrices()
    {
        var researches = ResearchCatalog.All;

        Assert.Equal(
            [
                ResearchType.UnlockStars,
                ResearchType.UnlockStarSlots,
                ResearchType.UnlockTemperatureBar,
                ResearchType.UnlockEnergy,
                ResearchType.UnlockSmelter,
                ResearchType.UnlockSaplings,
                ResearchType.UnlockHappiness,
                ResearchType.UnlockTreeGrow,
                ResearchType.UnlockTemperatureExchange,
                ResearchType.UnlockStarRecycler,
                ResearchType.UnlockWoodcutter,
                ResearchType.UnlockStarImprover,
                ResearchType.UnlockBuilder,
                ResearchType.UnlockFlowers,
                ResearchType.UnlockPassiveGemSlots,
                ResearchType.UnlockBurner
            ],
            researches.Select(research => research.Type));

        Assert.Equal(10d, researches[0].Price.ToDouble(), 6);
        Assert.Equal(20d, researches[1].Price.ToDouble(), 6);
        Assert.Equal(50d, researches[2].Price.ToDouble(), 6);
        Assert.Equal(100d, researches[3].Price.ToDouble(), 6);
        Assert.Equal(200d, researches[4].Price.ToDouble(), 6);
        Assert.Equal(400d, researches[5].Price.ToDouble(), 6);
        Assert.Equal(600d, researches[6].Price.ToDouble(), 6);
        Assert.Equal(800d, researches[7].Price.ToDouble(), 6);
        Assert.Equal(1000d, researches[8].Price.ToDouble(), 6);
        Assert.Equal(5000d, researches[9].Price.ToDouble(), 6);
        Assert.Equal(10000d, researches[10].Price.ToDouble(), 6);
        Assert.Equal(15000d, researches[11].Price.ToDouble(), 6);
        Assert.Equal(20000d, researches[12].Price.ToDouble(), 6);
        Assert.Equal(50000d, researches[13].Price.ToDouble(), 6);
        Assert.Equal(100000d, researches[14].Price.ToDouble(), 6);
        Assert.Equal(1000000d, researches[15].Price.ToDouble(), 6);
    }

    [Fact]
    public void TemperatureBarResearch_UnlocksTemperatureControlFromFrozenStart()
    {
        var gameState = new GameState();

        Assert.Equal(-5, gameState.TemperatureCelsius);
        Assert.False(gameState.HasResearch(ResearchType.UnlockTemperatureBar));
        Assert.False(gameState.CanAdjustTemperature());

        gameState.SetTemperature(3);

        Assert.Equal(-5, gameState.TemperatureCelsius);

        gameState.ResearchPoint.BaseValue = 100;
        gameState.BuyResearch(ResearchType.UnlockTemperatureBar);

        Assert.True(gameState.CanAdjustTemperature());
        Assert.Equal(-5, gameState.MaxTemperatureCelsius);

        gameState.SetTemperature(3);

        Assert.Equal(-5, gameState.TemperatureCelsius);
    }

    [Fact]
    public void ResearchUnlocks_GateResourcesFactoriesAndPassiveStats()
    {
        var gameState = new GameState();
        gameState.ResearchPoint.BaseValue = 2_000_000;

        Assert.False(gameState.IsResourceUnlocked(ResourceType.Energy));
        Assert.False(gameState.IsResourceUnlocked(ResourceType.Water));
        Assert.False(gameState.IsPassiveStatUnlocked(PassiveStatType.Stars));
        Assert.False(gameState.AreFactoryStarSlotsUnlocked());
        Assert.False(gameState.IsPassiveStatStarSlotsUnlocked());
        Assert.False(gameState.IsPassiveStatUnlocked(PassiveStatType.RecycledStars));
        Assert.False(gameState.IsPassiveStatUnlocked(PassiveStatType.Markets));
        Assert.False(gameState.IsPassiveStatUnlocked(PassiveStatType.Houses));
        Assert.False(gameState.IsResourceUnlocked(ResourceType.Flower));

        gameState.BuyResearch(ResearchType.UnlockEnergy);
        gameState.BuyResearch(ResearchType.UnlockSmelter);
        gameState.BuyResearch(ResearchType.UnlockStars);
        gameState.BuyResearch(ResearchType.UnlockStarSlots);
        gameState.BuyResearch(ResearchType.UnlockTreeGrow);
        gameState.BuyResearch(ResearchType.UnlockStarRecycler);
        gameState.BuyResearch(ResearchType.UnlockWoodcutter);
        gameState.BuyResearch(ResearchType.UnlockStarImprover);
        gameState.BuyResearch(ResearchType.UnlockBuilder);
        gameState.BuyResearch(ResearchType.UnlockFlowers);
        gameState.BuyResearch(ResearchType.UnlockPassiveGemSlots);
        gameState.BuyResearch(ResearchType.UnlockBurner);

        Assert.True(gameState.IsResourceUnlocked(ResourceType.Energy));
        Assert.True(gameState.IsResourceUnlocked(ResourceType.Water));
        Assert.True(gameState.IsResourceUnlocked(ResourceType.Tree));
        Assert.True(gameState.IsResourceUnlocked(ResourceType.StarDust));
        Assert.True(gameState.IsResourceUnlocked(ResourceType.Wood));
        Assert.True(gameState.IsResourceUnlocked(ResourceType.Flower));
        Assert.True(gameState.IsPassiveStatUnlocked(PassiveStatType.Stars));
        Assert.True(gameState.AreFactoryStarSlotsUnlocked());
        Assert.True(gameState.IsPassiveStatStarSlotsUnlocked());
        Assert.True(gameState.IsPassiveStatUnlocked(PassiveStatType.RecycledStars));
        Assert.True(gameState.IsPassiveStatUnlocked(PassiveStatType.Markets));
        Assert.True(gameState.IsPassiveStatUnlocked(PassiveStatType.Houses));
        Assert.Equal(1d, gameState.Stars.BaseValue.ToDouble(), 6);
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Energy));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Smelter));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Farm));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Recycler));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Woodcutter));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Builder));
        Assert.True(gameState.IsFactoryUnlocked(FieldNodeType.Bonfire));
    }

    [Fact]
    public void ExchangeUnlocks_UseRequestedCostsAndRewards()
    {
        var gameState = new GameState();
        gameState.ResearchPoint.BaseValue = 1_000_000;

        gameState.Ice.BaseValue = 10;
        gameState.BuySnowman();
        Assert.Equal(0d, gameState.Ice.BaseValue.ToDouble(), 6);
        Assert.Equal(1, gameState.SnowmanCount);
        Assert.Equal(1d, gameState.SnowmanCoinIncomePerSecond.ToDouble(), 6);
        gameState.BuyResearch(ResearchType.UnlockHappiness);
        Assert.Equal(1d, gameState.SnowmanCarrotIncomePerSecond.ToDouble(), 6);

        Assert.False(gameState.CanBuySapling());
        gameState.BuyResearch(ResearchType.UnlockSaplings);
        gameState.Coin.BaseValue = 100;
        Assert.True(gameState.CanBuySapling());

        gameState.BuySapling();

        Assert.Equal(0d, gameState.Coin.BaseValue.ToDouble(), 6);
        Assert.Equal(1d, gameState.Sapling.BaseValue.ToDouble(), 6);

        Assert.False(gameState.CanBuyTemperatureMax());
        gameState.BuyResearch(ResearchType.UnlockTemperatureBar);
        gameState.BuyResearch(ResearchType.UnlockTemperatureExchange);
        gameState.Energy.BaseValue = 1000;
        Assert.True(gameState.CanBuyTemperatureMax());

        gameState.BuyTemperatureMax();

        Assert.Equal(0d, gameState.Energy.BaseValue.ToDouble(), 6);
        Assert.Equal(-4, gameState.MaxTemperatureCelsius);

        Assert.False(gameState.CanUpgradeStars());
        gameState.BuyResearch(ResearchType.UnlockStars);
        gameState.BuyResearch(ResearchType.UnlockStarRecycler);
        gameState.BuyResearch(ResearchType.UnlockWoodcutter);
        gameState.BuyResearch(ResearchType.UnlockStarImprover);
        gameState.StarDust.BaseValue = 10;
        gameState.Wood.BaseValue = 10;
        Assert.True(gameState.CanUpgradeStars());

        gameState.UpgradeStars();

        Assert.Equal(0d, gameState.StarDust.BaseValue.ToDouble(), 6);
        Assert.Equal(0d, gameState.Wood.BaseValue.ToDouble(), 6);
        Assert.Equal(1, gameState.StarUpgradeLevel);
        Assert.Equal(2d, gameState.Stars.BaseValue.ToDouble(), 6);
    }

    [Fact]
    public void StarInventory_InitializesWithEightySlotsAndZeroStars()
    {
        var gameState = new GameState();

        Assert.Equal(80, gameState.StarInventory.Count);
        Assert.Equal(0, gameState.StarInventory.Count(slot => slot.HasStar));
    }

    [Fact]
    public void TryPlaceStar_ConsumesInventoryAndBoostsTargetNode()
    {
        var gameState = new GameState();
        gameState.ResearchPoint.BaseValue = 100;
        gameState.BuyResearch(ResearchType.UnlockStars);
        gameState.BuyResearch(ResearchType.UnlockStarSlots);
        gameState.AddInitialNodes();
        // Manually add nodes via FieldState to bypass GameState cost/limit checks
        var field = (FieldState)typeof(GameState).GetField("_field", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(gameState);
        field.AddFieldObject(FieldNodeType.Snow);
        
        var source = gameState.FieldObjects.First();
        var target = gameState.FieldObjects.Last();

        // Align them precisely on the same Y coordinate
        gameState.MoveFieldObject(source.Id, 0, 10);
        gameState.MoveFieldObject(target.Id, 250, 10);
        gameState.RefreshConnections();

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
        gameState.ResearchPoint.BaseValue = 100;
        gameState.BuyResearch(ResearchType.UnlockStars);
        // Add another star manually for testing
        gameState.CheatResources(); // Might need stars
        gameState.Stars.BaseValue = 10;
        gameState.AddStarToInventory();
        
        gameState.AddInitialNodes();
        var source = gameState.FieldObjects[0];

        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));

        gameState.BeginStarDrag(1);

        Assert.False(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));
        Assert.True(gameState.StarInventory[1].HasStar);
        Assert.Equal(1, gameState.StarInventory.Count(slot => slot.HasStar));

        gameState.EndStarDrag();

        Assert.False(gameState.TryPlaceStar(source.Id, FieldNodeSide.Left));
        Assert.True(gameState.StarInventory[1].HasStar);
    }

    [Fact]
    public void MultipleStarredBeams_StackOnTargetIncome()
    {
        var gameState = new GameState();
        gameState.ResearchPoint.BaseValue = 100;
        gameState.BuyResearch(ResearchType.UnlockStars);
        gameState.BuyResearch(ResearchType.UnlockStarSlots);
        // Add another star
        gameState.Stars.BaseValue = 10;
        gameState.AddStarToInventory();

        gameState.AddInitialNodes();
        var field = (FieldState)typeof(GameState).GetField("_field", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(gameState);
        field.AddFieldObject(FieldNodeType.Snow);
        field.AddFieldObject(FieldNodeType.Snow);

        var leftSource = gameState.FieldObjects.First();
        var target = gameState.FieldObjects.Skip(1).First();
        var rightSource = gameState.FieldObjects.Last();

        // Move them into alignment: leftSource (0,10), target (250,10), rightSource (500,10)
        gameState.MoveFieldObject(leftSource.Id, 0, 10);
        gameState.MoveFieldObject(target.Id, 250, 10);
        gameState.MoveFieldObject(rightSource.Id, 500, 10);
        gameState.RefreshConnections();
        
        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(leftSource.Id, FieldNodeSide.Right));

        gameState.BeginStarDrag(1);
        Assert.True(gameState.TryPlaceStar(rightSource.Id, FieldNodeSide.Left));

        // Two stars = 1.2 * 1.2 = 1.44
        Assert.Equal(1.44d, target.EvaluateValue().ToDouble(), 6);
    }

    [Fact]
    public void BeamStarSlots_ExistOnAllSidesEvenWithoutActiveBeams()
    {
        var gameState = new GameState();
        gameState.AddInitialNodes();
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
        gameState.ResearchPoint.BaseValue = 100;
        gameState.BuyResearch(ResearchType.UnlockStars);
        gameState.BuyResearch(ResearchType.UnlockStarSlots);
        gameState.AddInitialNodes();
        var field = (FieldState)typeof(GameState).GetField("_field", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(gameState);
        field.AddFieldObject(FieldNodeType.Snow);
        
        var source = gameState.FieldObjects.First();
        var originalTarget = gameState.FieldObjects.Last();

        // Align
        gameState.MoveFieldObject(source.Id, 0, 10);
        gameState.MoveFieldObject(originalTarget.Id, 250, 10);
        gameState.RefreshConnections();

        gameState.BeginStarDrag(0);
        Assert.True(gameState.TryPlaceStar(source.Id, FieldNodeSide.Right));
        Assert.Equal(1.2d, originalTarget.EvaluateValue().ToDouble(), 6);

        gameState.MoveFieldObject(originalTarget.Id, originalTarget.X, originalTarget.Y + 200);

        var starredSlot = gameState.BeamStarSlots.Single(slot =>
            slot.SourceFieldNodeId == source.Id &&
            slot.SourceSide == FieldNodeSide.Right);

        Assert.True(starredSlot.HasStar);
        Assert.Null(starredSlot.TargetFieldNodeId);
        Assert.Equal(1d, originalTarget.EvaluateValue().ToDouble(), 6);
    }
}
