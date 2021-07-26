﻿using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Core.Gui.Window;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using Widgets = Verse.Widgets;

namespace PrepareLanding
{
    public class MainWindow : MinimizableWindow
    {
        public const float SpaceForBottomButtons = 65f;

        private const float GapBetweenButtons = 10f;

        private const int MaxDisplayedTileWhenMinimized = 30;

        private readonly List<ButtonDescriptor> _bottomButtonsDescriptorList;
        private readonly Vector2 _bottomButtonSize = new Vector2(110f, 40f);
        private readonly Vector2 _bottomButtonSizeLowRes = new Vector2(110f, 25f);
        private readonly List<ButtonDescriptor> _minimizedWindowButtonsDescriptorList;

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        private Vector2 _scrollPosMatchingTiles;

        private readonly ButtonDescriptor _buttonCloseDescriptor;

        private readonly TabFilteredTiles _tabFilteredTiles;

        public MainWindow(GameData.GameData gameData)
        {
            doCloseButton = false; // explicitly disable close button, we'll draw it ourselves
            doCloseX = true;
            optionalTitle = "Prepare Landing"; // Do not translate
            MinimizedWindow.WindowLabel = optionalTitle;
            MinimizedWindow.AddMinimizedWindowContent += AddMinimizedWindowContent;

            /* 
             * GUI utilities (tabs)
             */
            _tabFilteredTiles = new TabFilteredTiles(0.48f);

            _tabGuiUtilities.Clear();
            _tabGuiUtilities.Add(new TabTerrain(gameData, 0.30f));
            _tabGuiUtilities.Add(new TabTemperature(gameData, 0.30f));
            _tabGuiUtilities.Add(_tabFilteredTiles);
            _tabGuiUtilities.Add(new TabInfo(gameData, 0.48f));
            _tabGuiUtilities.Add(new TabOptions(gameData, 0.35f));
            _tabGuiUtilities.Add(new TabLoadSave(gameData, 0.48f));
#if TAB_OVERLAYS
            _tabGuiUtilities.Add(new TabOverlays(gameData, 0.50f));
#endif
            _tabGuiUtilities.Add(new TabGodMode(gameData, 0.30f));

            TabController.Clear();
            TabController.AddTabRange(_tabGuiUtilities);

            /*
             * Bottom buttons
             */

            #region BOTTOM_BUTTONS

            var buttonFilterTiles = new ButtonDescriptor("PLMWBB_FilterTiles".Translate(),
                delegate
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();

                    // reset starting display index
                    _tabFilteredTiles.TileDisplayIndexStart = 0;

                    // reset selected index
                    _tabFilteredTiles.SelectedTileIndex = -1;

                    // do the tile filtering
                    PrepareLanding.Instance.TileFilter.Filter();
                });

            var buttonResetFilters = new ButtonDescriptor("PLMWBB_ResetFilters".Translate(),
                delegate
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    gameData.UserData.ResetAllFields();
                });

            var buttonMinimize = new ButtonDescriptor("PLMWBB_Minimize".Translate(),
                delegate
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    Minimize();
                });

            var buttonSelectRandomSite = new ButtonDescriptor("PLMWBB_SelectRandom".Translate(), 
                delegate
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    var tileId = PrepareLanding.Instance.TileFilter.RandomFilteredTile();
                    if (tileId == Tile.Invalid)
                        return;

                    Find.WorldInterface.SelectedTile = tileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                });

            _buttonCloseDescriptor = new ButtonDescriptor("CloseButton".Translate(),
                delegate
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();

                    // reset starting display index
                    _tabFilteredTiles.TileDisplayIndexStart = 0;

                    // reset selected index
                    _tabFilteredTiles.SelectedTileIndex = -1;

                    ForceClose();
                }, displayState: DisplayState.Entry | DisplayState.MapInitializing);


            _bottomButtonsDescriptorList =
                new List<ButtonDescriptor> {buttonFilterTiles, buttonResetFilters, buttonSelectRandomSite, buttonMinimize, _buttonCloseDescriptor};

            #endregion BOTTOM_BUTTONS

            /*
             * Minimized window buttons
             */

            #region MINIMIZED_WINDOW_BUTTONS

            //TODO: this is exactly the same code than in TabFilterTiles.cs: find a way to refactor the code.

            var buttonListStart = new ButtonDescriptor("<<", delegate
            {
                // reset starting display index
                _tabFilteredTiles.TileDisplayIndexStart = 0;
            }, "PLMWFTIL_GoToStartOfTileList".Translate());

            var buttonPreviousPage = new ButtonDescriptor("<", delegate
            {
                if (_tabFilteredTiles.TileDisplayIndexStart >= MaxDisplayedTileWhenMinimized)
                    _tabFilteredTiles.TileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                else
                    Messages.Message("PLMWFTIL_ReachedListStart".Translate(), MessageTypeDefOf.RejectInput);
            }, "PLMWFTIL_GoToPreviousListPage".Translate());

            var buttonNextPage = new ButtonDescriptor(">", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                _tabFilteredTiles.TileDisplayIndexStart += MaxDisplayedTileWhenMinimized;
                if (_tabFilteredTiles.TileDisplayIndexStart > matchingTilesCount)
                {
                    Messages.Message($"{"PLMWFTIL_NoMoreTilesAvailable".Translate()} {matchingTilesCount}).",
                        MessageTypeDefOf.RejectInput);
                    _tabFilteredTiles.TileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                }
            }, "PLMWFTIL_GoToNextListPage".Translate());

            var buttonListEnd = new ButtonDescriptor(">>", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                var tileDisplayIndexStart = matchingTilesCount - matchingTilesCount % MaxDisplayedTileWhenMinimized;
                if (tileDisplayIndexStart == _tabFilteredTiles.TileDisplayIndexStart)
                    Messages.Message($"{"PLMWFTIL_NoMoreTilesAvailable".Translate()} {matchingTilesCount}).",
                        MessageTypeDefOf.RejectInput);

                _tabFilteredTiles.TileDisplayIndexStart = tileDisplayIndexStart;
            }, "PLMWFTIL_GoToEndOfList".Translate());

            _minimizedWindowButtonsDescriptorList =
                new List<ButtonDescriptor> { buttonListStart, buttonPreviousPage, buttonNextPage, buttonListEnd };

            #endregion MINIMIZED_WINDOW_BUTTONS
        }

        public TabGuiUtilityController TabController { get; } = new TabGuiUtilityController();

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2(1024f, 768f);

        public override bool IsWindowValidInContext => WorldRendererUtility.WorldRenderedNow && (Find.WindowStack.IsOpen<MainWindow>() || Find.WindowStack.IsOpen<MinimizedWindow>());

        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect);

            TabController.DrawTabs(inRect);

            inRect = inRect.ContractedBy(17f);

            TabController.DrawSelectedTab(inRect);

            DoBottomsButtons(inRect);
        }

        public override void PreOpen()
        {
            base.PreOpen();

            /*
             * note: this code is in PreOpen() rather than in the ctor because otherwise RimWorld would crash (more precisely, Unity crashes).
             * I can't remember exactly where, but it deals with Unity calculating the text size of a floating menu.
             * So better to let this code here rather than in the ctor.
             */
            if (Enumerable.Any(_bottomButtonsDescriptorList, buttonDescriptor => buttonDescriptor.Label == "PLMWBB_LoadSave".Translate()))
                return;

            var buttonSaveLoadPreset = new ButtonDescriptor("PLMWBB_LoadSave".Translate(),
                "PLMWBB_LoadOrSaveFilterPresets".Translate());
            buttonSaveLoadPreset.AddFloatMenuOption("PLMWLODSAV_Save".Translate(), delegate /* action click */
                {
                    if (!(TabController.TabById("LoadSave") is TabLoadSave tab))
                        return;

                    tab.LoadSaveMode = LoadSaveMode.Save;
                    TabController.SetSelectedTabById("LoadSave");
                }, delegate /* action mouse over */
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, 30f, 30f);

                    TooltipHandler.TipRegion(rect, "PLMWBB_SaveFiltersToPreset".Translate());
                }
            );
            buttonSaveLoadPreset.AddFloatMenuOption("PLMWLODSAV_Load".Translate(), delegate
                {
                    if (!(TabController.TabById("LoadSave") is TabLoadSave tab))
                        return;

                    tab.LoadSaveMode = LoadSaveMode.Load;
                    TabController.SetSelectedTabById("LoadSave");

                }, delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, 30f, 30f);

                    TooltipHandler.TipRegion(rect, "PLMWBB_LoadPreset".Translate());
                }
            );
            buttonSaveLoadPreset.AddFloatMenu("PLMWBB_SelectAction".Translate());
            _bottomButtonsDescriptorList.Add(buttonSaveLoadPreset);

            // do not display the "close" button while playing (the "World" button on bottom menu bar was clicked)
            //    otherwise there's no way to get the window back...
            if (Current.ProgramState == ProgramState.Playing)
            {
                if(_bottomButtonsDescriptorList.Contains(_buttonCloseDescriptor)) 
                    _bottomButtonsDescriptorList.Remove(_buttonCloseDescriptor);
            }
            else
            {
                if (!_bottomButtonsDescriptorList.Contains(_buttonCloseDescriptor))
                    _bottomButtonsDescriptorList.Add(_buttonCloseDescriptor);

            }
        }

        public override void PostClose()
        {
            base.PostClose();

            // when the window is closed and it's not minimized, disable all highlighted tiles
            if (!Minimized)
                PrepareLanding.Instance.TileHighlighter.RemoveAllTiles(); // TODO: make an event for that: WindowClosed, and let subscribers do their stuff
        }

        protected void DoBottomsButtons(Rect inRect)
        {
            var numButtons = _bottomButtonsDescriptorList.Count;

            // fix #32; bottom buttons are not visible when resolution is 1280 (w) * 720 (h).
            // I can't do better than that without reshaping the whole GUI.
            //Log.ErrorOnce($"[PL] Height: {UI.screenHeight}; Width {UI.screenWidth}", 0x1beef);
            float buttonsY;
            Vector2 bottomButtonSize;
            if (UI.screenHeight <= 720)
            {
                buttonsY = windowRect.height - (SpaceForBottomButtons + 16f); ; // make buttons a little bit higher
                bottomButtonSize = _bottomButtonSizeLowRes; // thinner buttons
            }
            else
            {
                buttonsY = windowRect.height - SpaceForBottomButtons;
                bottomButtonSize = _bottomButtonSize;
            }

            var buttonsRect = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, bottomButtonSize.x,
                bottomButtonSize.y, GapBetweenButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce(
                    $"[PrepareLanding] Couldn't not get enough room for {numButtons} (in MainWindow.DoBottomsButtons)",
                    0x1237cafe);
                return;
            }

            for (var i = 0; i < numButtons; i++)
            {
                // get button descriptor
                var buttonDescriptor = _bottomButtonsDescriptorList[i];

                buttonDescriptor.DrawButton(buttonsRect[i]);
            }
        }

        protected void AddMinimizedWindowContent(Listing_Standard listingStandard, Rect inRect)
        {
            /* constants used for GUI elements */

            // default line height
            const float gapLineHeight = 4f;
            // default visual element height
            const float elementHeight = 30f;

            //check if we have something to display (tiles)
            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;
            var matchingTilesCount = matchingTiles.Count;
            if (matchingTilesCount == 0)
            {
                // revert to initial window size if needed
                MinimizedWindow.windowRect.height = MinimizedWindow.InitialSize.y;
                return;
            }

            /*
             * Buttons
             */

            if (listingStandard.ButtonText("PLMWMINW_ClearFilteredTiles".Translate()))
            {
                // clear everything
                PrepareLanding.Instance.TileFilter.ClearMatchingTiles();

                // reset starting display index
                _tabFilteredTiles.TileDisplayIndexStart = 0;

                // reset selected index
                _tabFilteredTiles.SelectedTileIndex = -1;

                // don't go further as there are no tile content to draw
                return;
            }

            var buttonsRectSpace = listingStandard.GetRect(30f);
            var splittedRect = buttonsRectSpace.SplitRectWidthEvenly(_minimizedWindowButtonsDescriptorList.Count);

            for (var i = 0; i < _minimizedWindowButtonsDescriptorList.Count; i++)
            {
                // get button descriptor
                var buttonDescriptor = _minimizedWindowButtonsDescriptorList[i];

                // display button; if clicked: call the related action
                if (Widgets.ButtonText(splittedRect[i], buttonDescriptor.Label))
                    buttonDescriptor.Action();

                // display tool-tip (if any)
                if (!string.IsNullOrEmpty(buttonDescriptor.ToolTip))
                    TooltipHandler.TipRegion(splittedRect[i], buttonDescriptor.ToolTip);
            }

            /*
             * Display label (where we actually are in the tile list)
             */

            // number of elements (tiles) to display
            var itemsToDisplay = Math.Min(matchingTilesCount - _tabFilteredTiles.TileDisplayIndexStart, MaxDisplayedTileWhenMinimized);

            // label to display where we actually are in the tile list
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            var heightBefore = listingStandard.StartCaptureHeight();
            listingStandard.Label(
                $"{_tabFilteredTiles.TileDisplayIndexStart}: {_tabFilteredTiles.TileDisplayIndexStart + itemsToDisplay - 1} / {matchingTilesCount - 1}",
                elementHeight);
            GenUI.ResetLabelAlign();
            var counterLabelRect = listingStandard.EndCaptureHeight(heightBefore);
            Core.Gui.Widgets.DrawHighlightColor(counterLabelRect, Color.cyan, 0.50f);

            // add a gap before the scroll view
            listingStandard.Gap(gapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = inRect.height - listingStandard.CurHeight;

            // recalculate window height: initial size + visible scroll view height + current height of the listing standard (hence accounting for all buttons above)
            var newWindowHeight = MinimizedWindow.InitialSize.y + maxScrollViewOuterHeight + listingStandard.CurHeight;

            // minimized window height can't be more than 70% of the screen height
            MinimizedWindow.windowRect.height = Mathf.Min(newWindowHeight, UI.screenHeight * 0.70f);

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = itemsToDisplay * elementHeight + gapLineHeight * MaxDisplayedTileWhenMinimized;

            /*
             * Scroll view
             */
            var innerLs = listingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosMatchingTiles, 16f);

            var endIndex = _tabFilteredTiles.TileDisplayIndexStart + itemsToDisplay;
            for (var i = _tabFilteredTiles.TileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedTileId = matchingTiles[i];

                // get latitude & longitude for the tile
                var vector = Find.WorldGrid.LongLatOf(selectedTileId);
                var labelText = $"{i}: {vector.y.ToStringLatitude()} {vector.x.ToStringLongitude()}";

                // display the label
                var labelRect = innerLs.GetRect(elementHeight);

                // get the Tab
                if (!(TabController.TabById("FilteredTiles") is TabFilteredTiles tab))
                    return;

                var selected = i == tab.SelectedTileIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // go to the location of the selected tile
                    tab.SelectedTileIndex = i;
                    Find.WorldInterface.SelectedTile = selectedTileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }
                // add a thin line between each label
                innerLs.GapLine(gapLineHeight);
            }

            listingStandard.EndScrollView(innerLs);
        }
    }
}