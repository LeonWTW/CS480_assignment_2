#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// GhostFireBuilder - Editor utility that builds the ghost-fire system:
///   * A blue additive ParticleSystem prefab (GhostFireWisp.prefab)
///   * A GhostFireMat material with the default soft particle texture
///   * A GhostFireSpawner GameObject placed in the currently-open scene,
///     wired to the freshly built prefab.
///
/// Menu: CS480 -> Build Ghost Fire System
/// Re-runnable; overwrites the prefab/material and re-uses the spawner
/// if one already exists in the scene.
/// </summary>
public static class GhostFireBuilder
{
    private const string PrefabPath   = "Assets/CustomScripts/GhostFireWisp.prefab";
    private const string MaterialPath = "Assets/CustomScripts/GhostFireMat.mat";

    [MenuItem("CS480/Build Ghost Fire System")]
    public static void Build()
    {
        Material mat = BuildOrLoadMaterial();
        GameObject prefab = BuildWispPrefab(mat);
        EnsureSpawnerInScene(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GhostFireBuilder: built prefab " + PrefabPath +
                  " and placed spawner 'GhostFireSpawner' in the scene.");
    }

    // ---------------------------------------------------------------------
    // Material
    // ---------------------------------------------------------------------
    private static Material BuildOrLoadMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            Debug.LogError("GhostFireBuilder: no additive particle shader found.");
            return null;
        }

        mat = new Material(shader) { name = "GhostFireMat" };

        // Borrow the soft-circle texture from Unity's default particle material.
        Material defaultPS = AssetDatabase.GetBuiltinExtraResource<Material>(
            "Default-ParticleSystem.mat");
        if (defaultPS != null && defaultPS.mainTexture != null)
        {
            mat.mainTexture = defaultPS.mainTexture;
        }

        mat.color = new Color(0.35f, 0.75f, 1f, 1f);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    // ---------------------------------------------------------------------
    // Wisp prefab
    // ---------------------------------------------------------------------
    private static GameObject BuildWispPrefab(Material mat)
    {
        GameObject root = new GameObject("GhostFireWisp");

        // The ParticleSystem lives on the root so GetComponent<ParticleSystem>
        // on the prefab finds it directly.
        ParticleSystem ps = root.AddComponent<ParticleSystem>();

        // --- Main ---
        // Local simulation = particles stay with the emitter (no trailing).
        // Short lifetime + high emission = tight concentrated flame, not a cloud.
        var main = ps.main;
        main.duration        = 1f;
        main.loop            = true;
        main.startLifetime   = 0.9f;
        main.startSpeed      = 1.2f;
        main.startSize       = 0.3f;
        main.startColor      = new Color(0.75f, 0.95f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = 0f;
        main.maxParticles    = 500;

        // --- Emission ---
        var emission = ps.emission;
        emission.rateOverTime = 80f;

        // --- Shape: narrow cone pointing UP ---
        // Default cone emits along +Z, so we rotate -90 on X to point up.
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 10f;
        shape.radius    = 0.05f;
        shape.rotation  = new Vector3(-90f, 0f, 0f);

        // --- Velocity over lifetime: extra upward push for a flame "lick" ---
        // Unity requires X/Y/Z all in the same MinMaxCurve mode, so all three
        // are set to the TwoConstants form explicitly.
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space   = ParticleSystemSimulationSpace.Local;
        vol.x       = new ParticleSystem.MinMaxCurve(0f,   0f);
        vol.y       = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        vol.z       = new ParticleSystem.MinMaxCurve(0f,   0f);

        // --- Color over lifetime (hot white core -> cool blue -> fade) ---
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.95f, 1f, 1f),    0f),
                new GradientColorKey(new Color(0.4f,  0.75f, 1f), 0.5f),
                new GradientColorKey(new Color(0.1f,  0.2f,  0.6f), 1f),
            },
            new[] {
                new GradientAlphaKey(0f,   0f),
                new GradientAlphaKey(1f,   0.2f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f,   1f),
            }
        );
        col.color = grad;

        // --- Size over lifetime: taper to a point (flame tip) ---
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f),
            new Keyframe(0.3f, 1.0f),
            new Keyframe(1f, 0.1f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // --- Noise: subtle flicker only, not sideways drift ---
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.2f;
        noise.frequency   = 2f;
        noise.scrollSpeed = 1f;

        // --- Renderer material ---
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;

        // --- Wisp movement script ---
        root.AddComponent<GhostFireWisp>();

        // --- Save as prefab and clean up scene instance ---
        if (!Directory.Exists("Assets/CustomScripts"))
            Directory.CreateDirectory("Assets/CustomScripts");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------------------------------------------------------------------
    // Spawner in scene
    // ---------------------------------------------------------------------
    private static void EnsureSpawnerInScene(GameObject prefab)
    {
        GameObject spawnerGO = GameObject.Find("GhostFireSpawner");
        if (spawnerGO == null)
        {
            spawnerGO = new GameObject("GhostFireSpawner");
            spawnerGO.transform.position = Vector3.zero;
        }

        GhostFireSpawner spawner = spawnerGO.GetComponent<GhostFireSpawner>();
        if (spawner == null) spawner = spawnerGO.AddComponent<GhostFireSpawner>();
        spawner.wispPrefab = prefab;

        Selection.activeObject = spawnerGO;
        EditorGUIUtility.PingObject(spawnerGO);
    }
}
#endif
