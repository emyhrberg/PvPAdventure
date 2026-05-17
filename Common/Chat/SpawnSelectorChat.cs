using Microsoft.Xna.Framework;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Chat;

public static class SpawnSelectorChat
{
    private static readonly Color MessageColor = Color.Yellow;

    public static void Announce(Player player, SpawnType type, int targetIdx = -1)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();
        if (!clientConfig.ShowTeleportPlayerMessages)
            return;

        if (player?.active != true)
            return;

        string destination = GetDestination(player, type, targetIdx);
        if (destination == "")
            return;

        SendSystemTeamMessage(player, $"{player.name} has teleported to {destination}", MessageColor);
    }

    public static void SendSystemTeamMessage(Player player, string text, Color color)
    {
        if (player == null || !player.active)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(text, color);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI == Main.myPlayer)
                Main.NewText(text, color);

            return;
        }

        NetworkText message = NetworkText.FromLiteral(player.team == 0 ? text : ChatPrefixFormatter.TeamChannelMarker + text);

        if (player.team == 0)
        {
            ChatHelper.SendChatMessageToClient(message, color, player.whoAmI);
            return;
        }

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player teammate = Main.player[i];
            if (teammate != null && teammate.active && teammate.team == player.team)
                ChatHelper.SendChatMessageToClient(message, color, i);
        }
    }

    private static string GetDestination(Player player, SpawnType type, int targetIdx)
    {
        return type switch
        {
            SpawnType.World => "world spawn",
            SpawnType.MyBed => "their own bed",
            SpawnType.MyPortal => "their own portal",
            SpawnType.Random => "a random location",
            SpawnType.TeammateBed => GetOwnedDestination(player, targetIdx, "bed"),
            SpawnType.TeammatePortal => GetOwnedDestination(player, targetIdx, "portal"),
            _ => ""
        };
    }

    private static string GetOwnedDestination(Player player, int targetIdx, string place)
    {
        if (targetIdx == player.whoAmI)
            return $"their own {place}";

        if (targetIdx < 0 || targetIdx >= Main.maxPlayers)
            return "";

        return Main.player[targetIdx] is { active: true } target ? $"{target.name}'s {place}" : "";
    }
}
