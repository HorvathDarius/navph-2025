using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int basicDamage = 10;
    [SerializeField] private int specialDamage = 20;
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 0.75f;

    [Header("Input")]
    [SerializeField] private InputAction attackAction;
    [SerializeField] private InputAction specialAttackAction;

    private Animator animator;
    private PlayerController controller;

    private bool canAttack = true;
    private bool combatEnabled;
    private bool inputLocked;
    private bool isAttacking;
    private bool isDead;
    private float lastDirectionX = 1f;

    void OnEnable()
    {
        attackAction.Enable();
        specialAttackAction.Enable();
    }

    void OnDisable()
    {
        attackAction.Disable();
        specialAttackAction.Disable();
    }
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        
        attackAction = InputSystem.actions.FindAction("Attack");
        specialAttackAction = InputSystem.actions.FindAction("SpecialAttack");
    }

    private void Update()
    {
        if (!combatEnabled || inputLocked)
            return;

        Vector2 moveDir = controller.GetCurrentMoveDirection();
        if (Mathf.Abs(moveDir.x) > 0.01f)
        {
            lastDirectionX = Mathf.Sign(moveDir.x);
            animator.SetFloat("DirectionX", lastDirectionX);
        }
        
        animator.SetBool("InCombat", combatEnabled);

        if (attackAction != null && attackAction.WasPressedThisFrame())
            TryBasicAttack();

        if (specialAttackAction != null && specialAttackAction.WasPressedThisFrame())
            TrySpecialAttack();
    }

    public void InitForBossFight()
    {
        combatEnabled = false;
        inputLocked = false;
        isAttacking = false;
        isDead = false;

        // animator.SetFloat("DirectionX", lastDirectionX);
        animator.SetBool("InCombat", false);

        Debug.Log("[PlayerCombat] InitForBossFight");
    }

    public void EnableCombat(bool enable)
    {
        combatEnabled = enable;
        animator.SetBool("InCombat", enable);
        Debug.Log($"[PlayerCombat] EnableCombat({enable})");
    }

    public void LockInput(bool locked)
    {
        inputLocked = locked;
        controller.SetMovementLocked(locked);
        Debug.Log($"[PlayerCombat] LockInput({locked})");
    }

    // ===== BASIC ATTACK =====

    private void TryBasicAttack()
    {
        bool isMoving = animator.GetBool("IsMoving");

        if (!canAttack || isAttacking || isMoving)
        {
            Debug.Log($"[PlayerCombat] TryBasicAttack blocked: canAttack={canAttack}, isAttacking={isAttacking}, isMoving={isMoving}");
            return;
        }

        StartCoroutine(BasicAttackRoutine());
    }

    private IEnumerator BasicAttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        controller.SetMovementLocked(true);
        animator.SetTrigger("Punch");

        AnimatorStateInfo state;
        float clipLength = 0.4f; // fallback
        yield return null;

        state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("main_char_punch_R") || state.IsName("main_char_punch_L"))
            clipLength = state.length;

        // damage sa aplikuje na konci animácie
        yield return new WaitForSeconds(clipLength);

        DealPlayerDamageToBoss(basicDamage);

        // cooldown po animácii
        yield return new WaitForSeconds(attackCooldown);

        controller.SetMovementLocked(false);
        isAttacking = false;
        canAttack = true;
    }

    // ===== SPECIAL ATTACK =====

    private void TrySpecialAttack()
    {
        bool isMoving = animator.GetBool("IsMoving");

        if (!canAttack || isAttacking || isMoving)
        {
            Debug.Log($"[PlayerCombat] TrySpecialAttack blocked: canAttack={canAttack}, isAttacking={isAttacking}, isMoving={isMoving}");
            return;
        }

        StartCoroutine(SpecialAttackRoutine());
    }

    private IEnumerator SpecialAttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        controller.SetMovementLocked(true);
        animator.SetTrigger("Throw");

        yield return null;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = state.length > 0 ? state.length : 0.5f;

        yield return new WaitForSeconds(clipLength);

        DealPlayerDamageToBoss(specialDamage, special: true);

        yield return new WaitForSeconds(attackCooldown);

        controller.SetMovementLocked(false);
        isAttacking = false;
        canAttack = true;
    }
    
    // ===== DAMAGE / FACING CHECK =====

    private void DealPlayerDamageToBoss(int damage, bool special = false)
    {
        var boss = FindAnyObjectByType<HomelessBossAI>();
        if (boss == null)
        {
            Debug.LogWarning("[PlayerCombat] DealPlayerDamageToBoss - boss not found.");
            return;
        }

        Vector2 toBoss = boss.transform.position - transform.position;
        float dist = toBoss.magnitude;

        // smer dopredu podľa lastDirectionX
        Vector2 forward = new Vector2(lastDirectionX, 0f);

        // musia byť približne vpredu
        float dot = Vector2.Dot(forward.normalized, toBoss.normalized);

        Debug.Log($"[PlayerCombat] DealDamage special={special} dist={dist} dot={dot}");

        if (dist <= attackRange * (special ? 1.5f : 1f) && dot > 0f)
        {
            Debug.Log("[PlayerCombat] HIT boss");
            FinalBossFightManager.Instance.ApplyDamageToBoss(damage);
        }
        else
        {
            Debug.Log("[PlayerCombat] MISS boss (range/facing)");
        }
    }

    // ===== DAMAGE / DEATH =====

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.ChangeHealth(-amount);

        if (GameManager.Instance.Health <= 0)
        {
            isDead = true;
            Debug.Log("[PlayerCombat] Player died, starting death routine.");
            StartCoroutine(PlayerDeathRoutine());
        }
    }

    public IEnumerator PlayerDeathRoutine()
    {
        LockInput(true);
        combatEnabled = false;
        animator.SetTrigger("IsDead");

        // Zmraz Rigidbody aby bossa ani fyzika neposúvala mŕtveho hráča
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        yield return FinalBossFightManager.Instance.HandlePlayerDeath();
    }
}
