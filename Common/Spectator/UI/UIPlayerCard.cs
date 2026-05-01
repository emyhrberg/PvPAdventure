using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Misc.DeadSystems;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.Drawers.Inventory;
using PvPAdventure.Common.Travel.UI;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class UIPlayerCard : UIPanel
{
    internal static int CardWidth => 115*2; // biome BG is 115 width
    internal static int CardHeight => 65*2; // biome BG is 65 height

    public int PlayerIndex { get; }
    public int ListIndex { get; }

    private readonly SpectatorControlsPanel owner;
    private readonly float scale;

    public UIPlayerCard(int playerIndex, int listIndex, SpectatorControlsPanel owner, float scale = 1f)
    {
        PlayerIndex = playerIndex;
        ListIndex = listIndex;
        this.owner = owner;
        this.scale = scale;

        SetPadding(0f);

        float buttonSize = 27f * scale;
        float buttonTop = CardHeight * scale - buttonSize - 5f * scale;
        float buttonLeft = 5f * scale;

        AddInventoryButton(buttonLeft, buttonTop, buttonSize);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        // Update border and background color if this player card is selected
        bool isSelected = PlayerIndex >= 0 &&
            PlayerIndex < Main.maxPlayers &&
            Main.player[PlayerIndex]?.active == true &&
            SpectatorTargetSystem.IsLockedTargeting(Main.player[PlayerIndex]);

        if (isSelected)
        {
            BackgroundColor = Color.Yellow;
            BorderColor = Color.Yellow;
        }
        else if (IsMouseHovering)
        {
            BackgroundColor = Colors.FancyUIFatButtonMouseOver * 0.25f;
            BorderColor = Colors.FancyUIFatButtonMouseOver;
        }
        else
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BorderColor = Color.Black;
        }

        base.DrawSelf(sb);

        if (PlayerIndex < 0 || PlayerIndex >= Main.maxPlayers)
            return;

        Player player = Main.player[PlayerIndex];

        if (player is null || !player.active)
            return;

        Rectangle rect = GetDimensions().ToRectangle();

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // Layout
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(27f * scale);
        int buttonRowHeight = buttonSize;
        int buttonRowGap = (int)MathF.Round(4f * scale);
        int buttonContentWidth = (int)MathF.Round(85f * scale);

        Rectangle backgroundRect = rect;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        Rectangle playerPreviewRect = new(contentRect.X, contentRect.Y, buttonContentWidth, contentRect.Height - buttonRowHeight - buttonRowGap);
        Rectangle infoRect = new(playerPreviewRect.Right + (int)MathF.Round(6f * scale), playerPreviewRect.Y + (int)MathF.Round(8f * scale), rect.Right - playerPreviewRect.Right - (int)MathF.Round(12f * scale), playerPreviewRect.Height - (int)MathF.Round(16f * scale));
        Rectangle nameRect = new(infoRect.X, infoRect.Y, infoRect.Width, (int)MathF.Round(26f * scale));

        // Draw biome BG
        BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, backgroundRect, player.Center, shrinkPadding: shrink);

        // Draw player preview background + player preview
        EntityDrawer.DrawEntityBackground(sb, playerPreviewRect);
        EntityDrawer.DrawPlayerPreview(sb, player, playerPreviewRect);

        // Draw player info to the right of the preview
        Rectangle healthRect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(6f * scale), infoRect.Width, (int)MathF.Round(27f * scale));

        // Draw name
        string name = PlayerIndex == Main.myPlayer ? "You" : player.name;
        float textScale = 1.0f;
        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, name, nameRect.Width, textScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * textScale;
        Vector2 namePosition = new(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f);

        Utils.DrawBorderString(sb, displayName, namePosition, Color.White, textScale);

        // Draw health stat
        //Rectangle stat1Rect = ...;
        //StatDrawer.DrawPlayerStat(sb, stat1Rect, stat.Biome);

        //StatDrawer.DrawBack(sb, healthRect);

        //string hp = $"{player.statLife}/{player.statLifeMax2}";
        //Texture2D heart = TextureAssets.Heart.Value;
        //Rectangle heartFrame = heart.Frame();
        //Rectangle heartArea = new(healthRect.X + 6, healthRect.Y + 5, 16, 16);

        //float heartScale = Math.Min(heartArea.Width / (float)heartFrame.Width, heartArea.Height / (float)heartFrame.Height);
        //Vector2 heartPosition = heartArea.Center.ToVector2();

        //sb.Draw(heart, heartPosition, heartFrame, Color.White, 0f, heartFrame.Size() * 0.5f, heartScale, SpriteEffects.None, 0f);

        //Rectangle hpTextArea = new(healthRect.X + 26, healthRect.Y + 4, healthRect.Width - 30, healthRect.Height - 8);
        //string displayHp = StatDrawer.Truncate(FontAssets.MouseText.Value, hp, hpTextArea.Width, 0.8f);

        //Utils.DrawBorderString(sb, displayHp, new Vector2(hpTextArea.X, hpTextArea.Y), Color.White, 0.8f);

        // Debug draw rectangles
        //DebugDrawer.DrawRectangle(playerPreviewRect, drawSize: true);
    }

    private void AddInventoryButton(float left, float top, float size)
    {
        InventoryButton button = new(PlayerIndex, owner);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        Append(button);
    }

    public static void TeleportToPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        Player target = Main.player[playerIndex];
        Player local = Main.LocalPlayer;

        if (target?.active != true || local?.active != true || target.whoAmI == local.whoAmI)
            return;

        Vector2 teleportPosition = target.Center - new Vector2(local.width, local.height) * 0.5f;

        if (Main.netMode == NetmodeID.SinglePlayer)
            local.Teleport(teleportPosition, TeleportationStyleID.RodOfDiscord);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 2, local.whoAmI, teleportPosition.X, teleportPosition.Y, TeleportationStyleID.PotionOfReturn);
    }

    private sealed class InventoryButton : UIElement
    {
        private readonly int playerIndex;
        private readonly SpectatorControlsPanel owner;

        public InventoryButton(int playerIndex, SpectatorControlsPanel owner)
        {
            this.playerIndex = playerIndex;
            this.owner = owner;

            OnLeftClick += (evt, element) =>
            {
                if (IsValidPlayer())
                    InventoryOverlay.Toggle(playerIndex);
            };

            OnMouseOver += (evt, element) => owner.SetStatusText(InventoryOverlay.IsOpen(playerIndex) ? "Close inventory" : "View inventory");
            OnMouseOut += (evt, element) => owner.ResetStatusText();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = InventoryOverlay.IsOpen(playerIndex);

            Texture2D background = isSelected
                ? TextureAssets.InventoryBack14.Value
                : IsMouseHovering
                    ? TextureAssets.InventoryBack7.Value
                    : TextureAssets.InventoryBack.Value;

            Asset<Texture2D> iconAsset = isSelected ? Ass.Icon_InventoryOpen : Ass.Icon_InventoryClosed;
            Texture2D icon = iconAsset.Value;

            float scale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            Color color = isSelected || IsMouseHovering ? Color.White : Color.White * 0.8f;

            sb.Draw(background, box, Color.White * 0.85f);
            sb.Draw(icon, box.Center.ToVector2(), null, color, 0f, icon.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
        }

        private bool IsValidPlayer()
        {
            return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
        }
    }
}