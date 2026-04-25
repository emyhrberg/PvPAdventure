using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.UI.Tabs;
using PvPAdventure.Common.Spectator.UI.Tabs.NPCs;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Common.Spectator.UI.Tabs.World;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

/// <summary>
/// Main spectator panel. Hosts tab buttons and displays the selected tab.
/// </summary>
internal sealed class SpectatorPanel : UIDraggablePanel
{
    private readonly List<ISpectatorTab> tabs = [];
    private readonly List<SpectatorTabButton> tabButtons = [];

    private ISpectatorTab currentTab;
    protected override bool ShowRefreshButton => false;

    protected override void OnClosePanelLeftClick() => Remove();
    protected override bool IsTabButtonHovered() => tabButtons.Any(static button => button.IsMouseHovering);
    protected override void OnPanelRebuilt()
    {
        tabButtons.Clear();
        BuildTabButtons();
        ShowTab(currentTab?.Tab ?? SpectatorTab.Player);
    }

    public SpectatorPanel() : base("")
    {
        Width.Set(560f, 0f);
        Height.Set(560f, 0f);
        HAlign = 0.32f;
        VAlign = 0.45f;

        tabs.Add(new PlayerTab());
        tabs.Add(new WorldTab());
        tabs.Add(new NPCTab());

        currentTab = tabs[0];

        BuildTabButtons();
        ShowTab(currentTab.Tab);
    }

    private void BuildTabButtons()
    {
        tabButtons.Clear();

        float left = 80f;

        foreach (ISpectatorTab tab in tabs)
        {
            ISpectatorTab capturedTab = tab;

            SpectatorTabButton button = new(
                capturedTab.Label,
                capturedTab.Icon,
                () => currentTab == capturedTab,
                () => ShowTab(capturedTab.Tab));

            button.Left.Set(left, 0f);

            TitlePanel.Append(button);
            tabButtons.Add(button);

            left += 76f;
        }
    }

    private void ShowTab(SpectatorTab tab)
    {
        ISpectatorTab nextTab = GetTab(tab);

        if (nextTab is null)
            return;

        ContentPanel.RemoveAllChildren();

        currentTab = nextTab;

        UIElement element = (UIElement)currentTab;
        element.Width.Set(0f, 1f);
        element.Height.Set(0f, 1f);
        element.SetPadding(0f);

        ContentPanel.Append(element);

        currentTab.Refresh();

        foreach (SpectatorTabButton button in tabButtons)
            button.Recalculate();

        Recalculate();
    }

    private ISpectatorTab GetTab(SpectatorTab tab)
    {
        foreach (ISpectatorTab candidate in tabs)
        {
            if (candidate.Tab == tab)
                return candidate;
        }

        return null;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        currentTab?.DrawOverlay(spriteBatch);
    }

    internal sealed class SpectatorTabButton : UIPanel
    {
        private readonly Func<bool> isSelected;
        private readonly string hoverText;

        public SpectatorTabButton(string text, Asset<Texture2D> icon, Func<bool> isSelected, Action onClick)
        {
            this.isSelected = isSelected;
            hoverText = text;

            Width.Set(76f, 0f);
            Height.Set(0f, 1f);
            VAlign = 0.5f;
            SetPadding(0f);

            OnLeftClick += (_, _) => onClick();

            UIImage image = new(icon.Value)
            {
                Left = new StyleDimension(6f, 0f),
                VAlign = 0.5f,
                Width = new StyleDimension(22f, 0f),
                Height = new StyleDimension(22f, 0f)
            };

            Append(image);

            UIText label = new(text, textScale: 0.72f)
            {
                Left = new StyleDimension(31f, 0f),
                VAlign = 0.5f
            };

            Append(label);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BackgroundColor = isSelected() ? new Color(83, 97, 168) : new Color(63, 82, 151) * 0.85f;
            BorderColor = IsMouseHovering ? Color.Yellow : GetBorderColor();

            if (IsMouseHovering)
                Main.instance.MouseText(hoverText);
        }

        private Color GetBorderColor()
        {
            return isSelected() ? Color.White : Color.Black;
        }
    }
}