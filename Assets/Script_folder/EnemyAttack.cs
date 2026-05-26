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
    public float pivotSpeed; // determines degrees per second when pivoting
    float cumulativeRotation = 0f;
    float totalTargetAngle = 0f;
    float angleDirection = 1f;
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
        //sets for enemies that can pivot around player
        else if (enemy.doesPivotAttack)
        {
            PivotAround();
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
    }
    void PivotAround()
    {
        // 1. Calculate random target at player distance
        float distance = Vector3.Distance(enemy.transform.position, playerTransform.position);
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        pivotTarget = playerTransform.position + (randomOffset * distance);

        // 2. Fix: Get vectors relative to the player pivot point
        Vector3 currentDir = enemy.transform.position - playerTransform.position;
        Vector3 targetDir = pivotTarget - playerTransform.position;

        // 3. Fix: Calculate the shortest signed angle
        float signedAngle = Vector3.SignedAngle(currentDir, targetDir, Vector3.up);
        
        // 4. Store total angle to travel and its direction (+1 or -1)
        totalTargetAngle = Mathf.Abs(signedAngle);
        angleDirection = Mathf.Sign(signedAngle);
        cumulativeRotation = 0f; // Reset tracker for the new movement
        if (cumulativeRotation < totalTargetAngle)
        {
            float deltaAngle = pivotSpeed * Time.deltaTime;

            // Fix: Shrink the last step to perfectly hit the target
            if (cumulativeRotation + deltaAngle >= totalTargetAngle)
            {
                deltaAngle = totalTargetAngle - cumulativeRotation;
            }

            // Fix: Apply direction (clockwise or counter-clockwise) to the step
            float actualRotationStep = deltaAngle * angleDirection;

            // Execute rotation around player pivot
            enemy.transform.RotateAround(playerTransform.position, Vector3.up, actualRotationStep);
            
            // Track progress using absolute values
            cumulativeRotation += deltaAngle;
        }
    }
}
