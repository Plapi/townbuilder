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

        if (GUILayout.Button("Set All Constructions To Max Stage"))
            SetAllConstructionsToMaxStage();
    }

    private void SetAllConstructionsToMaxStage()
    {
        var factorySystem = (FactorySystem)target;
        var constructions = factorySystem.GetComponentsInChildren<Construction>(true);

        factorySystem.SetAllConstructionsToMaxStage();

        foreach (var construction in constructions)
            EditorUtility.SetDirty(construction);

        SceneView.RepaintAll();
    }
}