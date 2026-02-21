using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using JsonFx.Json;

public partial class DebugWindow
{
    private const string BOOKMARKS_GUID_KEY = KEY_PREFIX + "BOOKMARKS_GUID";

    [SerializeField]
    private List<Object> _bookmarks;
    
    private SerializedObject serializedObject;
    private SerializedProperty serializedProperty;
    
    private void OnEnable()
    {
        var guids = new List<string> (PlayerPrefs.HasKey(BOOKMARKS_GUID_KEY) ? 
            JsonReader.Deserialize<string[]>(PlayerPrefs.GetString(BOOKMARKS_GUID_KEY)) : new string[0]);
        
        _bookmarks = new List<Object>();
        foreach (var guid in guids)
            _bookmarks.Add(AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)));
        
        serializedObject = new SerializedObject(this);
        serializedProperty = serializedObject.FindProperty(nameof(_bookmarks));
    }
    
    private void OnGUIBookmarks()
    {
        serializedObject.Update();
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.PropertyField(serializedProperty, true);
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            
            var guids = new List<string>();
            foreach (var bookmark in _bookmarks)
                guids.Add(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(bookmark)).ToString());
            
            PlayerPrefs.SetString(BOOKMARKS_GUID_KEY, JsonWriter.Serialize(guids));
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
