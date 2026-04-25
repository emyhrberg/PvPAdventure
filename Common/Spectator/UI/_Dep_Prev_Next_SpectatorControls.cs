//using Microsoft.Xna.Framework;
//using PvPAdventure.UI;
//using System;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ModLoader.UI;
//using Terraria.UI;

//namespace PvPAdventure.Common.Spectator.UI;

//internal sealed class SpectatorControls : UIElement
//{
//    private const float WidthPixels = 240f;
//    private const float CollapsedHeight = 80f;
//    private const float CollapsedTop = -100f;
//    private const float BottomPadding = 6f;

//    private const float TitleHeight = 30f;
//    private const float ControlHeight = 28f;
//    private const float ShowMoreTop = TitleHeight + ControlHeight;
//    private const float ShowMoreCollapsedHeight = CollapsedHeight - ShowMoreTop;

//    private readonly UIAutoScaleTextTextPanel<string> prevButton;
//    private readonly UIAutoScaleTextTextPanel<string> namePanel;
//    private readonly UIAutoScaleTextTextPanel<string> nextButton;

//    private readonly UIElement root;
//    private readonly UIPanel basePanel;
//    private readonly UIText titleText;
//    private readonly SpectatorShowMorePanel showMorePanel;
//    private readonly UIAutoScaleTextTextPanel<string> placeholderPanel;

//    public SpectatorControls()
//    {
//        Width.Set(WidthPixels, 0f);
//        Height.Set(CollapsedHeight, 0f);
//        HAlign = 0.5f;
//        VAlign = 1f;
//        Top.Set(CollapsedTop, 0f);
//        SetPadding(0f);

//        root = new UIElement();
//        root.Width.Set(WidthPixels, 0f);
//        root.Height.Set(CollapsedHeight, 0f);
//        root.SetPadding(0f);
//        Append(root);

//        Color backgroundColor = new Color(63, 82, 151) * 0.88f;
//        Color borderColor = new(89, 116, 213);

//        basePanel = new UIPanel();
//        basePanel.Width.Set(WidthPixels, 0f);
//        basePanel.Height.Set(ShowMoreTop, 0f);
//        basePanel.SetPadding(0f);
//        basePanel.BackgroundColor = backgroundColor;
//        basePanel.BorderColor = borderColor;
//        root.Append(basePanel);

//        titleText = new UIText("Spectate controls", 0.85f);
//        titleText.Width.Set(WidthPixels, 0f);
//        titleText.Height.Set(TitleHeight, 0f);
//        titleText.Left.Set(0f, 0f);
//        titleText.Top.Set(0f, 0f);
//        titleText.TextColor = Color.White;
//        root.Append(titleText);

//        prevButton = new UIAutoScaleTextTextPanel<string>("<", 0.75f);
//        prevButton.Width.Set(30f, 0f);
//        prevButton.Height.Set(ControlHeight, 0f);
//        prevButton.Left.Set(0f, 0f);
//        prevButton.Top.Set(TitleHeight, 0f);
//        prevButton.SetPadding(0f);
//        prevButton.UseInnerDimensions = true;
//        prevButton.BackgroundColor = backgroundColor;
//        prevButton.BorderColor = borderColor;
//        prevButton.TextColor = Color.White;
//        prevButton.OnLeftClick += (_, _) => SpectatorSystem.PreviousPlayerTarget();
//        root.Append(prevButton);

//        namePanel = new UIAutoScaleTextTextPanel<string>("", 0.85f);
//        namePanel.Width.Set(180f, 0f);
//        namePanel.Height.Set(ControlHeight, 0f);
//        namePanel.Left.Set(30f, 0f);
//        namePanel.Top.Set(TitleHeight, 0f);
//        namePanel.SetPadding(0f);
//        namePanel.UseInnerDimensions = true;
//        namePanel.BackgroundColor = backgroundColor;
//        namePanel.BorderColor = borderColor;
//        namePanel.TextColor = Color.White;
//        namePanel.OnLeftClick += (_, _) => SpectatorSystem.TogglePlayerTargetSelection();
//        root.Append(namePanel);

//        nextButton = new UIAutoScaleTextTextPanel<string>(">", 0.75f);
//        nextButton.Width.Set(30f, 0f);
//        nextButton.Height.Set(ControlHeight, 0f);
//        nextButton.Left.Set(210f, 0f);
//        nextButton.Top.Set(TitleHeight, 0f);
//        nextButton.SetPadding(0f);
//        nextButton.UseInnerDimensions = true;
//        nextButton.BackgroundColor = backgroundColor;
//        nextButton.BorderColor = borderColor;
//        nextButton.TextColor = Color.White;
//        nextButton.OnLeftClick += (_, _) => SpectatorSystem.NextPlayerTarget();
//        root.Append(nextButton);

//        showMorePanel = new SpectatorShowMorePanel(backgroundColor, borderColor);
//        showMorePanel.Width.Set(WidthPixels, 0f);
//        showMorePanel.Height.Set(ShowMoreCollapsedHeight, 0f);
//        showMorePanel.Top.Set(ShowMoreTop, 0f);
//        showMorePanel.SetPadding(0f);
//        root.Append(showMorePanel);

//        placeholderPanel = new UIAutoScaleTextTextPanel<string>("More spectator controls", 0.8f);
//        placeholderPanel.Width.Set(WidthPixels, 0f);
//        placeholderPanel.Height.Set(28f, 0f);
//        placeholderPanel.Top.Set(4f, 0f);
//        placeholderPanel.SetPadding(0f);
//        placeholderPanel.UseInnerDimensions = true;
//        placeholderPanel.BackgroundColor = backgroundColor;
//        placeholderPanel.BorderColor = borderColor;
//        placeholderPanel.TextColor = Color.White;
//        showMorePanel.Content.Append(placeholderPanel);
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);
//        ApplyHeight();

//        string prevText = "Spectate prev player: -";
//        string nextText = "Spectate next player: -";

//        List<int> targets = SpectatorSystem.GetTargets(Main.myPlayer);
//        Player current = SpectatorSystem.GetPlayerTarget();

//        Color activeText = Color.White;
//        Color inactiveText = new(160, 160, 160);

//        prevButton.TextColor = targets.Count > 0 ? activeText : inactiveText;
//        namePanel.TextColor = targets.Count > 0 ? activeText : inactiveText;
//        nextButton.TextColor = targets.Count > 0 ? activeText : inactiveText;

//        if (targets.Count > 0)
//        {
//            int index = current is null ? -1 : targets.IndexOf(current.whoAmI);
//            int prev = index < 0 ? targets.Count - 1 : (index - 1 + targets.Count) % targets.Count;
//            int next = index < 0 ? 0 : (index + 1) % targets.Count;

//            prevText = $"Spectate prev player: {Main.player[targets[prev]].name}";
//            nextText = $"Spectate next player: {Main.player[targets[next]].name}";
//        }

//        if (prevButton.IsMouseHovering)
//            Main.instance.MouseText(prevText);

//        if (nextButton.IsMouseHovering)
//            Main.instance.MouseText(nextText);

//        if (namePanel.IsMouseHovering)
//            Main.instance.MouseText(SpectatorSystem.GetTargetPanelTooltip());

//        if (IsMouseHovering)
//            Main.LocalPlayer.mouseInterface = true;
//    }

//    public void UpdateTarget()
//    {
//        namePanel.SetText(SpectatorSystem.GetCurrentTargetText(), 1f, false);
//    }

//    private void ApplyHeight()
//    {
//        float height = CollapsedHeight;
//        float top = CollapsedTop;
//        float showMoreHeight = ShowMoreCollapsedHeight;

//        if (showMorePanel.Expanded)
//        {
//            float collapsedScreenTop = Main.screenHeight - CollapsedHeight + CollapsedTop;
//            height = Math.Max(CollapsedHeight, Main.screenHeight - BottomPadding - collapsedScreenTop);
//            top = collapsedScreenTop - Main.screenHeight + height;
//            showMoreHeight = height - ShowMoreTop;
//        }

//        Top.Set(top, 0f);
//        Height.Set(height, 0f);
//        root.Height.Set(height, 0f);
//        showMorePanel.SetPanelHeight(showMoreHeight);
//        Recalculate();
//    }
//}

//internal sealed class SpectatorShowMorePanel : UIPanel
//{
//    private const float HeaderHeight = 22f;

//    private readonly UIElement header;
//    private readonly UIText label;

//    public UIElement Content { get; }
//    public bool Expanded { get; private set; }

//    public SpectatorShowMorePanel(Color backgroundColor, Color borderColor)
//    {
//        SetPadding(0f);
//        BackgroundColor = backgroundColor;
//        BorderColor = borderColor;

//        header = new UIElement();
//        header.Width.Set(0f, 1f);
//        header.Height.Set(HeaderHeight, 0f);
//        header.SetPadding(0f);
//        header.OnLeftClick += (_, _) => SetExpanded(!Expanded);
//        Append(header);

//        label = new UIText("Show more", 0.75f)
//        {
//            HAlign = 0.5f,
//            VAlign = 0.5f
//        };
//        header.Append(label);

//        Content = new UIElement();
//        Content.Width.Set(0f, 1f);
//        Content.Height.Set(0f, 1f);
//        Content.Top.Set(HeaderHeight, 0f);
//        Content.SetPadding(0f);
//    }

//    public void SetPanelHeight(float height)
//    {
//        Height.Set(height, 0f);
//        Content.Height.Set(Math.Max(0f, height - HeaderHeight), 0f);
//    }

//    private void SetExpanded(bool expanded)
//    {
//        Expanded = expanded;
//        label.SetText(Expanded ? "Show less" : "Show more");

//        if (Expanded && Content.Parent is null)
//            Append(Content);
//        else if (!Expanded)
//            Content.Remove();
//    }
//}