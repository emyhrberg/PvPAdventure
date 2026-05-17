using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Map;

[Autoload(Side = ModSide.Client)]
internal class DrawAllPlayerHeadsWhileSpectating : ModSystem
{
    public override void Load()
    {
        On_MapHeadRenderer.DrawPlayerHead += OnDrawPlayerHead;
    }

    public override void Unload()
    {
        On_MapHeadRenderer.DrawPlayerHead -= OnDrawPlayerHead;
    }

    private static void OnDrawPlayerHead(On_MapHeadRenderer.orig_DrawPlayerHead orig, MapHeadRenderer self, Camera camera, Player drawPlayer, Vector2 position, float alpha, float scale, Color borderColor)
    {
        if (ShouldDrawAllHeads(drawPlayer))
        {
            orig(self, camera, drawPlayer, position, 1f, scale, borderColor);
            return;
        }

        orig(self, camera, drawPlayer, position, alpha, scale, borderColor);
    }

    private static bool ShouldDrawAllHeads(Player drawPlayer)
    {
        if (drawPlayer == null || !drawPlayer.active)
            return false;

        if (Main.netMode == NetmodeID.SinglePlayer)
            return false;

        Player localPlayer = Main.LocalPlayer;
        if (!SpectatorSystem.IsInSpectateMode(localPlayer))
            return false;

        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();
        return config.DrawAllPlayerHeadsOnMapWhileSpectating;
    }
}