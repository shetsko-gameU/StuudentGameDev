using UnityEngine;

[CreateAssetMenu(menuName = "Attacks/MeleeAttack")]
public class MeleeAttack : AttackBase
{
    public override void Execute()
    {
        Debug.Log("Enemy performed melee attack");
    }
}
