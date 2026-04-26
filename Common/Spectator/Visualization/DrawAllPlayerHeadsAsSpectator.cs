using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Map;

[Autoload(Side = ModSide.Client)]
internal sealed class DrawAllPlayerHeadsOnMapSystem : ModSystem
{
    public override void Load()
    {
        On_Main.DrawMap += DrawMapOverride;
    }

    public override void Unload()
    {
        On_Main.DrawMap -= DrawMapOverride;
    }

    /// <summary>
    /// Thanks PvPFrameworkMini
    /// </summary>
    private static void DrawMapOverride(On_Main.orig_DrawMap orig, Main self, GameTime gameTime)
    {
        if (!SpectatorSystem.IsInSpectateMode(Main.LocalPlayer))
        {
            orig(self, gameTime);
            return;
        }

        bool[] hostile = new bool[Main.maxPlayers];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active != true)
                continue;

            hostile[i] = player.hostile;
            player.hostile = false;
        }

        try
        {
            orig(self, gameTime);
        }
        finally
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (player?.active == true)
                    player.hostile = hostile[i];
            }
        }
    }
}