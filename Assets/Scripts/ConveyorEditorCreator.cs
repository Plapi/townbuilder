#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class ConveyorEditorCreator : MonoBehaviour
{
    [SerializeField] private Object _targetFolder;
    
    public void Create()
    {
        Vector3 prevPos = transform.position;
        Quaternion prevRot = transform.rotation;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        var obj0 = MeshUtils.Combine(transform, false);
        var obj1 = MeshUtils.OptimizingMeshByReducingToOneMaterial(obj0.GetComponent<MeshRenderer>());
        
        DestroyImmediate(obj0);
        
        transform.SetPositionAndRotation(prevPos, prevRot);
        
        var folderPath = AssetDatabase.GetAssetPath(_targetFolder);
        
        var meshRenderer = obj1.GetComponent<MeshRenderer>();
        var meshFilter = obj1.GetComponent<MeshFilter>();
        
        var texturePath = $"{folderPath}/Texture.png";
        File.WriteAllBytes(texturePath, ((Texture2D)meshRenderer.sharedMaterial.mainTexture).EncodeToPNG());
        AssetDatabase.ImportAsset(texturePath);
        
        var meshPath = $"{folderPath}/Mesh.asset";
        AssetDatabase.CreateAsset(meshFilter.sharedMesh, meshPath);
        
        string matPath = $"{folderPath}/Material.mat";
        AssetDatabase.CreateAsset(meshRenderer.sharedMaterial, matPath);
        
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        meshRenderer.sharedMaterial = material;
        meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

        PrefabUtility.SaveAsPrefabAsset(obj1, $"{folderPath}/{_targetFolder.name}.prefab");
        
        DestroyImmediate(obj1);
    }
}

[CustomEditor(typeof(ConveyorEditorCreator))]
public class ConveyorEditorCreatorEditor : Editor
{
    private ConveyorEditorCreator _target;

    private void OnEnable()
    {
        _target = (ConveyorEditorCreator)target;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create"))
            _target.Create();
    }
}
#endif