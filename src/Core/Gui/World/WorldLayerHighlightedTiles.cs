﻿using RimWorld.Planet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.World
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WorldLayerHighlightedTiles : WorldLayer
    {
        private readonly float _blinkTick;

        private readonly List<Vector3> _vertices = new List<Vector3>();

        private float _alpha;

        private AlphaRampDirection _alphaRampDirection;

        /// <summary>
        ///     Layer constructor.
        /// </summary>
        /// <remarks>It is instantiated by the game engine. See constructor of the <see cref="RimWorld.Planet.WorldRenderer" /> class.</remarks>
        public WorldLayerHighlightedTiles()
        {
            _blinkTick = PrepareLanding.Instance.TileHighlighter.BlinkTick;

            PrepareLanding.Instance.TileHighlighter.HighlightedTilesWorldLayer = this;

            PrepareLanding.Instance.GameTicks.TicksIntervalElapsed += OnTicksIntervalElapsed;
            PrepareLanding.Instance.GameTicks.UpdateInterval = _blinkTick;
        }

        /// <summary>
        ///     Get the overall alpha value of the whole layer.
        /// </summary>
        protected override float Alpha => _alpha;

        /// <summary>
        ///     Called by RimWorld's engine when generating layers.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable Regenerate()
        {
            foreach (var result in base.Regenerate())
                yield return result;

            if (PrepareLanding.Instance.TileHighlighter.HighlightedTilesIds.Count == 0)
                yield break;

            foreach (var tileId in PrepareLanding.Instance.TileHighlighter.HighlightedTilesIds)
            {
                if (tileId < 0)
                    continue;


                var material = WorldLayerBehaviour.DefaultTileHighlighterMaterial;

                var subMesh = GetSubMesh(material);
                subMesh.finalized = false;

                Find.World.grid.GetTileVertices(tileId, _vertices);

                var startVertIndex = subMesh.verts.Count;
                var currentIndex = 0;
                var maxCount = _vertices.Count;

                while (currentIndex < maxCount)
                {
                    if (currentIndex % 1000 == 0)
                        yield return null;

                    if (subMesh.verts.Count > 60000)
                        subMesh = GetSubMesh(material);

                    subMesh.verts.Add(_vertices[currentIndex] + _vertices[currentIndex].normalized * 0.012f);
                    if (currentIndex < maxCount - 2)
                    {
                        subMesh.tris.Add(startVertIndex + currentIndex + 2);
                        subMesh.tris.Add(startVertIndex + currentIndex + 1);
                        subMesh.tris.Add(startVertIndex);
                    }
                    currentIndex++;
                }

                subMesh.FinalizeMesh(MeshParts.All);
            }
        }

        /// <summary>
        ///     Make the highlighted tile "breath" (blink) by changing their alpha value.
        /// </summary>
        protected virtual void TileBreath()
        {
            if (_alpha < float.Epsilon)
            {
                // current alpha value is near 0, so set it to 0 and now go towards 1
                _alpha = 0f;
                _alphaRampDirection = AlphaRampDirection.AlphaRampUp;
            }
            if (_alpha >= 1f)
            {
                // alpha as goes up to 1, now set to 1 and go down
                _alpha = 1f;
                _alphaRampDirection = AlphaRampDirection.AlphaRampDown;
            }

            // depending on the direction (towards 0 or towards 1), either add or subtract from the alpha value
            if (_alphaRampDirection == AlphaRampDirection.AlphaRampDown)
                _alpha -= _blinkTick;
            else
                _alpha += _blinkTick;
        }

        /// <summary>
        ///     Called each time an highlighting tick interval has elapsed.
        /// </summary>
        /// <remarks>The tick handler is managed by the <see cref="TileHighlighter" /> instance, not by the layer.</remarks>
        private void OnTicksIntervalElapsed()
        {
            if (PrepareLanding.Instance.TileHighlighter.DisableTileHighlighting)
            {
                // if tile highlighting is disabled, just don't show the layer.
                _alpha = 0;
                return;
            }

            if (PrepareLanding.Instance.TileHighlighter.DisableTileBlinking)
                _alpha = TileHighlighter.DefaultTileHighlightingAlphaValue;
            else
                TileBreath();
        }

        /// <summary>
        ///     Direction of the alpha value; either towards 0 or towards 1.
        /// </summary>
        private enum AlphaRampDirection
        {
            /// <summary>
            ///     Alpha value will go towards 1.
            /// </summary>
            AlphaRampUp,

            /// <summary>
            ///     Alpha value will go towards 0.
            /// </summary>
            AlphaRampDown
        }
    }
}