using System.Diagnostics;
using Emojimental.Models;
using Emojimental.Services;

internal static class Program
{
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

    private static async Task Main(string[] args)
    {
        var pidFilePath = args.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(pidFilePath))
        {
            var directory = Path.GetDirectoryName(pidFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(pidFilePath, Environment.ProcessId.ToString());
        }

        var gameState = CreateHeavyFactoryScenario();

        Console.WriteLine($"PID={Environment.ProcessId}");
        Console.WriteLine($"Factories={gameState.FieldObjects.Count}");
        Console.WriteLine($"ConnectedInputStars={gameState.BeamStarSlots.Count(slot => slot.TargetFieldNodeId != null)}");
        Console.WriteLine("Warmup=2s");

        await Task.Delay(TimeSpan.FromSeconds(2));

        var sw = Stopwatch.StartNew();
        var iterations = 0;
        while (sw.Elapsed < TimeSpan.FromSeconds(8))
        {
            gameState.Advance(1d / 60d);
            iterations++;
        }

        sw.Stop();
        Console.WriteLine($"Iterations={iterations}");
        Console.WriteLine($"AverageAdvanceMs={sw.Elapsed.TotalMilliseconds / Math.Max(1, iterations):F4}");
    }

    private static GameState CreateHeavyFactoryScenario()
    {
        var gameState = new GameState();
        gameState.SetFieldSize(1920, 1080);

        SeedResources(gameState);
        UnlockAllResearch(gameState);

        foreach (var type in PlayableFactoryTypes)
            gameState.AddFieldObject(type);

        foreach (var node in gameState.FieldObjects)
            node.CooldownSeconds.BaseValue = 0.5d;

        FillAllConnectedInputSlots(gameState);

        foreach (var recycler in gameState.FieldObjects.Where(node => node.Type == FieldNodeType.Recycler))
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


