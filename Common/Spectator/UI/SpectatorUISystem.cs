using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

[Autoload(Side = ModSide.Client)]
public sealed class SpectatorUISystem : ModSystem
{
    private static UserInterface spectatorInterface;
    private static SpectatorUIState spectatorState;

    public override void OnWorldLoad()
    {
        spectatorInterface = new();
        spectatorState = new();

        // If config wants it to open, then open!
        var config = ModContent.GetInstance<SpectatorConfig>();
        if (config.AllowPlayersToChooseSpectateMode && !config.ForceSpectateMode)
        {
            ToggleSpectateJoinUI();
        }
    }

    public static void EnterPlayerMode()
    {
        SpectatorSystem.RequestSetLocalMode(PlayerMode.Player);
        CloseJoinUI();
        Main.NewText("You are now a player.", Color.Yellow);
    }

    public static void EnterSpectateMode()
    {
        SpectatorSystem.RequestSetLocalMode(PlayerMode.Spectator);
        CloseJoinUI();
        Main.playerInventory = false; // spectators should never see their inventory, so close it if they have it open.
        Main.NewText("You are now a spectator. Use free camera or select a player to spectate.", Color.Yellow);
        EnsurePlayerSpectatorControlsOpen();
    }

    public static void ToggleSpectatePanel()
    {
        spectatorState?.ToggleSpectatePanel();
    }

    public static void ToggleSpectateJoinUI()
    {
        EnsureInitialized();
        spectatorState?.ToggleJoinPanel();
    }

    public static void CloseJoinUI()
    {
        spectatorState?.CloseJoinPanel();
    }

    public static void EnsurePlayerSpectatorControlsOpen()
    {
        spectatorState?.EnsurePlayerSpectatorControlsOpen();
    }

    private static void EnsureInitialized()
    {
        spectatorInterface ??= new();
        spectatorState ??= new();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ShouldShowSpectateUI())
        {
            if (spectatorInterface?.CurrentState == null)
                spectatorInterface?.SetState(spectatorState);
        }
        else
        {
            if (spectatorInterface?.CurrentState != null)
                spectatorInterface?.SetState(null);
        }

        spectatorInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
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

        // Always Show the UI in debug mode for testing purposes.
#if !DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
            return false;
#endif

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return false;

        if (spectatorState?.IsJoinPanelOpen() == true)
            return true;

        return SpectatorSystem.IsInSpectateMode(local);
    }
}
