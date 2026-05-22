using UnityEngine;

public class EnemyAttack : EnemyState
{
    Transform playerTransform;
    float timer;
    float timeBetweenAttacks = 1f; // Example attack cooldown
    float exitTimer;
    float timeToExitAfterAttack = 2f; // Time to exit attack state after
    float distanceToCountExit = 3f; // Distance to player to start exit timer
    Vector3 pivotTarget; //where enemy will pivot to after attacking
    public EnemyAttack(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
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
        enemy.moveState.StopMovement(); // Stop movement during attack
        if(timer >= timeBetweenAttacks)
        {
            OnAttack();
            timer = 0;
            //Vector2 dir = (playerTransform.position - enemy.transform.position).normalized;
        }
        if(exitTimer >= timeToExitAfterAttack)
        {
            enemy.stateMachine.ChangeState(enemy.moveState);
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

        if (enemy.doesPivotAttack)
        {
            pivotTarget = playerTransform.position + (new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1)).normalized * Vector3.Distance(enemy.transform.position, playerTransform.position));
            
        }
    }
}
