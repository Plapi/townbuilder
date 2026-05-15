using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneGameViewResolutionSaverAuto
{
    private const string PrefKeyPrefix = "SceneGameViewResolution_";
    private const double SaveIntervalSeconds = 0.5f;

    private static readonly BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    private static readonly Type GameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
    private static readonly Type GameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
    private static readonly Type GameViewSizeGroupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroup");
    private static readonly Type GameViewSizeKindType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");

    private static double _nextSaveTime;
    private static string _lastSavedSceneKey;
    private static string _lastSavedResolutionKey;
    private static bool _isRestoring;

    static SceneGameViewResolutionSaverAuto()
    {
        EditorApplication.update += OnEditorUpdate;
        EditorSceneManager.sceneOpened += OnSceneOpened;

        EditorApplication.delayCall += () => RestoreResolution(SceneManager.GetActiveScene());
    }

    private static void OnEditorUpdate()
    {
        if (_isRestoring || EditorApplication.timeSinceStartup < _nextSaveTime)
        {
            return;
        }

        _nextSaveTime = EditorApplication.timeSinceStartup + SaveIntervalSeconds;
        SaveCurrentResolution();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => RestoreResolution(scene);
    }

    private static void SaveCurrentResolution()
    {
        if (GameViewType == null || !TryGetCurrentResolution(out SavedResolution resolution))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        string sceneKey = GetSceneKey(scene);
        string resolutionKey = resolution.GetResolutionKey();
        if (string.IsNullOrEmpty(sceneKey) ||
            (_lastSavedSceneKey == sceneKey && _lastSavedResolutionKey == resolutionKey))
        {
            return;
        }

        EditorPrefs.SetString(PrefKeyPrefix + sceneKey, JsonUtility.ToJson(resolution));
        _lastSavedSceneKey = sceneKey;
        _lastSavedResolutionKey = resolutionKey;
    }

    private static void RestoreResolution(Scene scene)
    {
        string sceneKey = GetSceneKey(scene);
        if (string.IsNullOrEmpty(sceneKey))
        {
            return;
        }

        string prefKey = PrefKeyPrefix + sceneKey;
        if (!EditorPrefs.HasKey(prefKey))
        {
            return;
        }

        SavedResolution resolution = JsonUtility.FromJson<SavedResolution>(EditorPrefs.GetString(prefKey));
        EditorWindow gameView = GetGameView();
        object group = GetCurrentSizeGroup();
        if (gameView == null || group == null)
        {
            return;
        }

        int index = FindResolutionIndex(group, resolution);
        if (index < 0 && resolution.width > 0 && resolution.height > 0)
        {
            index = AddCustomResolution(group, resolution);
        }

        if (index < 0)
        {
            return;
        }

        _isRestoring = true;
        try
        {
            SetSelectedSizeIndex(gameView, index);
            gameView.Repaint();
            _lastSavedSceneKey = sceneKey;
            _lastSavedResolutionKey = resolution.GetResolutionKey();
        }
        finally
        {
            EditorApplication.delayCall += () => _isRestoring = false;
        }
    }

    private static bool TryGetCurrentResolution(out SavedResolution resolution)
    {
        resolution = default;

        EditorWindow gameView = GetGameView();
        if (gameView == null)
        {
            return false;
        }

        object gameViewSize = GameViewType.GetProperty("currentGameViewSize", AllFlags)?.GetValue(gameView);
        if (gameViewSize == null)
        {
            return false;
        }

        resolution = new SavedResolution
        {
            width = GetIntProperty(gameViewSize, "width"),
            height = GetIntProperty(gameViewSize, "height"),
            baseText = GetStringProperty(gameViewSize, "baseText"),
            sizeType = GetSizeTypeValue(gameViewSize),
            selectedIndex = GetIntProperty(gameView, "selectedSizeIndex"),
        };

        return true;
    }

    private static EditorWindow GetGameView()
    {
        if (GameViewType == null)
        {
            return null;
        }

        UnityEngine.Object[] gameViews = Resources.FindObjectsOfTypeAll(GameViewType);
        return gameViews.Length > 0 ? gameViews[0] as EditorWindow : null;
    }

    private static object GetCurrentSizeGroup()
    {
        object gameViewSizes = GetGameViewSizesInstance();
        object currentGroup = GameViewSizesType?.GetProperty("currentGroup", AllFlags)?.GetValue(gameViewSizes);
        if (currentGroup != null)
        {
            return currentGroup;
        }

        object currentGroupType = GameViewType?.GetProperty("currentSizeGroupType", AllFlags)?.GetValue(null);
        MethodInfo getGroup = GameViewSizesType?.GetMethod("GetGroup", AllFlags);
        return currentGroupType == null ? null : getGroup?.Invoke(gameViewSizes, new[] { currentGroupType });
    }

    private static object GetGameViewSizesInstance()
    {
        PropertyInfo instance = GameViewSizesType?.BaseType?.GetProperty("instance", AllFlags);
        return instance?.GetValue(null);
    }

    private static int FindResolutionIndex(object group, SavedResolution resolution)
    {
        int totalCount = (int)GameViewSizeGroupType.GetMethod("GetTotalCount", AllFlags).Invoke(group, null);
        for (int i = 0; i < totalCount; i++)
        {
            object size = GetGameViewSize(group, i);
            if (size == null)
            {
                continue;
            }

            bool sameDimensions = GetIntProperty(size, "width") == resolution.width &&
                                  GetIntProperty(size, "height") == resolution.height;
            bool sameType = GetSizeTypeValue(size) == resolution.sizeType;
            if (sameDimensions && sameType)
            {
                return i;
            }
        }

        return -1;
    }

    private static int AddCustomResolution(object group, SavedResolution resolution)
    {
        object fixedResolutionType = Enum.ToObject(GameViewSizeKindType, 1);
        string label = string.IsNullOrEmpty(resolution.baseText)
            ? $"Scene Resolution {resolution.width}x{resolution.height}"
            : resolution.baseText;
        object newSize = Activator.CreateInstance(GameViewSizeType, fixedResolutionType, resolution.width, resolution.height, label);

        GameViewSizeGroupType.GetMethod("AddCustomSize", AllFlags).Invoke(group, new[] { newSize });
        GameViewSizesType.GetMethod("SaveToHDD", AllFlags)?.Invoke(GetGameViewSizesInstance(), null);

        return FindResolutionIndex(group, resolution);
    }

    private static object GetGameViewSize(object group, int index)
    {
        return GameViewSizeGroupType.GetMethod("GetGameViewSize", AllFlags).Invoke(group, new object[] { index });
    }

    private static void SetSelectedSizeIndex(EditorWindow gameView, int index)
    {
        PropertyInfo selectedSizeIndex = GameViewType.GetProperty("selectedSizeIndex", AllFlags);
        if (selectedSizeIndex?.CanWrite == true)
        {
            selectedSizeIndex.SetValue(gameView, index);
            return;
        }

        GameViewType.GetMethod("SizeSelectionCallback", AllFlags)?.Invoke(gameView, new object[] { index, null });
    }

    private static int GetIntProperty(object target, string propertyName)
    {
        return (int)target.GetType().GetProperty(propertyName, AllFlags).GetValue(target);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        return (string)target.GetType().GetProperty(propertyName, AllFlags).GetValue(target);
    }

    private static int GetSizeTypeValue(object gameViewSize)
    {
        object sizeType = gameViewSize.GetType().GetProperty("sizeType", AllFlags).GetValue(gameViewSize);
        return Convert.ToInt32(sizeType);
    }

    private static string GetSceneKey(Scene scene)
    {
        return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
    }

    [Serializable]
    private struct SavedResolution
    {
        public int width;
        public int height;
        public string baseText;
        public int sizeType;
        public int selectedIndex;

        public string GetResolutionKey()
        {
            return $"{sizeType}:{width}x{height}:{baseText}:{selectedIndex}";
        }
    }
}
