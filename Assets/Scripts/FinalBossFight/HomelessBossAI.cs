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
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int punchDamage = 10;

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
        Debug.Log("[HomelessBossAI] Init");
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
        //Debug.Log($"[HomelessBossAI] MoveTowards {target} dist={dist}");

        if (dist <= 0.05f)
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

        SetState(BossState.SittingFightIdle);

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
        {
            Debug.LogWarning("[HomelessBossAI] HandleFighting - player is null.");
            return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        Vector2 dir = (player.transform.position - transform.position).normalized;
        SetDirection(Mathf.Sign(dir.x));

        if (dist > attackRange * 0.9f)
        {
            rb.linearVelocity = dir * walkSpeed;
            animator.SetBool("IsMoving", true);
            //Debug.Log($"[HomelessBossAI] Chasing player. Dist={dist}");
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsMoving", false);

            if (canAttack)
            {
                Debug.Log("[HomelessBossAI] In range, starting AttackRoutine.");
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        bool useKick = Random.value > 0.75f;
        if (useKick)
        {
            Debug.Log("[HomelessBossAI] AttackRoutine - Kick trigger.");
            animator.SetTrigger("Kick");
        }
        else
        {
            Debug.Log("[HomelessBossAI] AttackRoutine - Punch trigger.");
            animator.SetTrigger("Punch");
        }

        yield return new WaitForSeconds(0.3f);

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            Debug.Log($"[HomelessBossAI] Attack hit check. Dist={dist}");

            if (dist <= attackRange + 0.1f)
            {
                Debug.Log("[HomelessBossAI] Attack HIT - dealing damage.");
                player.TakeDamage(punchDamage);
            }
            else
            {
                Debug.Log("[HomelessBossAI] Attack MISS - player out of range.");
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        Debug.Log("[HomelessBossAI] AttackRoutine finished. canAttack=true");
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
        animator.SetBool("IsDead", true);
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
