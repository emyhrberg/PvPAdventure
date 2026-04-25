using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.Tabs.NPCs;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

internal static class StatDrawer
{
    #region Shared draw helpers
    public static void DrawBack(SpriteBatch spriteBatch, Rectangle area)
    {
        Color color = Color.White * 0.7f;

        Asset<Texture2D> texture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
        spriteBatch.Draw(texture.Value, new Vector2(area.X, area.Y), new Rectangle(0, 0, 8, texture.Height()), color);
        spriteBatch.Draw(texture.Value, new Vector2(area.X + 8f, area.Y), new Rectangle(8, 0, 8, texture.Height()), color, 0f, Vector2.Zero, new Vector2((area.Width - 16f) / 8f, 1f), SpriteEffects.None, 0f);
        spriteBatch.Draw(texture.Value, new Vector2(area.Right - 8f, area.Y), new Rectangle(16, 0, 8, texture.Height()), color);
    }

    public static string Truncate(DynamicSpriteFont font, string text, float maxWidth, float scale)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (font.MeasureString(text).X * scale <= maxWidth)
            return text;

        const string ellipsis = "..";
        for (int i = text.Length - 1; i >= 0; i--)
        {
            string candidate = text[..i] + ellipsis;
            if (font.MeasureString(candidate).X * scale <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    public static int GetResponsiveColumns(int width)
    {
        if (width < 220)
            return 2;
        if (width < 340)
            return 3;
        if (width < 460)
            return 4;

        return 5;
    }

    public static int GetGridColumns(Rectangle area)
    {
        return GetResponsiveColumns(area.Width);
    }

    private static int GetVisibleListRows(int height, int statSpacing)
    {
        return height switch
        {
            < 46 => 1,
            < 86 => 2,
            _ => 3
        };
    }
    private static int GetListStatHeight(Rectangle area, int rows, int statSpacing)
    {
        return Math.Clamp((area.Height - statSpacing * (rows - 1)) / rows, 27, 29);
    }
    private static void DrawStat(SpriteBatch spriteBatch, Rectangle area, Texture2D texture, Rectangle? frame, string text)
    {
        DrawBack(spriteBatch, area);

        Rectangle iconArea = new(area.X + 6, area.Y + 5, 16, 16);
        Rectangle source = frame ?? texture.Bounds;

        if (source.Width > 0 && source.Height > 0)
        {
            float scale = Math.Min(iconArea.Width / (float)source.Width, iconArea.Height / (float)source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            spriteBatch.Draw(texture, new Rectangle(iconArea.X, iconArea.Y + (iconArea.Height - height) / 2, width, height), source, Color.White);
        }

        Rectangle textArea = new(area.X + 26, area.Y + 4, area.Width - 30, area.Height - 8);
        Utils.DrawBorderString(spriteBatch, Truncate(FontAssets.MouseText.Value, text, textArea.Width, 0.8f), new Vector2(textArea.X, textArea.Y), Color.White, 0.8f);
    }
    #endregion

    #region Player stats
    public static string DrawPlayerListStats(SpriteBatch spriteBatch, Rectangle area, PlayerStatSnapshot[] stats)
    {
        const int statSpacing = 4;

        int columns = GetResponsiveColumns(area.Width);
        int rows = GetVisibleListRows(area.Height, statSpacing);

        return DrawPlayerStatGrid(spriteBatch, area, stats, columns, rows, GetListStatHeight(area, rows, statSpacing), statSpacing);
    }
    public static void DrawPlayerStat(SpriteBatch spriteBatch, Rectangle area, in PlayerStatSnapshot stat) => DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text);

    public static string DrawPlayerHeadStat(SpriteBatch spriteBatch, Rectangle area, Player player)
    {
        Rectangle textArea = new(area.X + 30, area.Y + 3, area.Width - 30, area.Height - 8);
        string text = Truncate(FontAssets.MouseText.Value, player.name, textArea.Width, 0.8f);
        if (text == "..")
            text = "";

        if (text.Length > 0)
            DrawBack(spriteBatch, area);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        Rectangle headBox = new(area.X + 2, area.Y - 2, 16, 16);
        Vector2 headPos = new(headBox.X + headBox.Width * 0.5f, headBox.Y + headBox.Height * 0.5f);
        PlayerDrawer.DrawPlayerHead(spriteBatch, player, headPos, 0.85f);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        Utils.DrawBorderString(spriteBatch, text, new Vector2(textArea.X, textArea.Y), Color.White, 1f);
        return area.Contains(Main.MouseScreen.ToPoint()) ? $"Player: {player.name}" : null;
    }

    public static string DrawPlayerStatGrid(SpriteBatch spriteBatch, Rectangle area, PlayerStatSnapshot[] stats, int columns, int rows, int statHeight, int statSpacing)
    {
        if (rows <= 0 || columns <= 0 || stats.Length == 0)
            return null;

        int panelWidth = (area.Width - statSpacing * (columns - 1)) / columns;
        int count = Math.Min(stats.Length, columns * rows);
        Point mouse = Main.MouseScreen.ToPoint();
        string hovered = null;

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle panel = new(area.X + column * (panelWidth + statSpacing), area.Y + row * (statHeight + statSpacing), panelWidth, statHeight);

            DrawPlayerStat(spriteBatch, panel, stats[i]);
            if (panel.Contains(mouse))
                hovered = stats[i].HoverText;
        }

        return hovered;
    }
    #endregion

    #region NPC stats
    public static string DrawNPCListStats(SpriteBatch spriteBatch, Rectangle area, NPCStatSnapshot[] stats)
    {
        int columns = GetResponsiveColumns(area.Width);
        int rows = GetVisibleListRows(area.Height, 4);
        return DrawNPCStatGrid(spriteBatch, area, stats, columns, rows, GetListStatHeight(area, rows, 4), 4);
    }

    public static string DrawNPCStatGrid(SpriteBatch spriteBatch, Rectangle area, NPCStatSnapshot[] stats, int columns, int rows, int statHeight, int statSpacing)
    {
        if (rows <= 0 || columns <= 0 || stats.Length == 0)
            return null;

        int panelWidth = (area.Width - statSpacing * (columns - 1)) / columns;
        int count = Math.Min(stats.Length, columns * rows);
        Point mouse = Main.MouseScreen.ToPoint();
        string hovered = null;

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle panel = new(area.X + column * (panelWidth + statSpacing), area.Y + row * (statHeight + statSpacing), panelWidth, statHeight);

            DrawNPCStat(spriteBatch, panel, stats[i]);
            if (panel.Contains(mouse))
                hovered = stats[i].HoverText;
        }

        return hovered;
    }
    public static void DrawNPCStat(SpriteBatch spriteBatch, Rectangle area, in NPCStatSnapshot stat) => DrawStat(spriteBatch, area, stat.Icon.Value, stat.IconFrame, stat.Text);
    #endregion

    #region World stat panels
    public static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, int itemId, string text, string hoverText, int iconSize = 22, Color? textColor = null)
    {
        DrawWorldStatPanel(spriteBatch, area, text, hoverText, textColor ?? Color.White, iconBox =>
        {
            if (itemId <= 0)
            {
                DrawTextureIcon(spriteBatch, Ass.Icon_World.Value, iconBox, iconSize + 2, Color.White * 0.75f);
                return;
            }

            Item item = new(itemId);
            ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, spriteBatch, iconBox.Center.ToVector2(), 0.85f, iconSize, Color.White);
        });
    }

    public static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, Texture2D icon, string text, string hoverText, int iconSize = 26, Color? textColor = null)
    {
        DrawWorldStatPanel(spriteBatch, area, text, hoverText, textColor ?? Color.White, iconBox => DrawTextureIcon(spriteBatch, icon, iconBox, iconSize, Color.White));
    }

    private static void DrawWorldStatPanel(SpriteBatch spriteBatch, Rectangle area, string text, string hoverText, Color textColor, Action<Rectangle> drawIcon)
    {
        DrawBack(spriteBatch, area);

        Rectangle iconBox = new(area.X + 4, area.Y + 2, area.Height - 4, area.Height - 4);
        Rectangle textArea = new(iconBox.Right + 6, area.Y + 5, area.Width - iconBox.Width - 14, area.Height - 10);

        drawIcon(iconBox);

        string displayText = Truncate(FontAssets.MouseText.Value, text, textArea.Width, 0.72f);
        Utils.DrawBorderString(spriteBatch, displayText, new Vector2(textArea.X, textArea.Y + 1), textColor, 0.72f);

        if (iconBox.Contains(Main.MouseScreen.ToPoint()) && !string.IsNullOrWhiteSpace(hoverText))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(hoverText);
        }
    }

    private static void DrawTextureIcon(SpriteBatch spriteBatch, Texture2D texture, Rectangle iconBox, int iconSize, Color color)
    {
        if (texture is null)
            return;

        float scale = Math.Min(iconSize / (float)texture.Width, iconSize / (float)texture.Height);
        spriteBatch.Draw(texture, iconBox.Center.ToVector2(), null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }
    #endregion

}
