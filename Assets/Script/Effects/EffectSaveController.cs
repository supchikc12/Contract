using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Debug-сохранение активных эффектов игрока в JSON.
public class EffectSaveController : MonoBehaviour
{
    [Serializable]
    public class SavedEffect
    {
        public string effectId;
        public EffectTargetMask targetMask;
        public int stacks;
        public float remainingTime;
        public bool permanent;
        public bool discovered;
    }

    [Serializable]
    public class SaveData
    {
        public List<SavedEffect> effects = new List<SavedEffect>();
    }

    [SerializeField] private EffectDatabase database;
    [SerializeField] private CharacterEffectAggregator aggregator;
    [SerializeField] private KeyCode saveKey = KeyCode.F8;
    [SerializeField] private KeyCode loadKey = KeyCode.F9;

    private string SavePath => Path.Combine(Application.persistentDataPath, "effects_save.json");

    private void Awake()
    {
        if (aggregator == null) aggregator = GetComponent<CharacterEffectAggregator>();
        if (database == null) database = Resources.Load<EffectDatabase>("Effects/EffectDatabase");
    }

    private void Update()
    {
        if (Input.GetKeyDown(saveKey)) Save();
        if (Input.GetKeyDown(loadKey)) Load();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        if (aggregator == null || database == null) return;

        SaveData data = new SaveData();
        List<ActiveEffect> effects = aggregator.GetSavableEffects();

        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect effect = effects[i];
            if (effect == null || effect.Definition == null || string.IsNullOrEmpty(effect.Definition.id))
                continue;

            data.effects.Add(new SavedEffect
            {
                effectId = effect.Definition.id,
                targetMask = effect.TargetMask,
                stacks = effect.Stacks,
                remainingTime = effect.RemainingTime,
                permanent = effect.Permanent,
                discovered = effect.Discovered
            });
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Ёффекты сохранены: " + SavePath);
    }

    public void Load()
    {
        if (aggregator == null || database == null || !File.Exists(SavePath)) return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null || data.effects == null) return;

        aggregator.ClearAllEffects();

        for (int i = 0; i < data.effects.Count; i++)
        {
            SavedEffect saved = data.effects[i];
            EffectDefinition definition = database.Find(saved.effectId);
            if (definition == null) continue;

            aggregator.ApplySavedEffect(
                definition,
                saved.targetMask,
                saved.stacks,
                saved.remainingTime,
                saved.permanent,
                saved.discovered);
        }

        Debug.Log("Ёффекты загружены: " + SavePath);
    }
}
