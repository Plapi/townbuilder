using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class OverlayCameraSync : MonoBehaviour
{
    [SerializeField] private Camera _baseCamera;
    private Camera _overlayCamera;
    
    private void OnEnable()
    {
        _overlayCamera = GetComponent<Camera>();
        Sync();
    }
    
    private void Update()
    {
        if (!Application.isPlaying)
            Sync();
    }
    
    private void LateUpdate()
    {
        if (Application.isPlaying)
            Sync();
    }
    
    private void Sync()
    {
        if (_baseCamera == null) return;

        _overlayCamera.fieldOfView = _baseCamera.fieldOfView;
        _overlayCamera.orthographic = _baseCamera.orthographic;
        _overlayCamera.orthographicSize = _baseCamera.orthographicSize;
        _overlayCamera.nearClipPlane = _baseCamera.nearClipPlane;
        _overlayCamera.farClipPlane = _baseCamera.farClipPlane;
    }
}