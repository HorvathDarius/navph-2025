using UnityEngine;

[RequireComponent(typeof(NavMeshMover2D))]
public class ChaserController : MonoBehaviour
{
    [SerializeField] private GameObject chasedPlayer;
    [SerializeField] private float chaseSpeed = 1f;
    public bool isChasing = true;

    [Header("Catch Audio")]
    [SerializeField] private AudioSource catchAudioSource;
    [SerializeField] private AudioClip[] catchClips;
    
    private Animator animator;
    private NavMeshMover2D mover;
    private float directionX = 1f; // default facing right

    private void Awake()
    {
        animator = GetComponent<Animator>();
        mover = GetComponent<NavMeshMover2D>();
    }

    private void Start()
    {
        mover.Speed = chaseSpeed;

        // Set parameters so transitions FROM roll work correctly
        animator.SetFloat("DirectionX", 1f);
        animator.SetInteger("LocomotionState", 1); // Wheelchair
        animator.SetBool("InCombat", true);
        animator.SetBool("IsMoving", true);

        // Jump directly into roll state – no AnyState transition exists from walk_L to roll
        animator.Play("homeless_roll_R");
    }

    void Update()
    {
        if (!isChasing)
        {
            return;
        }

        if (chasedPlayer != null && mover.IsReady)
        {
            mover.SetDestination(chasedPlayer.transform.position);
        }

        UpdateDirectionFromMovement();
    }

    /// <summary>Derives animation direction from NavMesh velocity.</summary>
    private void UpdateDirectionFromMovement()
    {
        if (!mover.IsReady) return;
        Vector2 vel = mover.Velocity;
        if (vel.sqrMagnitude > 0.01f)
        {
            SetDirection(Mathf.Sign(vel.x));
        }
    }

    private void SetDirection(float x)
    {
        directionX = Mathf.Clamp(x, -1f, 1f);
        animator.SetFloat("DirectionX", directionX);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == chasedPlayer)
        {
            isChasing = false;
            mover.Stop();

            // Face the player
            float dirX = chasedPlayer.transform.position.x - transform.position.x;
            SetDirection(Mathf.Sign(dirX));

            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Punch");

            // Play random catch voice line
            PlayRandomCatchSound();

            Debug.Log("Chaser caught the player!");
            Debug.Log("Start quick-time event");
            StartCoroutine(NivyChaseManager.Instance.StartQuickTimeEvent());
        }
    }

    private void PlayRandomCatchSound()
    {
        if (catchAudioSource == null || catchClips == null || catchClips.Length == 0)
            return;

        // If a catch sound is still playing, don't interrupt it
        if (catchAudioSource.isPlaying)
            return;

        AudioClip clip = catchClips[Random.Range(0, catchClips.Length)];
        catchAudioSource.clip = clip;
        catchAudioSource.Play();
    }
}
