using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Hooks;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorSettingsPanel : UIElement
{
    private const float PanelWidth = 240f;
    private const float HeaderHeight = 32f;
    private const float RowHeight = 28f;
    private const float TopOffset = 335f;
    private const float RightOffset = 0f;

    public UIPanel TitlePanel;
    public UIPanel ContentPanel;
    public UIPanel EyeTogglePanel;

    private bool expanded = true;

    public SpectatorSettingsPanel()
    {
        HAlign = 1f;
        Left.Set(-RightOffset, 0f);
        Top.Set(TopOffset, 0f);
        Width.Set(PanelWidth, 0f);

        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();

        TitlePanel = null;
        ContentPanel = null;
        EyeTogglePanel = null;

        Height.Set(expanded ? HeaderHeight + GetRows().Length * RowHeight : HeaderHeight, 0f);

        TitlePanel = new UIPanel();
        TitlePanel.Height.Set(HeaderHeight, 0f);
        TitlePanel.Width.Set(0f, 1f);
        TitlePanel.SetPadding(0f);
        TitlePanel.BackgroundColor = new Color(63, 82, 151);
        TitlePanel.BorderColor = Color.Black;

        UIText titleText = new("Spectator", large: true, textScale: 0.7f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        TitlePanel.Append(titleText);

        EyeTogglePanel = new UIPanel
        {
            Height = new StyleDimension(0f, 1f),
            Width = new StyleDimension(HeaderHeight, 0f),
            HAlign = 1f,
            VAlign = 0.5f
        };
        EyeTogglePanel.SetPadding(0f);
        EyeTogglePanel.OnLeftClick += (_, _) =>
        {
            expanded = !expanded;
            SoundEngine.PlaySound(expanded ? SoundID.MenuOpen : SoundID.MenuClose);
            Rebuild();
        };
        EyeTogglePanel.OnMouseOver += (_, _) => EyeTogglePanel.BorderColor = Color.Yellow;
        EyeTogglePanel.OnMouseOut += (_, _) => EyeTogglePanel.BorderColor = Color.Black;
        EyeTogglePanel.Append(new UIImage(Ass.Icon_Eye.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });

        TitlePanel.Append(EyeTogglePanel);
        Append(TitlePanel);

        if (!expanded)
            return;

        ContentPanel = new UIPanel
        {
            Top = new StyleDimension(HeaderHeight, 0f),
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(-HeaderHeight, 1f),
            BackgroundColor = new Color(20, 20, 60) * 0.7f,
            BorderColor = Color.Black
        };
        ContentPanel.SetPadding(0f);

        SettingField[] rows = GetRows();
        for (int i = 0; i < rows.Length; i++)
        {
            SettingTextRow row = new(rows[i]);
            row.Top.Set(i * RowHeight, 0f);
            row.Width.Set(0f, 1f);
            row.Height.Set(RowHeight, 0f);
            ContentPanel.Append(row);
        }

        Append(ContentPanel);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
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
