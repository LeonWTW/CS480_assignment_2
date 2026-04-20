using UnityEngine;

/// <summary>
/// GhostFireWisp - a wandering blue ghost flame particle effect.
///
/// ASSIGNMENT REQUIREMENTS COVERED (Part 4 - Particle Effect with triggers):
///   Particle: blue additive-blended flame, local-space simulation with
///   noise flicker and color-over-lifetime gradient.
///
///   Triggers:
///     1. SCENE-START trigger - GhostFireSpawner instantiates 5 wisps at
///        random positions when the scene begins playing.
///     2. TIMER trigger - each wisp cycles visible (4-8s) <-> hidden (1-3s)
///        on its own random timer. On each flip the ParticleSystem is
///        Stop()'d or Play()'d, and on re-appear the wisp teleports to a
///        brand new random point in the bounds.
///     3. WANDER-WAYPOINT trigger - every few seconds or when close to the
///        current target, a new random target point is picked.
///
/// The wisp has no collider, does no damage, and passes through walls.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class GhostFireWisp : MonoBehaviour
{
    [Header("=== Wander Bounds (world space) ===")]
    [Tooltip("Center of the box the wisp can wander in.")]
    public Vector3 boundsCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("Size (XYZ) of the wander box.")]
    public Vector3 boundsSize = new Vector3(30f, 2f, 30f);

    [Header("=== Motion ===")]
    [Tooltip("Units per second at which the wisp drifts toward its current target.")]
    public float wanderSpeed = 1.5f;

    [Tooltip("Seconds before the wisp picks a new wander target, even if it hasn't arrived.")]
    public float targetRefreshInterval = 4f;

    [Header("=== Lifecycle (appear/disappear triggers) ===")]
    [Tooltip("Random range in seconds the wisp stays visible before disappearing.")]
    public Vector2 visibleDuration = new Vector2(4f, 8f);

    [Tooltip("Random range in seconds the wisp stays hidden before re-appearing elsewhere.")]
    public Vector2 hiddenDuration = new Vector2(1f, 3f);

    // --- runtime state ---
    private ParticleSystem ps;
    private Vector3 target;
    private float targetExpiresAt;
    private float stateChangeAt;
    private bool visible = true;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        transform.position = RandomPoint();
        PickNewTarget();

        stateChangeAt = Time.time
                      + Random.Range(visibleDuration.x, visibleDuration.y)
                      + Random.Range(0f, 3f);
    }

    void Update()
    {
        if (visible)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                wanderSpeed * Time.deltaTime);

            bool arrived = Vector3.Distance(transform.position, target) < 0.3f;
            if (arrived || Time.time > targetExpiresAt)
            {
                PickNewTarget();
            }
        }

        if (Time.time >= stateChangeAt)
        {
            ToggleVisibility();
        }
    }

    void ToggleVisibility()
    {
        if (visible)
        {
            if (ps != null) ps.Stop();
            visible = false;
            stateChangeAt = Time.time + Random.Range(hiddenDuration.x, hiddenDuration.y);
        }
        else
        {
            transform.position = RandomPoint();
            PickNewTarget();
            if (ps != null) { ps.Clear(); ps.Play(); }
            visible = true;
            stateChangeAt = Time.time + Random.Range(visibleDuration.x, visibleDuration.y);
        }
    }

    void PickNewTarget()
    {
        target = RandomPoint();
        targetExpiresAt = Time.time + targetRefreshInterval;
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
