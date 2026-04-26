using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;

internal class DisableItemUseGhostPlayer : ModPlayer
{
    public override bool CanUseItem(Item item)
    {
        return !Player.ghost && !SpectatorSystem.IsInSpectateMode(Player);
    }

    public override void PreUpdate()
    {
        if (!Player.ghost && !SpectatorSystem.IsInSpectateMode(Player))
            return;

        Player.controlUseItem = false;
        Player.releaseUseItem = false;
        Player.channel = false;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.reuseDelay = 0;
    }
}
