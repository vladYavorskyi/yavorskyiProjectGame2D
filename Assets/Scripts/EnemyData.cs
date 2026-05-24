using UnityEngine;

public enum EnemyType
{
    Goblin,
    Orc,
    Ghost
}

[CreateAssetMenu(menuName = "TD/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    public EnemyType enemyType;
    public int maxHealth = 10;
    public float moveSpeed = 2f;
    public int attackCost = 10;
    public int rewardGold = 5;
    public int damageToBase = 1;
    public bool immuneToSlow = false;
}
