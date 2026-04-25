using Microsoft.Xna.Framework;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System;
using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class UIBrowser : UIElement
{
    private readonly Action<List<UIBrowserEntry>> populateEntries;
    private readonly Func<List<UIBrowserSort>> getSorts;

    private UIGrid grid;
    private UIScrollbar scrollbar;
    private UIBrowserViewToggle viewToggle;
    private UIElement headerRow;
    private UISearchbox searchbox;
    private BrowserSizeSlider sizeSlider;
    private UIBrowserSortButton sortButton;

    private readonly List<UIBrowserEntry> entries = [];
    private readonly List<UIBrowserSort> sorts = [];

    private bool listMode = true;
    private int sortIndex;
    private int listEntrySize = 100;
    private int gridEntrySize = 64;

    public int ListMinEntrySize { get; set; } = 64;
    public int ListMaxEntrySize { get; set; } = 128;
    public int GridMinEntrySize { get; set; } = 40;
    public int GridMaxEntrySize { get; set; } = 130;

    public int MinEntrySize => listMode ? ListMinEntrySize : GridMinEntrySize;
    public int MaxEntrySize => listMode ? ListMaxEntrySize : GridMaxEntrySize;

    private string CurrentSortText => sorts.Count > 0 ? $"Sort: {sorts[sortIndex].Name}" : "Sort";

    private int CurrentEntrySize
    {
        get => listMode ? listEntrySize : gridEntrySize;
        set
        {
            if (listMode)
                listEntrySize = Math.Clamp(value, ListMinEntrySize, ListMaxEntrySize);
            else
                gridEntrySize = Math.Clamp(value, GridMinEntrySize, GridMaxEntrySize);
        }
    }

    public UIBrowser(Action<List<UIBrowserEntry>> populateEntries, Func<List<UIBrowserSort>> getSorts = null)
    {
        this.populateEntries = populateEntries;
        this.getSorts = getSorts;

        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);

        listEntrySize = Math.Clamp(listEntrySize, ListMinEntrySize, ListMaxEntrySize);
        gridEntrySize = Math.Clamp(gridEntrySize, GridMinEntrySize, GridMaxEntrySize);

        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();

        headerRow = new UIElement();
        headerRow.Left.Set(8f, 0f);
        headerRow.Top.Set(8f, 0f);
        headerRow.Width.Set(-16f, 1f);
        headerRow.Height.Set(32f, 0f);
        headerRow.SetPadding(0f);
        Append(headerRow);

        searchbox = new UISearchbox("");
        searchbox.OnTextChanged += RefreshEntries;
        searchbox.Left.Set(0f, 0f);
        searchbox.Top.Set(0f, 0f);
        searchbox.Width.Set(220f, 0f);
        searchbox.Height.Set(32f, 0f);
        headerRow.Append(searchbox);

        viewToggle = new UIBrowserViewToggle(listMode ? Ass.List : Ass.Grid, true, () => listMode);
        viewToggle.Left.Set(240f, 0f);
        viewToggle.Top.Set(0f, 0f);
        viewToggle.SetVisibility(1f, 1f);
        viewToggle.OnLeftClick += (_, _) =>
        {
            listMode = !listMode;
            CurrentEntrySize = CurrentEntrySize;
            viewToggle.SetImageWithoutSettingSize(listMode ? Ass.List : Ass.Grid);

            if (sizeSlider != null)
                sizeSlider.Ratio = (CurrentEntrySize - MinEntrySize) / (float)(MaxEntrySize - MinEntrySize);

            RefreshEntries();
        };
        headerRow.Append(viewToggle);

        sizeSlider = new BrowserSizeSlider(this);
        sizeSlider.Left.Set(290f, 0f);
        sizeSlider.Top.Set(8f, 0f);
        sizeSlider.Width.Set(104f, 0f);
        sizeSlider.Height.Set(16f, 0f);
        sizeSlider.Ratio = (CurrentEntrySize - MinEntrySize) / (float)(MaxEntrySize - MinEntrySize);
        sizeSlider.OnDrag += SetEntrySizeFromSlider;
        headerRow.Append(sizeSlider);

        sorts.Clear();
        sorts.AddRange(getSorts?.Invoke() ?? []);

        if (sorts.Count > 0)
        {
            sortIndex = Math.Clamp(sortIndex, 0, sorts.Count - 1);

            sortButton = new UIBrowserSortButton(Ass.Sort, true, () => CurrentSortText);
            sortButton.Left.Set(402f, 0f);
            sortButton.Top.Set(0f, 0f);
            sortButton.SetVisibility(1f, 1f);
            sortButton.OnLeftClick += (_, _) =>
            {
                if (sorts.Count == 0)
                    return;

                sortIndex = (sortIndex + 1) % sorts.Count;
                RefreshEntries();
            };
            headerRow.Append(sortButton);
        }

        scrollbar = new UIScrollbar();
        scrollbar.Left.Set(-30f, 1f);
        scrollbar.Top.Set(48f, 0f);
        scrollbar.Height.Set(-80f, 1f);
        Append(scrollbar);

        grid = new UIClippedGrid();
        grid.Left.Set(10f, 0f);
        grid.Top.Set(50f, 0f);
        grid.Width.Set(-50f, 1f);
        grid.Height.Set(-80f, 1f);
        grid.ListPadding = 4f;
        grid.SetScrollbar(scrollbar);
        grid.OverflowHidden = true;
        Append(grid);

        entries.Clear();
        populateEntries?.Invoke(entries);

        RefreshEntries();
        Recalculate();
    }

    public void RefreshEntries()
    {
        if (grid is null)
            return;

        grid.Clear();

        string search = searchbox?.currentString?.Trim() ?? string.Empty;

        List<UIBrowserEntry> visibleEntries = [];

        foreach (UIBrowserEntry entry in entries)
        {
            bool visible = string.IsNullOrWhiteSpace(search) || entry.SearchText?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

            if (!visible)
                continue;

            entry.SetListMode(listMode);
            entry.SetEntrySize(CurrentEntrySize);
            visibleEntries.Add(entry);
        }

        if (sorts.Count > 0)
            visibleEntries.Sort(sorts[sortIndex].Compare);

        grid.AddRange(visibleEntries);
        grid.Recalculate();
        Recalculate();
    }

    private void SetEntrySizeFromSlider(float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        CurrentEntrySize = (int)MathHelper.Lerp(MinEntrySize, MaxEntrySize, progress);
        RefreshEntries();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateHeaderLayout();
        UpdateScrollbarVisibility();
    }

    private void UpdateHeaderLayout()
    {
        if (headerRow is null)
            return;

        float width = GetInnerDimensions().Width - 16f;

        bool showViewToggle = width >= 300f;
        bool showSlider = width >= 450f;
        bool showSort = width >= 560f && sorts.Count > 0;

        if (showViewToggle)
        {
            if (viewToggle?.Parent is null)
                headerRow.Append(viewToggle);
        }
        else
        {
            viewToggle?.Remove();
        }

        if (showSlider)
        {
            if (sizeSlider?.Parent is null)
                headerRow.Append(sizeSlider);
        }
        else
        {
            sizeSlider?.Remove();
        }

        if (showSort)
        {
            if (sortButton?.Parent is null)
                headerRow.Append(sortButton);
        }
        else
        {
            sortButton?.Remove();
        }
    }

    private void UpdateScrollbarVisibility()
    {
        if (scrollbar is null)
            return;

        bool needsScroll = scrollbar.CanScroll;

        if (needsScroll && scrollbar.Parent is null)
            Append(scrollbar);
        else if (!needsScroll)
            scrollbar.Remove();
    }

    private sealed class BrowserSizeSlider : UISlider
    {
        private readonly UIBrowser owner;

        public BrowserSizeSlider(UIBrowser owner)
        {
            this.owner = owner;

            OnDraw += _ =>
            {
                if (IsMouseHovering)
                    UICommon.TooltipMouseText($"Button size: {(int)MathHelper.Lerp(owner.MinEntrySize, owner.MaxEntrySize, Ratio)}");
            };
        }
    }
}