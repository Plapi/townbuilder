using com.Plapamaru.TownCrafter.Game;
using UnityEditor;
using UnityEngine;

public partial class DebugWindow
{
    private static void OnGUIGameSystem()
    {
        if (Application.isPlaying == false)
            return;

        var stateNames = GameSystem.Instance.GetCurrentStateNames();
        foreach (var stateName in stateNames)
            EditorGUILayout.LabelField(stateName);
    }
}
