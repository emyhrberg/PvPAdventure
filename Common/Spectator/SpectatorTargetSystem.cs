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
    private const int FollowUpdateDelayTicks = 12;
    private const float FollowSnapDistance = 1200f;
    private const float FollowTargetDistance = 220f;
    private const float FollowVerticalOffset = -80f;
    private const float FollowLerp = 0.2f;

    private static int target = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;
    private static int followDelayTicks;

    #region Targeting
    private static bool CanTarget(int playerId)
    {
        return playerId >= 0 &&
            playerId < Main.maxPlayers &&
            playerId != Main.myPlayer &&
            Main.player[playerId].active &&
            (SpectatorModeSystem.IsInPlayerMode(Main.player[playerId]) || SpectatorModeSystem.IsInSpectateMode(Main.player[playerId]) || Main.player[playerId].ghost);
    }

    public static void SetPlayerTarget(int slot, bool preserveAutoDirector = false)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        int next = CanTarget(slot) ? slot : -1;

        if (target != next)
            Log.Chat($"target {target}->{next}");

        target = next;

        if (CanTarget(target))
            SnapLocalPlayerNear(Main.player[target]);
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

        if (target == -1)
            return;

        Log.Chat($"clear {target}");

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
        followDelayTicks = 0;
    }

    public static bool IsTargeting(Player player) => player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    public static bool IsLockedTargeting(Player player) => player?.active == true && CanTarget(target) && target == player.whoAmI;

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
    #endregion

    #region Hooks
    public override void ModifyScreenPosition()
    {
        if (GetPlayerTarget() is Player player)
        {
            Vector2 screenPosition = player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            bool targetChanged = cameraTarget != player.whoAmI;

            SpectateCameraFade.SetScreenPosition(screenPosition, targetChanged);
            cameraTarget = player.whoAmI;
            return;
        }

        cameraTarget = -1;
    }

    public override void PostUpdatePlayers()
    {
        Player targetPlayer = GetLockedPlayerTarget();
        if (targetPlayer?.active != true)
        {
            followDelayTicks = 0;
            return;
        }

        FollowLockedTarget(targetPlayer);
    }

    private static void FollowLockedTarget(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        if (followDelayTicks++ < FollowUpdateDelayTicks)
            return;

        followDelayTicks = 0;

        Vector2 desiredCenter = GetFollowCenter(targetPlayer);
        float distanceSquared = Vector2.DistanceSquared(local.Center, desiredCenter);

        if (distanceSquared > FollowSnapDistance * FollowSnapDistance)
            local.Center = desiredCenter;
        else
            local.Center = Vector2.Lerp(local.Center, desiredCenter, FollowLerp);

        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, local.whoAmI);
    }

    private static void SnapLocalPlayerNear(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        local.Center = GetFollowCenter(targetPlayer);
        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);
        followDelayTicks = 0;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, local.whoAmI);
    }

    private static Vector2 GetFollowCenter(Player targetPlayer)
    {
        float horizontalDirection = MathHelper.Distance(targetPlayer.velocity.X, 0f) > 0.1f
            ? targetPlayer.velocity.X < 0f ? -1f : 1f
            : targetPlayer.direction == 0 ? 1f : targetPlayer.direction;
        Vector2 offset = new(-horizontalDirection * FollowTargetDistance, FollowVerticalOffset);

        return targetPlayer.Center + offset;
    }
    #endregion
}
