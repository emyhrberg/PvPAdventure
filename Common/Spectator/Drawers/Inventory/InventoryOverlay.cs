using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System;
using Terraria;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

internal static class InventoryOverlay
{
    private static int playerIndex = -1;
    private static bool releaseInventory = true;

    // Hotfix to prevent logging the inventory disabled text if we closed settings menu with escape.
    private static bool optionsWindowWasOpen;

    public static void Update()
    {
        bool optionsWindowIsOpen = Main.ingameOptionsWindow;
        bool optionsWindowWasOpenLastFrame = optionsWindowWasOpen;
        optionsWindowWasOpen = optionsWindowIsOpen;

        Player local = Main.LocalPlayer;

        if (local?.active != true || !SpectatorModeSystem.IsInSpectateMode(local))
        {
            Clear();
            releaseInventory = true;
            return;
        }

        if (!local.controlInv)
        {
            releaseInventory = true;
            return;
        }

        if (!releaseInventory)
            return;

        releaseInventory = false;

        if (optionsWindowIsOpen || optionsWindowWasOpenLastFrame)
            return;

        Player target = SpectatorTargetSystem.GetPlayerTarget();

        if (target?.active != true)
        {
            Main.NewText("Inventory is disabled as a spectator.", Color.Yellow);
            return;
        }

        Toggle(target.whoAmI);
    }

    public static bool IsOpen(int targetPlayerIndex)
    {
        return IsValidPlayerIndex(targetPlayerIndex) && playerIndex == targetPlayerIndex;
    }

    public static void Toggle(int targetPlayerIndex)
    {
        if (!IsValidPlayerIndex(targetPlayerIndex))
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(targetPlayerIndex) ? -1 : targetPlayerIndex;
    }

    public static void Clear()
    {
        playerIndex = -1;
    }

    public static void Draw(SpriteBatch spriteBatch)
    {
        if (!IsValidPlayerIndex(playerIndex))
        {
            Clear();
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);
        InventoryDrawer.DrawInventory(spriteBatch, new Vector2(20f, 20f), Main.player[playerIndex], viewport);
    }

    private static bool IsValidPlayerIndex(int targetPlayerIndex)
    {
        return targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers && Main.player[targetPlayerIndex]?.active == true;
    }
}