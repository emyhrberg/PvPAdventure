using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

public sealed class PVPScreenState : UIState
{
    // Title
    private UIText title;

    // Matchmaking button
    private UITextPanel<string> matchmakingButton;

    // Back button
    private UITextPanel<string> backButton;
    private readonly Action onBackButtonPressed;

    // Debug text
    private UIText debugText;

    // ----------------------------------------
    // Matchmaking variables
    private bool isQueuing = false;
    private int playersOnlineCount = 1;
    private int playersQueuingCount = 0;

    // Constructor
    public PVPScreenState(Action onBack)
    {
        onBackButtonPressed = onBack;
    }

    public override void OnInitialize()
    {
        // Title
        title = new UIText("PvP Adventure", 1.1f, true) { HAlign = 0.5f };
        title.Top.Set(200, 0f);
        Append(title);

        // Matchmaking button
        matchmakingButton = new UITextPanel<string>("Matchmaking", 0.9f, true);
        matchmakingButton.Width.Set(180f, 0f);
        matchmakingButton.Height.Set(44f, 0f);
        matchmakingButton.HAlign = 0.5f;
        matchmakingButton.Top.Set(320, 0f);
        matchmakingButton.DrawPanel = true;
        matchmakingButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
        matchmakingButton.BorderColor = Color.Black;
        matchmakingButton.OnLeftClick += (_, __) => onMatchmakingPressed();
        matchmakingButton.OnMouseOver += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            matchmakingButton.BorderColor = new Color(255, 240, 20);
        };
        matchmakingButton.OnMouseOut += (_, __) => matchmakingButton.BorderColor = Color.Black;
        Append(matchmakingButton);

        // Back button
        backButton = new UITextPanel<string>("Back", 0.9f, true);
        backButton.Width.Set(180f, 0f);
        backButton.Height.Set(44f, 0f);
        backButton.HAlign = 0.5f;
        backButton.Top.Set(700, 0);
        backButton.DrawPanel = true;
        backButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
        backButton.BorderColor = Color.Black;
        backButton.OnLeftClick += (_, __) => onBackButtonPressed?.Invoke();
        backButton.OnMouseOver += (_, __) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            backButton.BorderColor = new Color(255, 240, 20);
        };
        backButton.OnMouseOut += (_, __) => backButton.BorderColor = Color.Black;
        Append(backButton);

        // Debug text
        debugText = new("PvP Adventure Info", textScale: 1, large: false);
        debugText.Top.Set(255, 0);
        debugText.Left.Set(0, 0);
        debugText.HAlign = 0.5f;
        Append(debugText);

        // TODO: Initialize matchmaking variables from server
        // TODO: Send this client's matchmaking status to server
    }

    private void onMatchmakingPressed()
    {
        // Toggle flag
        isQueuing = !isQueuing;
        
        // Play sound
        SoundEngine.PlaySound(isQueuing ? SoundID.MenuClose : SoundID.MenuOpen);

        // Set text
        matchmakingButton.SetText(isQueuing ? "Cancel" : "Play Ranked");

        // Set players queuing count
        playersQueuingCount += isQueuing ? 1 : -1;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        // Debug text
        debugText.SetText(
            $"Players Online: {playersOnlineCount} \n" +
            $"Players Queuing: {playersQueuingCount} \n"
        );
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Main.hasFocus &&
            Main.keyState.IsKeyDown(Keys.Escape) &&
            !Main.oldKeyState.IsKeyDown(Keys.Escape))
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            onBackButtonPressed?.Invoke();
        }
    }
}
