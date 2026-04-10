using UnityEngine;

public class Enemy_Attack : Enemy_State
{
    public Enemy_Attack(Enemy_Base enemy, Enemy_State_Machine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public override void EnterState()
    {
        // prepare attack state (e.g., reset timers)
    }

    public override void FrameUpdate()
    {
        // per-frame attack logic
    }

    public override void PhysicsUpdate()
    {
        // physics-related attack logic
    }

    public void OnAttack()
    {
        if (enemy != null && enemy.animator != null)
            enemy.animator.SetTrigger("Attack");
    }
}
