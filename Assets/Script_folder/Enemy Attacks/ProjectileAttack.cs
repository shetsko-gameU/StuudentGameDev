using UnityEngine;

[CreateAssetMenu(menuName = "Attacks/ProjectileAttack")]
public class ProjectileAttack : AttackBase
{
    public override void Execute()
    {
        Debug.Log("Enemy performed melee attack");
    }
}
