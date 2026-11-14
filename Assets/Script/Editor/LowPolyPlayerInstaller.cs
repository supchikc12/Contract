using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Встраивает низкополигональную модель игрока (Assets/Models/Player/PlayerLowPoly.fbx)
// в сцену: создаёт Player/VisualBody/PlayerModel, выравнивает модель по старому
// примитивному ригу, прячет старые меши и привязывает модель к PlayerProceduralBody
// и PlayerAnimationDriver. Коллайдеры, BodyPartHealth и процедурная анимация не трогаются.
// Если на модели появится Animator (запечённые клипы), риг сам перейдёт в режим ведения
// от модели (syncRigFromModel).
public static class LowPolyPlayerInstaller
{
    private const string FbxPath = "Assets/Models/Player/PlayerLowPoly.fbx";
    private const string ModelRootName = "PlayerModel";

    [MenuItem("Tools/Contract/Apply Low Poly Player Model")]
    public static void ApplyLowPolyPlayerModel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            Debug.LogError("[LowPolyPlayer] FBX не найден: " + FbxPath);
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[LowPolyPlayer] В сцене нет объекта Player.");
            return;
        }

        Transform visual = player.transform.Find("VisualBody");
        if (visual == null)
        {
            Debug.LogError("[LowPolyPlayer] Не найден Player/VisualBody.");
            return;
        }

        Undo.SetCurrentGroupName("Apply Low Poly Player Model");
        int undoGroup = Undo.GetCurrentGroup();

        // Удаляем ранее вставленную модель, чтобы применить заново
        Transform existing = visual.Find(ModelRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject wrapper = new GameObject(ModelRootName);
        Undo.RegisterCreatedObjectUndo(wrapper, "Create player model");
        wrapper.transform.SetParent(visual, false);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Instantiate player model");
        instance.name = "PlayerLowPoly";

        // Убираем камеру и свет, которые лежат внутри FBX
        foreach (Light light in instance.GetComponentsInChildren<Light>(true))
            Undo.DestroyObjectImmediate(light.gameObject);

        Camera modelCamera = instance.GetComponentInChildren<Camera>(true);
        if (modelCamera != null)
            Undo.DestroyObjectImmediate(modelCamera.gameObject);

        // Размеры старого тела (по коллайдерам частей) и новой модели
        Bounds oldBounds = MeasureOldBounds(player.transform);
        Bounds newBounds = MeasureModelBounds(instance.transform);

        float scale = (oldBounds.size.y > 0.0001f && newBounds.size.y > 0.0001f)
            ? oldBounds.size.y / newBounds.size.y
            : 1f;

        // Смещения модели относительно её собственного корня
        Vector3 modelCenterLocal = newBounds.center - instance.transform.position;
        Vector3 modelMinLocal = newBounds.min - instance.transform.position;

        // Ставим модель под VisualBody и выравниваем по старому ригу
        instance.transform.SetParent(wrapper.transform, false);

        Vector3 targetWorld = new Vector3(
            oldBounds.center.x - modelCenterLocal.x * scale,
            oldBounds.min.y - modelMinLocal.y * scale,
            oldBounds.center.z - modelCenterLocal.z * scale);

        wrapper.transform.localPosition = visual.InverseTransformPoint(targetWorld);
        wrapper.transform.localRotation = Quaternion.identity;
        wrapper.transform.localScale = Vector3.one * scale;

        // Прячем старые примитивы, оставляя коллайдеры нетронутыми
        HidePlaceholderRenderers(player.transform, wrapper.transform);

        // Привязываем модель к процедурной анимации
        PlayerProceduralBody body = player.GetComponent<PlayerProceduralBody>();
        if (body != null)
        {
            SerializedObject serialized = new SerializedObject(body);
            serialized.FindProperty("modelRoot").objectReferenceValue = wrapper.transform;
            serialized.FindProperty("driveModelBones").boolValue = true;
            serialized.FindProperty("syncRigFromModel").boolValue = true;
            serialized.ApplyModifiedProperties();
        }

        // Мост геймплей -> Animator: параметры Speed/Strafe/Crouch/Lean/Grounded
        PlayerAnimationDriver driver = player.GetComponent<PlayerAnimationDriver>();
        if (driver == null)
            driver = Undo.AddComponent<PlayerAnimationDriver>(player);

        SerializedObject driverSerialized = new SerializedObject(driver);
        driverSerialized.FindProperty("modelRoot").objectReferenceValue = wrapper.transform;
        driverSerialized.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Debug.Log("[LowPolyPlayer] Модель игрока встроена: " + wrapper.transform.localScale.x.ToString("0.###")
            + "x, позиция " + wrapper.transform.localPosition.ToString("0.###"));
    }

    // Границы старого тела по коллайдерам частей (они не выключаются)
    private static Bounds MeasureOldBounds(Transform player)
    {
        Bounds bounds = new Bounds(player.position, Vector3.zero);
        bool has = false;

        foreach (BodyPartHealth part in player.GetComponentsInChildren<BodyPartHealth>(true))
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider == null) continue;

            if (!has)
            {
                bounds = collider.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return has ? bounds : new Bounds(player.position, Vector3.one);
    }

    private static Bounds MeasureModelBounds(Transform instance)
    {
        SkinnedMeshRenderer skinned = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skinned != null)
            return skinned.bounds;

        Bounds bounds = new Bounds(instance.position, Vector3.zero);
        bool has = false;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (!has)
            {
                bounds = renderer.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return has ? bounds : new Bounds(instance.position, Vector3.one);
    }

    private static void HidePlaceholderRenderers(Transform player, Transform skip)
    {
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if (skip != null && renderer.transform.IsChildOf(skip)) continue;
            if (!renderer.enabled) continue;

            Undo.RecordObject(renderer, "Hide placeholder renderer");
            renderer.enabled = false;
        }
    }
}
