using UnityEditor;
using UnityEngine;

// »нспектор данных предмета. —ама генераци€ вынесена в ItemPrefabFactory.
[CustomEditor(typeof(ItemData), true)]
public class ItemPrefabGenerator : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemData data = (ItemData)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("√енераци€ prefabs", EditorStyles.boldLabel);

        foreach (string error in ItemPrefabFactory.Validate(data))
            EditorGUILayout.HelpBox(error, MessageType.Warning);

        if (GUILayout.Button("—генерировать 2D и 3D", GUILayout.Height(32)))
        {
            if (ItemPrefabFactory.Generate(data, PrefabOverwriteMode.Overwrite))
                Debug.Log($"ItemPrefabGenerator: prefabs дл€ {data.itemName} сгенерированы.");
        }

        if (GUILayout.Button("–едактировать в мастере", GUILayout.Height(26)))
            ItemCreationWizard.OpenForEdit(data);

        if (GUILayout.Button("ќткрыть мастер создани€ предмета", GUILayout.Height(26)))
            ItemCreationWizard.OpenWindow();

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "2D-префаб генерируетс€ первым, 3D Ч вторым. " +
            "—сылки modelPrefab2D/modelPrefab3D и childrenUI проставл€ютс€ автоматически.",
            MessageType.Info);
    }
}
