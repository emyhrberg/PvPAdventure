using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorUIState : UIState
{
    private UIColoredImageButton spectatePlayersButton;

    private SpectatorControls spectatorControlsElement;
    private SpectatorPanel spectatePanel;

    private JoinPanel joinPanel;
    private bool showJoinPanel;

    public override void OnActivate()
    {
        RemoveAllChildren();

        spectatePlayersButton = CreateTopButton(2, ToggleSpectatePanel, Ass.Icon_Eye);

        UpdateTopButtons();
        UpdateJoinPanel();
    }

    private static UIColoredImageButton CreateTopButton(int index, Action onClick, Asset<Texture2D> icon)
    {
        UIColoredImageButton button = new(icon, isSmall: true);
        button.HAlign = 0.5f;
        //button.VAlign = 0.5f;
        button.Top.Set(4f, 0f);
        //button.Left.Set(-400f - index * 32f, 0f);
        button.Left.Set(200, 0);
        button.SetVisibility(1f, 1f);
        button.OnLeftClick += (_, _) => onClick();
        return button;
    }

    internal void ToggleJoinPanel()
    {
        showJoinPanel = !showJoinPanel;
        UpdateJoinPanel();
    }

    internal void CloseJoinPanel()
    {
        showJoinPanel = false;
        UpdateJoinPanel();
    }

    internal bool IsJoinPanelOpen() => showJoinPanel;

    private void UpdateJoinPanel()
    {
        if (showJoinPanel)
        {
            joinPanel?.Remove();
            joinPanel = new JoinPanel();
            Append(joinPanel);
        }
        else
        {
            joinPanel?.Remove();
            joinPanel = null;
        }
    }

    private void UpdateTopButtons()
    {
        bool shouldShow = SpectatorSystem.IsInSpectateMode(Main.LocalPlayer);

        SetTopButtonVisible(spectatePlayersButton, shouldShow);

        if (!shouldShow)
        {
            spectatePanel?.Remove();
            spectatorControlsElement?.Remove();
        }
    }

    private void SetTopButtonVisible(UIElement element, bool visible)
    {
        if (element is null)
            return;

        if (visible && element.Parent is null)
            Append(element);
        else if (!visible)
            element.Remove();
    }

    private static bool HandleHover(UIElement element, string text)
    {
        if (element?.IsMouseHovering != true)
            return false;

        Main.instance.MouseText(text);
        Main.LocalPlayer.mouseInterface = true;
        return true;
    }

    internal void EnsurePlayerSpectatorControlsOpen()
    {
        if (spectatorControlsElement?.Parent is not null)
            return;

        spectatorControlsElement ??= new SpectatorControls();
        Append(spectatorControlsElement);
    }

    internal void ToggleSpectatorControlsElement()
    {
        if (spectatorControlsElement?.Parent is null)
        {
            spectatorControlsElement ??= new SpectatorControls();
            Append(spectatorControlsElement);
        }
        else spectatorControlsElement.Remove();
    }

    internal void ToggleSpectatePanel()
    {
        if (spectatePanel?.Parent is null)
        {
            //SpectatorPlayerEntry.ClearSelectedInventory();
            spectatePanel ??= new SpectatorPanel();
            Append(spectatePanel);
        }
        else spectatePanel.Remove();
    }

    internal void EnsureSpectatePanelOpen()
    {
        if (spectatePanel?.Parent is not null)
            return;

        //SpectatorPlayerEntry.ClearSelectedInventory();

        spectatePanel ??= new SpectatorPanel();
        Append(spectatePanel);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateTopButtons();

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        HandleHover(spectatePlayersButton, "Toggle spectate panel");

        spectatorControlsElement?.UpdateTarget();
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

#if DEBUG
        //DebugDrawer.DrawElement(sb, eyeButton);
#endif
    }
}
