using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.UI;

//ty jopojelly and darthmorf
public class UISearchbox : UIPanel
{
    internal string currentString = string.Empty;

    internal bool focused = false;

    private readonly int _maxLength = 20;

    private readonly string hintText;
    private int textBlinkerCount;
    private int textBlinkerState;
    public float Scale;

    public event Action OnFocus;

    public event Action OnUnfocus;

    public event Action OnTextChanged;

    public event Action OnTabPressed;

    public event Action OnEnterPressed;

    internal bool unfocusOnEnter = true;

    internal bool unfocusOnTab = true;

    private readonly ClearSearchButton clearSearchButton;
    private readonly Asset<Texture2D> clearSearchTexture = Main.Assets.Request<Texture2D>("Images/UI/SearchCancel");

    internal UISearchbox(string hintText="", string text = "")
    {
        this.hintText = hintText;
        currentString = text;
        SetPadding(0);
        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BackgroundColor = Color.White;
        BorderColor = Color.Black;

        clearSearchButton = new ClearSearchButton();
        clearSearchButton.HAlign = 1f;
        clearSearchButton.VAlign = 0.5f;
        clearSearchButton.Left.Set(-8f, 0f);
        clearSearchButton.Width.Set(20f, 0f);
        clearSearchButton.Height.Set(20f, 0f);
        clearSearchButton.OnLeftClick += (_, _) =>
        {
            SetText("");
            Focus();
        };

        Append(clearSearchButton);
        UpdateClearSearchButton();
        clearSearchButton.Recalculate();
    }

    private void UpdateClearSearchButton()
    {
        bool visible = !string.IsNullOrEmpty(currentString);

        clearSearchButton.Visible = visible;
        //clearSearchButton.IgnoresMouseInteraction = !visible;
    }

    public override bool ContainsPoint(Vector2 point)
    {
        bool isInPoint = base.ContainsPoint(point);

        if (isInPoint && Main.mouseLeft)
        {
            Main.mouseLeftRelease = false;
            Focus();
        }

        return isInPoint;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        if (GetClearSearchButtonBox().Contains(evt.MousePosition.ToPoint()))
        {
            SetText("");
            Focus();
            return;
        }

        Focus();
        base.LeftClick(evt);
    }

    internal void Unfocus()
    {
        if (focused)
        {
            focused = false;
            Main.blockInput = false;

            OnUnfocus?.Invoke();
        }
    }

    internal void Focus()
    {
        if (!focused)
        {
            Main.clrInput();
            focused = true;
            Main.blockInput = true;

            OnFocus?.Invoke();
        }
    }

    public override void Update(GameTime gameTime)
    {
        // if (IsMouseHovering)
        // {
        //     Log.Info("Mouse is hovering over searchbox");
        // }
        // else
        // {
        //     Log.Info("Not hovering over searchbox");
        // }

        Vector2 MousePosition = new(Main.mouseX, Main.mouseY);
        if (!ContainsPoint(MousePosition) && (Main.mouseLeft || Main.mouseRight)) //This solution is fine, but we need a way to cleanly "unload" a UIElement
        {
            //TODO, figure out how to refocus without triggering unfocus while clicking enable button.
            Unfocus();
        }
        base.Update(gameTime);
    }

    internal void SetText(string text)
    {
        if (text.Length > _maxLength)
        {
            text = text.Substring(0, _maxLength);
        }
        if (currentString != text)
        {
            currentString = text;
            UpdateClearSearchButton();
            OnTextChanged?.Invoke();
        }
    }

    private static bool JustPressed(Keys key)
    {
        return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
    }

    private Rectangle GetClearSearchButtonBox()
    {
        Rectangle box = GetDimensions().ToRectangle();
        const int size = 20;

        return new Rectangle(box.Right - size - 6, box.Y + (box.Height - size) / 2, size, size);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        // Panel background etc.
        base.DrawSelf(sb);

        if (focused)
        {
            Terraria.GameInput.PlayerInput.WritingText = true;
            Main.instance.HandleIME();

            string newString = Main.GetInputText(currentString);
            if (!string.Equals(newString, currentString))
                SetText(newString);

            if (JustPressed(Keys.Tab))
            {
                if (unfocusOnTab)
                    Unfocus();

                OnTabPressed?.Invoke();
            }

            if (JustPressed(Keys.Enter))
            {
                Main.drawingPlayerChat = false;

                if (unfocusOnEnter)
                    Unfocus();

                OnEnterPressed?.Invoke();
            }

            textBlinkerCount++;
            if (textBlinkerCount >= 20)
            {
                textBlinkerState = (textBlinkerState + 1) % 2;
                textBlinkerCount = 0;
            }

            Main.instance.DrawWindowsIMEPanel(new Vector2(98f, Main.screenHeight - 36), 0f);
        }

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 drawPos = GetDimensions().Position() + new Vector2(8f, 3f);
        float maxTextWidth = GetDimensions().Width - 34f;

        bool hasText = !string.IsNullOrEmpty(currentString);
        string textToDraw = hasText ? currentString : hintText;
        //string textToDraw = hasText ? StatDrawer.Truncate(font, currentString, maxTextWidth, 1f) : hintText;

        if (hasText) drawPos.X += 3f;

        // Cursor only when there is actual text
        //if (focused && textBlinkerState == 1 && hasText)
        if (focused && textBlinkerState == 1)
            textToDraw += "|";

        float scale = hasText ? 1f : 1.0f;
        Color innerColor = hasText ? Color.White : Color.DimGray;
        Color outlineColor = Color.Black;

        Vector2[] offsets =
        [
            new(-1f, -1f),
            new( 1f, -1f),
            new(-1f,  1f),
            new( 1f,  1f)
        ];

        // Outline
        for (int i = 0; i < offsets.Length; i++)
        {
            sb.DrawString(font,textToDraw,drawPos + offsets[i],outlineColor,0f,Vector2.Zero,scale,SpriteEffects.None,0f);
        }

        // Fill
        sb.DrawString(font,textToDraw,drawPos,innerColor,0f,Vector2.Zero, scale,SpriteEffects.None, 0f);
    }

    private sealed class ClearSearchButton : UIElement
    {
        private readonly Asset<Texture2D> texture = Main.Assets.Request<Texture2D>("Images/UI/SearchCancel");

        public bool Visible { get; set; }

        public override bool ContainsPoint(Vector2 point)
        {
            return Visible && base.ContainsPoint(point);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //if (!Visible)
                //return;

            Rectangle box = GetDimensions().ToRectangle();
            Color color = IsMouseHovering ? Color.White : Color.White * 0.75f;

            spriteBatch.Draw(texture.Value, box, color);
        }
    }

}