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
    private SpectatorControls spectatorControlsElement;
    private SpectatorPanel spectatePanel;

    private SpectatorJoinPanel joinPanel;
    private bool showJoinPanel;

    public override void OnActivate()
    {
        RemoveAllChildren();
        UpdateJoinPanel();
    }

    internal bool IsSpectatePanelOpen() => spectatePanel?.Parent != null;

    internal void ToggleSpectatePanel()
    {
        if (IsSpectatePanelOpen())
        {
            spectatePanel.Remove();
            return;
        }

        spectatePanel ??= new SpectatorPanel();
        Append(spectatePanel);
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
            joinPanel = new SpectatorJoinPanel();
            Append(joinPanel);
        }
        else
        {
            joinPanel?.Remove();
            joinPanel = null;
        }
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
        EnsurePlayerSpectatorControlsOpen();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        if (SpectatorSystem.IsInSpectateMode(local))
            EnsurePlayerSpectatorControlsOpen();
        else
        {
            spectatorControlsElement?.Remove();
            spectatePanel?.Remove();
        }

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
