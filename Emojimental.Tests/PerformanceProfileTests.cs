using System.Diagnostics;
using Emojimental.GameEngine.Values;
using Emojimental.Models;
using Emojimental.Services;
using Xunit.Abstractions;

namespace Emojimental.Tests;

public class PerformanceProfileTests
{
    private readonly ITestOutputHelper _output;
    private static readonly FieldNodeType[] PlayableFactoryTypes =
    [
        FieldNodeType.Snow,
        FieldNodeType.Researcher,
        FieldNodeType.Energy,
        FieldNodeType.Smelter,
        FieldNodeType.Farm,
        FieldNodeType.Recycler,
        FieldNodeType.Woodcutter,
        FieldNodeType.Builder,
        FieldNodeType.Bonfire
    ];

    public PerformanceProfileTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProfileGameStateAdvance()
    {
        var gameState = new GameState();
        gameState.SetFieldSize(1920, 1080);
        
        // Setup 10 nodes of different types
        var nodeTypes = Enum.GetValues<FieldNodeType>();
        for (int i = 0; i < 10; i++)
        {
            var type = nodeTypes[i % nodeTypes.Length];
            gameState.AddFieldObject(type);
        }

        // Warmup
        for (int i = 0; i < 100; i++)
        {
            gameState.Advance(0.016);
        }

        var sw = Stopwatch.StartNew();
        int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            gameState.Advance(0.016);
        }
        sw.Stop();

        _output.WriteLine($"[DEBUG_LOG] GameState.Advance ({iterations} iterations with 10 nodes): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"[DEBUG_LOG] Average Advance time: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");
    }

    [Fact]
    public void ProfileStatEvaluation()
    {
        var gameState = new GameState();
        var ctx = new EvaluationContext();
        
        // Add many modifiers to Energy stat
        for (int i = 0; i < 100; i++)
        {
            gameState.Energy.AddMul(_ => 1.01);
        }

        // Warmup
        for (int i = 0; i < 100; i++)
        {
            gameState.Energy.Evaluate(ctx);
        }

        var sw = Stopwatch.StartNew();
        int iterations = 100000;
        for (int i = 0; i < iterations; i++)
        {
            gameState.Energy.Evaluate(ctx);
        }
        sw.Stop();

        _output.WriteLine($"[DEBUG_LOG] Stat.Evaluate (100 modifiers, {iterations} iterations): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"[DEBUG_LOG] Average Evaluate time: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");
    }

    [Fact]
    public void ProfileMixColor()
    {
        var gameState = new GameState();
        
        // Warmup
        for (int i = 0; i < 100; i++)
        {
            gameState.MixColor(44, 62, 120, 255, 245, 150, 0.5);
        }

        var sw = Stopwatch.StartNew();
        int iterations = 100000;
        for (int i = 0; i < iterations; i++)
        {
            gameState.MixColor(44, 62, 120, 255, 245, 150, 0.5);
        }
        sw.Stop();

        _output.WriteLine($"[DEBUG_LOG] GameState.MixColor ({iterations} iterations): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"[DEBUG_LOG] Average MixColor time: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");
    }

    [Fact]
    public void ProfileResourceGet()
    {
        var gameState = new GameState();
        var ctx = new EvaluationContext();

        var sw = Stopwatch.StartNew();
        int iterations = 100000;
        for (int i = 0; i < iterations; i++)
        {
            // Simulate multiple resource lookups in Advance
            ctx.Get(gameState.Ice);
            ctx.Get(gameState.Water);
            ctx.Get(gameState.Energy);
        }
        sw.Stop();

        _output.WriteLine($"[DEBUG_LOG] EvaluationContext.Get (3 types, {iterations} iterations): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"[DEBUG_LOG] Average Get (3 types) time: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");
    }

    [Fact]
    public void ProfileHeavyFactoryScenario()
    {
        var gameState = CreateHeavyFactoryScenario();

        for (var i = 0; i < 240; i++)
            gameState.Advance(1d / 60d);

        var sw = Stopwatch.StartNew();
        var iterations = 0;
        while (sw.Elapsed < TimeSpan.FromSeconds(3))
        {
            gameState.Advance(1d / 60d);
            iterations++;
        }

        sw.Stop();

        _output.WriteLine($"[DEBUG_LOG] Heavy factories: {gameState.FieldObjects.Count}");
        _output.WriteLine($"[DEBUG_LOG] Connected star slots: {gameState.BeamStarSlots.Count(slot => slot.TargetFieldNodeId != null)}");
        _output.WriteLine($"[DEBUG_LOG] Advance iterations during stress loop: {iterations}");
        _output.WriteLine($"[DEBUG_LOG] Average Advance time in stress loop: {sw.Elapsed.TotalMilliseconds / Math.Max(1, iterations):F4}ms");
    }

    [Fact]
    public void ProfileFactoryScaling()
    {
        foreach (var factoryCount in new[] { 3, 6, 9, 12, 18, 24 })
        {
            var gameState = CreateHeavyFactoryScenario(factoryCount);

            for (var i = 0; i < 180; i++)
                gameState.Advance(1d / 60d);

            const int iterations = 4000;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
                gameState.Advance(1d / 60d);
            sw.Stop();

            _output.WriteLine($"[DEBUG_LOG] Scaling nodes={gameState.FieldObjects.Count}, filledInputs={gameState.BeamStarSlots.Count(slot => slot.TargetFieldNodeId != null)}, avgAdvanceMs={sw.Elapsed.TotalMilliseconds / iterations:F4}");
        }
    }

    private static GameState CreateHeavyFactoryScenario(int factoryCount = 0)
    {
        var gameState = new GameState();
        gameState.SetFieldSize(1920, 1080);

        SeedResources(gameState);
        UnlockAllResearch(gameState);

        var targetFactories = factoryCount <= 0 ? PlayableFactoryTypes.Length : factoryCount;
        for (var i = 0; i < targetFactories; i++)
        {
            var type = PlayableFactoryTypes[i % PlayableFactoryTypes.Length];
            gameState.AddFieldObject(type);
        }

        foreach (var node in gameState.FieldObjects)
            node.CooldownSeconds.BaseValue = 0.5d;

        FillAllConnectedInputSlots(gameState);

        var recyclers = gameState.FieldObjects.Where(node => node.Type == FieldNodeType.Recycler).ToList();
        foreach (var recycler in recyclers)
        {
            for (var i = 0; i < 200; i++)
                recycler.EnqueueRecycle();
        }

        return gameState;
    }

    private static void SeedResources(GameState gameState)
    {
        gameState.Coin.BaseValue = 1_000_000_000;
        gameState.ResearchPoint.BaseValue = 1_000_000_000;
        gameState.Energy.BaseValue = 1_000_000_000;
        gameState.Ice.BaseValue = 1_000_000_000;
        gameState.Water.BaseValue = 1_000_000_000;
        gameState.Tree.BaseValue = 1_000_000_000;
        gameState.Wood.BaseValue = 1_000_000_000;
        gameState.Sapling.BaseValue = 1_000_000;
        gameState.StarDust.BaseValue = 1_000_000_000;
        gameState.RecycledStars.BaseValue = 1_000;
        gameState.Stars.BaseValue = 100;
    }

    private static void UnlockAllResearch(GameState gameState)
    {
        foreach (var research in ResearchCatalog.All)
            gameState.BuyResearch(research.Type);
    }

    private static void FillAllConnectedInputSlots(GameState gameState)
    {
        var connectedSlots = gameState.BeamStarSlots
            .Where(slot => slot.TargetFieldNodeId != null)
            .ToList();

        foreach (var _ in connectedSlots)
            gameState.AddStarToInventory(new Star(StarType.Yellow, 1.2, 0, 1));

        foreach (var slot in connectedSlots)
        {
            var inventorySlot = gameState.StarInventory.First(static s => s.HasStar);
            gameState.BeginStarDrag(inventorySlot.Index);
            gameState.TryPlaceStar(slot.SourceFieldNodeId, slot.SourceSide);
        }
    }
}
