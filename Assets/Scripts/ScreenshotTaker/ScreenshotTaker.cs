#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

public class ScreenshotTaker : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Object _locationFolder;
    [SerializeField] private string _fileName;
    [SerializeField] private Preset _importSettings;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SaveScreenshot();
        }
    }

    public void SaveScreenshot()
    {
        var path = $"{AssetDatabase.GetAssetPath(_locationFolder)}/{_fileName}.png";
        SaveScreenshot(path, _importSettings);
    }

    public void SaveScreenshot(string folderPath, string fileName, Preset importSettings)
    {
        SaveScreenshot($"{folderPath}/{fileName}.png", importSettings);
    }

    private void SaveScreenshot(string path, Preset importSettings)
    {
        File.WriteAllBytes(path, TakeScreenshot());
        AssetDatabase.ImportAsset(path);
        ApplyImportSettings(path, importSettings);
        AssetDatabase.Refresh();
    }
    
    private byte[] TakeScreenshot()
    {
        var width = 512;//Screen.width;
        var height = 512;//Screen.height;
        
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _camera.targetTexture = rt;
        
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0, 0, 0, 0);
        
        _camera.Render();
        
        RenderTexture.active = rt;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        
        var bytes = texture.EncodeToPNG();
        
        _camera.targetTexture = null;
        RenderTexture.active = null;
        if (Application.isPlaying)
        {
            Destroy(rt);
            Destroy(texture);    
        }
        else
        {
            DestroyImmediate(rt);
            DestroyImmediate(texture);
        }
        
        return bytes;
    }

    private void ApplyImportSettings(string path, Preset importSettings)
    {
        if (importSettings == null)
        {
            return;
        }

        var targetImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (targetImporter == null)
        {
            return;
        }

        if (!importSettings.ApplyTo(targetImporter))
        {
            Debug.LogWarning($"Could not apply import settings preset to {path}.", this);
            return;
        }

        targetImporter.SaveAndReimport();
    }

    [CustomEditor(typeof(ScreenshotTaker))]
    private class ScreenshotTakerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Take Screenshot"))
            {
                ((ScreenshotTaker)target).SaveScreenshot();
            }
        }
    }
}
#endif
