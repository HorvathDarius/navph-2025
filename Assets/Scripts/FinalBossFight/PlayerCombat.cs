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
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Input")]
    [SerializeField] private InputAction attackAction;
    [SerializeField] private InputAction specialAttackAction;

    private Animator animator;
    private PlayerController controller;

    private bool canAttack = true;
    private bool combatEnabled;
    private bool inputLocked;
    private bool isAttacking;
    private float lastDirectionX = 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        attackAction = InputSystem.actions.FindAction("Attack");
        specialAttackAction = InputSystem.actions.FindAction("SpecialAttack");

        Debug.Log("[PlayerCombat] Start - Attack actions bound.");
    }

    private void Update()
    {
        if (!combatEnabled || inputLocked)
            return;

        // počas útoku nemeň movement parametre – nech Animator ostane v útokovom stave
        if (!isAttacking)
        {
            Vector2 moveDir = controller.GetCurrentMoveDirection();
            bool isMoving = moveDir.sqrMagnitude > 0.0001f;

            if (Mathf.Abs(moveDir.x) > 0.01f)
                lastDirectionX = Mathf.Sign(moveDir.x);

            animator.SetFloat("DirectionX", lastDirectionX);
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsRunning", controller.IsRunning());

            //Debug.Log($"[PlayerCombat] MoveDir={moveDir}, IsMoving={isMoving}, IsRunning={controller.IsRunning()}, DirX={lastDirectionX}");
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
        lastDirectionX = 1f;

        animator.SetFloat("DirectionX", lastDirectionX);
        animator.SetBool("InCombat", false);
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsRunning", false);

        Debug.Log("[PlayerCombat] InitForBossFight - combat disabled, reset animator params.");
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
        controller.enabled = !locked;

        if (locked)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
        }

        Debug.Log($"[PlayerCombat] LockInput({locked})");
    }

    public Vector2 GetFacingDir() => new(lastDirectionX, 0f);

    // ===== BASIC ATTACK =====

    private void TryBasicAttack()
    {
        if (!canAttack || isAttacking)
        {
            Debug.Log($"[PlayerCombat] TryBasicAttack blocked: canAttack={canAttack}, isAttacking={isAttacking}");
            return;
        }

        Debug.Log("[PlayerCombat] TryBasicAttack - starting coroutine.");
        StartCoroutine(BasicAttackRoutine());
    }

    private IEnumerator BasicAttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        animator.SetTrigger("Punch");
        Debug.Log("[PlayerCombat] BasicAttackRoutine - Punch trigger set.");

        // počkaj na „hit frame“ – tu podľa dĺžky animácie
        yield return new WaitForSeconds(0.2f);

        var boss = FindAnyObjectByType<HomelessBossAI>();
        if (boss != null)
        {
            float dist = Vector2.Distance(transform.position, boss.transform.position);
            Debug.Log($"[PlayerCombat] BasicAttack hit check. Dist={dist}");

            if (dist <= attackRange)
            {
                Debug.Log("[PlayerCombat] BasicAttack HIT - applying damage.");
                FinalBossFightManager.Instance.ApplyDamageToBoss(basicDamage);
            }
            else
            {
                Debug.Log("[PlayerCombat] BasicAttack MISS - target out of range.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerCombat] BasicAttackRoutine - boss not found.");
        }

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
        Debug.Log("[PlayerCombat] BasicAttackRoutine finished. canAttack=true, isAttacking=false");
    }

    // ===== SPECIAL ATTACK =====

    private void TrySpecialAttack()
    {
        if (!canAttack || isAttacking)
        {
            Debug.Log($"[PlayerCombat] TrySpecialAttack blocked: canAttack={canAttack}, isAttacking={isAttacking}");
            return;
        }

        Debug.Log("[PlayerCombat] TrySpecialAttack - starting coroutine.");
        StartCoroutine(SpecialAttackRoutine());
    }

    private IEnumerator SpecialAttackRoutine()
    {
        canAttack = false;
        isAttacking = true;

        animator.SetTrigger("Throw");
        Debug.Log("[PlayerCombat] SpecialAttackRoutine - Throw trigger set.");

        yield return new WaitForSeconds(0.25f);

        var boss = FindAnyObjectByType<HomelessBossAI>();
        if (boss != null)
        {
            float dist = Vector2.Distance(transform.position, boss.transform.position);
            Debug.Log($"[PlayerCombat] SpecialAttack hit check. Dist={dist}");

            if (dist <= attackRange * 1.5f)
            {
                Debug.Log("[PlayerCombat] SpecialAttack HIT - applying damage.");
                FinalBossFightManager.Instance.ApplyDamageToBoss(specialDamage);
            }
            else
            {
                Debug.Log("[PlayerCombat] SpecialAttack MISS - target out of range.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerCombat] SpecialAttackRoutine - boss not found.");
        }

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
        Debug.Log("[PlayerCombat] SpecialAttackRoutine finished. canAttack=true, isAttacking=false");
    }

    // ===== DAMAGE / DEATH =====

    public void TakeDamage(int amount)
    {
        Debug.Log($"[PlayerCombat] TakeDamage({amount})");
        GameManager.Instance.ChangeHealth(-amount);

        if (GameManager.Instance.Health <= 0)
        {
            Debug.Log("[PlayerCombat] Player died, starting death routine.");
            StartCoroutine(PlayerDeathRoutine());
        }
    }

    private IEnumerator PlayerDeathRoutine()
    {
        LockInput(true);
        PlayDeath();
        yield return FinalBossFightManager.Instance.HandlePlayerDeath();
    }

    public void PlayDeath()
    {
        Debug.Log("[PlayerCombat] PlayDeath - Death trigger set.");
        animator.SetTrigger("Death");
    }
}
