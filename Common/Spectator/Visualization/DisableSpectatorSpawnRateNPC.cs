using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;

internal class DisableSpectatorSpawnRateNPC : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!SpectatorSystem.IsInSpectateMode(player))
            return;

        spawnRate = int.MaxValue;
        maxSpawns = 0;
    }
}
