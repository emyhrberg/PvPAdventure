using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.Map;
using PvPAdventure.Common.Spectator.Net;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

internal sealed class SpectatorSystem : ModSystem
{
    internal static readonly Dictionary<int, PlayerMode> Modes = [];

    private static int target = -1;

    public static PlayerMode GetMode(int slot) => Modes.TryGetValue(slot, out PlayerMode mode) ? mode : PlayerMode.Player;

    public static bool IsInSpectateMode(Player player)
    {
        return player?.active == true && GetMode(player.whoAmI) == PlayerMode.Spectator;
    }

    public static bool IsInPlayerMode(Player player) => player?.active == true && GetMode(player.whoAmI) == PlayerMode.Player;

    internal static PlayerMode GetJoinDefaultMode() => ModContent.GetInstance<SpectatorConfig>().ForceSpectateMode ? PlayerMode.Spectator : PlayerMode.Player;

    public static void RequestFullSync()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestFullSync();
    }

    public static void RequestSetPlayerMode(int slot) => RequestSetMode(slot, PlayerMode.Player);
    private static bool CanTarget(int slot)
    {
        return slot >= 0 &&
            slot < Main.maxPlayers &&
            slot != Main.myPlayer &&
            Main.player[slot].active &&
            IsInPlayerMode(Main.player[slot]) &&
            !Main.player[slot].ghost;
    }

    public static void SetPlayerTarget(int slot)
    {
        int next = CanTarget(slot) ? slot : -1;

        if (target != next)
            Log.Chat($"[Spectate] target {target}->{next}");

        target = next;
    }

    public static void ClearTarget()
    {
        if (target != -1)
            Log.Chat($"[Spectate] clear {target}");

        target = -1;
    }

    public static void RequestSetSpectatorMode(int slot) => RequestSetMode(slot, PlayerMode.Spectator);

    public static void RequestSetLocalMode(PlayerMode mode)
    {
        if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            return;

        SetModeLocal(Main.myPlayer, mode);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestSetMode(Main.myPlayer, mode);
    }

    public static void RequestSetMode(int slot, PlayerMode mode)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (slot == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            RequestSetLocalMode(mode);
            return;
        }

        if (Main.netMode == NetmodeID.Server)
            SetModeServer(slot, mode);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            SpectatorNetHandler.SendRequestSetMode(slot, mode);
        else
            SetModeLocal(slot, mode);
    }

    public static void SetModeServer(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.Server || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active || GetMode(slot) == mode)
            return;

        Modes[slot] = mode;
        SpectatorNetHandler.SendMode(slot, mode);
    }

    public static void SetModeLocal(int slot, PlayerMode mode)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        PlayerMode oldMode = GetMode(slot);
        Modes[slot] = mode;

        if (slot == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            if (oldMode != PlayerMode.Spectator && mode == PlayerMode.Spectator)
                MapRevealHelper.RevealLocalMap();

            if (oldMode == PlayerMode.Spectator && mode == PlayerMode.Player)
                MapRevealHelper.ClearLocalMap();
        }

        if (slot != Main.myPlayer || Main.netMode == NetmodeID.Server)
            return;

//#if !DEBUG
        Main.LocalPlayer.ghost = mode == PlayerMode.Spectator;
//#endif

        if (mode == PlayerMode.Spectator)
            Main.playerInventory = false;
        else
            ClearTarget();
    }

    public static void EnsureServerModes()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i].active && !Modes.ContainsKey(i))
                Modes[i] = GetJoinDefaultMode();
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i].active && i != exclude && IsInPlayerMode(Main.player[i]) && !Main.player[i].ghost)
                targets.Add(i);

        return targets;
    }
    public static bool IsTargeting(Player player) => player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    public static Player GetPlayerTarget()
    {
        if (!IsInSpectateMode(Main.LocalPlayer) || target < 0 || target >= Main.maxPlayers)
            return null;

        Player player = Main.player[target];
        return player.active && IsInPlayerMode(player) && !player.ghost ? player : null;
    }

    public static string GetCurrentTargetText()
    {
        if (GetPlayerTarget() is Player player)
            return $"Spectating: {player.name}";

        return GetTargets(Main.myPlayer).Count == 0 ? "No players to spectate" : "Spectate any player";
    }

    public static string GetTargetPanelTooltip()
    {
        if (GetPlayerTarget() is not null)
            return "Click to unspectate";

        return GetTargets(Main.myPlayer).Count == 0 ? "No players to spectate" : "Click to spectate any player";
    }


    public override void ModifyScreenPosition()
    {
        if (GetPlayerTarget() is Player player)
        {
            Vector2 screenPosition = player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            SpectateCameraFade.SetScreenPosition(screenPosition);
        }
    }

    public override void PreUpdatePlayers()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (!Main.player[i].active)
                Modes.Remove(i);

        if (Main.netMode == NetmodeID.Server)
        {
            EnsureServerModes();
            return;
        }

        Player local = Main.LocalPlayer;

//#if !DEBUG
        if (local?.active == true && GetMode(local.whoAmI) == PlayerMode.Spectator && !local.ghost)
            SetModeLocal(local.whoAmI, PlayerMode.Player);
//#endif

        if (!Main.gameMenu && GetPlayerTarget() is null)
            ClearTarget();
    }

    public override void OnWorldLoad() => Reset();

    public override void OnWorldUnload() => Reset();

    private static void Reset()
    {
        Modes.Clear();
        ClearTarget();
    }

    #region Cycle targets
    private static void CycleTarget(bool forward)
    {
        if (!IsInSpectateMode(Main.LocalPlayer))
            return;

        List<int> targets = GetTargets(Main.myPlayer);
        if (targets.Count == 0)
        {
            ClearTarget();
            return;
        }

        int index = targets.IndexOf(target);
        index = index < 0 ? (forward ? 0 : targets.Count - 1) : forward ? (index + 1) % targets.Count : (index - 1 + targets.Count) % targets.Count;
        target = targets[index];
    }
    public static void NextPlayerTarget() => CycleTarget(forward: true);

    public static void PreviousPlayerTarget() => CycleTarget(forward: false);

    #endregion
}

public class SpectatorPlayer : ModPlayer
{
    private int forceSpectatorDelayTicks;

    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
        if (spectatorConfig.ForceSpectateMode)
            forceSpectatorDelayTicks = 30;
    }

    public override void PostUpdate()
    {
        if (forceSpectatorDelayTicks <= 0)
            return;

        forceSpectatorDelayTicks--;

        if (forceSpectatorDelayTicks > 0)
            return;

        Log.Chat("Sending request to become a spectator");
        SpectatorNetHandler.SendRequestSetMode(Player.whoAmI, PlayerMode.Spectator);
    }
}