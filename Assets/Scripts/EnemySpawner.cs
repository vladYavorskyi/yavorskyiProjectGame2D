using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoundPhase
{
    Menu,
    Preparation,
    Battle,
    RoundEnd,
    GameOver
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyMover enemyPrefab;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private List<EnemyData> enemyTypes = new();
    [SerializeField] private WaypointPath path;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private GameEconomy economy;
    [SerializeField] private int totalRounds = 10;
    [SerializeField] private int orcUnlockRound = 3;
    [SerializeField] private int ghostUnlockRound = 5;
    [SerializeField] private int startAttackBudget = 200;
    [SerializeField] private int attackBudgetGrowthPerRound = 35;
    [SerializeField] private float earlyRoundBudgetScale = 0.6f;
    [SerializeField] private float lateRoundBudgetScale = 1f;
    [SerializeField] private int maxEnemiesPerWave = 50;
    [SerializeField] private int startWaveEnemyLimit = 12;
    [SerializeField] private int endWaveEnemyLimit = 50;
    [SerializeField] private float earlyMinSpawnInterval = 1.15f;
    [SerializeField] private float earlyMaxSpawnInterval = 1.45f;
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float maxSpawnInterval = 1.2f;
    [SerializeField] private float preparationDuration = 6.0f;
    [SerializeField] private float timeBetweenRounds = 2.0f;

    public event Action<int, int> RoundChanged;
    public event Action<int> AttackBudgetChanged;
    public event Action<RoundPhase> PhaseChanged;
    public event Action<bool> GameFinished;
    public int CurrentRound { get; private set; }
    public int TotalRounds => totalRounds;
    public int CurrentAttackBudget { get; private set; }
    public bool IsSpawning { get; private set; }
    public RoundPhase CurrentPhase { get; private set; }
    public bool IsGameFinished { get; private set; }
    public bool IsGameStarted { get; private set; }

    private void Start()
    {
        SetPhase(RoundPhase.Menu);
    }

    public void StartGame()
    {
        if (IsGameStarted)
        {
            return;
        }

        IsGameStarted = true;
        StartCoroutine(RunRounds());
    }

    [ContextMenu("Apply Stress Test Preset")]
    public void ApplyStressTestPreset()
    {
        totalRounds = 10;
        startAttackBudget = 500;
        attackBudgetGrowthPerRound = 40;
        maxEnemiesPerWave = 50;
        minSpawnInterval = 0.8f;
        maxSpawnInterval = 1.0f;
        preparationDuration = 10f;
        timeBetweenRounds = 2f;
    }

    [ContextMenu("Apply Progressive Wave Preset")]
    public void ApplyProgressiveWavePreset()
    {
        totalRounds = 10;
        orcUnlockRound = 3;
        ghostUnlockRound = 5;
        startAttackBudget = 200;
        attackBudgetGrowthPerRound = 35;
        earlyRoundBudgetScale = 0.6f;
        lateRoundBudgetScale = 1f;
        maxEnemiesPerWave = 50;
        startWaveEnemyLimit = 12;
        endWaveEnemyLimit = 50;
        earlyMinSpawnInterval = 1.15f;
        earlyMaxSpawnInterval = 1.45f;
        minSpawnInterval = 0.8f;
        maxSpawnInterval = 1.2f;
        preparationDuration = 8f;
        timeBetweenRounds = 2f;
    }

    private IEnumerator RunRounds()
    {
        if (enemyPrefab == null || enemyPool == null || path == null || baseHealth == null || enemyTypes.Count == 0)
        {
            Debug.LogError("EnemySpawner: Assign enemyPrefab, enemyPool, enemyTypes, path and baseHealth in Inspector.");
            yield break;
        }

        for (int round = 1; round <= totalRounds; round++)
        {
            if (baseHealth.CurrentHealth <= 0 || IsGameFinished)
            {
                FinishGame(false);
                yield break;
            }

            CurrentRound = round;
            RoundChanged?.Invoke(CurrentRound, totalRounds);
            int rawBudget = startAttackBudget + (round - 1) * attackBudgetGrowthPerRound;
            float budgetScale = Mathf.Lerp(earlyRoundBudgetScale, lateRoundBudgetScale, GetRoundProgress01(round));
            CurrentAttackBudget = Mathf.RoundToInt(rawBudget * budgetScale);
            AttackBudgetChanged?.Invoke(CurrentAttackBudget);

            SetPhase(RoundPhase.Preparation);
            yield return WaitWithDeathCheck(preparationDuration);

            if (baseHealth.CurrentHealth <= 0 || IsGameFinished)
            {
                FinishGame(false);
                yield break;
            }

            SetPhase(RoundPhase.Battle);
            List<EnemyData> wave = GenerateWave(CurrentAttackBudget);
            LogWaveComposition(wave);
            yield return SpawnWave(wave);

            while (EnemyMover.ActiveEnemies.Count > 0 && baseHealth.CurrentHealth > 0 && !IsGameFinished)
            {
                yield return null;
            }

            if (baseHealth.CurrentHealth <= 0 || IsGameFinished)
            {
                FinishGame(false);
                yield break;
            }

            SetPhase(RoundPhase.RoundEnd);
            if (round < totalRounds)
            {
                yield return WaitWithDeathCheck(timeBetweenRounds);
            }
        }

        if (baseHealth.CurrentHealth > 0 && !IsGameFinished)
        {
            Debug.Log("Victory: All rounds survived.");
            FinishGame(true);
        }
    }

    private IEnumerator WaitWithDeathCheck(float duration)
    {
        float timer = Mathf.Max(0f, duration);
        while (timer > 0f)
        {
            if (baseHealth != null && baseHealth.CurrentHealth <= 0)
            {
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private List<EnemyData> GenerateWave(int budget)
    {
        List<EnemyData> wave = new();
        EnemyData goblinData = null;
        EnemyData orcData = null;
        EnemyData ghostData = null;

        int minCost = int.MaxValue;
        foreach (EnemyData enemy in enemyTypes)
        {
            if (enemy == null || !IsEnemyUnlockedForRound(enemy, CurrentRound))
            {
                continue;
            }

            if (enemy.attackCost < minCost)
            {
                minCost = enemy.attackCost;
            }

            switch (enemy.enemyType)
            {
                case EnemyType.Goblin:
                    goblinData ??= enemy;
                    break;
                case EnemyType.Orc:
                    orcData ??= enemy;
                    break;
                case EnemyType.Ghost:
                    ghostData ??= enemy;
                    break;
            }
        }

        if (minCost == int.MaxValue)
        {
            return wave;
        }

        float difficulty01 = totalRounds > 1
            ? Mathf.Clamp01((CurrentRound - 1f) / (totalRounds - 1f))
            : 1f;
        int roundEnemyLimit = GetRoundEnemyLimit();

        // Guarantee visible enemy variety after unlock rounds.
        if (CurrentRound >= Mathf.Max(1, orcUnlockRound) && orcData != null && budget >= orcData.attackCost && wave.Count < roundEnemyLimit)
        {
            wave.Add(orcData);
            budget -= orcData.attackCost;
        }

        if (CurrentRound >= Mathf.Max(1, ghostUnlockRound) && ghostData != null && budget >= ghostData.attackCost && wave.Count < roundEnemyLimit)
        {
            wave.Add(ghostData);
            budget -= ghostData.attackCost;
        }

        if (wave.Count == 0 && goblinData != null && budget >= goblinData.attackCost && wave.Count < roundEnemyLimit)
        {
            wave.Add(goblinData);
            budget -= goblinData.attackCost;
        }

        int safety = 0;
        while (budget >= minCost && wave.Count < roundEnemyLimit && safety < 1000)
        {
            safety++;
            EnemyData pick = PickEnemyByDifficulty(budget, difficulty01, wave.Count);
            if (pick == null)
            {
                break;
            }

            wave.Add(pick);
            budget -= pick.attackCost;
        }

        return wave;
    }

    private EnemyData PickEnemyByDifficulty(int budget, float difficulty01, int waveIndex)
    {
        List<EnemyData> affordable = new();
        float totalWeight = 0f;

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemyData candidate = enemyTypes[i];
            if (candidate == null || candidate.attackCost > budget || !IsEnemyUnlockedForRound(candidate, CurrentRound))
            {
                continue;
            }

            float weight = GetDifficultyWeight(candidate, difficulty01, waveIndex);
            if (weight <= 0f)
            {
                continue;
            }

            affordable.Add(candidate);
            totalWeight += weight;
        }

        if (affordable.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < affordable.Count; i++)
        {
            EnemyData candidate = affordable[i];
            cumulative += GetDifficultyWeight(candidate, difficulty01, waveIndex);
            if (roll <= cumulative)
            {
                return candidate;
            }
        }

        return affordable[affordable.Count - 1];
    }

    private float GetDifficultyWeight(EnemyData enemy, float difficulty01, int waveIndex)
    {
        if (enemy == null)
        {
            return 0f;
        }

        // Early rounds: more weak/fast enemies. Late rounds: more tanks and special units.
        float weight = enemy.enemyType switch
        {
            EnemyType.Goblin => Mathf.Lerp(1.9f, 0.6f, difficulty01),
            EnemyType.Orc => Mathf.Lerp(0.5f, 1.8f, difficulty01),
            EnemyType.Ghost => Mathf.Lerp(0.05f, 1.4f, Mathf.InverseLerp(0.25f, 1f, difficulty01)),
            _ => 1f
        };

        // Add small variety spikes every few slots.
        if (waveIndex > 0 && waveIndex % 6 == 0)
        {
            weight *= enemy.enemyType == EnemyType.Orc ? 1.2f : 1f;
        }

        if (waveIndex > 0 && waveIndex % 9 == 0)
        {
            weight *= enemy.enemyType == EnemyType.Ghost ? 1.25f : 1f;
        }

        return Mathf.Max(0f, weight);
    }

    private bool IsEnemyUnlockedForRound(EnemyData enemy, int round)
    {
        if (enemy == null)
        {
            return false;
        }

        int unlockRound = enemy.enemyType switch
        {
            EnemyType.Orc => orcUnlockRound,
            EnemyType.Ghost => ghostUnlockRound,
            _ => 1
        };

        return round >= Mathf.Max(1, unlockRound);
    }

    private int GetRoundEnemyLimit()
    {
        int limit = Mathf.RoundToInt(
            Mathf.Lerp(startWaveEnemyLimit, endWaveEnemyLimit, GetRoundProgress01(CurrentRound)));

        limit = Mathf.Clamp(limit, 1, maxEnemiesPerWave);
        return limit;
    }

    private float GetRoundProgress01(int round)
    {
        if (totalRounds <= 1)
        {
            return 1f;
        }

        return Mathf.Clamp01((round - 1f) / (totalRounds - 1f));
    }

    private void LogWaveComposition(List<EnemyData> wave)
    {
        if (wave == null)
        {
            return;
        }

        int goblin = 0;
        int orc = 0;
        int ghost = 0;
        for (int i = 0; i < wave.Count; i++)
        {
            EnemyData data = wave[i];
            if (data == null)
            {
                continue;
            }

            switch (data.enemyType)
            {
                case EnemyType.Goblin:
                    goblin++;
                    break;
                case EnemyType.Orc:
                    orc++;
                    break;
                case EnemyType.Ghost:
                    ghost++;
                    break;
            }
        }

        Debug.Log($"Round {CurrentRound} wave => Goblin: {goblin}, Orc: {orc}, Ghost: {ghost}, Total: {wave.Count}");
    }

    private IEnumerator SpawnWave(List<EnemyData> wave)
    {
        IsSpawning = true;
        float roundProgress = GetRoundProgress01(CurrentRound);
        float roundMinSpawn = Mathf.Lerp(earlyMinSpawnInterval, minSpawnInterval, roundProgress);
        float roundMaxSpawn = Mathf.Lerp(earlyMaxSpawnInterval, maxSpawnInterval, roundProgress);
        if (roundMaxSpawn < roundMinSpawn)
        {
            (roundMinSpawn, roundMaxSpawn) = (roundMaxSpawn, roundMinSpawn);
        }

        for (int i = 0; i < wave.Count; i++)
        {
            if (baseHealth == null || baseHealth.CurrentHealth <= 0 || IsGameFinished)
            {
                break;
            }

            EnemyData enemyData = wave[i];
            EnemyMover enemy = enemyPool.GetEnemy(transform.position, Quaternion.identity);
            enemy.Setup(path, baseHealth, enemyData, economy, enemyPool);
            float delay = UnityEngine.Random.Range(roundMinSpawn, roundMaxSpawn);
            yield return new WaitForSeconds(delay);
        }

        IsSpawning = false;
    }

    private void SetPhase(RoundPhase phase)
    {
        CurrentPhase = phase;
        PhaseChanged?.Invoke(CurrentPhase);
    }

    private void FinishGame(bool defenderWon)
    {
        if (IsGameFinished)
        {
            return;
        }

        IsGameFinished = true;
        SetPhase(RoundPhase.GameOver);
        GameFinished?.Invoke(defenderWon);
    }
}
