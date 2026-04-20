using UnityEngine;

/// <summary>
/// EnemyRadar - An arrow that floats above the player's head, points at the
/// NEAREST enemy, and changes color from green -> red as that enemy gets closer.
///
/// ASSIGNMENT REQUIREMENTS COVERED:
/// - Linear Interpolation (Part 3), used in THREE places:
///     * Vector3.Lerp     -> arrow smoothly follows above the player's head
///     * Quaternion.Slerp -> arrow smoothly rotates to point at nearest enemy
///     * Color.Lerp       -> arrow color blends from safe (green) to danger (red)
///
/// USAGE:
///   1. Build a simple Arrow prefab from primitives (see README / instructions).
///   2. Attach this script to the ROOT of the Arrow prefab.
///   3. Drop the prefab into the scene. Player must be tagged "Player",
///      enemies must be tagged "Enemy".
/// </summary>
public class EnemyRadar : MonoBehaviour
{
    [Header("=== Tags ===")]
    [Tooltip("Tag on the player GameObject.")]
    public string playerTag = "Player";

    [Tooltip("Tag on the enemies to track (Ghosts, Gargoyles).")]
    public string enemyTag = "Enemy";

    [Header("=== Hover Position ===")]
    [Tooltip("How far above the player's pivot the arrow hovers.")]
    public float headHeight = 2.5f;

    [Tooltip("Speed at which the arrow catches up to the player's head position. Higher = snappier.")]
    public float positionLerpSpeed = 12f;

    [Header("=== Rotation ===")]
    [Tooltip("Speed at which the arrow rotates to point at the nearest enemy. Higher = snappier.")]
    public float rotationLerpSpeed = 8f;

    [Tooltip("Keep the arrow rotating only on the horizontal plane (recommended).")]
    public bool horizontalOnly = true;

    [Header("=== Danger Color Mapping ===")]
    [Tooltip("Arrow is fully this color when enemy is at OR further than 'safeDistance'.")]
    public Color safeColor = Color.green;

    [Tooltip("Arrow is fully this color when enemy is at OR closer than 'dangerDistance'.")]
    public Color dangerColor = Color.red;

    [Tooltip("Distance at which the arrow is fully safe (green).")]
    public float safeDistance = 20f;

    [Tooltip("Distance at which the arrow is fully dangerous (red).")]
    public float dangerDistance = 3f;

    [Header("=== Emission (so arrow stays visible in dark scenes) ===")]
    [Tooltip("Multiplier on the emission color. 1 = pure color, 2+ = bloom-bright.")]
    public float emissionIntensity = 1.5f;

    // --- Runtime state ---
    private Transform player;
    private Transform[] enemies;
    private Renderer[] arrowRenderers;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        // Find the player
        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null) player = playerGO.transform;
        else Debug.LogWarning("EnemyRadar: no GameObject tagged '" + playerTag + "' found.");

        // Cache all enemy transforms once
        GameObject[] enemyGOs = GameObject.FindGameObjectsWithTag(enemyTag);
        enemies = new Transform[enemyGOs.Length];
        for (int i = 0; i < enemyGOs.Length; i++)
        {
            enemies[i] = enemyGOs[i].transform;
        }
        Debug.Log("EnemyRadar: tracking " + enemies.Length + " enemies.");

        // Clone materials on every renderer so recoloring doesn't mutate shared assets.
        arrowRenderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            // Accessing .material (vs .sharedMaterial) auto-clones.
            arrowRenderers[i].material = new Material(arrowRenderers[i].material);
            // Turn on the emission shader keyword so _EmissionColor actually renders.
            arrowRenderers[i].material.EnableKeyword("_EMISSION");
            arrowRenderers[i].material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        // Start in the safe color.
        ApplyColor(safeColor);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // ---------------------------------------------------------------
        // 1. Find nearest enemy.
        // ---------------------------------------------------------------
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            float d = Vector3.Distance(player.position, enemies[i].position);
            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = enemies[i];
            }
        }

        // ---------------------------------------------------------------
        // 2. Vector3.Lerp: smoothly hover above the player's head.
        //    Lerp(current, target, speed * deltaTime) is the standard
        //    "exponential follow" idiom in Unity.
        // ---------------------------------------------------------------
        Vector3 headPos = player.position + Vector3.up * headHeight;
        transform.position = Vector3.Lerp(
            transform.position,
            headPos,
            positionLerpSpeed * Time.deltaTime
        );

        if (nearest == null) return; // no enemies - keep hovering, keep last color.

        // ---------------------------------------------------------------
        // 3. Quaternion.Slerp: rotate smoothly toward nearest enemy.
        //    Slerp = Spherical Linear Interpolation (the orientation
        //    analogue of Vector3.Lerp - counts as "linear interpolation
        //    of orientation" per the assignment brief).
        // ---------------------------------------------------------------
        Vector3 toEnemy = nearest.position - transform.position;
        if (horizontalOnly) toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toEnemy.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationLerpSpeed * Time.deltaTime
            );
        }

        // ---------------------------------------------------------------
        // 4. Color.Lerp: blend from safe color to danger color as the
        //    nearest enemy gets closer. Mathf.InverseLerp maps distance
        //    into a 0..1 parameter 't' (1 = fully red, 0 = fully green).
        // ---------------------------------------------------------------
        float t = Mathf.InverseLerp(safeDistance, dangerDistance, nearestDistance);
        Color c = Color.Lerp(safeColor, dangerColor, t);
        ApplyColor(c);
    }

    void ApplyColor(Color c)
    {
        if (arrowRenderers == null) return;
        Color emission = c * emissionIntensity;
        for (int i = 0; i < arrowRenderers.Length; i++)
        {
            if (arrowRenderers[i] == null) continue;
            Material m = arrowRenderers[i].material;
            m.color = c;
            m.SetColor(EmissionColorId, emission);
        }
    }

    // --- Editor gizmos: visualize safe / danger rings around the player ---
    void OnDrawGizmosSelected()
    {
        Transform p = player;
        if (p == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) p = go.transform;
        }
        if (p == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(p.position, safeDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p.position, dangerDistance);
    }
}
