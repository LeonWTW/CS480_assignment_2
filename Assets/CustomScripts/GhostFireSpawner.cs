using UnityEngine;

/// <summary>
/// GhostFireSpawner - spawns N wandering GhostFireWisp instances at Scene
/// Start inside the configured bounds. Each instance gets the same bounds
/// so they all wander within the same volume.
///
/// Drop ONE of these into the scene (the Editor menu
/// 'CS480 -> Build Ghost Fire System' does this automatically).
/// </summary>
public class GhostFireSpawner : MonoBehaviour
{
    [Header("=== Spawn Config ===")]
    [Tooltip("The GhostFireWisp prefab to instantiate.")]
    public GameObject wispPrefab;

    [Tooltip("How many wisps to spawn on scene start.")]
    public int count = 5;

    [Header("=== Spawn Bounds (also used as wander bounds) ===")]
    public Vector3 boundsCenter = new Vector3(0f, 1f, 0f);
    public Vector3 boundsSize   = new Vector3(30f, 2f, 30f);

    void Start()
    {
        if (wispPrefab == null)
        {
            Debug.LogError("GhostFireSpawner: wispPrefab is not set.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomPoint();
            GameObject go = Instantiate(wispPrefab, pos, Quaternion.identity, transform);
            go.name = "GhostFireWisp_" + i;

            GhostFireWisp w = go.GetComponent<GhostFireWisp>();
            if (w != null)
            {
                w.boundsCenter = boundsCenter;
                w.boundsSize   = boundsSize;
            }
        }
        Debug.Log("GhostFireSpawner: spawned " + count + " wisps.");
    }

    Vector3 RandomPoint()
    {
        Vector3 half = boundsSize * 0.5f;
        return boundsCenter + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z)
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
    }
}
