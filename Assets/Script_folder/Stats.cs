using UnityEngine;

public enum UnitType
{
    Player,
    Enemy
}

/// <summary>
/// The base stat block for one entity — loaded by StatsManager.LoadFromSO on Awake and
/// never modified at runtime; all buffs/debuffs from RolledModifierInstance are layered
/// on top of these values, not written back into the asset.
///
/// Setup:
///   1. Project window → right-click → Create → Game → Stats → Unit Stats (or the
///      Player Stats / Enemy Stats variants below, for organizing by unitType).
///   2. Fill in maxHealth/attack/defense/moveSpeed/attackSpeed/dodgeChance/healthSteal.
///   3. Drag the asset into StatsManager's baseStats field (Inspector-only — it's a
///      private serialized field, not exposed as a public setter) on the entity's prefab.
/// </summary>
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

    [Range(0f, 1f)] public float dodgeChance = 0f;

    [Range(0f, 1f)]
    [Tooltip("Fraction of damage dealt restored as health. 0.20 = 20% of damage dealt is healed back.")]
    public float healthSteal = 0f;
    

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