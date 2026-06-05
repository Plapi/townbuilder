using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.Plapamaru.TownCrafter.Factory.EditorTools
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FactoryConstructionTerrainPainter : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform _terrainRoot;
        [SerializeField] private Transform _constructionRoot;

        [Header("Terrain Layers")]
        [SerializeField] private TerrainLayer _defaultTerrainLayer;
        [SerializeField] private TerrainLayer _constructionTerrainLayer;
        [SerializeField] private TerrainLayer _roadTerrainLayer;

        [Header("Options")]
        [SerializeField] private bool _includeInactiveConstructions = true;
        [SerializeField] private bool _autoFindTerrainRoot = true;

#if UNITY_EDITOR
        public void PaintConstructionTerrain()
        {
            if (!TryGetTerrainRoot(out var terrainRoot))
                return;

            if (_defaultTerrainLayer == null || _constructionTerrainLayer == null || _roadTerrainLayer == null)
            {
                Debug.LogError($"{nameof(FactoryConstructionTerrainPainter)} requires all terrain layers to be assigned.", this);
                return;
            }

            var terrains = GetPaintableTerrains(terrainRoot);
            if (terrains.Count == 0)
            {
                Debug.LogWarning($"{nameof(FactoryConstructionTerrainPainter)} found no paintable terrains under '{terrainRoot.name}'.", this);
                return;
            }

            var constructions = GetComponentsInChildren<Construction>(_includeInactiveConstructions);
            if (_constructionRoot != null)
                constructions = _constructionRoot.GetComponentsInChildren<Construction>(_includeInactiveConstructions);

            try
            {
                for (var i = 0; i < terrains.Count; i++)
                {
                    var terrain = terrains[i];
                    EditorUtility.DisplayProgressBar(
                        "Paint Construction Terrain",
                        terrain.name,
                        terrains.Count <= 1 ? 1f : (float)i / terrains.Count);

                    PaintTerrain(terrain, constructions);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"Painted {terrains.Count} terrain(s) from {constructions.Length} construction(s).", this);
        }

        private bool TryGetTerrainRoot(out Transform terrainRoot)
        {
            terrainRoot = _terrainRoot;

            if (terrainRoot == null && _autoFindTerrainRoot)
            {
                var terrainRootObject = GameObject.Find("Terrain");
                if (terrainRootObject != null)
                    terrainRoot = terrainRootObject.transform;
            }

            if (terrainRoot != null)
                return true;

            Debug.LogError($"{nameof(FactoryConstructionTerrainPainter)} needs a terrain root assigned.", this);
            return false;
        }

        private static List<Terrain> GetPaintableTerrains(Transform terrainRoot)
        {
            var results = new List<Terrain>();
            var terrains = terrainRoot.GetComponentsInChildren<Terrain>(true);

            foreach (var terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                    continue;

                if (terrain.name.IndexOf("river", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var paintTarget = terrain.GetComponent<FactoryTerrainPaintTarget>();
                if (paintTarget != null && !paintTarget.CanBePainted)
                    continue;

                results.Add(terrain);
            }

            return results;
        }

        private void PaintTerrain(Terrain terrain, Construction[] constructions)
        {
            var terrainData = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(terrainData, "Paint Construction Terrain");

            var defaultLayerIndex = EnsureTerrainLayer(terrainData, _defaultTerrainLayer);
            var constructionLayerIndex = EnsureTerrainLayer(terrainData, _constructionTerrainLayer);
            var roadLayerIndex = EnsureTerrainLayer(terrainData, _roadTerrainLayer);

            var width = terrainData.alphamapWidth;
            var height = terrainData.alphamapHeight;
            var layerCount = terrainData.alphamapLayers;
            var alphaMaps = new float[height, width, layerCount];

            FillLayer(alphaMaps, width, height, layerCount, defaultLayerIndex);

            foreach (var construction in constructions)
            {
                if (construction == null)
                    continue;

                var layerIndex = IsRoad(construction) ? roadLayerIndex : constructionLayerIndex;
                PaintConstruction(alphaMaps, width, height, layerCount, terrain, construction, layerIndex);
            }

            terrainData.SetAlphamaps(0, 0, alphaMaps);
            EditorUtility.SetDirty(terrainData);
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

        private static void FillLayer(float[,,] alphaMaps, int width, int height, int layerCount, int layerIndex)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    for (var layer = 0; layer < layerCount; layer++)
                        alphaMaps[y, x, layer] = layer == layerIndex ? 1f : 0f;
                }
            }
        }

        private static void PaintConstruction(
            float[,,] alphaMaps,
            int width,
            int height,
            int layerCount,
            Terrain terrain,
            Construction construction,
            int layerIndex)
        {
            var terrainTransform = terrain.transform;
            var terrainData = terrain.terrainData;
            var terrainPosition = terrainTransform.position;
            var terrainSize = terrainData.size;
            var origin = construction.transform.position;
            var right = construction.transform.right;
            var forward = construction.transform.forward;
            var constructionSize = construction.Size;

            for (var y = 0; y < height; y++)
            {
                var normalizedZ = (y + 0.5f) / height;
                var worldZ = terrainPosition.z + normalizedZ * terrainSize.z;

                for (var x = 0; x < width; x++)
                {
                    var normalizedX = (x + 0.5f) / width;
                    var worldX = terrainPosition.x + normalizedX * terrainSize.x;
                    var delta = new Vector3(worldX, origin.y, worldZ) - origin;
                    var localX = Vector3.Dot(delta, right);
                    var localZ = Vector3.Dot(delta, forward);

                    if (localX < 0f || localZ < 0f || localX > constructionSize.x || localZ > constructionSize.y)
                        continue;

                    for (var layer = 0; layer < layerCount; layer++)
                        alphaMaps[y, x, layer] = layer == layerIndex ? 1f : 0f;
                }
            }
        }

        private static bool IsRoad(Construction construction)
        {
            return construction.name.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   HasRoadName(construction.Id) ||
                   HasRoadName(construction.Data != null ? construction.Data.name : null);
        }

        private static bool HasRoadName(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0;
        }
#endif
    }
}
