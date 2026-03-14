using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int rewardGold = 1;
    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = new(1f, 0.72f, 0.72f, 1f);
    [SerializeField] private float hitFlashDuration = 0.09f;
    [Header("Health Bar")]
    [SerializeField] private Transform healthBarRoot;
    [SerializeField] private Transform healthBarFill;
    [SerializeField] private bool hideHealthBarAtFullHealth;
    [SerializeField] private bool hideHealthBarAtZeroHealth = true;

    private int currentHealth;
    private GameEconomy economy;
    private EnemyMover owner;
    private SpriteRenderer spriteRenderer;
    private Color baseColor = Color.white;
    private Coroutine hitFlashRoutine;
    private Vector3 baseHealthBarFillScale = Vector3.one;
    private Vector3 baseHealthBarFillLocalPosition = Vector3.zero;

    private void Awake()
    {
        currentHealth = maxHealth;
        owner = GetComponent<EnemyMover>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        AutoResolveHealthBarRefs();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        if (healthBarFill != null)
        {
            baseHealthBarFillScale = healthBarFill.localScale;
            baseHealthBarFillLocalPosition = healthBarFill.localPosition;
        }

        UpdateHealthBar();
    }

    private void AutoResolveHealthBarRefs()
    {
        if (healthBarRoot == null)
        {
            Transform foundRoot = transform.Find("HealthBarRoot");
            if (foundRoot != null)
            {
                healthBarRoot = foundRoot;
            }
        }

        if (healthBarFill == null && healthBarRoot != null)
        {
            Transform foundFill = healthBarRoot.Find("HealthBarFill");
            if (foundFill != null)
            {
                healthBarFill = foundFill;
            }
        }
    }

    public void SetupFromData(EnemyData data, GameEconomy gameEconomy)
    {
        if (data != null)
        {
            maxHealth = Mathf.Max(1, data.maxHealth);
            rewardGold = Mathf.Max(0, data.rewardGold);
        }

        currentHealth = maxHealth;
        economy = gameEconomy;
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayHitFlash();
        UpdateHealthBar();

        if (currentHealth == 0)
        {
            if (economy != null && rewardGold > 0)
            {
                economy.AddGold(rewardGold);
            }

            if (owner != null)
            {
                owner.Despawn();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void PlayHitFlash()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        float duration = Mathf.Max(0.02f, hitFlashDuration);
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            spriteRenderer.color = Color.Lerp(baseColor, hitFlashColor, k);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half);
            spriteRenderer.color = Color.Lerp(hitFlashColor, baseColor, k);
            yield return null;
        }

        spriteRenderer.color = baseColor;
        hitFlashRoutine = null;
    }

    private void UpdateHealthBar()
    {
        float normalized = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        if (healthBarFill != null)
        {
            Vector3 scale = baseHealthBarFillScale;
            scale.x = baseHealthBarFillScale.x * normalized;
            healthBarFill.localScale = scale;
            float xOffset = (baseHealthBarFillScale.x - scale.x) * 0.5f;
            healthBarFill.localPosition = baseHealthBarFillLocalPosition + new Vector3(-xOffset, 0f, 0f);
        }

        if (healthBarRoot != null)
        {
            bool visible = true;
            if (hideHealthBarAtZeroHealth && currentHealth <= 0)
            {
                visible = false;
            }
            else if (hideHealthBarAtFullHealth && currentHealth >= maxHealth)
            {
                visible = false;
            }

            healthBarRoot.gameObject.SetActive(visible);
        }
    }
}
