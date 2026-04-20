using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DangerDetector - Uses DOT PRODUCT to detect when the player is facing an enemy.
/// When facing an enemy within range, displays a "DANGER" warning and plays a heartbeat sound.
/// 
/// ASSIGNMENT REQUIREMENTS COVERED:
/// - Dot Product: Vector3.Dot() to calculate facing direction vs enemy direction
/// - Sound Effect: AudioSource plays heartbeat sound when danger is detected
/// </summary>
public class DangerDetector : MonoBehaviour
{
    [Header("=== Dot Product Settings ===")]
    [Tooltip("How closely the player must face an enemy (1.0 = exact, 0.5 = wide cone)")]
    [Range(0f, 1f)]
    public float dotThreshold = 0.7f;

    [Tooltip("Max distance to detect enemies")]
    public float detectionRange = 15f;

    [Header("=== Sound Effect ===")]
    [Tooltip("Drag your heartbeat/danger AudioClip here")]
    public AudioClip dangerSound;

    [Tooltip("Volume of the danger sound")]
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Header("=== UI Warning ===")]
    [Tooltip("Drag the DangerText UI element here")]
    public Text dangerText;

    // Private references
    private AudioSource audioSource;
    private bool isDangerActive = false;

    // Cache enemy transforms for performance
    private Transform[] enemyTransforms;

    void Start()
    {
        // Set up AudioSource component for sound effect
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = dangerSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Hide warning text at start
        if (dangerText != null)
        {
            dangerText.enabled = false;
        }

        // Find all enemies in the scene by tag
        // (We'll tag Ghosts and Gargoyles as "Enemy" in the Unity Editor)
        FindEnemies();
    }

    void FindEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemyTransforms = new Transform[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            enemyTransforms[i] = enemies[i].transform;
        }
        Debug.Log("DangerDetector: Found " + enemyTransforms.Length + " enemies.");
    }

    void Update()
    {
        bool facingEnemy = false;

        // Check each enemy
        for (int i = 0; i < enemyTransforms.Length; i++)
        {
            if (enemyTransforms[i] == null) continue;

            // === DOT PRODUCT CALCULATION ===
            // Step 1: Get direction vector from player to enemy
            Vector3 directionToEnemy = (enemyTransforms[i].position - transform.position);
            float distance = directionToEnemy.magnitude; // distance = length of vector

            // Step 2: Skip if too far away
            if (distance > detectionRange) continue;

            // Step 3: Normalize the direction (make it unit length for accurate dot product)
            directionToEnemy.Normalize();

            // Step 4: Calculate DOT PRODUCT
            // transform.forward = the direction the player is facing
            // directionToEnemy = the direction from player toward the enemy
            // dotResult = 1.0 means facing directly at enemy
            // dotResult = 0.0 means enemy is to the side (90 degrees)
            // dotResult = -1.0 means enemy is directly behind
            float dotResult = Vector3.Dot(transform.forward, directionToEnemy);

            // Step 5: Check if player is facing the enemy (dot > threshold)
            if (dotResult > dotThreshold)
            {
                facingEnemy = true;
                Debug.Log("DANGER! Facing enemy. Dot: " + dotResult.ToString("F2") 
                          + " Distance: " + distance.ToString("F1"));
                break; // One enemy detected is enough
            }
        }

        // === TRIGGER DANGER WARNING + SOUND ===
        if (facingEnemy && !isDangerActive)
        {
            ActivateDanger();
        }
        else if (!facingEnemy && isDangerActive)
        {
            DeactivateDanger();
        }
    }

    void ActivateDanger()
    {
        isDangerActive = true;

        // Show UI warning
        if (dangerText != null)
        {
            dangerText.text = "⚠ DANGER ⚠";
            dangerText.enabled = true;
        }

        // Play heartbeat sound effect
        if (dangerSound != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void DeactivateDanger()
    {
        isDangerActive = false;

        // Hide UI warning
        if (dangerText != null)
        {
            dangerText.enabled = false;
        }

        // Stop sound
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // === VISUALIZE THE DETECTION CONE IN SCENE VIEW (for debugging) ===
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * detectionRange);
    }
}