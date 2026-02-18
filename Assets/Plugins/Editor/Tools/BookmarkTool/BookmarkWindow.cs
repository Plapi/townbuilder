using UnityEngine;
using UnityEditor;

public class BookmarkWindow : EditorWindow
{
    private Vector2 _scroll;

    [MenuItem("Tools/Bookmarks")]
    public static void ShowWindow()
    {
        GetWindow<BookmarkWindow>("Bookmarks");
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        DrawDropArea();
        GUILayout.Space(10);
        DrawBookmarks();
    }

    private void DrawDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Any Unity Object Here", EditorStyles.helpBox);

        Event evt = Event.current;

        if (!dropArea.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (Object dragged in DragAndDrop.objectReferences)
            {
                BookmarkStorage.instance.Add(dragged);
            }

            evt.Use();
        }
    }

    private void DrawBookmarks()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        var list = BookmarkStorage.instance.Objects;

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            list[i] = EditorGUILayout.ObjectField(list[i], typeof(Object), true);

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = list[i];
                EditorGUIUtility.PingObject(list[i]);
            }

            if (i > 0 && GUILayout.Button("U", GUILayout.Width(25)))
            {
                var item = list[i];
                BookmarkStorage.instance.RemoveAt(i);
                BookmarkStorage.instance.InsertAt(i - 1, item);
                EditorGUILayout.EndHorizontal();
                break;
            }
            
            if (i < list.Count - 1 && GUILayout.Button("D", GUILayout.Width(25)))
            {
                var item = list[i];
                BookmarkStorage.instance.RemoveAt(i);
                BookmarkStorage.instance.InsertAt(i + 1, item);
                EditorGUILayout.EndHorizontal();
                break;
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                BookmarkStorage.instance.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}