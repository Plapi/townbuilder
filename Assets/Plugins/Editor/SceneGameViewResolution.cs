using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "SceneGameViewResolutionSettings", menuName = "Editor/Scene Game View Resolution Settings")]
public class SceneGameViewResolutionSettings : ScriptableObject
{
    [SerializeField] private List<SceneGameViewResolutionEntry> _sceneResolutions = new List<SceneGameViewResolutionEntry>();

    public IReadOnlyList<SceneGameViewResolutionEntry> SceneResolutions => _sceneResolutions;

    public void AddOrUpdateCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        string scenePath = scene.path;
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("Save the current scene before adding a Game view resolution entry.");
            return;
        }

        if (!SceneGameViewResolutionUtility.TryGetCurrentResolution(out SceneGameViewResolutionEntry resolution))
        {
            Debug.LogWarning("Could not read the current Game view resolution. Open the Game window and choose a resolution first.");
            return;
        }

        resolution.scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        resolution.sceneName = scene.name;
        resolution.scenePath = scenePath;

        int existingIndex = _sceneResolutions.FindIndex(entry => entry.scenePath == scenePath);
        if (existingIndex >= 0)
        {
            _sceneResolutions[existingIndex] = resolution;
        }
        else
        {
            _sceneResolutions.Add(resolution);
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssetIfDirty(this);
    }

    public bool TryGetResolution(Scene scene, out SceneGameViewResolutionEntry resolution)
    {
        string scenePath = scene.path;
        for (int i = 0; i < _sceneResolutions.Count; i++)
        {
            if (_sceneResolutions[i].scenePath == scenePath)
            {
                resolution = _sceneResolutions[i];
                return true;
            }
        }

        resolution = default;
        return false;
    }
}

[Serializable]
public struct SceneGameViewResolutionEntry
{
    public SceneAsset scene;
    public string sceneName;
    public string scenePath;
    public int width;
    public int height;
    public string baseText;
    public int sizeType;
    public int selectedIndex;
}

[CustomEditor(typeof(SceneGameViewResolutionSettings))]
public class SceneGameViewResolutionSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Add Current Scene"))
        {
            ((SceneGameViewResolutionSettings)target).AddOrUpdateCurrentScene();
        }
    }
}

[InitializeOnLoad]
public static class SceneGameViewResolutionSaverAuto
{
    private static bool _isRestoring;

    static SceneGameViewResolutionSaverAuto()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += () => RestoreResolution(SceneManager.GetActiveScene());
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => RestoreResolution(scene);
    }

    private static void RestoreResolution(Scene scene)
    {
        if (_isRestoring)
        {
            return;
        }

        SceneGameViewResolutionSettings settings = FindSettings();
        if (settings == null || !settings.TryGetResolution(scene, out SceneGameViewResolutionEntry resolution))
        {
            return;
        }

        _isRestoring = true;
        try
        {
            SceneGameViewResolutionUtility.ApplyResolution(resolution);
        }
        finally
        {
            EditorApplication.delayCall += () => _isRestoring = false;
        }
    }

    private static SceneGameViewResolutionSettings FindSettings()
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneGameViewResolutionSettings)}");
        if (guids.Length == 0)
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SceneGameViewResolutionSettings>(path);
    }
}

public static class SceneGameViewResolutionUtility
{
    private static readonly BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    private static readonly Type GameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
    private static readonly Type GameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
    private static readonly Type GameViewSizeGroupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroup");
    private static readonly Type GameViewSizeKindType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");

    public static bool TryGetCurrentResolution(out SceneGameViewResolutionEntry resolution)
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

        resolution = new SceneGameViewResolutionEntry
        {
            width = GetIntProperty(gameViewSize, "width"),
            height = GetIntProperty(gameViewSize, "height"),
            baseText = GetStringProperty(gameViewSize, "baseText"),
            sizeType = GetSizeTypeValue(gameViewSize),
            selectedIndex = GetIntProperty(gameView, "selectedSizeIndex"),
        };

        return true;
    }

    public static void ApplyResolution(SceneGameViewResolutionEntry resolution)
    {
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

        SetSelectedSizeIndex(gameView, index);
        SetScaleToMinimum(gameView);
        gameView.Repaint();
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

    private static int FindResolutionIndex(object group, SceneGameViewResolutionEntry resolution)
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

    private static int AddCustomResolution(object group, SceneGameViewResolutionEntry resolution)
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

    private static void SetScaleToMinimum(EditorWindow gameView)
    {
        MethodInfo snapZoom = GameViewType.GetMethod("SnapZoom", AllFlags);
        if (snapZoom == null)
        {
            return;
        }

        float minimumScale = GetMinimumScale(gameView);
        snapZoom.Invoke(gameView, new object[] { minimumScale });
        EditorApplication.delayCall += () =>
        {
            if (gameView != null)
            {
                snapZoom.Invoke(gameView, new object[] { GetMinimumScale(gameView) });
                gameView.Repaint();
            }
        };
    }

    private static float GetMinimumScale(EditorWindow gameView)
    {
        PropertyInfo minScale = GameViewType.GetProperty("minScale", AllFlags);
        return minScale == null ? 0f : (float)minScale.GetValue(gameView);
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
}
