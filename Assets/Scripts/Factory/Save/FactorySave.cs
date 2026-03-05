using System.Collections.Generic;
using com.Plapamaru.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.Plapamaru.TownCrafter.Factory
{
    public class FactorySaveSystem : MonoBehaviour
    {
        [SerializeField] private bool prettyJson;

        public void Save(Dictionary<Vector2Int, Entity> entitiesDict)
        {
            var entities = new List<Entity>();
            var entitiesSaveData = new List<EntitySaveData>();
            foreach (var (_, entity) in entitiesDict)
            {
                if (!entities.Contains(entity) && entity is Conveyor or Extractor)
                {
                    entities.Add(entity);
                    entitiesSaveData.Add(EntityToSaveData(entity));
                }
            }

            var json = prettyJson ? entitiesSaveData.AsPrettyJson() : entitiesSaveData.AsJson();
            PlayerPrefs.SetString(FactoryConstants.FACTORY_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public List<EntitySaveData> Load()
        {
            if (!PlayerPrefs.HasKey(FactoryConstants.FACTORY_SAVE_KEY))
                return new List<EntitySaveData>();

            var json = PlayerPrefs.GetString(FactoryConstants.FACTORY_SAVE_KEY);
            return json.AsModel<List<EntitySaveData>>();
        }

        private static EntitySaveData EntityToSaveData(Entity entity)
        {
            return new EntitySaveData
            {
                id = entity.Id,
                gridPos = entity.GridPos,
                rotationY = Mathf.RoundToInt(entity.transform.eulerAngles.y),
            };
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(FactoryConstants.FACTORY_SAVE_KEY);
            PlayerPrefs.Save();
        }

        public static void LogAndCopy()
        {
            var json = PlayerPrefs.GetString(FactoryConstants.FACTORY_SAVE_KEY);
            json = json.AsModel<List<EntitySaveData>>().AsPrettyJson();
            GUIUtility.systemCopyBuffer = json;
            Debug.Log(json);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FactorySaveSystem))]
    public class FactorySaveSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Delete"))
                FactorySaveSystem.Delete();
            if (GUILayout.Button("Log and Copy"))
                FactorySaveSystem.LogAndCopy();
        }
    }
#endif
}