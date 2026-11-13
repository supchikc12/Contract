using System.Collections.Generic;
using UnityEngine;

// Список эффектов для загрузки по id при сохранении/загрузке.
[CreateAssetMenu(fileName = "Effect Database", menuName = "Effects/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    public List<EffectDefinition> effects = new List<EffectDefinition>();

    public EffectDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < effects.Count; i++)
        {
            EffectDefinition effect = effects[i];
            if (effect != null && effect.id == id)
                return effect;
        }

        return null;
    }
}
