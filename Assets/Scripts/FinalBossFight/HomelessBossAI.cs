using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NavMeshMover2D))]
public class HomelessBossAI : MonoBehaviour
{
    public enum BossState
    {
        IntroWalkToMiddle,
        IntroWalkBackToCart,
        SittingFightIdle,
        Fighting,
        Retreating,
        Regenerating,
        Dead
    }

    public enum HomelessLocomotionState
    {
        Walk = 0,
        Wheelchair = 1
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float retreatSpeed = 2.2f;
    [SerializeField] private Transform leftPatrolPoint;
    [SerializeField] private Transform rightPatrolPoint;
    [SerializeField] private GameObject cartObject;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private int kickDamage = 20;

    [Header("Retreat & Regen")]
    [Tooltip("Prah HP (0–1) pri ktorom boss začne ustupovať a healiť sa")]
    [SerializeField] private float retreatHpThreshold = 0.4f;
    [Tooltip("Cooldown (s) medzi dvoma ústupmi – boss nemôže unikať donekonečna")]
    [SerializeField] private float retreatCooldown = 15f;
    [Tooltip("Minimálna vzdialenosť od hráča, ktorú sa boss snaží udržať pri ústupe")]
    [SerializeField] private float retreatDistance = 4f;
    [Tooltip("Ako dlho po trafení počas healu boss počká, kým sa vráti do Fighting (s)")]
    [SerializeField] private float regenInterruptDelay = 1.2f;
    [Tooltip("Linear drag Rigidbody2D počas regenerácie – zabraňuje posúvaniu")]
    [SerializeField] private float regenLinearDrag = 20f;

    [Header("NavMesh")]
    [Tooltip("Ako často sa prepočítava cesta k hráčovi (s)")]
    [SerializeField] private float pathRefreshInterval = 0.25f;

    // --- private refs ---
    private FinalBossFightManager manager;
    private Animator animator;
    private Rigidbody2D rb;
    private NavMeshMover2D mover;
    private PlayerCombat player;

    private BossState currentState;
    private bool canAttack = true;
    private bool aiLocked;
    private float directionX = -1f;
    private HomelessLocomotionState locomotionState = HomelessLocomotionState.Walk;

    private Coroutine regenRoutine;
    private Coroutine regenInterruptRoutine;
    private float defaultLinearDrag;
    private bool hitDuringRegen;

    // --- retreat ---
    private bool retreatOnCooldown;
    private float retreatCooldownTimer;
    private Vector2 retreatTargetPos;

    // --- path refresh ---
    private float pathRefreshTimer;

    // ──────────────────────────────────────────────
    //  Init
    // ──────────────────────────────────────────────

    public void Init(FinalBossFightManager mgr)
    {
        manager = mgr;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<NavMeshMover2D>();
        player = FindAnyObjectByType<PlayerCombat>();
        defaultLinearDrag = rb.linearDamping;
    }

    private void Start()
    {
        // Počas intra používame Rigidbody, nie NavMesh
        UseRigidbodyMovement();
    }

    // ──────────────────────────────────────────────
    //  Movement mode switching
    // ──────────────────────────────────────────────

    /// <summary>Prepne na Rigidbody2D pohyb (intro, regen). Vypne NavMeshAgent.</summary>
    private void UseRigidbodyMovement()
    {
        mover.Stop();
        mover.SetEnabled(false);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>Prepne na NavMesh pohyb (fight, retreat). Vypne Rigidbody.</summary>
    private void UseNavMeshMovement(float speed)
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        mover.SetEnabled(true);
        mover.Speed = speed;
        mover.StoppingDistance = 0.1f;
    }

    // ──────────────────────────────────────────────
    //  Update
    // ──────────────────────────────────────────────

    private void Update()
    {
        if (aiLocked || currentState == BossState.Dead)
            return;

        // retreat cooldown odpočítavanie
        if (retreatOnCooldown)
        {
            retreatCooldownTimer -= Time.deltaTime;
            if (retreatCooldownTimer <= 0f)
                retreatOnCooldown = false;
        }

        // Animácia smeru z NavMesh velocity
        UpdateDirectionFromMovement();

        animator.SetFloat("DirectionX", directionX);

        switch (currentState)
        {
            case BossState.IntroWalkToMiddle:
                MoveTowards(leftPatrolPoint.position, BossState.IntroWalkBackToCart, -1f, OnReachedMiddle);
                break;
            case BossState.IntroWalkBackToCart:
                MoveTowards(rightPatrolPoint.position, BossState.SittingFightIdle, 1f, OnReachedCart);
                break;
            case BossState.Fighting:
                HandleFighting();
                break;
            case BossState.Retreating:
                HandleRetreating();
                break;
            case BossState.Regenerating:
                FacePlayer();
                break;
        }
    }

    private void FixedUpdate()
    {
        // Aktívne brzdi počas stavov kde boss stojí – zabraňuje "kĺzaniu" po kolízii
        if (currentState == BossState.Regenerating ||
            currentState == BossState.SittingFightIdle ||
            currentState == BossState.Dead)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>Odvodí smer animácie z NavMesh velocity (ak sa hýbe).</summary>
    private void UpdateDirectionFromMovement()
    {
        if (!mover.IsReady) return;
        Vector2 vel = mover.Velocity;
        if (vel.sqrMagnitude > 0.01f)
        {
            SetDirection(Mathf.Sign(vel.x));
        }
    }

    // ──────────────────────────────────────────────
    //  Intro  (stále cez Rigidbody – predtým než je NavMesh aktívny)
    // ──────────────────────────────────────────────

    public void StartIntro()
    {
        SetState(BossState.IntroWalkToMiddle);
        SetDirection(-1f);
        SetLocomotion(HomelessLocomotionState.Walk);
        animator.SetBool("IsMoving", true);
        Debug.Log("[HomelessBossAI] StartIntro - walking from spawn to middle.");
    }

    private void MoveTowards(Vector3 target, BossState nextState, float dirXWhenMoving, System.Action onReached = null)
    {
        Vector2 dir = (target - transform.position).normalized;
        SetDirection(dirXWhenMoving);
        rb.linearVelocity = dir * walkSpeed;

        float dist = Vector2.Distance(transform.position, target);

        if (dist <= 0.85f)
        {
            rb.linearVelocity = Vector2.zero;
            SetState(nextState);
            Debug.Log($"[HomelessBossAI] Reached {nextState}, stopping move.");
            onReached?.Invoke();
        }
    }

    private void OnReachedCart()
    {
        Debug.Log("[HomelessBossAI] OnReachedCart - switching to wheelchair mode.");

        if (cartObject != null)
            cartObject.SetActive(false);

        manager.ShowBossHealthUI(true);

        SetDirection(-1f);
        SetLocomotion(HomelessLocomotionState.Wheelchair);
        animator.SetBool("IsMoving", false);
        animator.SetBool("InCombat", true);

        FinalBossFightManager.Instance.NotifyBossReadyToFight();
    }

    private void OnReachedMiddle()
    {
        Debug.Log("[HomelessBossAI] OnReachedMiddle - reached middle point.");
        manager.StartCountdownToFight();
    }

    // ──────────────────────────────────────────────
    //  Fight Phase
    // ──────────────────────────────────────────────

    public void StartFightPhase()
    {
        Debug.Log("[HomelessBossAI] StartFightPhase – switching to NavMesh movement.");
        UseNavMeshMovement(walkSpeed);
        mover.StoppingDistance = attackRange * 0.9f;
        SetState(BossState.Fighting);
    }

    // ──────────────────────────────────────────────
    //  HandleFighting  (NavMesh chase → attack, s retreat triggrom)
    // ──────────────────────────────────────────────

    private void HandleFighting()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // --- Trigger retreat keď je HP nízke a cooldown prešiel ---
        if (!retreatOnCooldown && manager.BossHpRatio <= retreatHpThreshold)
        {
            TriggerRetreat();
            return;
        }

        // --- čakáme na koniec attack cooldownu ---
        if (!canAttack)
        {
            mover.Stop();
            animator.SetBool("IsMoving", false);
            return;
        }

        // --- sme v dosahu – útočíme ---
        if (distToPlayer <= attackRange)
        {
            mover.Stop();
            animator.SetBool("IsMoving", false);
            FacePlayer();
            if (canAttack)
                StartCoroutine(AttackRoutine());
            return;
        }

        // --- NavMesh cesta k hráčovi ---
        pathRefreshTimer -= Time.deltaTime;
        if (pathRefreshTimer <= 0f)
        {
            mover.Speed = walkSpeed;
            mover.SetDestination(player.transform.position);
            pathRefreshTimer = pathRefreshInterval;
        }

        // Animácia pohybu
        bool isMoving = mover.Velocity.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);
    }

    // ──────────────────────────────────────────────
    //  Retreat & Regen
    // ──────────────────────────────────────────────

    private void TriggerRetreat()
    {
        if (currentState == BossState.Dead) return;

        retreatTargetPos = ChooseRetreatPosition();

        Debug.Log($"[HomelessBossAI] TriggerRetreat -> {retreatTargetPos}");
        SetState(BossState.Retreating);
        mover.Speed = retreatSpeed;
        mover.StoppingDistance = 0.5f;
        mover.SetDestination(retreatTargetPos);
        animator.SetBool("IsMoving", true);
        canAttack = false;
    }

    private Vector2 ChooseRetreatPosition()
    {
        if (player == null) return (Vector2)transform.position + Vector2.left * retreatDistance;

        // Smer preč od hráča
        Vector2 awayDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        return (Vector2)transform.position + awayDir * retreatDistance;
    }

    private void HandleRetreating()
    {
        if (player == null) { StartRegeneration(); return; }

        float distToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // Dosiahli sme retreat bod ALEBO sme dostatočne ďaleko od hráča
        if (mover.HasReachedDestination || distToPlayer >= retreatDistance)
        {
            mover.Stop();
            animator.SetBool("IsMoving", false);
            Debug.Log("[HomelessBossAI] Reached retreat position, starting regen.");
            StartRegeneration();
            return;
        }

        // Animácia
        bool isMoving = mover.Velocity.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);
    }

    // ──────────────────────────────────────────────
    //  Attack Routine
    // ──────────────────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        canAttack = false;
        mover.Stop();
        animator.SetBool("IsMoving", false);

        bool useKick = Random.value > 0.75f;
        animator.SetTrigger(useKick ? "Kick" : "Punch");

        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = state.length > 0 ? state.length : 0.4f;

        yield return new WaitForSeconds(clipLength);

        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            float dist = toPlayer.magnitude;
            Vector2 forward = new Vector2(directionX, 0f);
            float dot = Vector2.Dot(forward.normalized, toPlayer.normalized);

            Debug.Log($"[HomelessBossAI] Attack end. useKick={useKick} dist={dist} dot={dot}");

            if (dist <= attackRange + 0.1f && dot > 0f)
            {
                int dmg = useKick ? kickDamage : punchDamage;
                Debug.Log("[HomelessBossAI] HIT player");
                player.TakeDamage(dmg);
            }
            else
            {
                Debug.Log("[HomelessBossAI] MISS player (range/facing)");
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // ──────────────────────────────────────────────
    //  Regeneration
    // ──────────────────────────────────────────────

    public void StartRegeneration()
    {
        if (regenRoutine != null || currentState == BossState.Dead)
            return;

        Debug.Log("[HomelessBossAI] StartRegeneration");
        SetState(BossState.Regenerating);
        hitDuringRegen = false;
        animator.SetBool("IsMoving", false);

        // Vypni NavMesh a zmraz Rigidbody – hráč ho nemôže posúvať
        UseRigidbodyMovement();
        rb.linearVelocity = Vector2.zero;
        rb.linearDamping = regenLinearDrag;
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

        regenRoutine = StartCoroutine(manager.StartBossRegeneration());
    }

    public bool IsRegenerating() => currentState == BossState.Regenerating;

    public void StopRegeneration()
    {
        if (regenRoutine != null)
        {
            StopCoroutine(regenRoutine);
            regenRoutine = null;
        }

        // Obnov pohyb
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = defaultLinearDrag;

        if (currentState != BossState.Dead)
        {
            Debug.Log("[HomelessBossAI] StopRegeneration - back to Fighting.");
            canAttack = true;

            // Prepni späť na NavMesh
            UseNavMeshMovement(walkSpeed);
            mover.StoppingDistance = attackRange * 0.9f;

            // Spusti retreat cooldown – boss nemôže hneď znovu utekať
            retreatOnCooldown = true;
            retreatCooldownTimer = retreatCooldown;
            Debug.Log($"[HomelessBossAI] Retreat cooldown started ({retreatCooldown}s)");

            SetState(BossState.Fighting);
        }
    }

    // ──────────────────────────────────────────────
    //  Hit / Death
    // ──────────────────────────────────────────────

    public void OnHit()
    {
        Debug.Log($"[HomelessBossAI] OnHit in state {currentState}");
        if (currentState == BossState.Regenerating)
        {
            if (hitDuringRegen) return;
            hitDuringRegen = true;

            if (regenInterruptRoutine != null)
                StopCoroutine(regenInterruptRoutine);
            regenInterruptRoutine = StartCoroutine(RegenInterruptRoutine());
        }
    }

    private IEnumerator RegenInterruptRoutine()
    {
        Debug.Log($"[HomelessBossAI] RegenInterruptRoutine – čakám {regenInterruptDelay}s");
        yield return new WaitForSeconds(regenInterruptDelay);
        regenInterruptRoutine = null;
        manager.InterruptBossRegeneration();
    }

    public void PlayDeath()
    {
        Debug.Log("[HomelessBossAI] PlayDeath");
        SetState(BossState.Dead);
        mover.Stop();
        mover.SetEnabled(false);
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("IsDead");
        animator.SetBool("IsMoving", false);
    }

    public void LockAI(bool locked)
    {
        aiLocked = locked;
        if (locked)
        {
            mover.Stop();
            mover.SetEnabled(false);
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            StopAllCoroutines();
            regenRoutine = null;
            regenInterruptRoutine = null;
        }
        Debug.Log($"[HomelessBossAI] LockAI({locked})");
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = player.transform.position.x - transform.position.x;
        SetDirection(Mathf.Sign(dirX));
    }

    private void SetDirection(float x)
    {
        directionX = Mathf.Clamp(x, -1f, 1f);
        animator.SetFloat("DirectionX", directionX);
    }

    private void SetLocomotion(HomelessLocomotionState state)
    {
        locomotionState = state;
        animator.SetInteger("LocomotionState", (int)locomotionState);
        Debug.Log($"[HomelessBossAI] SetLocomotion {locomotionState}");
    }

    private void SetState(BossState state)
    {
        if (currentState == state)
            return;

        Debug.Log($"[HomelessBossAI] State change {currentState} -> {state}");
        currentState = state;
    }
}
