using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Players;

internal sealed class SpectatorPlayerEntry : UIBrowserEntry
{
    private readonly Player player;
    private readonly UIElement listChrome;
    private readonly UIElement playerButtonRow;
    private static Player inventoryPlayer;
    private bool needsLateLayout = true;
    private string hoveredStatText;
    public Player Player => player;

    // Cache stats for performance
    private readonly PlayerStatSnapshot[] cachedStats;
    private readonly PlayerStatSnapshot[] cachedStatsWithoutHead;
    private ulong cachedStatsUpdate = ulong.MaxValue;

    public int TeamSortValue => player.team == 0 ? int.MaxValue : player.team;

    public int BiomeSortValue
    {
        get
        {
            int value = BiomeHelper.GetBiomeVisual(player).BackgroundIndex;
            return value < 0 ? int.MaxValue : value;
        }
    }

    public SpectatorPlayerEntry(Player targetPlayer) : base()
    {
        player = targetPlayer ?? new Player();
        SearchText = BuildSearchText();

        cachedStats = new PlayerStatSnapshot[PlayerStats.All.Count];
        cachedStatsWithoutHead = new PlayerStatSnapshot[Math.Max(0, PlayerStats.All.Count - 1)];

        listChrome = new UIElement();
        listChrome.Width.Set(0f, 1f);
        listChrome.Height.Set(0f, 1f);
        Append(listChrome);

        playerButtonRow = new UIElement();
        listChrome.Append(playerButtonRow);

        float left = 0f;

        AddPlayerButton(TextureAssets.Item[ItemID.TeleportationPotion], ref left, "Teleport", OnTeleportClicked);
        AddPlayerButton(Ass.Icon_Eye, ref left, "Spectate", OnSpectateClicked, IsSpectating);
        AddPlayerButton(TextureAssets.Item[ItemID.PiggyBank], ref left, "Toggle inventory", OnInventoryClicked, IsInventoryOpen);

        ApplyLayout();
    }

    public override void SetListMode(bool value)
    {
        listMode = value;
        ApplyLayout();
    }

    public override void SetEntrySize(int size)
    {
        entrySize = size;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);

            if (listChrome.Parent is null)
                Append(listChrome);

            const int buttonSize = 32;
            const int buttonGap = 4;
            const int buttonBottomPadding = 2;

            float rowWidth = buttonSize * 3 + buttonGap * 2;
            float previewHeight = Math.Max(0f, entrySize - buttonSize - buttonBottomPadding);
            float previewWidth = Math.Max(0f, previewHeight - 10f);

            playerButtonRow.Width.Set(rowWidth, 0f);
            playerButtonRow.Height.Set(buttonSize, 0f);
            playerButtonRow.Left.Set(4f + Math.Max(0f, previewWidth - rowWidth) * 0.5f, 0f);
            playerButtonRow.Top.Set(entrySize - buttonSize - buttonBottomPadding, 0f);

            if (ShouldShowButtons())
            {
                if (playerButtonRow.Parent is null)
                    listChrome.Append(playerButtonRow);
            }
            else
            {
                playerButtonRow.Remove();
            }
        }
        else
        {
            Width.Set(entrySize, 0f);
            Height.Set(entrySize, 0f);

            listChrome.Remove();
        }

        Recalculate();
    }

    private bool ShouldShowButtons()
    {
        return listMode && entrySize >= 100;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        ApplyLateLayout();

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
        }
    }

    private void ApplyLateLayout()
    {
        if (!needsLateLayout || Parent == null || GetDimensions().Width <= 0f)
            return;

        ApplyLayout();
        listChrome.Recalculate();
        Recalculate();

        needsLateLayout = false;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle box = GetDimensions().ToRectangle();
        Rectangle biomeBox = box;
        hoveredStatText = null;

        if (ShouldShowButtons())
        {
            const int buttonSize = 32;
            biomeBox.Height = Math.Max(0, biomeBox.Height - buttonSize);
        }

        Utils.DrawInvBG(sb, box, Color.Black * 0.35f);
        //Utils.DrawInvBG(spriteBatch, box, IsMouseHovering ? new Color(73, 94, 171, 50) : new Color(0,0,0,25));
        BackgroundDrawer.DrawMapFullscreenBackground(sb, biomeBox, player, listMode);

        if (listMode)
        {
            PlayerDrawer.DrawFullPlayerPreview(sb, player, biomeBox);
            DrawListMode(sb, box);
        }
        else
            DrawGridMode(sb, box);

        if (!string.IsNullOrEmpty(hoveredStatText))
            UICommon.TooltipMouseText(hoveredStatText);
    }

    private void DrawGridMode(SpriteBatch spriteBatch, Rectangle box)
    {
        const int outerPadding = 6;
        const int statSpacing = 2;
        const int statHeight = 27;

        int availableHeight = box.Height - outerPadding * 2;
        int totalRows = Math.Max(0, (availableHeight + statSpacing) / (statHeight + statSpacing));
        if (totalRows <= 0)
            return;

        Rectangle headStatBox = new(box.X + outerPadding, box.Y + outerPadding, box.Width - outerPadding * 2, statHeight);
        hoveredStatText = StatDrawer.DrawPlayerHeadStat(spriteBatch, headStatBox, player) ?? hoveredStatText;

        int statRows = Math.Max(0, totalRows - 1);
        if (statRows <= 0)
            return;

        int top = headStatBox.Bottom + statSpacing;
        Rectangle statArea = new(box.X + outerPadding, top, box.Width - outerPadding * 2, box.Bottom - outerPadding - top);
        int columns = StatDrawer.GetGridColumns(statArea);

        hoveredStatText = StatDrawer.DrawPlayerStatGrid(spriteBatch, statArea, BuildStats(skipPlayerHead: true), columns, statRows, statHeight, statSpacing) ?? hoveredStatText;
    }

    private void DrawListMode(SpriteBatch spriteBatch, Rectangle box)
    {
        const int buttonSize = 32;

        //int previewHeight = Math.Max(0, box.Height - buttonSize);
        //int previewWidth = Math.Max(0, previewHeight + 32);
        //int previewHeight = ShouldShowButtons() ? Math.Max(0, box.Height - buttonSize) : box.Height;
        int previewHeight = Math.Max(0, box.Height - buttonSize);
        int previewWidth = 100;

        Rectangle area = new(box.X + 4 + previewWidth + 5, box.Y+4, box.Width - previewWidth - 14, previewHeight);

        if (area.Width <= 0 || area.Height <= 0)
            return;

        hoveredStatText = StatDrawer.DrawPlayerListStats(spriteBatch, area, BuildStats(skipPlayerHead: true));
    }

    private void AddPlayerButton(Asset<Texture2D> texture, ref float leftOffset, string label, UIElement.MouseEvent click = null, Func<bool> selected = null)
    {
        const int buttonSize = 28;

        SpectatorPlayerButton button = new(texture, label, selected);
        button.Width.Set(buttonSize, 0f);
        button.Height.Set(buttonSize, 0f);
        button.Left.Set(leftOffset + 4, 0f);
        button.Top.Set(4f, 0f);

        if (click != null)
            button.OnLeftClick += click;

        playerButtonRow.Append(button);
        leftOffset += buttonSize + 4f;
    }

    private bool IsInventoryOpen()
    {
        return ReferenceEquals(inventoryPlayer, player);
    }

    private bool IsSpectating()
    {
        return SpectatorSystem.IsTargeting(player);
    }

    private void OnInventoryClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        inventoryPlayer = IsInventoryOpen() ? null : player;
    }

    internal static void DrawSelectedInventory(SpriteBatch sb)
    {
        if (inventoryPlayer?.active != true)
        {
            inventoryPlayer = null;
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);

        InventoryDrawer.DrawInventory(sb, new Vector2(20f, 20f), inventoryPlayer, viewport);
        InventoryDrawer.DrawEquipment(sb, inventoryPlayer, viewport);
    }

    internal static void ClearSelectedInventory()
    {
        inventoryPlayer = null;
    }

    private PlayerStatSnapshot[] BuildStats(bool skipPlayerHead)
    {
        if (cachedStatsUpdate != Main.GameUpdateCount)
        {
            for (int i = 0; i < PlayerStats.All.Count; i++)
                cachedStats[i] = PlayerStats.All[i].Build(player);

            for (int i = 0; i < cachedStatsWithoutHead.Length; i++)
                cachedStatsWithoutHead[i] = cachedStats[i + 1];

            cachedStatsUpdate = Main.GameUpdateCount;
        }

        return skipPlayerHead ? cachedStatsWithoutHead : cachedStats;
    }

    private string BuildSearchText()
    {
        StringBuilder text = new();
        text.Append(player.name);
        text.Append(' ');
        text.Append(player.whoAmI);
        text.Append(' ');
        text.Append(player.team);

        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            PlayerStatSnapshot stat = PlayerStats.All[i].Build(player);
            //text.Append(' ');
            //text.Append(stat.Label);
            //text.Append(' ');
            text.Append(stat.Text);
        }

        return text.ToString();
    }

    private void OnSpectateClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player?.active != true)
            return;

        Player local = Main.LocalPlayer;

        if (local?.active != true || player.whoAmI == local.whoAmI)
            return;

        if (IsSpectating())
        {
            SpectatorSystem.ClearTarget();
            SpectatorUISystem.TogglePlayerSpectatorControls();
            Log.Chat("Stopped spectating " + player.name);
            return;
        }

        if (!SpectatorSystem.IsInSpectateMode(local))
            SpectatorSystem.RequestSetLocalMode(PlayerMode.Spectator);

        SpectatorSystem.SetPlayerTarget(player.whoAmI);
        SpectatorUISystem.EnsurePlayerSpectatorControlsOpen();

        Log.Chat($"Now spectating {player.name}");
        //Main.NewText($"Now spectating {player.name}");
    }

    private void OnTeleportClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player is null || !player.active)
            return;

        Player localPlayer = Main.LocalPlayer;

        Vector2 telePos = player.Center - new Vector2(localPlayer.width, localPlayer.height) * 0.5f;

        if (Main.netMode == NetmodeID.SinglePlayer)
            localPlayer.Teleport(telePos, TeleportationStyleID.RodOfDiscord);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 2, Main.LocalPlayer.whoAmI, telePos.X, telePos.Y, TeleportationStyleID.PotionOfReturn);

        Log.Chat($"Teleported to {player.name}");
        //Main.NewText($"Teleported to {player.name}");
    }

    private sealed class SpectatorPlayerButton : UIElement
    {
        private readonly Asset<Texture2D> texture;
        private readonly string hoverText;
        private readonly Func<bool> selected;

        public SpectatorPlayerButton(Asset<Texture2D> texture, string hoverText, Func<bool> selected = null)
        {
            this.texture = texture;
            this.hoverText = hoverText;
            this.selected = selected;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = selected?.Invoke() == true;

            // Draw the Background
            // Use InventoryBack14 (Gold/Yellow) if selected, otherwise standard Back (Blue/Grey)
            Texture2D backTex = (isSelected ? TextureAssets.InventoryBack14 : TextureAssets.InventoryBack).Value;
            spriteBatch.Draw(backTex, box, Color.White * 0.8f);

            // Draw the Icon (Centered)
            Texture2D iconTex = texture.Value;
            float iconScale = 1f;
            if (iconTex.Width > box.Width || iconTex.Height > box.Height)
            {
                iconScale = iconTex.Width > iconTex.Height
                    ? (box.Width - 8f) / iconTex.Width
                    : (box.Height - 8f) / iconTex.Height;
            }

            Vector2 origin = iconTex.Size() / 2f;
            Vector2 position = box.Center.ToVector2();

            Color iconColor = IsMouseHovering || isSelected ? Color.White : Color.White * 0.8f;

            spriteBatch.Draw(iconTex, position, null, iconColor, 0f, origin, iconScale, SpriteEffects.None, 0f);

            if (IsMouseHovering)
                Main.instance.MouseText(hoverText);
        }
    }
}