using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public partial class DebugWindow
{
    [MenuItem("Editor/Take Screenshot %k")]
    private static void TakeScreenshot() {
        EditorCoroutine.Start(TakeScreenshotIEnumerator());
    }
	
    private static IEnumerator TakeScreenshotIEnumerator() {
        string screenCaptureName = "ScreenCapture " + DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss") + ".png";
        
        ScreenCapture.CaptureScreenshot(screenCaptureName);
        while (!File.Exists(Application.dataPath.Replace("Assets", screenCaptureName))) {
            yield return null;
        }
        
        string screenshotPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + screenCaptureName;

        File.WriteAllBytes(screenshotPath, File.ReadAllBytes(Application.dataPath.Replace("Assets", screenCaptureName)));
        File.Delete(Application.dataPath.Replace("Assets", screenCaptureName));

        System.Diagnostics.Process m_process = new System.Diagnostics.Process {
            StartInfo = new System.Diagnostics.ProcessStartInfo(screenshotPath)
        };

        m_process.Start();
    }
}
