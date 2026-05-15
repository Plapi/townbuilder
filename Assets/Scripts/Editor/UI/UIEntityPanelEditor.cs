using System;
using System.Linq;
using com.Plapamaru.TownCrafter.Factory;
using com.Plapamaru.TownCrafter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(UIEntityPanel))]
public class UIEntityPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Select Next Entity Data"))
            SelectNextEntityData();

        if (GUILayout.Button("Force Update UI"))
            ForceUpdateUI();
    }

    private void SelectNextEntityData()
    {
        var debugEntityDataProperty = serializedObject.FindProperty("_debugEntityData");
        var entityData = AssetDatabase
            .FindAssets("t:EntityData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => AssetDatabase.LoadAssetAtPath<EntityData>(path))
            .Where(data => data != null)
            .ToArray();

        if (entityData.Length == 0)
        {
            Debug.LogWarning("No EntityData assets found.");
            return;
        }

        var current = debugEntityDataProperty.objectReferenceValue as EntityData;
        var currentIndex = Array.IndexOf(entityData, current);
        var next = entityData[(currentIndex + 1) % entityData.Length];
        var panel = (UIEntityPanel)target;

        Undo.RecordObject(panel, "Select Next Entity Data");
        panel.DebugSetEntityData(next);

        serializedObject.Update();
        debugEntityDataProperty.objectReferenceValue = next;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void ForceUpdateUI()
    {
        var panel = (UIEntityPanel)target;

        Undo.RecordObject(panel, "Force Update Entity UI");
        panel.DebugUpdateUI();

        EditorUtility.SetDirty(panel);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}