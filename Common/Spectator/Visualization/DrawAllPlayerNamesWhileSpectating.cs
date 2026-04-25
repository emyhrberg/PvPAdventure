using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Config;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.Spectator.Visualization;

[Autoload(Side = ModSide.Client)]
internal sealed class DrawAllPlayerNamesWhileSpectating : ModSystem
{
    public override void Load()
    {
        On_NewMultiplayerClosePlayersOverlay.Draw += DrawNamesAfterNewOverlay;
        On_LegacyMultiplayerClosePlayersOverlay.Draw += DrawNamesAfterLegacyOverlay;
    }

    public override void Unload()
    {
        On_NewMultiplayerClosePlayersOverlay.Draw -= DrawNamesAfterNewOverlay;
        On_LegacyMultiplayerClosePlayersOverlay.Draw -= DrawNamesAfterLegacyOverlay;
    }

    private static void DrawNamesAfterNewOverlay(On_NewMultiplayerClosePlayersOverlay.orig_Draw orig, NewMultiplayerClosePlayersOverlay self)
    {
        orig(self);
        DrawMissingNames();
    }

    private static void DrawNamesAfterLegacyOverlay(On_LegacyMultiplayerClosePlayersOverlay.orig_Draw orig, LegacyMultiplayerClosePlayersOverlay self)
    {
        orig(self);
        DrawMissingNames();
    }

    private static void DrawMissingNames()
    {
        if (!ModContent.GetInstance<SpectatorConfig>().DrawAllPlayerHeadsOnMapWhileSpectating)
            return;

        Player localPlayer = Main.LocalPlayer;
        if (!SpectatorSystem.IsInSpectateMode(localPlayer))
            return;

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float uiScale = Main.UIScale;
        Vector2 screenCenter = new(Main.screenWidth / 2f + Main.screenPosition.X, Main.screenHeight / 2f + Main.screenPosition.Y);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (!ShouldDrawName(localPlayer, player))
                continue;

            string name = player.name;
            Vector2 size = font.MeasureString(name);
            float yOffset = player.chatOverhead.timeLeft > 0 || player.emoteTime > 0 ? -size.Y * uiScale : 0f;
            Vector2 position = player.position;
            position += (position - screenCenter) * (Main.GameViewMatrix.Zoom - Vector2.One);
            position = new Vector2(position.X + player.width / 2f - size.X / 2f, position.Y - size.Y - 2f + yOffset - Main.screenPosition.Y);
            position.X -= Main.screenPosition.X;
            position += size / 2f;
            position *= 1f / uiScale;
            position -= size / 2f;

            if (localPlayer.gravDir == -1f)
                position.Y = Main.screenHeight - position.Y;

            Color color = (player.team > 0 ? Main.teamColor[player.team] : Color.White) * (Main.mouseTextColor / 255f);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, name, position, color, 0f, Vector2.Zero, Vector2.One);
        }
    }

    private static bool ShouldDrawName(Player localPlayer, Player player)
    {
        if (player == null || !player.active || player.dead || player.whoAmI == Main.myPlayer)
            return false;

        return localPlayer.team == 0 || player.team == 0 || player.team != localPlayer.team;
    }
}