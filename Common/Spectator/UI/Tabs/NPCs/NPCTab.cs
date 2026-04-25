using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.NPCs;

internal sealed class NPCTab : UIElement, ISpectatorTab
{
    private readonly UIBrowser browser;

    public SpectatorTab Tab => SpectatorTab.NPCs;
    public string Label => "NPCs";
    public Asset<Texture2D> Icon => Ass.Icon_NPCs;
    public string ActionHoverText => null;
    public bool HasAction => false;

    public NPCTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);

        browser = new UIBrowser(PopulateEntries, GetSorts);
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
    }

    public void DrawOverlay(SpriteBatch spriteBatch)
    {
    }

    private void PopulateEntries(List<UIBrowserEntry> entries)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (npc == null || !npc.active)
                continue;

            entries.Add(new SpectatorNPCEntry(npc));
        }
    }

    private List<UIBrowserSort> GetSorts()
    {
        return [];
    }
}