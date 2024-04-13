﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.Tab
{
    /// <summary>
    ///     A tab controller used to control the tabs in a tabbed GUI view.
    /// </summary>
    public class TabGuiUtilityController
    {
        /// <summary>
        ///     List of tabs.
        /// </summary>
        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        /// <summary>
        ///     The previously selected tab.
        /// </summary>
        private ITabGuiUtility _previouslySelectedTab;

        /// <summary>
        ///     The currently selected tab in the GUI.
        /// </summary>
        public ITabGuiUtility SelectedTab { get; private set; }

        /// <summary>
        ///     Add a tab to the controller.
        /// </summary>
        /// <param name="tab">The tab to add.</param>
        public void AddTab(ITabGuiUtility tab)
        {
            _tabGuiUtilities.Add(tab);

            SetupTabs();
        }

        /// <summary>
        ///     Add a range of tabs to the controller.
        /// </summary>
        /// <param name="tabList">The list of tabs to add.</param>
        public void AddTabRange(List<ITabGuiUtility> tabList)
        {
            _tabGuiUtilities.AddRange(tabList);

            SetupTabs();
        }

        /// <summary>
        ///     Remove all tabs in the controller.
        /// </summary>
        public void Clear()
        {
            _tabGuiUtilities.Clear();
        }

        /// <summary>
        ///     Draw the content of the selected Tab.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> in which to draw the tab content.</param>
        public void DrawSelectedTab(Rect inRect)
        {
            if (SelectedTab == null)
                return;

            if (SelectedTab.CanBeDrawn)
                SelectedTab.Draw(inRect);
        }

        /// <summary>
        ///     Draw the frame around the tabs (and not the tab contents!).
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> of the tabs.</param>
        public void DrawTabs(Rect inRect)
        {
            if (_tabGuiUtilities.Count == 0)
                return;

            var tabRecordsToDraw = _tabGuiUtilities.Where(tab => tab.CanBeDrawn)
                .Select(tab =>
                {
                    /* fix for god mode: if god mode is selected in world map and then you settle your colony 
                     * and then you click on the world button, you end up having 'God mode' selected, 
                     * which can't be displayed while playing */
                    if (!SelectedTab.CanBeDrawn)
                        SetPreviousTabAsSelectedTab(); // try to get the previously displayed tab
                    if (!SelectedTab.CanBeDrawn)
                        SetSelectedTabById("Terrain"); // fallback to terrain
                    /* end fix */

                        tab.TabRecord.selected = SelectedTab == tab;
                    return tab.TabRecord;
                }).ToList(); //TODO 1.0 what was before (as 2nd arg to TabDrawer.DrawTabs) an enumerable is now a list; check if correct.

            TabDrawer.DrawTabs(inRect, tabRecordsToDraw);
        }

        /// <summary>
        ///     Select the previously selected tab as the currently selected tab
        /// </summary>
        public void SetPreviousTabAsSelectedTab()
        {
            if (_previouslySelectedTab == null)
                _previouslySelectedTab = _tabGuiUtilities[0];

            if (SelectedTab != null)
                SelectedTab.TabRecord.selected = false;

            SelectedTab = _previouslySelectedTab;
            SelectedTab.TabRecord.selected = true;
        }

        /// <summary>
        ///     Select a tab by its identifier  (<see cref="ITabGuiUtility.Id" />).
        /// </summary>
        /// <param name="id">The identifier of the tab to be selected.</param>
        public bool SetSelectedTabById(string id)
        {
            var tab = TabById(id);
            if (tab == null)
                return false;

            SelectedTab = tab;
            SelectedTab.TabRecord.selected = true;
            return true;
        }

        /// <summary>
        ///     Get a Tab given its identifier (<see cref="ITabGuiUtility.Id" />).
        /// </summary>
        /// <param name="id">The identifier of the tab to get.</param>
        /// <returns>A tab if such a tab with the given id exists or null otherwise.</returns>
        public ITabGuiUtility TabById(string id)
        {
            return _tabGuiUtilities.FirstOrDefault(tab => tab.Id == id);
        }

        /// <summary>
        ///     Setup the tabs to be displayed.
        /// </summary>
        protected void SetupTabs()
        {
            foreach (var tabGuiUtility in _tabGuiUtilities)
            {
                var currentTab = tabGuiUtility;

                currentTab.TabRecord = new TabRecord(currentTab.Name,
                    delegate
                    {
                        _previouslySelectedTab = SelectedTab;
                        SelectedTab = currentTab;
                    },
                    false);
            }

            SelectedTab = _tabGuiUtilities[0];
        }
    }
}