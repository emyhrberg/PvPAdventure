using PvPAdventure.Common.Game;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

internal class MatchStatsPlayer : ModPlayer
{
    // Server-side: populated by StatisticsPlayer.PostHurt
    public uint DamageDealt { get; private set; }
    public uint DamageTaken { get; private set; }
    public readonly Dictionary<int, uint> WeaponDamage = new();

    // Per-player totals for client-sourced stats (applied via MatchStatsDelta packets on server)
    public uint ConsumablesUsed { get; private set; }
    public uint TilesPlaced { get; private set; }
    public uint TilesMined { get; private set; }
    public uint MiningToolsUsed { get; private set; }
    public readonly Dictionary<int, uint> ConsumableItems = new();
    public readonly Dictionary<int, uint> PlacementItems = new();
    public readonly Dictionary<int, uint> MinedTiles = new();
    public readonly Dictionary<int, uint> MiningTools = new();

    private int _prevHeldItemStack;
    private int _prevHeldItemType;

    public void Reset()
    {
        DamageDealt = 0;
        DamageTaken = 0;
        WeaponDamage.Clear();
        ConsumablesUsed = 0;
        TilesPlaced = 0;
        TilesMined = 0;
        MiningToolsUsed = 0;
        ConsumableItems.Clear();
        PlacementItems.Clear();
        MinedTiles.Clear();
        MiningTools.Clear();
    }

    // Server-side: called from StatisticsPlayer.PostHurt
    public void AddDamageDealt(int itemId, uint amount)
    {
        if (amount == 0) return;
        DamageDealt += amount;
        WeaponDamage[itemId] = WeaponDamage.GetValueOrDefault(itemId) + amount;
    }

    public void AddDamageTaken(uint amount)
    {
        if (amount == 0) return;
        DamageTaken += amount;
    }

    // Called from GlobalTile or PostItemCheck (client-side), sends packet in multiplayer
    public void TrackConsumable(int itemId, uint amount = 1)
    {
        if (!IsMatchPlaying()) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            SendDelta(StatCategory.ConsumablesUsed, itemId, amount);
        else
            ApplyDelta(StatCategory.ConsumablesUsed, itemId, amount);
    }

    public void TrackTilePlaced(int itemId)
    {
        if (!IsMatchPlaying()) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            SendDelta(StatCategory.TilesPlaced, itemId, 1);
        else
            ApplyDelta(StatCategory.TilesPlaced, itemId, 1);
    }

    public void TrackTileMined(int tileType, int toolItemId)
    {
        if (!IsMatchPlaying()) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SendDelta(StatCategory.TilesMined, tileType, 1);
            SendDelta(StatCategory.MiningToolsUsed, toolItemId, 1);
        }
        else
        {
            ApplyDelta(StatCategory.TilesMined, tileType, 1);
            ApplyDelta(StatCategory.MiningToolsUsed, toolItemId, 1);
        }
    }

    // Applied on server via packet handler (whoAmI-attributed)
    public void ApplyDelta(StatCategory category, int itemId, uint amount)
    {
        switch (category)
        {
            case StatCategory.ConsumablesUsed:
                ConsumablesUsed += amount;
                ConsumableItems[itemId] = ConsumableItems.GetValueOrDefault(itemId) + amount;
                break;
            case StatCategory.TilesPlaced:
                TilesPlaced += amount;
                PlacementItems[itemId] = PlacementItems.GetValueOrDefault(itemId) + amount;
                break;
            case StatCategory.TilesMined:
                TilesMined += amount;
                MinedTiles[itemId] = MinedTiles.GetValueOrDefault(itemId) + amount;
                break;
            case StatCategory.MiningToolsUsed:
                MiningToolsUsed += amount;
                MiningTools[itemId] = MiningTools.GetValueOrDefault(itemId) + amount;
                break;
        }
    }

    public IDictionary<string, uint> BuildStats() => new Dictionary<string, uint>
    {
        ["damage_dealt"] = DamageDealt,
        ["damage_taken"] = DamageTaken,
        ["consumables_used"] = ConsumablesUsed,
        ["tiles_placed"] = TilesPlaced,
        ["tiles_mined"] = TilesMined,
        ["mining_tools_used"] = MiningToolsUsed,
    };

    public IDictionary<string, IDictionary<int, uint>> BuildItemStats()
    {
        var result = new Dictionary<string, IDictionary<int, uint>>();
        if (WeaponDamage.Count > 0) result["damage_dealt"] = new Dictionary<int, uint>(WeaponDamage);
        if (ConsumableItems.Count > 0) result["consumables_used"] = new Dictionary<int, uint>(ConsumableItems);
        if (PlacementItems.Count > 0) result["tiles_placed"] = new Dictionary<int, uint>(PlacementItems);
        if (MinedTiles.Count > 0) result["tiles_mined"] = new Dictionary<int, uint>(MinedTiles);
        if (MiningTools.Count > 0) result["mining_tools_used"] = new Dictionary<int, uint>(MiningTools);
        return result;
    }

    public static void HandleDeltaPacket(BinaryReader reader, int whoAmI)
    {
        var category = (StatCategory)reader.ReadByte();
        int itemId = reader.ReadInt32();
        uint amount = reader.ReadUInt32();

        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
            return;

        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
            return;

        Player player = Main.player[whoAmI];
        if (player == null || !player.active)
            return;

        player.GetModPlayer<MatchStatsPlayer>().ApplyDelta(category, itemId, amount);
    }

    public override bool PreItemCheck()
    {
        if (Main.netMode != NetmodeID.Server && IsMatchPlaying())
        {
            _prevHeldItemStack = Player.HeldItem.stack;
            _prevHeldItemType = Player.HeldItem.type;
        }
        return true;
    }

    public override void PostItemCheck()
    {
        if (Main.netMode == NetmodeID.Server || !IsMatchPlaying())
            return;

        if (_prevHeldItemType == 0)
            return;

        if (Player.HeldItem.type != _prevHeldItemType)
            return;

        if (!Player.HeldItem.consumable)
            return;

        if (Player.HeldItem.stack >= _prevHeldItemStack)
            return;

        uint consumed = (uint)(_prevHeldItemStack - Player.HeldItem.stack);
        TrackConsumable(_prevHeldItemType, consumed);
    }

    private void SendDelta(StatCategory category, int itemId, uint amount)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.MatchStatsDelta);
        packet.Write((byte)category);
        packet.Write(itemId);
        packet.Write(amount);
        packet.Send();
    }

    private static bool IsMatchPlaying()
        => ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
}

internal enum StatCategory : byte
{
    ConsumablesUsed = 0,
    TilesPlaced = 1,
    TilesMined = 2,
    MiningToolsUsed = 3,
}
