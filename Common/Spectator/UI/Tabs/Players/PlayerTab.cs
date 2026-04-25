using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.Players;

internal sealed class PlayerTab : UIElement, ISpectatorTab
{
    private readonly List<Player> debugPlayers = [];
    private readonly UIBrowser browser;

    public SpectatorTab Tab => SpectatorTab.Player;
    public string Label => "Player";
    public Asset<Texture2D> Icon => Ass.Icon_Player;

    public PlayerTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);

        browser = new UIBrowser(PopulateEntries, GetSorts)
        {
            ListMinEntrySize = 60,
            ListMaxEntrySize = 132
        };

        browser.Width.Set(0f, 1f);
        browser.Height.Set(0f, 1f);

        Append(browser);
    }

    public void Refresh()
    {
        browser.Rebuild();
    }

    public void OnAction()
    {
        SpectatorUISystem.TogglePlayerSpectatorControls();
    }

    public void DrawOverlay(SpriteBatch spriteBatch)
    {
        SpectatorPlayerEntry.DrawSelectedInventory(spriteBatch);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        PopulateDebugPlayers();
#endif
    }

    private void PopulateEntries(List<UIBrowserEntry> entries)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

#if !DEBUG
            if (player.whoAmI == Main.myPlayer || player.ghost)
                continue;
#endif

            if (player.active)
                entries.Add(new SpectatorPlayerEntry(player));
        }

        foreach (Player debugPlayer in debugPlayers)
            entries.Add(new SpectatorPlayerEntry(debugPlayer));
    }

    private List<UIBrowserSort> GetSorts()
    {
        return [];
    }

    private void PopulateDebugPlayers()
    {
        string GetNextDebugName()
        {
            int count = debugPlayers.Count + 1;

            if (count <= 9)
            {
                string seq = "";

                for (int i = 1; i <= count; i++)
                    seq += i;

                return $"Debug{seq}";
            }

            return $"Debug{Main.rand.Next(10000, 99999999)}";
        }

        if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
        {
            Player clonedPlayer = (Player)Main.LocalPlayer.Clone();
            clonedPlayer.name = GetNextDebugName();

            debugPlayers.Add(clonedPlayer);
            Log.Chat($"Added debug player: {clonedPlayer.name}");

            Refresh();
        }

        if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
        {
            if (debugPlayers.Count <= 0)
                return;

            string removedName = debugPlayers[^1].name;
            debugPlayers.RemoveAt(debugPlayers.Count - 1);

            Log.Chat($"Removed debug player: {removedName}");

            Refresh();
        }
    }
}