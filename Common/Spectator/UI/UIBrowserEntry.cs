using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal class UIBrowserEntry : UIElement
{
    internal static Rectangle? DrawClip;
    internal static bool MouseInsideDrawClip => DrawClip is not Rectangle clip || clip.Contains(Main.MouseScreen.ToPoint());

    public string SearchText;

    protected bool listMode = true;
    protected int entrySize = 80;

    public UIBrowserEntry()
    {
        listMode = true;
        Width.Set(0f, 1f);
        Height.Set(entrySize, 0f);
    }

    public virtual void SetListMode(bool value)
    {
        listMode = value;

        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);
        }
        else
        {
            Width.Set(entrySize, 0f);
            Height.Set(entrySize, 0f);
        }

        Recalculate();
    }

    public virtual void SetEntrySize(int size)
    {
        entrySize = size;

        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);
        }
        else
        {
            Width.Set(entrySize * 2.2f, 0f);
            Height.Set(entrySize * 1.4f, 0f);
        }

        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (DrawClip is Rectangle clip && !GetDimensions().ToRectangle().Intersects(clip))
            return;

        base.Draw(spriteBatch);
    }
}

internal sealed class UIClippedGrid : UIGrid
{
    private static readonly RasterizerState ScissorRasterizer = new()
    {
        CullMode = CullMode.CullCounterClockwiseFace,
        ScissorTestEnable = true
    };

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle drawClip = GetDimensions().ToRectangle();
        if (drawClip.Width <= 0 || drawClip.Height <= 0)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        Rectangle oldScissor = device.ScissorRectangle;
        RasterizerState oldRasterizer = device.RasterizerState;

        Rectangle scissor = GetClippingRectangle(spriteBatch);
        Rectangle screen = new(0, 0, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight);
        scissor = Rectangle.Intersect(scissor, screen);

        if (oldRasterizer?.ScissorTestEnable == true)
            scissor = Rectangle.Intersect(scissor, oldScissor);

        if (scissor.Width <= 0 || scissor.Height <= 0)
            return;

        Rectangle? oldDrawClip = UIBrowserEntry.DrawClip;
        bool oldOverflowHidden = OverflowHidden;
        UIBrowserEntry.DrawClip = drawClip;
        // This grid owns the scissor region; avoid UIElement's nested clipping pass.
        OverflowHidden = false;

        spriteBatch.End();
        device.ScissorRectangle = scissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, ScissorRasterizer, null, Main.UIScaleMatrix);

        try
        {
            base.Draw(spriteBatch);
        }
        finally
        {
            UIBrowserEntry.DrawClip = oldDrawClip;
            OverflowHidden = oldOverflowHidden;
            spriteBatch.End();
            device.ScissorRectangle = oldScissor;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, oldRasterizer ?? RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        }
    }
}

internal sealed class UIBrowserSizeSlider : UISlider
{
    public UIBrowserSizeSlider(UIBrowser owner)
    {
        OnDraw += _ =>
        {
            if (IsMouseHovering)
            {
                string tooltip = $"Button size: {(int)MathHelper.Lerp(owner.MinEntrySize, owner.MaxEntrySize, Ratio)}";
                UICommon.TooltipMouseText(tooltip);
                //Main.instance.MouseText(tooltip);
            }
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}

internal sealed class UIBrowserViewToggle : UIColoredImageButton
{
    private readonly Func<bool> getListMode;

    public UIBrowserViewToggle(Asset<Texture2D> texture, bool isSmall, Func<bool> getListMode) : base(texture, isSmall)
    {
        this.getListMode = getListMode;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (IsMouseHovering)
            UICommon.TooltipMouseText(getListMode() ? "List mode\nClick to switch to grid mode" : "Grid mode\nClick to switch to list mode");
    }
}

internal sealed class UIBrowserSortButton : UIColoredImageButton
{
    private readonly Func<string> getText;

    public UIBrowserSortButton(Asset<Texture2D> texture, bool isSmall, Func<string> getText) : base(texture, isSmall)
    {
        this.getText = getText;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (IsMouseHovering)
            UICommon.TooltipMouseText($"{getText()}");
    }
}

internal sealed class UIBrowserSort
{
    public string Name { get; }
    public Comparison<UIBrowserEntry> Compare { get; }

    public UIBrowserSort(string name, Comparison<UIBrowserEntry> compare)
    {
        Name = name;
        Compare = compare;
    }
}
