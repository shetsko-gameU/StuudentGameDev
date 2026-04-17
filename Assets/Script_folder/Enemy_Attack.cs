using UnityEngine;

public class Enemy_Attack : Enemy_State
{
    Transform playerTransform;
    float timer;
    float timeBetweenAttacks = 1f; // Example attack cooldown
    float exitTimer;
    float timeToExitAfterAttack = 2f; // Time to exit attack state after
    float distanceToCountExit = 3f; // Distance to player to start exit timer
    public Enemy_Attack(Enemy_Base enemy, Enemy_State_Machine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void EnterState()
    {
        // prepare attack state (e.g., reset timers)
    }

    public override void FrameUpdate()
    {
        enemy.moveState(Vector3.zero); // Stop movement during attack
        if(timer >= timeBetweenAttacks)
        {
            OnAttack();
            timer = 0;
            //Vector2 dir = (playerTransform.position - enemy.transform.position).normalized;
        }
        if(exitTimer >= timeToExitAfterAttack)
        {
            enemy.StateMachine.ChangeState(enemy.moveState);
        }
        else if(Vector3.Distance(enemy.transform.position, playerTransform.position) > distanceToCountExit)
        {
            exitTimer += Time.deltaTime;
        }
        else
        {
            exitTimer = 0; // reset exit timer if player is close again
        }
        timer += Time.deltaTime;
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
