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
                        saveData.extractors.Add(extractor.ToSaveData());
                    else if (entity is Conveyor conveyor)
                        saveData.conveyors.Add(conveyor.ToSaveData());

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
        public GridPosition gridPos;
        public int rotationY;
    }

    [Serializable]
    public class ConveyorSaveData : EntitySaveData
    {
        public GridPosition? nextConveyorGridPos;
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