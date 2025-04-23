using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.System;

public class WorldGenerationManager : ModSystem
{
    public override void Load()
    {
        IL_WorldGen.UpdateWorld_GrassGrowth += EditWorldGenUpdateWorld_GrassGrowth;
        IL_WorldGen.hardUpdateWorld += OnWorldGenhardUpdateWorld;
    }

    private void EditWorldGenUpdateWorld_GrassGrowth(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first reference to NPC.downedMechBoss3...
        cursor.GotoNext(i => i.MatchLdsfld<NPC>("downedMechBoss3"));

        // ...and go to the constant load of the Plantera Bulb denominator
        cursor.Index += 3;

        // ...and replace it with a delegate that loads from our config instance.
        cursor.Remove().EmitDelegate(() =>
            ModContent.GetInstance<AdventureConfig>().WorldGeneration.PlanteraBulbChanceDenominator);

        // Find the first reference to NPC.downedMechBossAny...
        cursor.GotoNext(i => i.MatchLdsfld<NPC>("downedMechBossAny"));

        // ...and go back to the constant load of non-expert mode Life Fruit denominator
        cursor.Index -= 6;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() => ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitChanceDenominator);

        // Then, advance past else branch, to the constant load of the expert mode Life Fruit denominator...
        cursor.Index += 1;

        // ...while ensuring that instructions removed and emitted are labeled correctly...
        cursor.MoveAfterLabels();
        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitExpertChanceDenominator);
        // Return to default labeling behavior.
        cursor.MoveBeforeLabels();

        // Then, go forward to the constant load of the minimum distance between Life Fruit.
        cursor.Index += 11;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitMinimumDistanceBetween);
    }


    private void OnWorldGenhardUpdateWorld(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to WorldGen.genRand.Next(3)...
        cursor.GotoNext(i => i.MatchCallvirt<UnifiedRandom>("Next") && i.Previous.MatchLdcI4(3));
        //  ...and go back to the constant load...
        cursor.Index -= 1;
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.ChlorophyteSpreadChanceDenominator);
    }
}