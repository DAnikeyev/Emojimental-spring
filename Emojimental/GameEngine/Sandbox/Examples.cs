using BreakInfinity;
using Emojimental.GameEngine.Values;

namespace Emojimental.GameEngine.Sandbox;

public static class Examples
{
    public static BigDouble Example_AddThenMul_IsStageBased_NotInsertionBased()
    {
        var damage = new Stat { BaseValue = 10 };

        damage.AddModifier(new MulModifier(_ => 2));
        damage.AddModifier(new AddModifier(_ => 5));
        damage.AddModifier(new AddModifier(_ => 3));

        var ctx = new EvaluationContext();
        return ctx.Get(damage); // (10 + 5 + 3) * 2 = 36
    }

    public static BigDouble Example_OverridePipeline_MulThenAdd()
    {
        var stat = new Stat { BaseValue = 10 };

        stat.StageOrder = new[]
        {
            ModifierStage.Multiply,
            ModifierStage.Add,
            ModifierStage.Clamp,
            ModifierStage.Post,
        };

        stat.AddModifier(new AddModifier(_ => 5));
        stat.AddModifier(new MulModifier(_ => 2));

        var ctx = new EvaluationContext();
        return ctx.Get(stat); // (10 * 2) + 5 = 25
    }

    public static BigDouble Example_MaxValue_AndClamping()
    {
        var energyMax = new Stat { BaseValue = 100 };
        energyMax.AddAdd(_ => 25);
        energyMax.AddMul(_ => 1.5);

        var energyCapacity = new Stat { BaseValue = 130 };
        energyCapacity.AddClampMax(ctx => ctx.Get(energyMax));

        var ctx = new EvaluationContext();
        return ctx.Get(energyCapacity);
    }

    public static BigDouble Example_ModifierPriority_WithinSameStage()
    {
        var stat = new Stat { BaseValue = 10 };

        stat.AddAdd(_ => 100, priority: 10);
        stat.AddAdd(_ => -50, priority: -10);

        var ctx = new EvaluationContext();
        return ctx.Get(stat);
    }

    public static BigDouble Example_ModifierDependsOnOtherStat()
    {
        var level = new Stat { BaseValue = 1 };

        var damage = new Stat { BaseValue = 10 };
        damage.AddAdd(ctx => ctx.Get(level) * 2);
        damage.AddMul(ctx => 1 + (ctx.Get(level) * 0.1));

        var ctx = new EvaluationContext();
        return ctx.Get(damage);
    }
}