using System.Collections;
using UnityEngine;

public class CharacterShooter : MonoBehaviour
{
    public enum AttackType
    {
        Melee,
        Projectile
    }

    public static CharacterShooter Active { get; private set; }

    [Header("Attack")]
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField, Min(1)] private int damage = 100;
    [SerializeField, Min(0f)] private float hitDelay = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.01f)] private float animationDuration = 0.5f;

    [Header("Projectile")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private CheeseProjectile projectilePrefab;

    [Header("Automatic Attack Card")]
    [SerializeField, Min(0.1f)] private float automaticAttackInterval = 1f;
    [SerializeField] private bool automaticAttackUnlocked;
    [SerializeField] private bool automaticAttackEnabled;

    private bool isAttacking;
    private bool attackQueued;
    private CheeseController currentTarget;
    private Coroutine attackCoroutine;
    private float nextAutomaticAttackTime;

    public bool AutomaticAttackUnlocked => automaticAttackUnlocked;
    public bool AutomaticAttackEnabled => automaticAttackEnabled;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        ShowIdlePose();
    }

    private void OnEnable()
    {
        Active = this;
        ShowIdlePose();
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;

        if (currentTarget != null)
            currentTarget.Died -= HandleTargetDied;

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        isAttacking = false;
        attackQueued = false;
        currentTarget = null;
        attackCoroutine = null;
    }

    private void Update()
    {
        if (!automaticAttackUnlocked || !automaticAttackEnabled ||
            Time.time < nextAutomaticAttackTime)
            return;

        CheeseController target = FindTarget();
        if (target == null)
            return;

        nextAutomaticAttackTime = Time.time + Mathf.Max(0.1f, automaticAttackInterval);
        Attack(target);
    }

    public void Attack(CheeseController target)
    {
        if (target == null || !target.IsAlive)
            return;

        if (!isAttacking)
        {
            BeginAttack(target);
            return;
        }

        if (target == currentTarget && currentTarget.IsAlive)
            attackQueued = true;
    }

    // The automatic-attack shop card can call this after a successful purchase.
    public void UnlockAutomaticAttack()
    {
        automaticAttackUnlocked = true;
        automaticAttackEnabled = true;
        nextAutomaticAttackTime = Time.time;
    }

    public void SetAutomaticAttackEnabled(bool enabled)
    {
        automaticAttackEnabled = automaticAttackUnlocked && enabled;
        if (automaticAttackEnabled)
            nextAutomaticAttackTime = Time.time;
    }

    private void BeginAttack(CheeseController target)
    {
        if (target == null || !target.IsAlive)
            return;

        isAttacking = true;
        attackQueued = false;
        currentTarget = target;
        currentTarget.Died += HandleTargetDied;
        attackCoroutine = StartCoroutine(AttackSequence(target));
    }

    private IEnumerator AttackSequence(CheeseController target)
    {
        PlayAttackAnimation();

        if (hitDelay > 0f)
            yield return new WaitForSeconds(hitDelay);

        if (target != null && target.IsAlive)
        {
            if (attackType == AttackType.Projectile)
                LaunchProjectile(target);
            else
                target.TakeDamage(damage, target.HitPosition);
        }

        float remainingAnimationTime = Mathf.Max(0f, animationDuration - hitDelay);
        if (remainingAnimationTime > 0f)
            yield return new WaitForSeconds(remainingAnimationTime);

        FinishAttack(target);
    }

    private void FinishAttack(CheeseController target)
    {
        bool canRunQueuedAttack =
            attackQueued &&
            currentTarget == target &&
            currentTarget != null &&
            currentTarget.IsAlive &&
            currentTarget.CurrentHealth > 0;

        CheeseController queuedTarget = canRunQueuedAttack ? currentTarget : null;

        if (currentTarget != null)
            currentTarget.Died -= HandleTargetDied;

        isAttacking = false;
        attackQueued = false;
        currentTarget = null;
        attackCoroutine = null;

        if (canRunQueuedAttack)
            BeginAttack(queuedTarget);
        else
            ShowIdlePose();
    }

    private void HandleTargetDied(CheeseController deadCheese)
    {
        if (deadCheese != currentTarget)
            return;

        deadCheese.Died -= HandleTargetDied;
        attackQueued = false;
        currentTarget = null;
    }

    private void LaunchProjectile(CheeseController target)
    {
        if (firePoint == null || projectilePrefab == null)
        {
            Debug.LogError("A projectile attacker needs a Fire Point and Projectile Prefab.", this);
            return;
        }

        GameAssets assets = GameAssets.Instance;
        Transform parent = assets != null ? assets.EffectsParent : null;
        CheeseProjectile projectile = Instantiate(
            projectilePrefab, firePoint.position, Quaternion.identity, parent);
        projectile.Launch(target, damage);
    }

    private void PlayAttackAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.enabled = true;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
    }

    private void ShowIdlePose()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.enabled = true;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
        animator.enabled = false;
    }

    private static CheeseController FindTarget()
    {
        foreach (CheeseController cheese in FindObjectsByType<CheeseController>())
        {
            if (cheese.IsAlive)
                return cheese;
        }

        return null;
    }
}
