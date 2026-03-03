using System.Collections.Generic;
using UnityEngine;

public static class LayersUtils
{
    private static readonly Dictionary<LayerType, LayerMask> _layers = new Dictionary<LayerType, LayerMask>();
    
    public static LayerMask GetLayer(LayerType layerType)
    {
        if (!_layers.TryGetValue(layerType, out LayerMask layerMask))
        {
            layerMask = LayerMask.NameToLayer(layerType.ToString());
            _layers.Add(layerType, layerMask);
        }
        return layerMask;
    }
    
    private const float RAY_MAX_DISTANCE = 10000f;
    
    public static bool Raycast(Camera camera, LayerType layerType, out RaycastHit hit)
    {
        return Raycast(camera, layerType, Input.mousePosition, out hit);
    }
    
    public static bool Raycast(Camera camera, LayerType layerType, Vector3 screenPosition, out RaycastHit hit)
    {
        var ray = camera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hit, RAY_MAX_DISTANCE, 1 << GetLayer(layerType));
    }
}
