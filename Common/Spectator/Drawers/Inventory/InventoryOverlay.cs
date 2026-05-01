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
        //bool optionsWindowIsOpen = Main.ingameOptionsWindow;
        //bool optionsWindowWasOpenLastFrame = optionsWindowWasOpen;
        //optionsWindowWasOpen = optionsWindowIsOpen;
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

        Player target = SpectatorTargetSystem.GetPlayerTarget();

        if (target?.active != true)
        {
            Main.NewText("Inventory is disabled as a spectator.", Color.Yellow);
            return;
        }

        Toggle(target);
    }

    public static bool IsOpen(Player target)
    {
        return target?.active == true && playerIndex == target.whoAmI;
    }

    public static bool IsOpen(int targetPlayerIndex)
    {
        return IsValidPlayerIndex(targetPlayerIndex) && playerIndex == targetPlayerIndex;
    }

    public static void Toggle(Player target)
    {
        if (target?.active != true)
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(target) ? -1 : target.whoAmI;
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
        Player target = GetDrawTarget();

        if (target?.active != true)
        {
            Clear();
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);

        if (IsOpen(target))
        {
            InventoryDrawer.DrawInventory(spriteBatch, new Vector2(20f, 20f), target, viewport);
            return;
        }

        DrawHotbarPlaceholder(spriteBatch, target);
    }

    private static Player GetDrawTarget()
    {
        if (IsValidPlayerIndex(playerIndex))
            return Main.player[playerIndex];

        return SpectatorTargetSystem.GetPlayerTarget();
    }

    private static void DrawHotbarPlaceholder(SpriteBatch spriteBatch, Player target)
    {
        string text = $"Hotbar: {target.name}";
        Vector2 position = new(20f, 20f);

        Utils.DrawBorderString(spriteBatch, text, position, Color.White, 1f);
    }

    private static bool IsValidPlayerIndex(int targetPlayerIndex)
    {
        return targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers && Main.player[targetPlayerIndex]?.active == true;
    }
}
