using System;
using System.Collections.Generic;
using com.Plapamaru.Singletons;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.Plapamaru.TownCrafter.Factory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FactoryTerrainModifier : MonoBehaviourSingleton<FactoryTerrainModifier>
    {
        [Header("Terrains")]
        [SerializeField] private List<Terrain> _modifiableTerrains = new List<Terrain>();

        [Header("Terrain Layers")]
        [SerializeField] private TerrainLayer _defaultLayer;
        [SerializeField] private TerrainLayer _constructionLayer;
        [SerializeField] private TerrainLayer _roadLayer;

        private void PaintArea(TerrainPaintLayer paintLayer, TerrainPaintArea area)
        {
            if (!TryGetTerrainLayer(paintLayer, out var terrainLayer))
                return;

            foreach (var terrain in _modifiableTerrains)
            {
                if (!CanModify(terrain))
                    continue;

                PaintTerrainArea(terrain, terrainLayer, area);
            }
        }

        private void SetAreaHole(TerrainPaintArea area, bool isHole)
        {
            foreach (var terrain in _modifiableTerrains)
            {
                if (!CanModify(terrain))
                    continue;

                SetTerrainHoleArea(terrain, area, isHole);
            }
        }

        public void ApplyConstructionArea(Construction construction, bool createHole)
        {
            if (construction == null)
                return;

            var paintLayer = IsRoad(construction) ? TerrainPaintLayer.Road : TerrainPaintLayer.Construction;
            var area = CreateArea(construction.transform, construction.Size);

            PaintArea(paintLayer, area);
            SetAreaHole(area, createHole);
        }

        private static TerrainPaintArea CreateArea(Transform areaTransform, Vector2 size)
        {
            return new TerrainPaintArea(
                areaTransform.position,
                areaTransform.right,
                areaTransform.forward,
                size);
        }

        private bool TryGetTerrainLayer(TerrainPaintLayer paintLayer, out TerrainLayer terrainLayer)
        {
            terrainLayer = paintLayer switch
            {
                TerrainPaintLayer.Default => _defaultLayer,
                TerrainPaintLayer.Construction => _constructionLayer,
                TerrainPaintLayer.Road => _roadLayer,
                _ => null
            };

            if (terrainLayer != null)
                return true;

            Debug.LogError($"{nameof(FactoryTerrainModifier)} has no TerrainLayer assigned for {paintLayer}.", this);
            return false;
        }

        private static bool CanModify(Terrain terrain)
        {
            return terrain != null && terrain.terrainData != null;
        }

        private static void PaintTerrainArea(Terrain terrain, TerrainLayer terrainLayer, TerrainPaintArea area)
        {
            var terrainData = terrain.terrainData;

#if UNITY_EDITOR
            RegisterTerrainDataUndo(terrainData, "Paint Terrain Area");
#endif

            var layerIndex = EnsureTerrainLayer(terrainData, terrainLayer);
            var width = terrainData.alphamapWidth;
            var height = terrainData.alphamapHeight;
            var layerCount = terrainData.alphamapLayers;
            var alphaMaps = terrainData.GetAlphamaps(0, 0, width, height);

            for (var y = 0; y < height; y++)
            {
                var normalizedZ = (y + 0.5f) / height;
                var worldZ = terrain.transform.position.z + normalizedZ * terrainData.size.z;

                for (var x = 0; x < width; x++)
                {
                    var normalizedX = (x + 0.5f) / width;
                    var worldX = terrain.transform.position.x + normalizedX * terrainData.size.x;

                    if (!area.Contains(worldX, worldZ))
                        continue;

                    for (var layer = 0; layer < layerCount; layer++)
                        alphaMaps[y, x, layer] = layer == layerIndex ? 1f : 0f;
                }
            }

            terrainData.SetAlphamaps(0, 0, alphaMaps);
            MarkTerrainDataDirty(terrainData);
        }

        private static void SetTerrainHoleArea(Terrain terrain, TerrainPaintArea area, bool isHole)
        {
            var terrainData = terrain.terrainData;
            var resolution = terrainData.holesResolution;

            if (resolution <= 0)
                return;

#if UNITY_EDITOR
            RegisterTerrainDataUndo(terrainData, isHole ? "Create Terrain Hole" : "Restore Terrain Hole");
#endif

            var holes = terrainData.GetHoles(0, 0, resolution, resolution);
            var visible = !isHole;

            for (var y = 0; y < resolution; y++)
            {
                var normalizedZ = (y + 0.5f) / resolution;
                var worldZ = terrain.transform.position.z + normalizedZ * terrainData.size.z;

                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = (x + 0.5f) / resolution;
                    var worldX = terrain.transform.position.x + normalizedX * terrainData.size.x;

                    if (area.Contains(worldX, worldZ))
                        holes[y, x] = visible;
                }
            }

            terrainData.SetHoles(0, 0, holes);
            MarkTerrainDataDirty(terrainData);
        }

        private static int EnsureTerrainLayer(TerrainData terrainData, TerrainLayer terrainLayer)
        {
            var terrainLayers = terrainData.terrainLayers;

            for (var i = 0; i < terrainLayers.Length; i++)
            {
                if (terrainLayers[i] == terrainLayer)
                    return i;
            }

            Array.Resize(ref terrainLayers, terrainLayers.Length + 1);
            terrainLayers[^1] = terrainLayer;
            terrainData.terrainLayers = terrainLayers;
            return terrainLayers.Length - 1;
        }

        private static bool IsRoad(Construction construction)
        {
            return HasRoadName(construction.name) ||
                   HasRoadName(construction.Id) ||
                   HasRoadName(construction.Data != null ? construction.Data.name : null);
        }

        private static bool HasRoadName(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void MarkTerrainDataDirty(TerrainData terrainData)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(terrainData);
#endif
        }

#if UNITY_EDITOR
        private static void RegisterTerrainDataUndo(TerrainData terrainData, string undoName)
        {
            if (!Application.isPlaying)
                Undo.RegisterCompleteObjectUndo(terrainData, undoName);
        }
#endif
    }

    public enum TerrainPaintLayer
    {
        Default,
        Construction,
        Road
    }

    [Serializable]
    public readonly struct TerrainPaintArea
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Right;
        public readonly Vector3 Forward;
        public readonly Vector2 Size;

        public TerrainPaintArea(Vector3 origin, Vector3 right, Vector3 forward, Vector2 size)
        {
            Origin = origin;
            Right = right.normalized;
            Forward = forward.normalized;
            Size = size;
        }

        public bool Contains(float worldX, float worldZ)
        {
            var delta = new Vector3(worldX, Origin.y, worldZ) - Origin;
            var localX = Vector3.Dot(delta, Right);
            var localZ = Vector3.Dot(delta, Forward);

            return localX >= 0f &&
                   localZ >= 0f &&
                   localX <= Size.x &&
                   localZ <= Size.y;
        }
    }
}