using UnityEngine;
using System.Collections.Generic;

public class EnemyMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private int damageToBase = 1;
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite goblinSprite;
    [SerializeField] private Sprite orcSprite;
    [SerializeField] private Sprite ghostSprite;

    public static readonly List<EnemyMover> ActiveEnemies = new();

    public float PathProgress => ComputePathProgressDistance();
    public EnemyHealth Health { get; private set; }
    public bool ImmuneToSlow { get; private set; }

    private WaypointPath path;
    private BaseHealth baseHealth;
    private EnemyPool enemyPool;
    private int targetWaypointIndex;
    private float baseMoveSpeed;
    private float currentMoveSpeed;
    private float slowTimer;
    private float[] cumulativePathDistances;
    private float totalPathLength;

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        baseMoveSpeed = moveSpeed;
        currentMoveSpeed = baseMoveSpeed;
    }

    private void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    public void Setup(
        WaypointPath waypointPath,
        BaseHealth targetBase,
        EnemyData data,
        GameEconomy economy,
        EnemyPool pool)
    {
        path = waypointPath;
        baseHealth = targetBase;
        enemyPool = pool;
        targetWaypointIndex = 0;
        baseMoveSpeed = data != null ? Mathf.Max(0.1f, data.moveSpeed) : moveSpeed;
        currentMoveSpeed = baseMoveSpeed;
        slowTimer = 0f;
        ImmuneToSlow = data != null && (data.immuneToSlow || data.enemyType == EnemyType.Ghost);

        if (path == null || path.Waypoints.Count == 0)
        {
            Debug.LogError("EnemyMover: Path is missing or empty.");
            enabled = false;
            return;
        }

        BuildPathDistanceCache(path);
        ApplyVisualFromData(data);

        if (Health != null)
        {
            Health.SetupFromData(data, economy);
        }

        transform.position = path.Waypoints[0].position;
        targetWaypointIndex = path.Waypoints.Count > 1 ? 1 : 0;
    }

    public void Despawn()
    {
        if (enemyPool != null)
        {
            enemyPool.ReleaseEnemy(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (ImmuneToSlow)
        {
            return;
        }

        float clampedMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f);
        currentMoveSpeed = baseMoveSpeed * clampedMultiplier;
        slowTimer = Mathf.Max(slowTimer, Mathf.Max(0f, duration));
    }

    private void BuildPathDistanceCache(WaypointPath waypointPath)
    {
        int waypointCount = waypointPath != null ? waypointPath.Waypoints.Count : 0;
        cumulativePathDistances = new float[Mathf.Max(waypointCount, 1)];
        totalPathLength = 0f;

        for (int i = 1; i < waypointCount; i++)
        {
            Transform prev = waypointPath.Waypoints[i - 1];
            Transform next = waypointPath.Waypoints[i];
            if (prev == null || next == null)
            {
                cumulativePathDistances[i] = totalPathLength;
                continue;
            }

            totalPathLength += Vector3.Distance(prev.position, next.position);
            cumulativePathDistances[i] = totalPathLength;
        }
    }

    private void ApplyVisualFromData(EnemyData data)
    {
        if (spriteRenderer == null || data == null)
        {
            return;
        }

        switch (data.enemyType)
        {
            case EnemyType.Goblin:
                if (goblinSprite != null)
                {
                    spriteRenderer.sprite = goblinSprite;
                }

                break;
            case EnemyType.Orc:
                if (orcSprite != null)
                {
                    spriteRenderer.sprite = orcSprite;
                }

                break;
            case EnemyType.Ghost:
                if (ghostSprite != null)
                {
                    spriteRenderer.sprite = ghostSprite;
                }

                break;
        }
    }

    private float ComputePathProgressDistance()
    {
        if (path == null || path.Waypoints.Count == 0)
        {
            return 0f;
        }

        if (targetWaypointIndex <= 0)
        {
            return 0f;
        }

        if (targetWaypointIndex >= path.Waypoints.Count)
        {
            return totalPathLength;
        }

        int previousWaypointIndex = targetWaypointIndex - 1;
        Transform from = path.Waypoints[previousWaypointIndex];
        Transform to = path.Waypoints[targetWaypointIndex];
        if (from == null || to == null)
        {
            return previousWaypointIndex < cumulativePathDistances.Length
                ? cumulativePathDistances[previousWaypointIndex]
                : 0f;
        }

        Vector3 segment = to.position - from.position;
        float segmentLengthSq = segment.sqrMagnitude;
        if (segmentLengthSq <= Mathf.Epsilon)
        {
            return previousWaypointIndex < cumulativePathDistances.Length
                ? cumulativePathDistances[previousWaypointIndex]
                : 0f;
        }

        float segmentT = Mathf.Clamp01(Vector3.Dot(transform.position - from.position, segment) / segmentLengthSq);
        float segmentLength = Mathf.Sqrt(segmentLengthSq);
        float distanceBeforeSegment = previousWaypointIndex < cumulativePathDistances.Length
            ? cumulativePathDistances[previousWaypointIndex]
            : 0f;

        return distanceBeforeSegment + segmentT * segmentLength;
    }

    private void Update()
    {
        if (path == null || targetWaypointIndex >= path.Waypoints.Count)
        {
            return;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                currentMoveSpeed = baseMoveSpeed;
                slowTimer = 0f;
            }
        }

        Transform target = path.Waypoints[targetWaypointIndex];
        if (target == null)
        {
            targetWaypointIndex++;
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            currentMoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.02f)
        {
            targetWaypointIndex++;
            if (targetWaypointIndex >= path.Waypoints.Count)
            {
                if (baseHealth != null)
                {
                    baseHealth.TakeDamage(damageToBase);
                }

                Despawn();
            }
        }
    }
}
