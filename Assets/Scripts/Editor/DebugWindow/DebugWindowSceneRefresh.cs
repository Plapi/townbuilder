using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public partial class DebugWindow
{
    [MenuItem("Editor/Reload Current Scene Or Prefab %t")]
    private static void ReloadCurrentSceneOrPrefab() {
        if (!Application.isPlaying) {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(prefabStage.assetPath));
            } else {
                CheckSaveScene(() => {
                    var obj = Selection.activeGameObject;
                    EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);
                    Selection.activeGameObject = obj;
                });
            }
        }
    }
    
    private static void CheckSaveScene(Action onComplete) {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.isDirty) {
            if (EditorUtility.DisplayDialog("Save Scene", "Do you want to save " + scene.name + " before playing?", "Yes", "No")) {
                EditorSceneManager.SaveScene(scene, "", false);
                onComplete();
            } else {
                onComplete();
            }
        } else {
            onComplete();
        }
    }
}
