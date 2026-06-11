using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

internal class MatchStatsGlobalTile : GlobalTile
{
    public override void PlaceInWorld(int i, int j, int tileType, Item item)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (item == null || item.type == ItemID.None)
            return;

        Main.LocalPlayer.GetModPlayer<MatchStatsPlayer>().TrackTilePlaced(item.type);
    }

    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (fail || effectOnly || Main.netMode == NetmodeID.Server)
            return;

        Player localPlayer = Main.LocalPlayer;
        if (localPlayer == null)
            return;

        // Only attribute to local player if they are actively mining at this tile
        if (localPlayer.HeldItem.pick <= 0 || localPlayer.itemAnimation <= 0)
            return;

        //if (localPlayer.tileTargetX != i || localPlayer.tileTargetY != j)
        //    return;

        Tile tile = Main.tile[i, j];
        if (!tile.HasTile)
            return;

        localPlayer.GetModPlayer<MatchStatsPlayer>().TrackTileMined(tile.TileType, localPlayer.HeldItem.type);
    }
}
