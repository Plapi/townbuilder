using UnityEngine;

public static class Constants
{
    public const float RAY_MAX_DISTANCE = 10000f;
    
    private const string INTERACTABLE_LAYER_NAME = "Interactable";
    private static LayerMask? _interactableLayer;
    public static LayerMask InteractableLayer
    {
        get
        {
            _interactableLayer ??= LayerMask.NameToLayer(INTERACTABLE_LAYER_NAME);
            return _interactableLayer.Value;
        }
    }
    
    private const string GROUND_LAYER_NAME = "Ground";
    private static LayerMask? _groundLayer;
    public static LayerMask GroundLayer
    {
        get
        {
            _groundLayer ??= LayerMask.NameToLayer(GROUND_LAYER_NAME);
            return _groundLayer.Value;
        }
    }
}
