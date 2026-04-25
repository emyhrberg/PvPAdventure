using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

internal static class SpectatorNetHandler
{
    internal enum SpectatorOperation : byte
    {
        RequestFullSync,
        FullSync,
        RequestSetMode,
        SetMode
    }

    public static void Receive(BinaryReader reader, int sender)
    {
        SpectatorOperation op = (SpectatorOperation)reader.ReadByte();

        switch (op)
        {
            case SpectatorOperation.RequestFullSync:
                if (Main.netMode == NetmodeID.Server)
                    SendFullSync(sender);
                break;

            case SpectatorOperation.FullSync:
                ReceiveFullSync(reader);
                break;

            case SpectatorOperation.RequestSetMode:
                ReceiveRequestSetMode(reader, sender);
                break;

            case SpectatorOperation.SetMode:
                ReceiveSetMode(reader);
                break;
        }
    }

    public static void SendRequestFullSync()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Packet(SpectatorOperation.RequestFullSync).Send();
    }

    public static void SendRequestSetMode(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = Packet(SpectatorOperation.RequestSetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send();
    }

    public static void SendFullSync(int toClient)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        SpectatorSystem.EnsureServerModes();

        ModPacket packet = Packet(SpectatorOperation.FullSync);
        packet.Write(SpectatorSystem.Modes.Count);

        foreach ((int slot, PlayerMode mode) in SpectatorSystem.Modes)
        {
            packet.Write(slot);
            packet.Write((byte)mode);
        }

        packet.Send(toClient);
    }

    public static void SendMode(int slot, PlayerMode mode, int toClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = Packet(SpectatorOperation.SetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send(toClient);
    }

    private static void ReceiveFullSync(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        SpectatorSystem.Modes.Clear();

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            SpectatorSystem.SetModeLocal(reader.ReadInt32(), (PlayerMode)reader.ReadByte());
    }

    private static void ReceiveRequestSetMode(BinaryReader reader, int sender)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        int slot = reader.ReadInt32();
        PlayerMode mode = (PlayerMode)reader.ReadByte();

        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (slot != sender && !HasDragonLensAdminPermission(sender))
            return;

        SpectatorSystem.SetModeServer(slot, mode);
    }

    private static void ReceiveSetMode(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        SpectatorSystem.SetModeLocal(reader.ReadInt32(), (PlayerMode)reader.ReadByte());
    }

    private static ModPacket Packet(SpectatorOperation operation)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)operation);
        return packet;
    }

    private static bool HasDragonLensAdminPermission(int sender)
    {
        return sender >= 0 && sender < Main.maxPlayers &&
            ModLoader.HasMod("DragonLens") &&
            HasDragonLensAdminPermission_DragonLens(sender);
    }

    [JITWhenModsEnabled("DragonLens")]
    private static bool HasDragonLensAdminPermission_DragonLens(int sender)
    {
        Player player = Main.player[sender];
        return player?.active == true && DragonLens.Core.Systems.PermissionHandler.CanUseTools(player);
    }
}