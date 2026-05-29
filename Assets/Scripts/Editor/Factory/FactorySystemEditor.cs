using com.Plapamaru.TownCrafter.Factory;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FactorySystem))]
public class FactorySystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Set All Constructions To Debug Stage"))
            SetAllConstructionsToDebugStage();
    }

    private void SetAllConstructionsToDebugStage()
    {
        var factorySystem = (FactorySystem)target;
        var constructions = factorySystem.GetComponentsInChildren<Construction>(true);

        factorySystem.SetAllConstructionsToDebugStage();

        foreach (var construction in constructions)
            EditorUtility.SetDirty(construction);

        SceneView.RepaintAll();
    }
}
