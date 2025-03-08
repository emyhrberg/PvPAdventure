using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.Chat;
using Terraria.GameContent.Creative;
using Terraria.GameContent.NetModules;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Server)]
public class GameManager : ModSystem
{
    private int TimeRemaining { get; set; }
    private int? _startGameCountdown = 0;
    private Phase _currentPhase;

    public Phase CurrentPhase
    {
        get => _currentPhase;
        private set
        {
            if (_currentPhase == value)
                return;

            OnPhaseChange(value);
            _currentPhase = value;
        }
    }

    public enum Phase
    {
        Waiting,
        Playing,
    }

    public override void Load()
    {
        // Prevent the world from entering the lunar apocalypse (killing cultist and spawning pillars)
        On_WorldGen.TriggerLunarApocalypse += _ => { };
        // Prevent tombstones.
        On_Player.DropTombstone += (_, _, _, _, _) => { };

        On_Main.StartInvasion += OnMainStartInvasion;
        // Only send world map pings to teammates.
        On_NetPingModule.Deserialize += OnNetPingModuleDeserialize;
    }

    private void OnMainStartInvasion(On_Main.orig_StartInvasion orig, int type)
    {
        orig(type);

        if (Main.invasionType == InvasionID.None)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();
        if (!adventureConfig.InvasionSizes.TryGetValue(type, out var invasionSize))
            return;

        // We shouldn't increase the invasion size, only ever decrease it.
        if (Main.invasionSize > invasionSize)
        {
            Mod.Logger.Info($"Reducing invasion {type} size from {Main.invasionSize} to {invasionSize}");
            Main.invasionSize = Main.invasionSizeStart = Main.invasionProgressMax = invasionSize;
        }
    }

    // NOTE: This should only ever be applied to the server.
    private bool OnNetPingModuleDeserialize(On_NetPingModule.orig_Deserialize orig, NetPingModule self,
        BinaryReader reader, int userid)
    {
        var position = reader.ReadVector2();
        var packet = NetPingModule.Serialize(position);

        var senderTeam = (Team)Main.player[userid].team;

        foreach (var client in Netplay.Clients)
        {
            if (!client.IsActive)
                continue;

            var player = Main.player[client.Id];
            if (!player.active || player.team == (int)Team.None || player.team == (int)senderTeam)
                NetManager.Instance.SendToClient(packet, client.Id);
        }

        return true;
    }

    public override void PostUpdateTime()
    {
        switch (CurrentPhase)
        {
            case Phase.Waiting:
            {
                if (_startGameCountdown.HasValue)
                {
                    if (--_startGameCountdown <= 0)
                    {
                        _startGameCountdown = null;
                        CurrentPhase = Phase.Playing;
                    }
                    else if (_startGameCountdown <= (60 * 3) && _startGameCountdown % 60 == 0)
                    {
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"{_startGameCountdown / 60}..."),
                            Color.Green);
                    }
                }

                break;
            }
            case Phase.Playing:
            {
                if (--TimeRemaining <= 0)
                    CurrentPhase = Phase.Waiting;

                break;
            }
        }
    }

    public void StartGame(int time)
    {
        CurrentPhase = Phase.Waiting;
        TimeRemaining = time;
        _startGameCountdown = 60 * 10;

        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"The game will begin in {_startGameCountdown / 60} seconds."), Color.Green);
    }

    private void OnPhaseChange(Phase newPhase)
    {
        switch (newPhase)
        {
            case Phase.Waiting:
            {
                // NOTE: We currently have one region, which is the spawn region. We'll use this assumption for now.
                var spawnRegion = ModContent.GetInstance<RegionManager>().Regions[0];
                spawnRegion.CanRandomTeleport = false;
                spawnRegion.CanUseWormhole = false;
                spawnRegion.CanExit = false;
                NetMessage.SendData(MessageID.WorldData);

                // Remove everything that is hostile
                foreach (var npc in Main.ActiveNPCs)
                {
                    if (npc.townNPC || npc.isLikeATownNPC || npc.type == NPCID.TargetDummy)
                        continue;

                    npc.life = 0;
                    npc.netSkip = -1;
                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                }

                var spawnPosition = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                foreach (var player in Main.ActivePlayers)
                {
                    player.Teleport(spawnPosition, TeleportationStyleID.RecallPotion);
                    // FIXME: I think this is right-ish?
                    NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, spawnPosition.X,
                        spawnPosition.Y, 2);
                }

                UpdateFreezeTime(true);

                break;
            }
            case Phase.Playing:
            {
                // NOTE: We currently have one region, which is the spawn region. We'll use this assumption for now.
                var spawnRegion = ModContent.GetInstance<RegionManager>().Regions[0];
                spawnRegion.CanRandomTeleport = true;
                spawnRegion.CanUseWormhole = true;
                spawnRegion.CanExit = true;
                NetMessage.SendData(MessageID.WorldData);

                UpdateFreezeTime(false);

                break;
            }
        }
    }

    private void UpdateFreezeTime(bool value)
    {
        var freezeTimeModule = CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>();
        freezeTimeModule.SetPowerInfo(value);
        var packet = NetCreativePowersModule.PreparePacket(freezeTimeModule.PowerId, 1);
        packet.Writer.Write(freezeTimeModule.Enabled);
        NetManager.Instance.Broadcast(packet);
    }

    public override void ClearWorld()
    {
        _startGameCountdown = null;
        TimeRemaining = 0;

        // If we are already waiting, we need to do a subset of things we would have done during phase change.
        if (CurrentPhase == Phase.Waiting)
        {
            UpdateFreezeTime(true);
        }
        // ...but otherwise, simply changing our phase will handle it.
        else
        {
            CurrentPhase = Phase.Waiting;
        }
    }

    public class TeamCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length < 2)
                return;

            var player = Main.player
                .Where(player => player.active)
                .Where(player => player.name.Contains(args[0], StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            if (player == null)
                return;

            if (!Enum.TryParse(args[1], true, out Team team) || (int)team >= Enum.GetValues<Team>().Length)
                return;

            player.team = (int)team;
            NetMessage.SendData(MessageID.PlayerTeam, number: player.whoAmI);
        }

        public override string Command => "team";
        public override CommandType Type => CommandType.Console;
    }

    public class StartGameCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var time))
            {
                caller.Reply("Invalid time.", Color.Red);
                return;
            }

            var gameManager = ModContent.GetInstance<GameManager>();
            if (gameManager.CurrentPhase == Phase.Playing)
            {
                caller.Reply("The game is already being played.", Color.Red);
                return;
            }

            if (gameManager._startGameCountdown.HasValue)
            {
                caller.Reply("The game is already being started.", Color.Red);
                return;
            }

            gameManager.StartGame(time);
        }

        public override string Command => "startgame";
        public override CommandType Type => CommandType.Console;
    }

    public class TimeLeftCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var gameManager = ModContent.GetInstance<GameManager>();

            if (gameManager == null)
                return;

            if (gameManager.CurrentPhase != Phase.Playing)
                return;

            caller.Reply($"{gameManager.TimeRemaining / 60} seconds remain", Color.Green);
        }

        public override string Command => "timeleft";
        public override CommandType Type => CommandType.World;
    }

    public class CrashoutCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var crashoutMessages = ModContent.GetInstance<AdventureConfig>().CrashoutMessages;
            var message = crashoutMessages[Main.rand.Next(0, crashoutMessages.Count)];

            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"{caller.Player.name} crashed out: {message}"),
                Color.Red, caller.Player.whoAmI);
            NetMessage.SendData(MessageID.Kick, caller.Player.whoAmI, text: NetworkText.FromLiteral(message));
        }

        public override string Command => "crashout";
        public override CommandType Type => CommandType.World;
    }
}