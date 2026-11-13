using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Replaces the primitive placeholder meshes of the player body with the
// low-poly meshes from Assets/Models/Player/PlayerLowPoly.fbx.
// Keeps every existing Transform, collider and BodyPartHealth component intact,
// so PlayerProceduralBody keeps working without changes.
public static class LowPolyPlayerInstaller
{
    private const string FbxPath = "Assets/Models/Player/PlayerLowPoly.fbx";
    private const string HairMaterialPath = "Assets/Materials/Player/Hair.mat";
    private const string SkinMaterialPath = "Assets/Materials/Player/Skin.mat";

    private static readonly string[] BodyPartNames =
    {
        "Pelvis", "Torso", "Neck",
        "UpperArm_L", "UpperArm_R",
        "Forearm_L", "Forearm_R",
        "Hand_L", "Hand_R",
        "Thigh_L", "Thigh_R",
        "Shin_L", "Shin_R",
        "Foot_L", "Foot_R"
    };

    [MenuItem("Tools/Contract/Apply Low Poly Player Meshes")]
    public static void ApplyLowPolyPlayerMeshes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (prefab == null)
        {
            Debug.LogError($"[LowPolyPlayer] FBX not found at {FbxPath}. Let Unity finish importing it, then retry.");
            return;
        }

        Dictionary<string, Mesh> meshes = AssetDatabase
            .LoadAllAssetsAtPath(FbxPath)
            .OfType<Mesh>()
            .GroupBy(m => m.name)
            .ToDictionary(g => g.Key, g => g.First());

        if (meshes.Count == 0)
        {
            Debug.LogError($"[LowPolyPlayer] No meshes found inside {FbxPath}.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[LowPolyPlayer] No GameObject named 'Player' in the open scene.");
            return;
        }

        Transform visual = player.transform.Find("VisualBody");
        if (visual == null)
        {
            Debug.LogError("[LowPolyPlayer] Player/VisualBody not found.");
            return;
        }

        Undo.SetCurrentGroupName("Apply Low Poly Player Meshes");
        int undoGroup = Undo.GetCurrentGroup();

        int applied = 0;
        var missing = new List<string>();

        foreach (string partName in BodyPartNames)
        {
            Transform part = FindDeep(visual, partName);
            MeshFilter filter = part != null ? part.GetComponent<MeshFilter>() : null;

            if (filter == null || !meshes.TryGetValue(partName, out Mesh mesh))
            {
                missing.Add(partName);
                continue;
            }

            Undo.RecordObject(filter, "Swap player mesh");
            filter.sharedMesh = mesh;
            EditorUtility.SetDirty(filter);
            applied++;
        }

        Transform head = FindDeep(player.transform, "HeadVisual");
        if (head != null && meshes.TryGetValue("HeadVisual", out Mesh headMesh))
        {
            MeshFilter filter = head.GetComponent<MeshFilter>();
            MeshRenderer renderer = head.GetComponent<MeshRenderer>();
            Material skin = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);
            Material hair = EnsureHairMaterial();

            if (filter != null)
            {
                Undo.RecordObject(filter, "Swap player head mesh");
                filter.sharedMesh = headMesh;
                EditorUtility.SetDirty(filter);
                applied++;
            }

            if (renderer != null && skin != null && hair != null)
            {
                Undo.RecordObject(renderer, "Set player head materials");
                renderer.sharedMaterials = new[] { skin, hair };
                EditorUtility.SetDirty(renderer);
            }
        }
        else
        {
            missing.Add("HeadVisual");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(player.scene);

        if (missing.Count > 0)
            Debug.LogWarning($"[LowPolyPlayer] Applied {applied} meshes. Missing: {string.Join(", ", missing)}");
        else
            Debug.Log($"[LowPolyPlayer] Applied {applied} low-poly meshes to '{player.name}'.");
    }

    private static Material EnsureHairMaterial()
    {
        Material hair = AssetDatabase.LoadAssetAtPath<Material>(HairMaterialPath);
        if (hair != null) return hair;

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("[LowPolyPlayer] Standard shader not found.");
            return null;
        }

        hair = new Material(shader) { name = "Hair" };
        hair.color = new Color(0.13f, 0.09f, 0.07f, 1f);
        hair.SetFloat("_Glossiness", 0.25f);

        AssetDatabase.CreateAsset(hair, HairMaterialPath);
        AssetDatabase.SaveAssets();
        return hair;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;

        foreach (Transform child in root)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }

        return null;
    }
}
