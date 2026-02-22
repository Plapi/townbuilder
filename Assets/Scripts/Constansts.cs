using UnityEngine;

public static class Constants
{
    private const string INTERACTABLE_LAYER_NAME = "Interactable";

    private static LayerMask _interactableLayer;
    public static LayerMask InteractableLayer => LayerMask.NameToLayer(INTERACTABLE_LAYER_NAME);
}
