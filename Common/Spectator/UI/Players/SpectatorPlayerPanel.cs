using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI.Players;

internal sealed class SpectatorPlayerPanel : UIBrowserPanel
{
    private readonly List<Player> debugPlayers = [];

    public SpectatorPlayerPanel() : base("Spectate")
    {
        Width.Set(560f, 0f);
        Height.Set(560f, 0f);
        HAlign = 0.32f;
        VAlign = 0.45f;
    }

    protected override float MinResizeH => base.MinResizeH+10;
    protected override float MaxResizeH => base.MaxResizeH;
    protected override float MinResizeW => base.MinResizeW;
    protected override float MaxResizeW => base.MaxResizeW;
    public override int ListMinEntrySize => 60;
    public override int ListMaxEntrySize => 132;
    protected override Asset<Texture2D> ActionPanelIconAsset => Ass.Icon_Eye;
    protected override string ActionPanelHoverText => "Open player spectate controls";

    protected override void OnActionPanelLeftClick()
    {
        SpectatorUISystem.TogglePlayerSpectatorControls();
    }

    protected override void PopulateEntries()
    {
        // 1. Add all active players in the server/world (Includes Main.LocalPlayer)
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];

            // Skip myself and other ghosts (spectators)
#if !DEBUG
            if (p.whoAmI == Main.myPlayer || p.ghost)
                continue;
#endif

            if (p.active)
            {
                AddEntry(new SpectatorPlayerEntry(p));
            }
        }

        // 2. Add our cloned debug players
        foreach (Player debugPlayer in debugPlayers)
        {
            AddEntry(new SpectatorPlayerEntry(debugPlayer));
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        PopulateDebugPlayers();
#endif
    }

    protected override List<UIBrowserSort> GetSorts()
    {
        return
        [
            new("Alphabetical", static (a, b) =>
        {
            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;
            return string.Compare(left.Player.name, right.Player.name, StringComparison.OrdinalIgnoreCase);
        }),

        new("Distance", static (a, b) =>
        {
            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;

            Player me = Main.LocalPlayer;
            float leftDistance = me?.active == true ? Vector2.DistanceSquared(me.Center, left.Player.Center) : float.MaxValue;
            float rightDistance = me?.active == true ? Vector2.DistanceSquared(me.Center, right.Player.Center) : float.MaxValue;

            int result = leftDistance.CompareTo(rightDistance);
            if (result != 0)
                return result;

            return string.Compare(left.Player.name, right.Player.name, StringComparison.OrdinalIgnoreCase);
        }),

        new("Team", static (a, b) =>
        {
            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;

            int result = left.TeamSortValue.CompareTo(right.TeamSortValue);
            if (result != 0)
                return result;

            return string.Compare(left.Player.name, right.Player.name, StringComparison.OrdinalIgnoreCase);
        }),

        new("Biome", static (a, b) =>
        {
            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;

            int result = left.BiomeSortValue.CompareTo(right.BiomeSortValue);
            if (result != 0)
                return result;

            return string.Compare(left.Player.name, right.Player.name, StringComparison.OrdinalIgnoreCase);
        })
        ];
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        SpectatorPlayerEntry.DrawSelectedInventory(spriteBatch);
    }

    private void PopulateDebugPlayers()
    {
        string GetNextDebugName()
        {
            int count = debugPlayers.Count + 1; // +1 because we haven't added yet
            if (count <= 9)
            {
                string seq = "";
                for (int i = 1; i <= count; i++)
                    seq += i;
                return $"Debug{seq}";
            }
            return $"Debug{Main.rand.Next(10000, 99999999)}";
        }

        // Numpad 1: Clone the local player and add them to the debug list
        if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
        {
            Player clonedPlayer = (Player)Main.LocalPlayer.Clone();
            clonedPlayer.name = GetNextDebugName();

            debugPlayers.Add(clonedPlayer);
            Log.Chat($"Added debug player: {clonedPlayer.name}");

            ForceRepopulate();
        }

        // Numpad 2: Remove the most recently added debug player
        if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
        {
            if (debugPlayers.Count > 0)
            {
                string removedName = debugPlayers[^1].name;
                debugPlayers.RemoveAt(debugPlayers.Count - 1);

                Log.Chat($"Removed debug player: {removedName}");

                ForceRepopulate();
            }
        }
    }

    private void ForceRepopulate()
    {
        entries.Clear();
        PopulateEntries();
        RefreshEntries();
    }
}
