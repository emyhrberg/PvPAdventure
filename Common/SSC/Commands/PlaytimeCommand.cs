using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC.Commands;

public class PlaytimeCommand : ModCommand
{
    public override string Command => "playtime";

    public override string Description => "Shows your player's playtime in this world.";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (caller.Player == null || !caller.Player.active)
        {
            Main.NewText("Error: Player not found. Could not display playtime.", Color.Red);
            return;
        }

        PlayerPositionSystem.PrintWelcomeMessage(caller.Player);
    }
}

