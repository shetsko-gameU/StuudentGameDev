using UnityEngine;

public enum UnitType
{
    Player,
    Enemy
}

[CreateAssetMenu(fileName = "UnitStats", menuName = "Game/Stats/Unit Stats")]
public class BaseStatsSO : ScriptableObject
{
    [Header("Identity")]
    public UnitType unitType = UnitType.Enemy;
    public string displayName;

    [Header("Core Stats")]
    [Min(1)] public float maxHealth = 100f;
    [Min(0)] public float attack = 10f;
    [Min(0)] public float defense = 0f;

    [Header("Combat Feel")]
    [Min(0)] public float moveSpeed = 5f;
    [Min(0.01f)] public float attackSpeed = 1f;      // attacks per second

    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Stats/Player Stats")]
    public class PlayerStatsSO : BaseStatsSO
    {
    }

    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Game/Stats/Enemy Stats")]
    public class EnemyStatsSO : BaseStatsSO
    {

    }



    public float GetDamagePerHit()
    {
        return attack;
    }
}