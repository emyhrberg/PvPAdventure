using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

[Autoload(Side = ModSide.Client)]
public class SpectatorUISystem : ModSystem
{
    private UserInterface spectatorInterface;
    private SpectatorUIState spectatorState;

    public override void OnWorldLoad()
    {
        spectatorInterface = new();
        spectatorState = new();
    }

    public void RebuildUI()
    {
        spectatorState?.RebuildSpectatorControlsPanel();
    }

    public void OnLocalModeAccepted(PlayerMode mode)
    {
        if (mode == PlayerMode.Spectator)
        {
            Main.playerInventory = false;
            Main.NewText("You are now a spectator. Use free camera or select a player to spectate.", Color.Yellow);
            EnsureSpectatorHUDStaysOpen();
            return;
        }

        Main.NewText("You are now a player.", Color.Yellow);
    }

    public void EnsureSpectatorHUDStaysOpen()
    {
        spectatorState?.EnsureSpectatorHUDStaysOpen();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ShouldShowSpectateUI())
        {
            if (spectatorInterface?.CurrentState == null)
            {
                spectatorInterface?.SetState(spectatorState);
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }
        else
        {
            if (spectatorInterface?.CurrentState != null)
            {
                spectatorInterface?.SetState(null);
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
        }

        spectatorInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        // TESTME: Draw the UI below the config?
        // Update: Seems to work!
        if (IsAnyConfigUIOpen())
            index = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (index == -1)
            return;

        if (spectatorInterface?.CurrentState != null)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer(
                "PvPAdventure: Spectator UI",
                () =>
                {
                    spectatorInterface.Draw(Main.spriteBatch, new GameTime());
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }

    private static bool ShouldShowSpectateUI()
    {
        if (Main.gameMenu)
            return false;

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return false;

        return SpectatorModeSystem.IsInSpectateMode(local);
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return Main.ingameOptionsWindow || s is UIModConfig or UIModConfigList;
    }
}
