using Microsoft.Xna.Framework;
using PvPAdventure.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorControls : UIElement
{
    private readonly UIAutoScaleTextTextPanel<string> prevButton;
    private readonly UIAutoScaleTextTextPanel<string> namePanel;
    private readonly UIAutoScaleTextTextPanel<string> nextButton;

    public SpectatorControls()
    {
        Width.Set(300f, 0f);
        Height.Set(36f, 0f);
        HAlign = 0.5f;
        VAlign = 1f;
        Top.Set(-56f, 0f);

        UIElement root = new();
        root.Width.Set(300f, 0f);
        root.Height.Set(36f, 0f);
        Append(root);

        Color backgroundColor = new Color(63, 82, 151) * 0.88f;
        Color borderColor = new(89, 116, 213);

        prevButton = new UIAutoScaleTextTextPanel<string>("<", 0.8f);
        prevButton.Width.Set(36f, 0f);
        prevButton.Height.Set(36f, 0f);
        prevButton.Left.Set(0f, 0f);
        prevButton.SetPadding(0f);
        prevButton.UseInnerDimensions = true;
        prevButton.BackgroundColor = backgroundColor;
        prevButton.BorderColor = borderColor;
        prevButton.TextColor = Color.White;
        prevButton.OnLeftClick += (_, _) => SpectatorSystem.PreviousPlayerTarget();
        root.Append(prevButton);

        namePanel = new UIAutoScaleTextTextPanel<string>("", 1);
        namePanel.Width.Set(220f, 0f);
        namePanel.Height.Set(36f, 0f);
        namePanel.Left.Set(40f, 0f);
        namePanel.SetPadding(0f);
        namePanel.UseInnerDimensions = true;
        namePanel.BackgroundColor = backgroundColor;
        namePanel.BorderColor = borderColor;
        namePanel.TextColor = Color.White;
        namePanel.OnLeftClick += (_, _) => SpectatorSystem.TogglePlayerTargetSelection();
        root.Append(namePanel);

        nextButton = new UIAutoScaleTextTextPanel<string>(">", 0.8f);
        nextButton.Width.Set(36f, 0f);
        nextButton.Height.Set(36f, 0f);
        nextButton.Left.Set(264f, 0f);
        nextButton.SetPadding(0f);
        nextButton.UseInnerDimensions = true;
        nextButton.BackgroundColor = backgroundColor;
        nextButton.BorderColor = borderColor;
        nextButton.TextColor = Color.White;
        nextButton.OnLeftClick += (_, _) => SpectatorSystem.NextPlayerTarget();
        root.Append(nextButton);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        string prevText = "Spectate prev player: -";
        string nextText = "Spectate next player: -";

        List<int> targets = SpectatorSystem.GetTargets(Main.myPlayer);
        Player current = SpectatorSystem.GetPlayerTarget();

        Color activeText = Color.White;
        Color inactiveText = new(160, 160, 160);

        prevButton.TextColor = targets.Count > 0 ? activeText : inactiveText;
        namePanel.TextColor = targets.Count > 0 ? activeText : inactiveText;
        nextButton.TextColor = targets.Count > 0 ? activeText : inactiveText;

        if (targets.Count > 0)
        {
            int index = current is null ? -1 : targets.IndexOf(current.whoAmI);
            int prev = index < 0 ? targets.Count - 1 : (index - 1 + targets.Count) % targets.Count;
            int next = index < 0 ? 0 : (index + 1) % targets.Count;

            prevText = $"Spectate prev player: {Main.player[targets[prev]].name}";
            nextText = $"Spectate next player: {Main.player[targets[next]].name}";
        }

        if (prevButton.IsMouseHovering)
            Main.instance.MouseText(prevText);

        if (nextButton.IsMouseHovering)
            Main.instance.MouseText(nextText);

        if (namePanel.IsMouseHovering)
            Main.instance.MouseText(SpectatorSystem.GetTargetPanelTooltip());

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public void UpdateTarget()
    {
        namePanel.SetText(SpectatorSystem.GetCurrentTargetText(), 1, false);
    }
}