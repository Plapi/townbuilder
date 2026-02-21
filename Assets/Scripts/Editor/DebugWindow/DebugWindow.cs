using System;
using UnityEditor;
using UnityEngine;

public partial class DebugWindow : EditorWindow
{
    [MenuItem("Window/Debug Window")]
    private static void Init()
    {
        ((DebugWindow)GetWindow(typeof(DebugWindow))).Show();
    }
    
    private const string KEY_PREFIX = "TOWNBUILDER_EDITOR_";
    private const string SCROLL_POS_Y_KEY = KEY_PREFIX + "SCROLL_POS_Y";
    private const string FOLDABLE_SECTION_TOGGLE_KEY = KEY_PREFIX + "FOLDABLE_SECTION_TOGGLE_KEY";

    private static int _sectionIndex;
    
    private static Vector2 ScrollPos 
    {
        get => new Vector2(0f, EditorPrefs.GetFloat(SCROLL_POS_Y_KEY, 0f));
        set => EditorPrefs.SetFloat(SCROLL_POS_Y_KEY, value.y);
    }

    private static bool GetFoldableSectionToggle(int index)
    {
        return EditorPrefs.GetBool($"{FOLDABLE_SECTION_TOGGLE_KEY}_{index}", false);
    }

    private static bool SetFoldableSectionToggle(int index, bool toggle)
    {
        EditorPrefs.SetBool($"{FOLDABLE_SECTION_TOGGLE_KEY}_{index}", toggle);
        return toggle;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
        
        OnGUIBookmarks();
        
        Time.timeScale = EditorGUILayout.Slider("Time Scale", Time.timeScale, 0f, 1f);
        
        _sectionIndex = 0;
        FoldableSection("Game System", OnGUIGameSystem);
        FoldableSection("Default", OnGUIDefault);
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private static void OnGUIDefault()
    {
        if (GUILayout.Button("Delete Player Prefs")) {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
        
        if (GUILayout.Button("Take Screenshot"))
            EditorCoroutine.Start(TakeScreenshotIEnumerator());
        
        if (GUILayout.Button("Test")) 
        {
            
        }
    }
    
    private static void FoldableSection(string text, Action onComplete) {
        EditorGUILayout.Space();
        if (SetFoldableSectionToggle(_sectionIndex, 
                EditorGUILayout.BeginFoldoutHeaderGroup(GetFoldableSectionToggle(_sectionIndex), text)))
            onComplete.Invoke();
        EditorGUILayout.EndFoldoutHeaderGroup();
        _sectionIndex++;
    }
}
