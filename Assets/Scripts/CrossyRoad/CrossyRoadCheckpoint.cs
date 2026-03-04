using System.Collections;
using UnityEngine;

/// <summary>
/// Checkpoint system for CrossyRoad scene.
/// Place this on a trigger collider in the scene. When the player enters,
/// the checkpoint position is saved. On death, the player respawns here
/// instead of restarting the entire level.
/// </summary>
public class CrossyRoadCheckpoint : MonoBehaviour
{
    public static CrossyRoadCheckpoint ActiveCheckpoint { get; private set; }

    /// <summary>
    /// When true, the player is temporarily invincible after respawning.
    /// Checked in PlayerController.OnCollisionEnter2D to skip damage.
    /// </summary>
    public static bool IsPlayerInvincible { get; private set; }

    /// <summary>
    /// The order of the currently active checkpoint. -1 means no checkpoint is active yet.
    /// </summary>
    private static int currentActiveOrder = -1;

    [Header("Checkpoint Order")]
    [Tooltip("Sequential order of this checkpoint (0, 1, 2...). A checkpoint only activates if its order is higher than the currently active one.")]
    [SerializeField] private int checkpointOrder = 0;

    [Header("Respawn Settings")]
    [Tooltip("Offset from the checkpoint trigger position where the player will respawn")]
    [SerializeField] private Vector2 respawnOffset = Vector2.zero;
    
    [Tooltip("Health restored on respawn (0 = full health)")]
    [SerializeField] private int respawnHealth = 100;

    [Tooltip("Brief invincibility time after respawn")]
    [SerializeField] private float invincibilityDuration = 2f;

    [Tooltip("Direction the player faces after respawn")]
    [SerializeField] private RespawnDirection respawnFacingDirection = RespawnDirection.Left;

    private enum RespawnDirection { Left, Right }

    private bool activated;

    /// <summary>
    /// The world position where the player will respawn.
    /// </summary>
    public Vector3 RespawnPosition => transform.position + (Vector3)respawnOffset;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || activated)
            return;

        // Only activate if this checkpoint is exactly the next one in sequence
        // Order 0 needs currentActiveOrder == -1, order 1 needs currentActiveOrder == 0, etc.
        if (checkpointOrder != currentActiveOrder + 1)
        {
            Debug.Log($"[Checkpoint] Skipped order {checkpointOrder} — expecting order {currentActiveOrder + 1}");
            return;
        }

        activated = true;
        currentActiveOrder = checkpointOrder;
        ActiveCheckpoint = this;
        Debug.Log($"[Checkpoint] Checkpoint order {checkpointOrder} activated at {RespawnPosition}");
    }

    /// <summary>
    /// Respawns the player at this checkpoint. Returns true if successful.
    /// Called from PlayerController when a checkpoint is active.
    /// </summary>
    public static bool TryRespawnPlayer(PlayerController player)
    {
        if (ActiveCheckpoint == null)
            return false;

        ActiveCheckpoint.StartCoroutine(ActiveCheckpoint.RespawnCoroutine(player));
        return true;
    }

    private IEnumerator RespawnCoroutine(PlayerController player)
    {
        Debug.Log("[Checkpoint] Respawning player at checkpoint...");

        // Wait for death animation to play briefly
        yield return new WaitForSeconds(1f);

        // Restore health
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeHealth(respawnHealth);
        }

        // Reset player position
        player.transform.position = RespawnPosition;

        // Re-enable player physics
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        // Reset animator - exit death state back to idle
        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("IsDead");

            // Set facing direction and play correct idle animation
            bool faceRight = respawnFacingDirection == RespawnDirection.Right;
            anim.SetFloat("DirectionX", faceRight ? 1f : -1f);
            anim.Play(faceRight ? "main_char_idle_R" : "main_char_idle_L", 0, 0f);
        }

        // Re-enable input by toggling the component (OnDisable/OnEnable re-registers input actions)
        player.enabled = false;
        player.enabled = true;
        player.SetMovementLocked(false);

        // Brief invincibility with flashing visual feedback
        IsPlayerInvincible = true;
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null && invincibilityDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < invincibilityDuration)
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(0.15f);
                elapsed += 0.15f;
            }
            sr.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }
        IsPlayerInvincible = false;

        Debug.Log("[Checkpoint] Player respawned successfully.");
    }

    /// <summary>
    /// Reset checkpoint state (call when scene loads).
    /// </summary>
    public static void ResetCheckpoints()
    {
        ActiveCheckpoint = null;
        IsPlayerInvincible = false;
        currentActiveOrder = -1;
    }

    private void OnDestroy()
    {
        if (ActiveCheckpoint == this)
            ActiveCheckpoint = null;
    }
}
