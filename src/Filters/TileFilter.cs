﻿using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Filters
{
    public abstract class TileFilter : ITileFilter
    {
        protected readonly List<int> _filteredTiles = new List<int>();

        protected readonly UserData UserData;

        protected TileFilter(UserData userData, string attachedProperty, FilterHeaviness heaviness)
        {
            UserData = userData;
            AttachedProperty = attachedProperty;
            Heaviness = heaviness;
            FilterAction = Filter;
        }

        public abstract string SubjectThingDef { get; }

        public abstract bool IsFilterActive { get; }

        public virtual List<int> FilteredTiles => _filteredTiles;

        public virtual string RunningDescription => $"{"PLFILT_Filtering".Translate()} {SubjectThingDef}";

        public string AttachedProperty { get; }

        public Action<List<int>> FilterAction { get; }

        public FilterHeaviness Heaviness { get; }

        public virtual void Filter(List<int> inputList)
        {
            _filteredTiles.Clear();
        }

        protected virtual bool TileHasDef(Tile tile)
        {
            throw new NotImplementedException(); 
        }

        protected virtual List<T> TileDefs<T>(Tile tile) where T: Def
        {
            throw new NotImplementedException();
        }

        protected void FilterOr<T>(IEnumerable<int> inputList, ThreeStateItemContainer<T> container) where T: Def
        {
            // get a list of Defs that *must not* be present
            var selectedDefOff = (from entry in container
                              where entry.Value.State == MultiCheckboxState.Off
                              select entry.Key).ToList();

            // get a list of Defs that *must* be present
            var selectedDefOn = (from entry in container
                             where entry.Value.State == MultiCheckboxState.On
                             select entry.Key).ToList();

            // get a list of Defs that may or may not be present
            var selectedDefPartial = (from entry in container
                                  where entry.Value.State == MultiCheckboxState.Partial
                                  select entry.Key).ToList();

            // foreach each tile in the input list
            foreach (var tileId in inputList)
            {
                // get the tile and check if it has any of the Def type
                var tile = Find.World.grid[tileId];
                var tileHasDefs = TileHasDef(tile);

                // get the Defs in the tile (or an empty list if no Defs)
                var tileDefs = TileDefs<T>(tile);

                if (tileHasDefs)
                {
                    // if we have more "absolutely wanted" (ON) defs than there are defs in the tile, then we know it can't match
                    if (selectedDefOn.Count > tileDefs.Count)
                        continue;

                    // No Def contained in the tile should be in the 'Off' selected state
                    if (selectedDefOff.Intersect(tileDefs).Any())
                        continue;

                    // all the selected On defs must be in the tile
                    if (!tileDefs.ContainsAll(selectedDefOn))
                        continue;

                    // tile might contains defs that are in partial state
                    if (selectedDefPartial.Count > 0 && selectedDefOn.Count > 0)
                    {
                        if (!tileDefs.ContainsAll(selectedDefOn))
                        {
                            var doNotAddTile = Enumerable.Any(selectedDefPartial,
                                currentDefPartial => !tileDefs.Contains(currentDefPartial));
                            if (doNotAddTile)
                                continue;
                        }
                    }

                    // add the tile
                    _filteredTiles.Add(tileId);
                }
                else // tile has no road
                {
                    // if any Def is in 'On' selected state, then it can't match
                    if (selectedDefOn.Count > 0)
                        continue;

                    // Partial of Off state is ok, add it.
                    _filteredTiles.Add(tileId);
                }
            }
        }

        protected void FilterAnd<T>(IEnumerable<int> inputList, ThreeStateItemContainer<T> container, bool offNoSelect) where T : Def
        {
            // foreach each tile in the input list
            foreach (var tileId in inputList)
            {
                // get the tile and check if it has any road
                var tile = Find.World.grid[tileId];
                var tileHasDef = TileHasDef(tile);

                // get the Defs in the tile (or an empty list if no Defs)
                var tileDefs = TileDefs<T>(tile);

                // loop through user selection items (key value pair) : 
                //    - key -> current item road def
                //    - value -> user choice state: either ON / OFF / PARTIAL
                foreach (var threeStateItemKvp in container)
                {
                    var currentSelectedDef = threeStateItemKvp.Key; // current def
                    var currentSelectionState = threeStateItemKvp.Value; // current user choice for this def

                    if (tileHasDef)
                    {
                        switch (currentSelectionState.State)
                        {
                            // user wants this type of defs
                            case MultiCheckboxState.On:
                                if (tileDefs.Contains(currentSelectedDef))
                                    _filteredTiles.Add(tileId);
                                break;

                            // user doesn't want this type of defs
                            case MultiCheckboxState.Off:
                                if (!tileDefs.Contains(currentSelectedDef) && !offNoSelect)
                                    _filteredTiles.Add(tileId);
                                break;

                            // user don't care if it's present or not
                            case MultiCheckboxState.Partial:
                                if (!offNoSelect)
                                    _filteredTiles.Add(tileId);
                                else
                                {
                                    if (tileDefs.Contains(currentSelectedDef))
                                        _filteredTiles.Add(tileId);
                                }
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else // current tile has no defs
                    {
                        switch (currentSelectionState.State)
                        {
                            case MultiCheckboxState.On:
                                break;

                            case MultiCheckboxState.Off:
                                if (container.IsAllOff())
                                {
                                    _filteredTiles.Add(tileId);
                                }
                                else
                                {
                                    if (!offNoSelect)
                                        _filteredTiles.Add(tileId);
                                }
                                break;

                            case MultiCheckboxState.Partial:
                                if (!offNoSelect)
                                    _filteredTiles.Add(tileId);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    if (_filteredTiles.Count > 0 && _filteredTiles.Last() == tileId)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Abstract base class for Temperature Filters.
    /// </summary>
    public abstract class TileFilterTemperatures : TileFilter
    {
        protected TileFilterTemperatures(UserData userData, string attachedProperty,
            FilterHeaviness heaviness) : base(userData, attachedProperty, heaviness)
        {
        }

        protected float AverageTemperatureForTile(int tileId)
        {
            return Find.WorldGrid[tileId].temperature;
        }

        protected  float MinTemperatureForTile(int tileId)
        {
            return GenTemperature.MinTemperatureAtTile(tileId);
        }

        protected float MaxTemperatureAtTile(int tileId)
        {
            return GenTemperature.MaxTemperatureAtTile(tileId);
        }

        public override void Filter(List<int> inputList)
        {
            base.Filter(inputList);

            if (!IsFilterActive)
                return;

            // e.g UserData.AverageTemperature, UserData.SummerTemperature or UserData.WinterTemperature
            var temperatureItem = (UsableMinMaxNumericItem<float>)UserData.GetType().GetProperty(AttachedProperty)
                ?.GetValue(UserData, null);
            if (temperatureItem == null)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"{"PLFILT_TemperatureIsNull".Translate()} TileFilterTemperatures.Filter.", sendToLog: true);
                return;
            }

            if (!temperatureItem.IsCorrectRange)
            {
                var message =
                    $"{SubjectThingDef}: {"PLFILT_VerifyMinIsLessOrEqualMax".Translate()}: {temperatureItem.Min} <= {temperatureItem.Max}).";
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(message);
                return;
            }

            Func<int, float> temperatureFunc;
            switch (AttachedProperty)
            {
                case nameof(UserData.AverageTemperature):
                    temperatureFunc = AverageTemperatureForTile;
                    break;

                case nameof(UserData.MinTemperature):
                    temperatureFunc = MinTemperatureForTile;
                    break;

                case nameof(UserData.MaxTemperature):
                    temperatureFunc = MaxTemperatureAtTile;
                    break;

                default:
                    Log.Error($"[PrepareLanding] Unknown attached property in TileFilterTemperatures.Filter(): {AttachedProperty}");
                    return;
            }

            foreach (var tileId in inputList)
            {
                var tileTempInC = temperatureFunc(tileId);

                var tileTemp = GenTemperature.CelsiusTo(tileTempInC, Prefs.TemperatureMode);

                if (temperatureItem.InRange(tileTemp))
                    _filteredTiles.Add(tileId);
            }
        }
    }
}