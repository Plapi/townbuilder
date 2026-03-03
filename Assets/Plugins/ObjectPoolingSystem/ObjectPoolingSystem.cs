using System.Collections.Generic;
using com.Plapamaru.Singletons;
using UnityEngine;

namespace com.Plapamaru.Pooling
{
    public class ObjectPoolingSystem : MonoBehaviourSingleton<ObjectPoolingSystem>
    {
        [SerializeField] private List<MonoBehaviour> _items;

        private readonly Dictionary<string, Queue<IPoolableObject>> _pools = new Dictionary<string, Queue<IPoolableObject>>();

        protected override void Awake()
        {
            base.Awake();
            foreach (var item in _items)
            {
                if (item is not IPoolableObject poolableObject)
                {
                    Debug.LogError($"{item.name} is not type of {nameof(IPoolableObject)}");
                    continue;
                }

                var queue = new Queue<IPoolableObject>();
                for (int i = 0; i < poolableObject.CacheCount; i++)
                {
                    var obj = Instantiate(poolableObject.Behaviour, transform);
                    obj.gameObject.SetActive(false);
                    obj.name = $"{item.name}{i}";
                    queue.Enqueue(obj as IPoolableObject);
                }
                _pools.Add(poolableObject.Id, queue);
            }
        }

        public T GetObject<T>(string id, Transform parent = null)
        {
            if (_pools.TryGetValue(id, out var queue) == false)
                throw new KeyNotFoundException($"Object with id {id} not found");

            var obj = (MonoBehaviour)null;
            if (queue.Count == 0)
            {
                if (TryGetItem(id, out obj) == false)
                    throw new KeyNotFoundException($"Object with id {id} not found");
                obj = Instantiate(obj, transform);
                obj.name = $"{id}";
            }
            else
            {
                obj = queue.Dequeue().Behaviour;
            }

            if (obj is not T objT)
                throw new KeyNotFoundException($"Object with id {id} with type {typeof(T).Name} not found");

            if (parent != null)
            {
                obj.transform.parent = parent;
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            obj.gameObject.SetActive(true);

            return objT;
        }

        public void ReleaseObject(IPoolableObject poolableObject)
        {
            if (_pools.TryGetValue(poolableObject.Id, out var queue) == false)
                throw new KeyNotFoundException($"Pool with id {poolableObject.Id} not found");

            var obj = poolableObject.Behaviour;
            obj.transform.parent = transform;
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.gameObject.SetActive(false);
            queue.Enqueue(poolableObject);

            poolableObject.OnRelease();
        }

        private bool TryGetItem(string id, out MonoBehaviour obj)
        {
            obj = null;
            foreach (var item in _items)
            {
                if (item is not IPoolableObject poolableObject)
                {
                    Debug.LogError($"{item.name} is not type of {nameof(IPoolableObject)}");
                    continue;
                }

                if (poolableObject.Id == id)
                {
                    obj = item;
                    return true;
                }
            }
            return false;
        }
    }
}