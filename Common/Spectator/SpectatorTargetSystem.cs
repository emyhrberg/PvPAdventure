using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
public class SpectatorTargetSystem : ModSystem
{
    private static int target = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;

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
    #endregion
}
