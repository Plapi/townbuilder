using UnityEngine;

public interface IPoolableObject
{
    private const int DEFAULT_POOL_SIZE = 10;
    
    string Id { get; }
    MonoBehaviour Behaviour { get; }
    int CacheCount => DEFAULT_POOL_SIZE;
}