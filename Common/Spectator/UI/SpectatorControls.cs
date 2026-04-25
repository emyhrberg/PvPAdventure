using Microsoft.Xna.Framework;
using PvPAdventure.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorControls : UIElement
{
    private readonly UIAutoScaleTextTextPanel<string> prevButton;
    private readonly UIAutoScaleTextTextPanel<string> namePanel;
    private readonly UIAutoScaleTextTextPanel<string> nextButton;

    private readonly UIElement root;
    private readonly UIPanel basePanel;
    private readonly UIExpandablePanel showMorePanel;
    private readonly UIElement expandedContent;
    private readonly UIAutoScaleTextTextPanel<string> placeholderPanel;

    private bool showMoreExpanded;

    public SpectatorControls()
    {
        Width.Set(240f, 0f);
        Height.Set(80f, 0f);
        HAlign = 0.5f;
        VAlign = 1f;
        Top.Set(-100f, 0f);
        SetPadding(0f);

        root = new UIElement();
        root.Width.Set(240f, 0f);
        root.Height.Set(80f, 0f);
        root.SetPadding(0f);
        Append(root);

        Color backgroundColor = new Color(63, 82, 151) * 0.88f;
        Color borderColor = new(89, 116, 213);

        basePanel = new UIPanel();
        basePanel.Width.Set(240f, 0f);
        basePanel.Height.Set(50f, 0f);
        basePanel.SetPadding(0f);
        basePanel.BackgroundColor = backgroundColor;
        basePanel.BorderColor = borderColor;
        root.Append(basePanel);

        prevButton = new UIAutoScaleTextTextPanel<string>("<", 0.8f);
        prevButton.Width.Set(36f, 0f);
        prevButton.Height.Set(50f, 0f);
        prevButton.Left.Set(0f, 0f);
        prevButton.SetPadding(0f);
        prevButton.UseInnerDimensions = true;
        prevButton.BackgroundColor = backgroundColor;
        prevButton.BorderColor = borderColor;
        prevButton.TextColor = Color.White;
        prevButton.OnLeftClick += (_, _) => SpectatorSystem.PreviousPlayerTarget();
        root.Append(prevButton);

        namePanel = new UIAutoScaleTextTextPanel<string>("", 1f);
        namePanel.Width.Set(168f, 0f);
        namePanel.Height.Set(50f, 0f);
        namePanel.Left.Set(36f, 0f);
        namePanel.SetPadding(0f);
        namePanel.UseInnerDimensions = true;
        namePanel.BackgroundColor = backgroundColor;
        namePanel.BorderColor = borderColor;
        namePanel.TextColor = Color.White;
        namePanel.OnLeftClick += (_, _) => SpectatorSystem.TogglePlayerTargetSelection();
        root.Append(namePanel);

        nextButton = new UIAutoScaleTextTextPanel<string>(">", 0.8f);
        nextButton.Width.Set(36f, 0f);
        nextButton.Height.Set(50f, 0f);
        nextButton.Left.Set(204f, 0f);
        nextButton.SetPadding(0f);
        nextButton.UseInnerDimensions = true;
        nextButton.BackgroundColor = backgroundColor;
        nextButton.BorderColor = borderColor;
        nextButton.TextColor = Color.White;
        nextButton.OnLeftClick += (_, _) => SpectatorSystem.NextPlayerTarget();
        root.Append(nextButton);

        showMorePanel = new UIExpandablePanel();
        showMorePanel.Width.Set(240f, 0f);
        showMorePanel.Height.Set(30f, 0f);
        showMorePanel.Top.Set(50f, 0f);
        showMorePanel.SetPadding(0f);
        showMorePanel.BackgroundColor = backgroundColor;
        showMorePanel.BorderColor = borderColor;
        showMorePanel.OnExpanded += () => showMoreExpanded = true;
        showMorePanel.OnCollapsed += () => showMoreExpanded = false;
        root.Append(showMorePanel);

        expandedContent = new UIElement();
        expandedContent.Width.Set(240f, 0f);
        expandedContent.Height.Set(70f, 0f);
        expandedContent.Top.Set(30f, 0f);
        expandedContent.SetPadding(0f);
        showMorePanel.VisibleWhenExpanded.Add(expandedContent);

        placeholderPanel = new UIAutoScaleTextTextPanel<string>("More spectator controls", 0.8f);
        placeholderPanel.Width.Set(240f, 0f);
        placeholderPanel.Height.Set(28f, 0f);
        placeholderPanel.SetPadding(0f);
        placeholderPanel.UseInnerDimensions = true;
        placeholderPanel.BackgroundColor = backgroundColor;
        placeholderPanel.BorderColor = borderColor;
        placeholderPanel.TextColor = Color.White;
        expandedContent.Append(placeholderPanel);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float height = showMoreExpanded ? 150f : 80f;

        Height.Set(height, 0f);
        root.Height.Set(height, 0f);
        expandedContent.IgnoresMouseInteraction = !showMoreExpanded;

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