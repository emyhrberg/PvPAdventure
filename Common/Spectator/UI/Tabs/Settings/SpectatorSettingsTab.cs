using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Hooks;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Spectator.UI.Tabs;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.Settings;

internal sealed class SpectatorSettingsTab : UIElement, ISpectatorTab
{
    private const float RowHeight = 28f;

    public SpectatorTab Tab => SpectatorTab.Settings;
    public string HeaderText => "Settings";
    public string TooltipText => "Spectator settings";
    public Asset<Texture2D> Icon => Ass.Icon_Refresh;

    public SpectatorSettingsTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);
    }

    public void Refresh() => Build();

    private void Build()
    {
        RemoveAllChildren();

        SettingField[] rows = GetRows();
        for (int i = 0; i < rows.Length; i++)
        {
            SettingTextRow row = new(rows[i]);
            row.Top.Set(i * RowHeight, 0f);
            row.Width.Set(0f, 1f);
            row.Height.Set(RowHeight, 0f);
            Append(row);
        }
    }

    private static SettingField[] GetRows()
    {
        return
        [
            new("Players online", () => SpectatorModeSystem.GetPlayersOnlineCount().ToString()),
            new("Spectators", () => SpectatorModeSystem.GetSpectatorCount().ToString()),
            new("Fullbright", () => OnOff(FloodlightSpectatorSystem.Enabled), () => FloodlightSpectatorSystem.Enabled = !FloodlightSpectatorSystem.Enabled),
            new("Reveal Map", () => OnOff(MapRevealHelper.Revealed), () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
            new("Draw Players", () => SpectatorClientSettings.DrawPlayersLabel, SpectatorClientSettings.CycleDrawPlayers),
            new(
                "Player Cards",
                () => SpectatorControlsPanel.ShownPlayerCardCount.ToString(),
                () => SpectatorControlsPanel.ChangeShownPlayerCards(1),
                () => SpectatorControlsPanel.ChangeShownPlayerCards(-1),
                "Left click: to increase\nRight click to decrease"),
            new("Auto Director", () => OnOff(AutoDirectorSystem.Enabled), () => AutoDirectorSystem.Enabled = !AutoDirectorSystem.Enabled)
        ];
    }

    private static string OnOff(bool value) => value ? "On" : "Off";

    private readonly struct SettingField
    {
        public readonly string Label;
        public readonly Func<string> GetValue;
        public readonly Action OnLeftClick;
        public readonly Action OnRightClick;
        public readonly string Tooltip;

        public SettingField(string label, Func<string> getValue, Action onLeftClick = null, Action onRightClick = null, string tooltip = null)
        {
            Label = label;
            GetValue = getValue;
            OnLeftClick = onLeftClick;
            OnRightClick = onRightClick;
            Tooltip = tooltip;
        }
    }

    private sealed class SettingTextRow : UIElement
    {
        private readonly SettingField field;
        private readonly UIText text;

        public SettingTextRow(SettingField field)
        {
            this.field = field;

            text = new UIText("", textScale: 0.85f)
            {
                HAlign = 0f,
                VAlign = 0.5f,
                Left = new StyleDimension(10f, 0f),
                TextColor = Color.Gray
            };

            Append(text);

            if (field.OnLeftClick is not null)
                OnLeftClick += (_, _) => field.OnLeftClick();

            if (field.OnRightClick is not null)
                OnRightClick += (_, _) => field.OnRightClick();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            text.SetText($"{field.Label}: {field.GetValue()}");
            text.TextColor = IsMouseHovering ? Color.White : Color.Gray;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;

                if (!string.IsNullOrEmpty(field.Tooltip))
                    Main.instance.MouseText(field.Tooltip);
            }
        }
    }
}
