namespace PvPAdventure;

public enum AdventurePacketIdentifier : byte
{
    BountyTransaction,
    PlayerStatistics,
    WorldMapLighting,
    PingPong,
    PlayerItemPickup,
    PlayerTeam,

    QueueToggle,        // client -> server: player toggled queue on/off
    QueueCounts,        // server -> client: online + queued counts
    QueueCountsRequest  // client -> server: please send counts to me
}