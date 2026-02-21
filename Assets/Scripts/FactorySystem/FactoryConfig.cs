using UnityEngine;

[CreateAssetMenu(fileName = "FactoryConfig", menuName = "Scriptable Objects/FactoryConfig")]
public class FactoryConfig : ScriptableObjectSingleton<FactoryConfig>
{
    public Color correctColor;
    public Color wrongColor;
}
