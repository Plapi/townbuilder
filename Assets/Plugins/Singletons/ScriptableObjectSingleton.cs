using UnityEngine;

namespace com.Plapamaru.Singletons
{
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<T>(typeof(T).Name);
                    (instance as ScriptableObjectSingleton<T>).OnInitialize();
                }
                return instance;
            }
        }

        // Optional overridable method for initializing the instance.
        protected virtual void OnInitialize() { }
    }
}