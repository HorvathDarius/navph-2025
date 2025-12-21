using System.Collections;
using UnityEngine;

public class HomelessBossAI : MonoBehaviour
{
    public enum BossState
    {
        IntroWalkToMiddle,
        IntroWalkBackToCart,
        SittingFightIdle,
        Fighting,
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
    [SerializeField] private Transform leftPatrolPoint;
    [SerializeField] private Transform rightPatrolPoint;
    [SerializeField] private GameObject cartObject;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private int kickDamage = 20;

    private FinalBossFightManager manager;
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerCombat player;

    private BossState currentState;
    private bool canAttack = true;
    private bool aiLocked;
    private float directionX = -1f;
    private HomelessLocomotionState locomotionState = HomelessLocomotionState.Walk;

    private Coroutine regenRoutine;

    public void Init(FinalBossFightManager mgr)
    {
        manager = mgr;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        player = FindAnyObjectByType<PlayerCombat>();
    }

    private void Update()
    {
        if (aiLocked || currentState == BossState.Dead)
            return;

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
            case BossState.Regenerating:
                FacePlayer();
                break;
        }
    }

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
        Debug.Log($"[HomelessBossAI] MoveTowards {target} dist={dist}");

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

    public void StartFightPhase()
    {
        Debug.Log("[HomelessBossAI] StartFightPhase");
        SetState(BossState.Fighting);
    }

    private void HandleFighting()
    {
        if (player == null)
            return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        Vector2 dir = (player.transform.position - transform.position).normalized;
        SetDirection(Mathf.Sign(dir.x));

        if (!canAttack && currentState == BossState.Fighting)
        {
            // počas cooldownu po útoku stojí
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);
            return;
        }

        if (dist > attackRange)
        {
            rb.linearVelocity = dir * walkSpeed;
            animator.SetBool("IsMoving", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            if (canAttack)
                StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsMoving", false);

        bool useKick = Random.value > 0.75f;
        animator.SetTrigger(useKick ? "Kick" : "Punch");

        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = state.length > 0 ? state.length : 0.4f;

        // čakáme na koniec animácie, až potom riešime damage
        yield return new WaitForSeconds(clipLength);

        if (player != null)
        {
            Vector2 toPlayer = player.transform.position - transform.position;
            float dist = toPlayer.magnitude;

            Vector2 forward = new Vector2(directionX, 0f); // directionX už určuje facing
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

    public void StartRegeneration()
    {
        if (regenRoutine != null || currentState == BossState.Dead)
            return;

        Debug.Log("[HomelessBossAI] StartRegeneration");
        SetState(BossState.Regenerating);
        animator.SetBool("IsMoving", false);
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

        if (currentState != BossState.Dead)
        {
            Debug.Log("[HomelessBossAI] StopRegeneration - back to Fighting.");
            SetState(BossState.Fighting);
        }
    }

    public void OnHit()
    {
        Debug.Log($"[HomelessBossAI] OnHit in state {currentState}");
        if (currentState == BossState.Regenerating)
            manager.InterruptBossRegeneration();
    }

    public void PlayDeath()
    {
        Debug.Log("[HomelessBossAI] PlayDeath");
        SetState(BossState.Dead);
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("IsDead");
        animator.SetBool("IsMoving", false);
    }

    public void LockAI(bool locked)
    {
        aiLocked = locked;
        if (locked)
            rb.linearVelocity = Vector2.zero;

        Debug.Log($"[HomelessBossAI] LockAI({locked})");
    }

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
        //Debug.Log($"[HomelessBossAI] SetDirection {directionX}");
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
