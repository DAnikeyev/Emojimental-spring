using Emojimental.Models;
using Emojimental.Services;
using Xunit;
using BreakInfinity;
using System.Linq;

namespace Emojimental.Tests;

public class HappinessTests
{
    [Fact]
    public void HappinessEvaluation_IsDerivedFromCarrotsFlowersHouses()
    {
        var gameState = new GameState();
        
        // Set base values for the three components
        gameState.Carrot.BaseValue = 2;
        gameState.Flower.BaseValue = 3;
        gameState.Houses.BaseValue = 4;
        
        // Formula: (Carrots + 1) * (Flowers + 1) * (Houses + 1)
        // = (2+1) * (3+1) * (4+1) = 3 * 4 * 5 = 60
        var happiness = gameState.EvaluateHappiness();
        Assert.Equal(60d, happiness.ToDouble(), 6);
    }

    [Fact]
    public void HappinessEvaluation_WithZeroValues_ReturnsOne()
    {
        var gameState = new GameState();
        
        // All zero: (0+1) * (0+1) * (0+1) = 1
        var happiness = gameState.EvaluateHappiness();
        Assert.Equal(1d, happiness.ToDouble(), 6);
    }

    [Fact]
    public void CarrotStat_HasGemSlot()
    {
        var gameState = new GameState();
        
        // Unlock passive stat star slots
        gameState.ResearchPoint.BaseValue = 1000000;
        gameState.BuyResearch(ResearchType.UnlockPassiveGemSlots);
        
        // Set base carrot
        gameState.Carrot.BaseValue = 5;
        
        // Initial evaluation (no stars)
        var initialEval = gameState.EvaluatePassiveStat(PassiveStatType.Carrots);
        Assert.Equal(5d, initialEval.ToDouble(), 6);
        
        // Add a star to Carrots slot
        gameState.BuyResearch(ResearchType.UnlockStars);
        Assert.Contains(gameState.StarInventory, s => s.HasStar);
        var starIndex = gameState.StarInventory.Select((s, i) => new { s, i }).First(x => x.s.HasStar).i;
        
        gameState.BeginStarDrag(starIndex);
        Assert.True(gameState.TryPlacePassiveStatStar(PassiveStatType.Carrots));
        
        // Evaluate with 1 star (multiplier should be 2x by default)
        var withStarEval = gameState.EvaluatePassiveStat(PassiveStatType.Carrots);
        Assert.Equal(10d, withStarEval.ToDouble(), 6);
    }
}
