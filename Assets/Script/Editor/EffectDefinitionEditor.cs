using UnityEditor;
using UnityEngine;

// Инспектор EffectDefinition: быстрые кнопки открытия мастера эффектов.
[CustomEditor(typeof(EffectDefinition))]
public class EffectDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Редактировать в мастере", GUILayout.Height(26f)))
            ItemCreationWizard.OpenForEffectEdit((EffectDefinition)target);

        if (GUILayout.Button("Создать новый эффект", GUILayout.Height(26f)))
            ItemCreationWizard.OpenEffectCreation();
    }
}
