using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorPanelTabButton : UIPanel
{
    private readonly Func<bool> isSelected;
    private readonly string hoverText;

    public SpectatorPanelTabButton(string text, Asset<Texture2D> icon, Func<bool> isSelected, Action onClick)
    {
        this.isSelected = isSelected;
        hoverText = text;

        Width.Set(76f, 0f);
        Height.Set(0f, 1f);
        VAlign = 0.5f;
        SetPadding(0f);

        OnLeftClick += (_, _) => onClick();
        OnMouseOver += (_, _) => BorderColor = Color.Yellow;
        OnMouseOut += (_, _) => BorderColor = GetBorderColor();

        UIImage image = new(icon.Value)
        {
            Left = new StyleDimension(6f, 0f),
            VAlign = 0.5f,
            Width = new StyleDimension(22f, 0f),
            Height = new StyleDimension(22f, 0f)
        };
        Append(image);

        UIText label = new(text, textScale: 0.72f)
        {
            Left = new StyleDimension(31f, 0f),
            VAlign = 0.5f
        };
        Append(label);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        BackgroundColor = isSelected() ? new Color(83, 97, 168) : new Color(63, 82, 151) * 0.85f;
        BorderColor = IsMouseHovering ? Color.Yellow : GetBorderColor();

        if (IsMouseHovering)
            Main.instance.MouseText(hoverText);
    }

    private Color GetBorderColor() => isSelected() ? Color.White : Color.Black;
}
