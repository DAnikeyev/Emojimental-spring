using System.Diagnostics;
using Emojimental.GameEngine.Values;
using Emojimental.Models;
using Emojimental.Services;
using Xunit.Abstractions;

namespace Emojimental.Tests;

public class PerformanceProfileTests
{
    private readonly ITestOutputHelper _output;

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
}
