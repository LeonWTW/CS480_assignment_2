#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// ArrowPrefabBuilder - Editor utility that builds the EnemyRadar Arrow prefab
/// from Unity primitives, with the correct transforms, a dedicated material,
/// and the EnemyRadar script already attached.
///
/// USAGE:
///   1. Open this project in Unity and let it compile.
///   2. Top menu: CS480 -> Build Arrow Prefab
///   3. The prefab is saved to Assets/CustomScripts/Arrow.prefab
///      and automatically placed in the currently open scene.
///   4. Re-run the menu item any time to regenerate; it overwrites safely.
/// </summary>
public static class ArrowPrefabBuilder
{
    private const string PrefabPath   = "Assets/CustomScripts/Arrow.prefab";
    private const string MaterialPath = "Assets/CustomScripts/ArrowMat.mat";

    [MenuItem("CS480/Build Arrow Prefab")]
    public static void Build()
    {
        // 1. Create (or reuse) a simple unlit-ish material.
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            mat = new Material(shader) { name = "ArrowMat" };
            mat.color = Color.green;
            // Enable emission so the arrow is visible in dark scenes.
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.green * 1.5f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }

        // 2. Build the hierarchy in the scene, using primitives.
        GameObject root = new GameObject("Arrow");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        // Sized to match JohnLemon (~1 unit tall). Total arrow length ~0.8 units.
        // NOTE: no "tail" part - that caused the double-headed look previously.

        GameObject shaft = BuildPart(
            name:     "Shaft",
            parent:   root.transform,
            localPos: new Vector3(0f, 0f, 0f),
            localRot: Vector3.zero,
            localScale: new Vector3(0.08f, 0.08f, 0.5f),
            mat:      mat
        );

        GameObject head = BuildPart(
            name:     "Head",
            parent:   root.transform,
            localPos: new Vector3(0f, 0f, 0.3f),
            localRot: new Vector3(0f, 45f, 0f),
            localScale: new Vector3(0.2f, 0.15f, 0.2f),
            mat:      mat
        );

        // 3. Attach the EnemyRadar script (looked up by type so we don't
        //    hard-code a GUID).
        System.Type radarType = System.Type.GetType("EnemyRadar, Assembly-CSharp");
        if (radarType != null)
        {
            root.AddComponent(radarType);
        }
        else
        {
            Debug.LogWarning(
                "ArrowPrefabBuilder: EnemyRadar type not found. " +
                "Make sure EnemyRadar.cs has compiled, then run this menu again."
            );
        }

        // 4. Save as a prefab asset, overwriting if it already exists.
        //    SaveAsPrefabAssetAndConnect keeps the scene instance linked to the prefab.
        if (!Directory.Exists("Assets/CustomScripts"))
        {
            Directory.CreateDirectory("Assets/CustomScripts");
        }

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(
            root, PrefabPath, InteractionMode.UserAction);

        if (prefabAsset != null)
        {
            Debug.Log("ArrowPrefabBuilder: prefab saved to " + PrefabPath);
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }
        else
        {
            Debug.LogError("ArrowPrefabBuilder: failed to save prefab at " + PrefabPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject BuildPart(
        string name,
        Transform parent,
        Vector3 localPos,
        Vector3 localRot,
        Vector3 localScale,
        Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localRot;
        go.transform.localScale = localScale;

        // The Arrow does not need colliders for rendering; strip the one
        // CreatePrimitive adds so it cannot interfere with gameplay triggers.
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;

        return go;
    }
}
#endif
