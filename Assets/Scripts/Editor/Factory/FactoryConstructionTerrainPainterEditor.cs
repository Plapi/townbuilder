using com.Plapamaru.TownCrafter.Factory.EditorTools;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FactoryConstructionTerrainPainter))]
public class FactoryConstructionTerrainPainterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Paint Construction Terrain"))
        {
            var painter = (FactoryConstructionTerrainPainter)target;
            painter.PaintConstructionTerrain();
        }
    }
}
