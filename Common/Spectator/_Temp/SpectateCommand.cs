using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.Net;
using PvPAdventure.Common.Spectator.UI;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;
using tModPorter;

namespace PvPAdventure.Common.Spectator._Temp;

// Make everyone spectate, or everyone players.
internal class SpectateCommand : ModCommand
{
    public override string Command => "spec";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Show spectate UI.
        //SpectatorUISystem.ToggleSpectateJoinUI();

        // If not allowed, just print a message to user saying its not allowed.
        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();
        if (!config.AllowPlayersToChooseSpectateMode)
        {
            Main.NewText("Choosing spectator mode is disabled on this server.", Color.OrangeRed);
            return;
        }

        // Toggle spectate mode.
        SpectatorSystem.RequestSetLocalMode(
        SpectatorSystem.IsInSpectateMode(Main.LocalPlayer)
            ? PlayerMode.Player
            : PlayerMode.Spectator);
    }
}


internal class GhostCommand : ModCommand
{
    public override string Command => "g";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Toggle ghost mode.
        if (Main.LocalPlayer.ghost) 
            Main.LocalPlayer.ghost = false;
        else 
            Main.LocalPlayer.ghost = true;
    }
}
