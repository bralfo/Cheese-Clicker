using System;
using System.Collections;
using UnityEngine;

public class CheeseController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 1000;

    [Header("Appearance")]
    [SerializeField, Min(0.01f)] private float shrinkDuration = 0.15f;
    [SerializeField, Range(0f, 0.9f)] private float bottomColliderInset = 0.25f;

    private int cheeseLife;
    private Vector3 initialScale;
    private Coroutine shrinkCoroutine;
    private SpriteRenderer cheeseSpriteRenderer;

    public event Action<CheeseController> Died;

    public bool IsAlive => cheeseLife > 0 && isActiveAndEnabled;
    public int CurrentHealth => cheeseLife;
    public Vector3 HitPosition => cheeseSpriteRenderer != null
        ? cheeseSpriteRenderer.bounds.center : transform.position;

    private void Awake()
    {
        cheeseSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cheeseLife = maxHealth;
        initialScale = transform.localScale;
    }

    private void Start()
    {
        FitColliderToSprite();
        UpdateHealthBar();
    }

    private void OnMouseDown()
    {
        if (!IsAlive)
            return;

        CharacterShooter attacker = CharacterShooter.Active;
        if (attacker == null)
            attacker = FindAnyObjectByType<CharacterShooter>();

        if (attacker != null)
            attacker.Attack(this);
    }

    public void InitializeHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        cheeseLife = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        if (!IsAlive || damage <= 0)
            return;

        int appliedDamage = Mathf.Min(cheeseLife, damage);
        cheeseLife -= appliedDamage;
        UpdateHealthBar();
        ShowDamageFeedback(appliedDamage, hitPosition);

        if (cheeseLife == 0)
        {
            DestroyCheese();
            return;
        }

        if (shrinkCoroutine != null)
            StopCoroutine(shrinkCoroutine);

        shrinkCoroutine = StartCoroutine(ShrinkCheese());
    }

    private void ShowDamageFeedback(int damage, Vector3 hitPosition)
    {
        if (cheeseSpriteRenderer != null)
        {
            Vector3 popupPosition = cheeseSpriteRenderer.bounds.center;
            popupPosition.y = cheeseSpriteRenderer.bounds.max.y;
            DamagePopup.Create(popupPosition, damage);
        }

        DamageEffect.Create(hitPosition);
    }

    private void DestroyCheese()
    {
        Died?.Invoke(this);
        gameObject.SetActive(false);
        GameAssets assets = GameAssets.Instance;
        if (assets != null && assets.CheeseSpawner != null)
            assets.CheeseSpawner.SpawnCheese(maxHealth * 2);

        Destroy(gameObject);
    }

    private void FitColliderToSprite()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (cheeseSpriteRenderer == null || cheeseSpriteRenderer.sprite == null || boxCollider == null)
            return;

        Vector2[] vertices = cheeseSpriteRenderer.sprite.vertices;
        if (vertices.Length == 0)
            return;

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector2 vertex in vertices)
        {
            Vector2 point = transform.InverseTransformPoint(
                cheeseSpriteRenderer.transform.TransformPoint(vertex));
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        min.y += (max.y - min.y) * Mathf.Clamp(bottomColliderInset, 0f, 0.9f);
        boxCollider.offset = (min + max) * 0.5f;
        boxCollider.size = max - min;
    }

    private IEnumerator ShrinkCheese()
    {
        Vector3 targetScale = initialScale * ((float) cheeseLife / maxHealth);
        Vector3 currentScale = transform.localScale;

        float elapsedTime = 0f;

        while (elapsedTime < shrinkDuration)
        {
            transform.localScale = Vector3.Lerp(currentScale, targetScale, elapsedTime / shrinkDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void UpdateHealthBar()
    {
        GameAssets assets = GameAssets.Instance;
        if (assets != null && assets.CheeseHealthBar != null)
            assets.CheeseHealthBar.SetHealth(cheeseLife, maxHealth);
    }
}
