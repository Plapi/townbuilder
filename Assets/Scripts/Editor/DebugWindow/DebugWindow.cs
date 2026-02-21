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
    private const string SCROLL_POS_Y = KEY_PREFIX + "SCROLL_POS_Y";
    
    private static Vector2 ScrollPos 
    {
        get => new Vector2(0f, EditorPrefs.GetFloat(SCROLL_POS_Y, 0f));
        set => EditorPrefs.SetFloat(SCROLL_POS_Y, value.y);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
        
        OnGUIBookmarks();
        
        Time.timeScale = EditorGUILayout.Slider("Time Scale", Time.timeScale, 0f, 1f);
        
        if (GUILayout.Button("Delete Player Prefs")) {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
        
        if (GUILayout.Button("Take Screenshot"))
            EditorCoroutine.Start(TakeScreenshotIEnumerator());
        
        if (GUILayout.Button("Test")) 
        {
            
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
}
