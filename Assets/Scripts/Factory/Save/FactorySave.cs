using System;
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
            var saveData = new SaveData();
            foreach (var (_, entity) in entitiesDict)
            {
                if (!entities.Contains(entity))
                {
                    if (entity is Extractor extractor)
                        saveData.extractors.Add(EntityToSaveData<EntitySaveData>(extractor));
                    else if (entity is Conveyor conveyor)
                    {
                        var conveyorSaveData = EntityToSaveData<ConveyorSaveData>(conveyor);
                        if (conveyor.NextConveyor != null)
                            conveyorSaveData.nextConveyorGridPos = conveyor.NextConveyor.GridPos;
                        conveyorSaveData.beltDirection = conveyor.BeltDirection;
                        saveData.conveyors.Add(conveyorSaveData);
                    }

                    entities.Add(entity);
                }
            }

            var json = prettyJson ? saveData.AsPrettyJson() : saveData.AsJson();
            PlayerPrefs.SetString(FactoryConstants.FACTORY_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public SaveData Load()
        {
            if (!PlayerPrefs.HasKey(FactoryConstants.FACTORY_SAVE_KEY))
                return new SaveData();

            var json = PlayerPrefs.GetString(FactoryConstants.FACTORY_SAVE_KEY);
            return json.AsModel<SaveData>();
        }

        private static T EntityToSaveData<T>(Entity entity) where T : EntitySaveData, new()
        {
            return new T()
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
            json = json.AsModel<SaveData>().AsPrettyJson();
            GUIUtility.systemCopyBuffer = json;
            Debug.Log(json);
        }
    }

    [Serializable]
    public class SaveData
    {
        public List<EntitySaveData> extractors = new List<EntitySaveData>();
        public List<ConveyorSaveData> conveyors = new List<ConveyorSaveData>();
    }

    [Serializable]
    public class EntitySaveData
    {
        public string id;
        public Vector2Int gridPos;
        public int rotationY;
    }

    [Serializable]
    public class ConveyorSaveData : EntitySaveData
    {
        public Vector2Int? nextConveyorGridPos;
        public int beltDirection;
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