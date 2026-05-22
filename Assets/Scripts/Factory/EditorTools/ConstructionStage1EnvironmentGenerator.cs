using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace com.Plapamaru.TownCrafter.Factory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("TownCrafter/Editor/Construction Stage 1 Environment Generator")]
    public class ConstructionStage1EnvironmentGenerator : MonoBehaviour
    {
        [SerializeField] private Construction _construction;
        [SerializeField] private ConstructionPropsCatalog _floorCatalog;
        [SerializeField] private ConstructionPropsCatalog _propsCatalog;
        [SerializeField] private int _propsCount = 8;
        [SerializeField] private ConstructionPropsCatalog _wallCatalog;
        [SerializeField] private List<GameObject> _wallPrefabs = new List<GameObject>();
        [SerializeField] private ConstructionPropsCatalog _houseCatalog;
        [SerializeField] private Transform _houseEnvironment;
        [SerializeField] private Transform _house;
        [Range(0f, 1f)]
        [SerializeField] private float _wallGapChance;
        [SerializeField] private float _footprintPadding;

        public Construction Construction => _construction;
        public ConstructionPropsCatalog FloorCatalog => _floorCatalog;
        public ConstructionPropsCatalog PropsCatalog => _propsCatalog;
        public int PropsCount => Mathf.Max(0, _propsCount);
        public ConstructionPropsCatalog WallCatalog => _wallCatalog;
        public IReadOnlyList<GameObject> WallPrefabs => _wallPrefabs;
        public ConstructionPropsCatalog HouseCatalog => _houseCatalog;
        public Transform HouseEnvironment => _houseEnvironment;
        public Transform House => _house;
        public float WallGapChance => Mathf.Clamp01(_wallGapChance);
        public float FootprintPadding => Mathf.Max(0f, _footprintPadding);

        private void Reset()
        {
            TryAssignConstructionFromParents();
            TryAssignFloorCatalog();
            TryAssignPropsCatalog();
            TryAssignWallCatalog();
            TryAssignWallPrefabs();
            TryAssignHouseCatalog();
            TryAssignHouseEnvironment();
            TryAssignHouse();
        }

        private void OnValidate()
        {
            if (_construction == null)
                TryAssignConstructionFromParents();

            if (_floorCatalog == null)
                TryAssignFloorCatalog();

            if (_propsCatalog == null)
                TryAssignPropsCatalog();

            if (_wallCatalog == null)
                TryAssignWallCatalog();

            if (_wallPrefabs.Count == 0)
                TryAssignWallPrefabs();

            if (_houseCatalog == null)
                TryAssignHouseCatalog();

            if (_houseEnvironment == null)
                TryAssignHouseEnvironment();

            if (_house == null)
                TryAssignHouse();

            _wallGapChance = Mathf.Clamp01(_wallGapChance);
            _footprintPadding = Mathf.Max(0f, _footprintPadding);
            _propsCount = Mathf.Max(0, _propsCount);
        }

        private void TryAssignConstructionFromParents()
        {
            _construction = GetComponentInParent<Construction>();
        }

        private void TryAssignFloorCatalog()
        {
#if UNITY_EDITOR
            _floorCatalog = ConstructionConcreteFloorCatalogEditorUtility.FindDefaultCatalog();
#endif
        }

        private void TryAssignPropsCatalog()
        {
#if UNITY_EDITOR
            _propsCatalog = ConstructionHousePropsCatalogEditorUtility.FindDefaultCatalog();
#endif
        }

        private void TryAssignWallCatalog()
        {
#if UNITY_EDITOR
            _wallCatalog = ConstructionHouseWallCatalogEditorUtility.FindDefaultCatalog();
#endif
        }

        private void TryAssignWallPrefabs()
        {
            if (_wallCatalog == null)
                return;

            foreach (var wall in _wallCatalog.Props)
            {
                if (wall == null)
                    continue;

                _wallPrefabs.Add(wall);
                return;
            }
        }

        private void TryAssignHouseCatalog()
        {
#if UNITY_EDITOR
            _houseCatalog = ConstructionHouseCatalogEditorUtility.FindDefaultCatalog();
#endif
        }

        private void TryAssignHouseEnvironment()
        {
            var graphic = _construction != null ? _construction.transform.Find("Graphic") : null;
            var stage2 = graphic != null ? graphic.Find("Stage2NotOptimized") : null;
            _houseEnvironment = stage2 != null ? stage2.Find("Environment") : null;
        }

        private void TryAssignHouse()
        {
            _house = FindCatalogHouse(_houseEnvironment, _houseCatalog);
        }

        private static Transform FindCatalogHouse(Transform houseEnvironment, ConstructionPropsCatalog houseCatalog)
        {
            if (houseEnvironment == null || houseCatalog == null)
                return null;

            foreach (var prefab in houseCatalog.Props)
            {
                if (prefab == null)
                    continue;

                for (int i = 0; i < houseEnvironment.childCount; i++)
                {
                    var child = houseEnvironment.GetChild(i);
                    if (NormalizeHouseName(child.name) == prefab.name &&
                        child.GetComponentInChildren<MeshRenderer>(true) != null)
                        return child;
                }
            }

            return null;
        }

        private static string NormalizeHouseName(string objectName)
        {
            const string generatedHouseNameSuffix = " (Generated House)";
            return objectName.EndsWith(generatedHouseNameSuffix)
                ? objectName.Substring(0, objectName.Length - generatedHouseNameSuffix.Length)
                : objectName;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ConstructionStage1EnvironmentGenerator))]
    public class ConstructionStage1EnvironmentGeneratorEditor : Editor
    {
        private const string GENERATED_FLOOR_NAME_SUFFIX = " (Generated Concrete Floor)";
        private const string GENERATED_PROP_NAME_SUFFIX = " (Generated House Prop)";
        private const string GENERATED_WALL_NAME_SUFFIX = " (Generated House Wall)";
        private const string GENERATED_HOUSE_NAME_SUFFIX = " (Generated House)";
        private const int MAX_PLACEMENT_ATTEMPTS_PER_PROP = 40;

        private SerializedProperty _constructionProperty;
        private SerializedProperty _floorCatalogProperty;
        private SerializedProperty _propsCatalogProperty;
        private SerializedProperty _wallCatalogProperty;
        private SerializedProperty _wallPrefabsProperty;
        private SerializedProperty _houseCatalogProperty;
        private SerializedProperty _houseEnvironmentProperty;
        private SerializedProperty _houseProperty;

        private void OnEnable()
        {
            _constructionProperty = serializedObject.FindProperty("_construction");
            _floorCatalogProperty = serializedObject.FindProperty("_floorCatalog");
            _propsCatalogProperty = serializedObject.FindProperty("_propsCatalog");
            _wallCatalogProperty = serializedObject.FindProperty("_wallCatalog");
            _wallPrefabsProperty = serializedObject.FindProperty("_wallPrefabs");
            _houseCatalogProperty = serializedObject.FindProperty("_houseCatalog");
            _houseEnvironmentProperty = serializedObject.FindProperty("_houseEnvironment");
            _houseProperty = serializedObject.FindProperty("_house");

            TryAssignDefaultCatalogs();
            TryAssignHouse();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            DrawWallCatalogSelector();
            serializedObject.ApplyModifiedProperties();

            TryAssignHouse();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanGenerateConcreteFloors()))
            {
                if (GUILayout.Button("Generate Concrete Floors"))
                    GenerateConcreteFloors();
            }

            using (new EditorGUI.DisabledScope(!CanGenerateWalls()))
            {
                if (GUILayout.Button("Generate Walls"))
                    GenerateWalls();
            }

            using (new EditorGUI.DisabledScope(!CanGenerateProps()))
            {
                if (GUILayout.Button("Generate Props"))
                    GenerateProps();
            }

            if (GUILayout.Button("Clear Generated Concrete Floors"))
                ClearGeneratedConcreteFloors();

            if (GUILayout.Button("Clear Generated Walls"))
                ClearGeneratedWalls();

            if (GUILayout.Button("Clear Generated Props"))
                ClearGeneratedProps();
        }

        private bool CanGenerateConcreteFloors()
        {
            if (_constructionProperty.objectReferenceValue == null ||
                _houseProperty.objectReferenceValue == null)
                return false;

            var catalog = _floorCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return false;

            foreach (var floor in catalog.Props)
            {
                if (floor != null)
                    return true;
            }

            return false;
        }

        private bool CanGenerateWalls()
        {
            if (_constructionProperty.objectReferenceValue == null ||
                !HasAvailableWallPrefab())
                return false;

            var catalog = _floorCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return false;

            return HasCatalogFloorChild(((ConstructionStage1EnvironmentGenerator)target).transform, catalog);
        }

        private bool CanGenerateProps()
        {
            if (_constructionProperty.objectReferenceValue == null)
                return false;

            var catalog = _propsCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return false;

            foreach (var prop in catalog.Props)
            {
                if (prop != null)
                    return true;
            }

            return false;
        }

        private void TryAssignDefaultCatalogs()
        {
            serializedObject.Update();

            if (_floorCatalogProperty.objectReferenceValue == null)
                _floorCatalogProperty.objectReferenceValue = ConstructionConcreteFloorCatalogEditorUtility.FindDefaultCatalog();

            if (_propsCatalogProperty.objectReferenceValue == null)
                _propsCatalogProperty.objectReferenceValue = ConstructionHousePropsCatalogEditorUtility.FindDefaultCatalog();

            if (_wallCatalogProperty.objectReferenceValue == null)
                _wallCatalogProperty.objectReferenceValue = ConstructionHouseWallCatalogEditorUtility.FindDefaultCatalog();

            if (_wallPrefabsProperty.arraySize == 0)
                AddFirstCatalogProp(_wallPrefabsProperty, _wallCatalogProperty.objectReferenceValue as ConstructionPropsCatalog);

            if (_houseCatalogProperty.objectReferenceValue == null)
                _houseCatalogProperty.objectReferenceValue = ConstructionHouseCatalogEditorUtility.FindDefaultCatalog();

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DrawWallCatalogSelector()
        {
            var catalog = _wallCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            if (catalog == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Catalog Walls", EditorStyles.boldLabel);

            foreach (var wall in catalog.Props)
            {
                if (wall == null)
                    continue;

                var selected = ContainsObjectReference(_wallPrefabsProperty, wall);
                var nextSelected = EditorGUILayout.ToggleLeft(wall.name, selected);
                if (nextSelected != selected)
                    SetObjectReferenceSelected(_wallPrefabsProperty, wall, nextSelected);
            }
        }

        private void TryAssignHouse()
        {
            serializedObject.Update();

            if (_houseProperty.objectReferenceValue != null)
                return;

            var houseEnvironment = _houseEnvironmentProperty.objectReferenceValue as Transform;
            var houseCatalog = _houseCatalogProperty.objectReferenceValue as ConstructionPropsCatalog;
            _houseProperty.objectReferenceValue = FindCatalogHouse(houseEnvironment, houseCatalog);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void GenerateConcreteFloors()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            var construction = placer.Construction;
            if (construction == null || placer.FloorCatalog == null)
                return;

            var house = placer.House != null
                ? placer.House
                : FindCatalogHouse(placer.HouseEnvironment, placer.HouseCatalog);
            if (house == null)
            {
                Debug.LogError("Generate concrete floors failed: no house matching the house catalog was found in the house environment.", placer);
                return;
            }

            if (!TryGetHouseFootprint(house, construction, placer.FootprintPadding, out var min, out var max))
            {
                Debug.LogError("Generate concrete floors failed: the assigned house has no mesh renderer footprint.", placer);
                return;
            }

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var tiles = GetFloorTiles(placer.FloorCatalog, scale);
            if (tiles.Count == 0)
            {
                Debug.LogError("Generate concrete floors failed: the floor catalog has no prefabs with renderer bounds.", placer);
                return;
            }

            ClearGeneratedConcreteFloors(placer.transform);

            TileFootprint(placer.transform, construction, tiles, min, max, scale);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private void GenerateWalls()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            var construction = placer.Construction;
            if (construction == null || placer.FloorCatalog == null)
                return;

            if (!TryGetConcreteFloorFootprints(placer.transform, construction, placer.FloorCatalog, out var floorRects, out var concreteTopHeight))
            {
                Debug.LogError("Generate walls failed: no concrete floors matching the floor catalog were found under this placer.", placer);
                return;
            }

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            ClearGeneratedWalls(placer.transform);
            GenerateWallsOnBorder(placer.transform, construction, floorRects, GetAvailableWalls(placer.WallPrefabs, placer.WallCatalog), scale, concreteTopHeight, placer.WallGapChance);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private void GenerateProps()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            var construction = placer.Construction;
            if (construction == null || placer.PropsCatalog == null)
                return;

            var availableProps = GetAvailableProps(placer.PropsCatalog);
            if (availableProps.Count == 0)
                return;

            TryGetConcreteFloorFootprints(placer.transform, construction, placer.FloorCatalog, out var blockedRects, out _);
            ClearGeneratedProps(placer.transform);

            var scale = FactoryConfig.Instance != null ? FactoryConfig.Instance.constructionPropsScale : 0.4f;
            var placedRects = new List<Rect>();
            var placedCount = 0;
            var attempts = 0;
            var maxAttempts = Mathf.Max(placer.PropsCount * MAX_PLACEMENT_ATTEMPTS_PER_PROP, MAX_PLACEMENT_ATTEMPTS_PER_PROP);

            while (placedCount < placer.PropsCount && attempts < maxAttempts)
            {
                attempts++;
                var propPrefab = availableProps[Random.Range(0, availableProps.Count)];
                var rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                var worldRotation = Quaternion.LookRotation(ToWorld(construction.Forward), Vector3.up) * rotation;

                if (!TryGetRandomPropPlacement(construction, propPrefab, rotation, scale, blockedRects, placedRects, out var position, out var rect))
                    continue;

                var instance = PrefabUtility.InstantiatePrefab(propPrefab, placer.transform) as GameObject;
                if (instance == null)
                    instance = Instantiate(propPrefab, placer.transform);

                Undo.RegisterCreatedObjectUndo(instance, "Generate House Prop");
                instance.transform.SetPositionAndRotation(position, worldRotation);
                instance.transform.localScale = Vector3.one * scale;
                instance.name = $"{propPrefab.name}{GENERATED_PROP_NAME_SUFFIX}";

                placedRects.Add(rect);
                placedCount++;
            }

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static List<Rect> TileFootprint(
            Transform parent,
            Construction construction,
            List<FloorTile> tiles,
            Vector2 min,
            Vector2 max,
            float scale)
        {
            const float epsilon = 0.001f;
            var placedRects = new List<Rect>();
            var y = min.y;

            while (y < max.y - epsilon)
            {
                var x = min.x;
                var rowHeight = 0f;

                while (x < max.x - epsilon)
                {
                    var remainingSize = new Vector2(max.x - x, max.y - y);
                    var tile = SelectTile(tiles, remainingSize);
                    PlaceTile(parent, construction, tile, new Vector2(x, y), scale);
                    placedRects.Add(Rect.MinMaxRect(x, y, x + tile.Size.x, y + tile.Size.y));

                    x += tile.Size.x;
                    rowHeight = Mathf.Max(rowHeight, tile.Size.y);
                }

                if (rowHeight <= epsilon)
                    break;

                y += rowHeight;
            }

            return placedRects;
        }

        private static void PlaceTile(
            Transform parent,
            Construction construction,
            FloorTile tile,
            Vector2 desiredMin,
            float scale)
        {
            var instance = PrefabUtility.InstantiatePrefab(tile.Prefab, parent) as GameObject;
            if (instance == null)
                instance = Instantiate(tile.Prefab, parent);

            Undo.RegisterCreatedObjectUndo(instance, "Generate Concrete Floor");
            instance.name = $"{tile.Prefab.name}{GENERATED_FLOOR_NAME_SUFFIX}";

            var worldPosition = construction.transform.position +
                                ToWorld(construction.Right) * (desiredMin.x - tile.Min.x) +
                                ToWorld(construction.Forward) * (desiredMin.y - tile.Min.y);
            var worldRotation = Quaternion.LookRotation(ToWorld(construction.Forward), Vector3.up);

            instance.transform.SetPositionAndRotation(worldPosition, worldRotation);
            instance.transform.localScale = Vector3.one * scale;
        }

        private static FloorTile SelectTile(List<FloorTile> tiles, Vector2 remainingSize)
        {
            FloorTile best = null;
            var bestArea = -1f;

            foreach (var tile in tiles)
            {
                if (tile.Size.x > remainingSize.x + 0.001f ||
                    tile.Size.y > remainingSize.y + 0.001f)
                    continue;

                var area = tile.Size.x * tile.Size.y;
                if (area > bestArea)
                {
                    best = tile;
                    bestArea = area;
                }
            }

            return best ?? tiles[tiles.Count - 1];
        }

        private static List<FloorTile> GetFloorTiles(ConstructionPropsCatalog catalog, float scale)
        {
            var tiles = new List<FloorTile>();
            foreach (var prefab in catalog.Props)
            {
                if (prefab == null)
                    continue;

                if (TryGetLocalFootprint(prefab, scale, out var min, out var max))
                    tiles.Add(new FloorTile(prefab, min, max));
            }

            tiles.Sort((a, b) => (b.Size.x * b.Size.y).CompareTo(a.Size.x * a.Size.y));
            return tiles;
        }

        private static bool TryGetConcreteFloorFootprints(
            Transform root,
            Construction construction,
            ConstructionPropsCatalog floorCatalog,
            out List<Rect> floorRects,
            out float concreteTopHeight)
        {
            floorRects = new List<Rect>();
            concreteTopHeight = 0f;

            if (root == null || construction == null || floorCatalog == null)
                return false;

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!IsCatalogInstance(child.name, floorCatalog, GENERATED_FLOOR_NAME_SUFFIX))
                    continue;

                if (!TryGetWorldFootprint(child, construction, out var rect, out var topHeight))
                    continue;

                floorRects.Add(rect);
                concreteTopHeight = Mathf.Max(concreteTopHeight, topHeight);
            }

            return floorRects.Count > 0;
        }

        private static bool HasCatalogFloorChild(Transform root, ConstructionPropsCatalog floorCatalog)
        {
            if (root == null || floorCatalog == null)
                return false;

            for (int i = 0; i < root.childCount; i++)
            {
                if (IsCatalogInstance(root.GetChild(i).name, floorCatalog, GENERATED_FLOOR_NAME_SUFFIX))
                    return true;
            }

            return false;
        }

        private static List<GameObject> GetAvailableProps(ConstructionPropsCatalog catalog)
        {
            var availableProps = new List<GameObject>();
            foreach (var prop in catalog.Props)
            {
                if (prop != null)
                    availableProps.Add(prop);
            }

            return availableProps;
        }

        private static bool TryGetRandomPropPlacement(
            Construction construction,
            GameObject propPrefab,
            Quaternion rotation,
            float scale,
            List<Rect> blockedRects,
            List<Rect> placedRects,
            out Vector3 position,
            out Rect rect)
        {
            position = default;
            rect = default;

            if (!TryGetLocalFootprint(propPrefab, rotation, scale, out var min, out var max))
                return false;

            var minPivotX = -min.x;
            var maxPivotX = construction.Size.x - max.x;
            var minPivotZ = -min.y;
            var maxPivotZ = construction.Size.y - max.y;

            if (minPivotX > maxPivotX || minPivotZ > maxPivotZ)
                return false;

            for (int i = 0; i < MAX_PLACEMENT_ATTEMPTS_PER_PROP; i++)
            {
                var pivot = new Vector2(
                    Random.Range(minPivotX, maxPivotX),
                    Random.Range(minPivotZ, maxPivotZ));

                rect = Rect.MinMaxRect(
                    pivot.x + min.x,
                    pivot.y + min.y,
                    pivot.x + max.x,
                    pivot.y + max.y);

                if (OverlapsAny(rect, blockedRects) || OverlapsAny(rect, placedRects))
                    continue;

                position = construction.transform.position +
                           ToWorld(construction.Right) * pivot.x +
                           ToWorld(construction.Forward) * pivot.y;
                return true;
            }

            return false;
        }

        private static bool OverlapsAny(Rect rect, List<Rect> rects)
        {
            foreach (var other in rects)
            {
                if (rect.Overlaps(other))
                    return true;
            }

            return false;
        }

        private static bool IsCatalogInstance(string objectName, ConstructionPropsCatalog catalog, string generatedSuffix)
        {
            var normalizedName = NormalizeGeneratedName(objectName, generatedSuffix);
            foreach (var prefab in catalog.Props)
            {
                if (prefab != null && normalizedName == prefab.name)
                    return true;
            }

            return false;
        }

        private static string NormalizeGeneratedName(string objectName, string generatedSuffix)
        {
            return objectName.EndsWith(generatedSuffix)
                ? objectName.Substring(0, objectName.Length - generatedSuffix.Length)
                : objectName;
        }

        private static void GenerateWallsOnBorder(
            Transform parent,
            Construction construction,
            List<Rect> floorRects,
            List<GameObject> wallPrefabs,
            float scale,
            float wallBaseHeight,
            float wallGapChance)
        {
            if (wallPrefabs.Count == 0 || floorRects.Count == 0)
                return;

            if (!TryGetWallLength(wallPrefabs[0], scale, out var wallLength))
                return;

            var boundaryEdges = GetBoundaryEdges(floorRects);
            foreach (var edge in boundaryEdges)
                PlaceWallRun(parent, construction, wallPrefabs, edge, wallLength, scale, wallBaseHeight, wallGapChance);
        }

        private static List<BoundaryEdge> GetBoundaryEdges(List<Rect> rects)
        {
            var xCoordinates = new List<float>();
            var yCoordinates = new List<float>();

            foreach (var rect in rects)
            {
                AddCoordinate(xCoordinates, rect.xMin);
                AddCoordinate(xCoordinates, rect.xMax);
                AddCoordinate(yCoordinates, rect.yMin);
                AddCoordinate(yCoordinates, rect.yMax);
            }

            xCoordinates.Sort();
            yCoordinates.Sort();

            var edges = new List<BoundaryEdge>();
            for (int x = 0; x < xCoordinates.Count - 1; x++)
            {
                for (int y = 0; y < yCoordinates.Count - 1; y++)
                {
                    var cell = Rect.MinMaxRect(xCoordinates[x], yCoordinates[y], xCoordinates[x + 1], yCoordinates[y + 1]);
                    if (!IsInsideAnyRect(cell.center, rects))
                        continue;

                    var backCenter = new Vector2(cell.center.x, cell.yMin - 0.001f);
                    var rightCenter = new Vector2(cell.xMax + 0.001f, cell.center.y);
                    var forwardCenter = new Vector2(cell.center.x, cell.yMax + 0.001f);
                    var leftCenter = new Vector2(cell.xMin - 0.001f, cell.center.y);

                    if (!IsInsideAnyRect(backCenter, rects))
                        edges.Add(new BoundaryEdge(new Vector2(cell.xMin, cell.yMin), new Vector2(cell.xMax, cell.yMin), EdgeDirection.Back));

                    if (!IsInsideAnyRect(rightCenter, rects))
                        edges.Add(new BoundaryEdge(new Vector2(cell.xMax, cell.yMin), new Vector2(cell.xMax, cell.yMax), EdgeDirection.Right));

                    if (!IsInsideAnyRect(forwardCenter, rects))
                        edges.Add(new BoundaryEdge(new Vector2(cell.xMax, cell.yMax), new Vector2(cell.xMin, cell.yMax), EdgeDirection.Forward));

                    if (!IsInsideAnyRect(leftCenter, rects))
                        edges.Add(new BoundaryEdge(new Vector2(cell.xMin, cell.yMax), new Vector2(cell.xMin, cell.yMin), EdgeDirection.Left));
                }
            }

            return edges;
        }

        private static void AddCoordinate(List<float> coordinates, float value)
        {
            foreach (var coordinate in coordinates)
            {
                if (Mathf.Abs(coordinate - value) < 0.001f)
                    return;
            }

            coordinates.Add(value);
        }

        private static bool IsInsideAnyRect(Vector2 point, List<Rect> rects)
        {
            foreach (var rect in rects)
            {
                if (point.x > rect.xMin + 0.001f &&
                    point.x < rect.xMax - 0.001f &&
                    point.y > rect.yMin + 0.001f &&
                    point.y < rect.yMax - 0.001f)
                    return true;
            }

            return false;
        }

        private static void PlaceWallRun(
            Transform parent,
            Construction construction,
            List<GameObject> wallPrefabs,
            BoundaryEdge edge,
            float wallLength,
            float scale,
            float wallBaseHeight,
            float wallGapChance)
        {
            const float epsilon = 0.001f;
            var runLength = Vector2.Distance(edge.Start, edge.End);
            if (runLength <= epsilon)
                return;

            var direction = (edge.End - edge.Start).normalized;
            var placedLength = 0f;

            while (placedLength < runLength - epsilon)
            {
                var segmentLength = Mathf.Min(wallLength, runLength - placedLength);
                var center = edge.Start + direction * (placedLength + segmentLength * 0.5f);

                if (wallGapChance <= 0f || Random.value >= wallGapChance)
                    PlaceWall(parent, construction, GetRandomWall(wallPrefabs), center, edge.Direction, segmentLength / wallLength, scale, wallBaseHeight);

                placedLength += segmentLength;
            }
        }

        private static void PlaceWall(
            Transform parent,
            Construction construction,
            GameObject wallPrefab,
            Vector2 localCenter,
            EdgeDirection direction,
            float lengthScale,
            float scale,
            float wallBaseHeight)
        {
            var instance = PrefabUtility.InstantiatePrefab(wallPrefab, parent) as GameObject;
            if (instance == null)
                instance = Instantiate(wallPrefab, parent);

            Undo.RegisterCreatedObjectUndo(instance, "Generate House Wall");
            instance.name = $"{wallPrefab.name}{GENERATED_WALL_NAME_SUFFIX}";

            var worldPosition = construction.transform.position +
                                ToWorld(construction.Right) * localCenter.x +
                                ToWorld(construction.Forward) * localCenter.y +
                                Vector3.up * wallBaseHeight;
            var worldRotation = Quaternion.LookRotation(GetWallForward(construction, direction), Vector3.up);

            instance.transform.SetPositionAndRotation(worldPosition, worldRotation);
            instance.transform.localScale = new Vector3(scale * lengthScale, scale, scale);
        }

        private static Vector3 GetWallForward(Construction construction, EdgeDirection direction)
        {
            switch (direction)
            {
                case EdgeDirection.Forward:
                    return ToWorld(construction.Forward);
                case EdgeDirection.Back:
                    return -ToWorld(construction.Forward);
                case EdgeDirection.Right:
                    return ToWorld(construction.Right);
                case EdgeDirection.Left:
                    return -ToWorld(construction.Right);
                default:
                    return ToWorld(construction.Forward);
            }
        }

        private static bool TryGetWallLength(GameObject prefab, float scale, out float length)
        {
            length = 0f;

            if (!TryGetLocalFootprint(prefab, scale, out var min, out var max))
                return false;

            var size = max - min;
            length = Mathf.Max(size.x, size.y);
            return length > 0f;
        }

        private static GameObject GetRandomWall(List<GameObject> wallPrefabs)
        {
            return wallPrefabs[Random.Range(0, wallPrefabs.Count)];
        }

        private static bool TryGetHouseFootprint(
            Transform house,
            Construction construction,
            float padding,
            out Vector2 min,
            out Vector2 max)
        {
            min = default;
            max = default;

            if (!TryGetWorldFootprint(house, construction, out var rect, out _))
                return false;

            min = rect.min;
            max = rect.max;
            min -= Vector2.one * padding;
            max += Vector2.one * padding;
            return true;
        }

        private static bool TryGetWorldFootprint(Transform target, Construction construction, out Rect rect, out float topHeight)
        {
            rect = default;
            topHeight = 0f;

            if (target == null || construction == null)
                return false;

            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return false;

            var right = ToWorld(construction.Right);
            var forward = ToWorld(construction.Forward);
            var origin = construction.transform.position;
            var min = Vector2.zero;
            var max = Vector2.zero;
            var hasBounds = false;

            foreach (var renderer in renderers)
            {
                var rendererBounds = renderer.localBounds;
                var matrix = renderer.transform.localToWorldMatrix;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var corner = rendererBounds.center + Vector3.Scale(rendererBounds.extents, new Vector3(x, y, z));
                            var worldCorner = matrix.MultiplyPoint3x4(corner);
                            var offset = worldCorner - origin;
                            var point = new Vector2(Vector3.Dot(offset, right), Vector3.Dot(offset, forward));

                            if (!hasBounds)
                            {
                                min = point;
                                max = point;
                                topHeight = offset.y;
                                hasBounds = true;
                            }
                            else
                            {
                                min = Vector2.Min(min, point);
                                max = Vector2.Max(max, point);
                                topHeight = Mathf.Max(topHeight, offset.y);
                            }
                        }
                    }
                }
            }

            if (!hasBounds)
                return false;

            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private static Transform FindCatalogHouse(Transform houseEnvironment, ConstructionPropsCatalog houseCatalog)
        {
            if (houseEnvironment == null || houseCatalog == null)
                return null;

            foreach (var prefab in houseCatalog.Props)
            {
                if (prefab == null)
                    continue;

                for (int i = 0; i < houseEnvironment.childCount; i++)
                {
                    var child = houseEnvironment.GetChild(i);
                    if (NormalizeHouseName(child.name) == prefab.name &&
                        child.GetComponentInChildren<MeshRenderer>(true) != null)
                        return child;
                }
            }

            return null;
        }

        private static string NormalizeHouseName(string objectName)
        {
            return objectName.EndsWith(GENERATED_HOUSE_NAME_SUFFIX)
                ? objectName.Substring(0, objectName.Length - GENERATED_HOUSE_NAME_SUFFIX.Length)
                : objectName;
        }

        private void ClearGeneratedConcreteFloors()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            ClearGeneratedConcreteFloors(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private void ClearGeneratedWalls()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            ClearGeneratedWalls(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private void ClearGeneratedProps()
        {
            var placer = (ConstructionStage1EnvironmentGenerator)target;
            ClearGeneratedProps(placer.transform);

            EditorUtility.SetDirty(placer);
            EditorSceneManager.MarkSceneDirty(placer.gameObject.scene);
        }

        private static void ClearGeneratedConcreteFloors(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_FLOOR_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void ClearGeneratedWalls(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_WALL_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void ClearGeneratedProps(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.EndsWith(GENERATED_PROP_NAME_SUFFIX))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static bool TryGetLocalFootprint(GameObject prefab, float scale, out Vector2 min, out Vector2 max)
        {
            return TryGetLocalFootprint(prefab, Quaternion.identity, scale, out min, out max);
        }

        private static bool TryGetLocalFootprint(GameObject prefab, Quaternion rotation, float scale, out Vector2 min, out Vector2 max)
        {
            return TryGetLocalBounds(prefab, rotation, scale, out min, out max, out _);
        }

        private static bool TryGetLocalBounds(GameObject prefab, float scale, out Vector2 min, out Vector2 max, out float topY)
        {
            return TryGetLocalBounds(prefab, Quaternion.identity, scale, out min, out max, out topY);
        }

        private static bool TryGetLocalBounds(GameObject prefab, Quaternion rotation, float scale, out Vector2 min, out Vector2 max, out float topY)
        {
            min = default;
            max = default;
            topY = 0f;

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0)
                return false;

            var rootToLocal = prefab.transform.worldToLocalMatrix;
            var hasBounds = false;

            foreach (var renderer in renderers)
            {
                var matrix = rootToLocal * renderer.transform.localToWorldMatrix;
                var rendererBounds = renderer.localBounds;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            var corner = rendererBounds.center + Vector3.Scale(rendererBounds.extents, new Vector3(x, y, z));
                            var localCorner = rotation * (matrix.MultiplyPoint3x4(corner) * scale);
                            var point = new Vector2(localCorner.x, localCorner.z);

                            if (!hasBounds)
                            {
                                min = point;
                                max = point;
                                topY = localCorner.y;
                                hasBounds = true;
                            }
                            else
                            {
                                min = Vector2.Min(min, point);
                                max = Vector2.Max(max, point);
                                topY = Mathf.Max(topY, localCorner.y);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static Vector3 ToWorld(Vector2Int gridDirection)
        {
            return new Vector3(gridDirection.x, 0f, gridDirection.y);
        }

        private bool HasAvailableWallPrefab()
        {
            return GetAvailableWalls(GetSelectedWallPrefabs(), _wallCatalogProperty.objectReferenceValue as ConstructionPropsCatalog).Count > 0;
        }

        private List<GameObject> GetSelectedWallPrefabs()
        {
            var selectedWalls = new List<GameObject>();
            for (int i = 0; i < _wallPrefabsProperty.arraySize; i++)
            {
                var wall = _wallPrefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (wall != null)
                    selectedWalls.Add(wall);
            }

            return selectedWalls;
        }

        private static List<GameObject> GetAvailableWalls(IReadOnlyList<GameObject> selectedWalls, ConstructionPropsCatalog wallCatalog)
        {
            var walls = new List<GameObject>();
            if (selectedWalls != null)
            {
                foreach (var wall in selectedWalls)
                {
                    if (wall != null)
                        walls.Add(wall);
                }
            }

            if (walls.Count > 0)
                return walls;

            var firstWall = GetFirstCatalogProp(wallCatalog);
            if (firstWall != null)
                walls.Add(firstWall);

            return walls;
        }

        private static GameObject GetFirstCatalogProp(ConstructionPropsCatalog catalog)
        {
            if (catalog == null)
                return null;

            foreach (var prop in catalog.Props)
            {
                if (prop != null)
                    return prop;
            }

            return null;
        }

        private static void AddFirstCatalogProp(SerializedProperty listProperty, ConstructionPropsCatalog catalog)
        {
            var firstProp = GetFirstCatalogProp(catalog);
            if (firstProp == null)
                return;

            listProperty.arraySize = 1;
            listProperty.GetArrayElementAtIndex(0).objectReferenceValue = firstProp;
        }

        private static bool ContainsObjectReference(SerializedProperty listProperty, Object targetObject)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue == targetObject)
                    return true;
            }

            return false;
        }

        private static void SetObjectReferenceSelected(SerializedProperty listProperty, Object targetObject, bool selected)
        {
            if (selected)
            {
                if (ContainsObjectReference(listProperty, targetObject))
                    return;

                var index = listProperty.arraySize;
                listProperty.arraySize++;
                listProperty.GetArrayElementAtIndex(index).objectReferenceValue = targetObject;
                return;
            }

            for (int i = listProperty.arraySize - 1; i >= 0; i--)
            {
                if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue == targetObject)
                    listProperty.DeleteArrayElementAtIndex(i);
            }
        }

        private sealed class FloorTile
        {
            public readonly GameObject Prefab;
            public readonly Vector2 Min;
            public readonly Vector2 Size;

            public FloorTile(GameObject prefab, Vector2 min, Vector2 max)
            {
                Prefab = prefab;
                Min = min;
                Size = max - min;
            }
        }

        private readonly struct BoundaryEdge
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public readonly EdgeDirection Direction;

            public BoundaryEdge(Vector2 start, Vector2 end, EdgeDirection direction)
            {
                Start = start;
                End = end;
                Direction = direction;
            }
        }

        private enum EdgeDirection
        {
            Back,
            Right,
            Forward,
            Left
        }
    }

    public static class ConstructionConcreteFloorCatalogEditorUtility
    {
        private const string DEFAULT_CATALOG_NAME = "ConcreteFloorCatalog";

        public static ConstructionPropsCatalog FindDefaultCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:ConstructionPropsCatalog");
            if (guids.Length == 0)
                return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<ConstructionPropsCatalog>(path);
                if (catalog != null && catalog.name == DEFAULT_CATALOG_NAME)
                    return catalog;
            }

            return null;
        }
    }

    public static class ConstructionHouseWallCatalogEditorUtility
    {
        private const string DEFAULT_CATALOG_NAME = "HouseWallCatalog";

        public static ConstructionPropsCatalog FindDefaultCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:ConstructionPropsCatalog");
            if (guids.Length == 0)
                return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<ConstructionPropsCatalog>(path);
                if (catalog != null && catalog.name == DEFAULT_CATALOG_NAME)
                    return catalog;
            }

            return null;
        }
    }

    public static class ConstructionHousePropsCatalogEditorUtility
    {
        private const string DEFAULT_CATALOG_NAME = "HouseConstructionPropsCatalog";

        public static ConstructionPropsCatalog FindDefaultCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:ConstructionPropsCatalog");
            if (guids.Length == 0)
                return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var catalog = AssetDatabase.LoadAssetAtPath<ConstructionPropsCatalog>(path);
                if (catalog != null && catalog.name == DEFAULT_CATALOG_NAME)
                    return catalog;
            }

            return null;
        }
    }
#endif
}
