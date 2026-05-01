using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
public class SpectatorTargetSystem : ModSystem
{
    private const int FullSyncIntervalTicks = 15;
    private const float FollowSnapDistance = 800f;
    private const float FollowTargetDistance = 16f;
    private const float FollowVerticalOffset = 0f;
    private const float FollowTargetLerp = 0.45f;
    private const float FollowLocalLerp = 0.65f;

    private static int target = -1;
    private static int npcTarget = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;
    private static int followTargetKey = -1;
    private static int netSyncTicks;
    private static bool hasSmoothedFollowCenter;
    private static Vector2 smoothedFollowCenter;

    #region Targeting
    private static bool CanTarget(int playerId)
    {
        return playerId >= 0 &&
            playerId < Main.maxPlayers &&
            playerId != Main.myPlayer &&
            Main.player[playerId].active &&
            (SpectatorModeSystem.IsInPlayerMode(Main.player[playerId]) || SpectatorModeSystem.IsInSpectateMode(Main.player[playerId]) || Main.player[playerId].ghost);
    }

    private static bool CanTargetNPC(int npcId)
    {
        return npcId >= 0 &&
            npcId < Main.maxNPCs &&
            Main.npc[npcId]?.active == true;
    }

    public static void SetPlayerTarget(int slot, bool preserveAutoDirector = false)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        int next = CanTarget(slot) ? slot : -1;

        if (target != next)
            Log.Chat($"target {target}->{next}");

        target = next;
        npcTarget = -1;
        ResetFollowState();

        if (CanTarget(target))
            SnapLocalPlayerNear(Main.player[target]);
    }

    public static void SetNPCTarget(int slot, bool preserveAutoDirector = false)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        int next = CanTargetNPC(slot) ? slot : -1;

        if (npcTarget != next)
            Log.Chat($"npc target {npcTarget}->{next}");

        npcTarget = next;
        target = -1;
        previewTarget = -1;
        ResetFollowState();

        if (CanTargetNPC(npcTarget))
            SnapLocalPlayerNear(Main.npc[npcTarget]);
    }

    public static void TogglePlayerTarget(int slot)
    {
        if (target == slot)
        {
            ClearTarget();
            return;
        }

        SetPlayerTarget(slot);
    }

    public static void ToggleNPCTarget(int slot)
    {
        if (npcTarget == slot)
        {
            ClearTarget();
            return;
        }

        SetNPCTarget(slot);
    }

    public static void SetPreviewTarget(int slot)
    {
        previewTarget = CanTarget(slot) ? slot : -1;
    }

    public static void ClearPreviewTarget()
    {
        previewTarget = -1;
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxPlayers; i++)
            if (CanTarget(i) && i != exclude)
                targets.Add(i);

        return targets;
    }

    public static void ClearTarget(bool preserveAutoDirector = false, bool moveCameraToLocal = true)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        if (target == -1 && npcTarget == -1)
            return;

        if (target != -1)
            Log.Chat($"clear {target}");

        if (npcTarget != -1)
            Log.Chat($"clear npc {npcTarget}");

        bool previewStillOwnsCamera = CanTarget(previewTarget);

        if (!previewStillOwnsCamera && moveCameraToLocal)
        {
            Player local = Main.LocalPlayer;
            if (local?.active == true)
            {
                Vector2 screenPosition = local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                SpectateCameraFade.SetScreenPosition(screenPosition, allowFade: true);
            }
        }

        if (!previewStillOwnsCamera)
            cameraTarget = -1;

        target = -1;
        npcTarget = -1;
        ResetFollowState();
    }

    public static bool IsTargeting(Player player) => player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    public static bool IsLockedTargeting(Player player) => player?.active == true && CanTarget(target) && target == player.whoAmI;
    public static bool IsLockedTargeting(NPC npc) => npc?.active == true && CanTargetNPC(npcTarget) && npcTarget == npc.whoAmI;

    public static Player GetPlayerTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return null;

        if (CanTarget(previewTarget))
            return Main.player[previewTarget];

        if (CanTarget(target))
            return Main.player[target];

        return null;
    }

    public static Player GetLockedPlayerTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || !CanTarget(target))
            return null;

        return Main.player[target];
    }

    public static NPC GetLockedNPCTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || !CanTargetNPC(npcTarget))
            return null;

        return Main.npc[npcTarget];
    }

    public static string GetLockedTargetStatusText()
    {
        if (GetLockedNPCTarget() is NPC npc)
            return $"Spectating \"{npc.FullName}\"";

        if (GetLockedPlayerTarget() is Player player)
            return $"Spectating {player.name}";

        return null;
    }
    #endregion

    #region Hooks
    public override void ModifyScreenPosition()
    {
        if (GetPlayerTarget() is Player player)
        {
            Vector2 screenPosition = player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            int cameraId = player.whoAmI;
            bool targetChanged = cameraTarget != cameraId;

            SpectateCameraFade.SetScreenPosition(screenPosition, targetChanged);
            cameraTarget = cameraId;
            return;
        }

        if (GetLockedNPCTarget() is NPC npc)
        {
            Vector2 screenPosition = npc.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            int cameraId = 1000 + npc.whoAmI;
            bool targetChanged = cameraTarget != cameraId;

            SpectateCameraFade.SetScreenPosition(screenPosition, targetChanged);
            cameraTarget = cameraId;
            return;
        }

        cameraTarget = -1;
    }

    public override void PostUpdatePlayers()
    {
        Player targetPlayer = GetLockedPlayerTarget();
        if (targetPlayer?.active == true)
        {
            FollowLockedTarget(targetPlayer);
            return;
        }

        NPC targetNPC = GetLockedNPCTarget();
        if (targetNPC?.active == true)
        {
            FollowLockedTarget(targetNPC);
            return;
        }

        ResetFollowState();
    }

    private static void FollowLockedTarget(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        FollowCenter(local, targetPlayer.Center, targetPlayer.velocity, targetPlayer.direction, targetPlayer.whoAmI);
    }

    private static void SnapLocalPlayerNear(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        SnapLocalPlayerNear(targetPlayer.Center, targetPlayer.velocity, targetPlayer.direction, targetPlayer.whoAmI);
    }

    private static void FollowLockedTarget(NPC targetNPC)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetNPC?.active != true)
            return;

        FollowCenter(local, targetNPC.Center, targetNPC.velocity, targetNPC.direction, 1000 + targetNPC.whoAmI);
    }

    private static void SnapLocalPlayerNear(NPC targetNPC)
    {
        if (targetNPC?.active != true)
            return;

        SnapLocalPlayerNear(targetNPC.Center, targetNPC.velocity, targetNPC.direction, 1000 + targetNPC.whoAmI);
    }

    private static void FollowCenter(Player local, Vector2 targetCenter, Vector2 targetVelocity, int direction, int targetKey)
    {
        Vector2 desiredCenter = GetFollowCenter(targetCenter, targetVelocity, direction);
        bool targetChanged = followTargetKey != targetKey;
        bool shouldSnap = targetChanged ||
            !hasSmoothedFollowCenter ||
            Vector2.DistanceSquared(local.Center, desiredCenter) > FollowSnapDistance * FollowSnapDistance;

        followTargetKey = targetKey;

        if (shouldSnap)
        {
            smoothedFollowCenter = desiredCenter;
            hasSmoothedFollowCenter = true;
            local.Center = desiredCenter;
        }
        else
        {
            smoothedFollowCenter = Vector2.Lerp(smoothedFollowCenter, desiredCenter, FollowTargetLerp);
            local.Center = Vector2.Lerp(local.Center, smoothedFollowCenter, FollowLocalLerp);
        }

        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);

        SyncLocalPlayerPosition(forceFullSync: false);
    }

    private static void SnapLocalPlayerNear(Vector2 targetCenter, Vector2 targetVelocity, int direction, int targetKey)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        Vector2 desiredCenter = GetFollowCenter(targetCenter, targetVelocity, direction);

        followTargetKey = targetKey;
        smoothedFollowCenter = desiredCenter;
        hasSmoothedFollowCenter = true;
        local.Center = desiredCenter;
        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);

        SyncLocalPlayerPosition(forceFullSync: true);
    }

    private static void ResetFollowState()
    {
        followTargetKey = -1;
        hasSmoothedFollowCenter = false;
        netSyncTicks = 0;
    }

    private static void SyncLocalPlayerPosition(bool forceFullSync)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Player local = Main.LocalPlayer;

        NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, local.whoAmI);

        if (forceFullSync || ++netSyncTicks >= FullSyncIntervalTicks)
        {
            netSyncTicks = 0;
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, local.whoAmI);
        }
    }

    private static Vector2 GetFollowCenter(Vector2 center, Vector2 velocity, int direction)
    {
        float horizontalDirection = MathHelper.Distance(velocity.X, 0f) > 0.1f
            ? velocity.X < 0f ? -1f : 1f
            : direction == 0 ? 1f : direction;
        Vector2 offset = new(-horizontalDirection * FollowTargetDistance, FollowVerticalOffset);

        return center + offset;
    }
    #endregion
}
