using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack : EnemyState
{
<<<<<<< HEAD
    Transform playerTransform;
    float timer;
=======

    bool canSeeTarget;
    float attackTimer;
>>>>>>> ScriptBreanchfixs
    float timeBetweenAttacks = 1f; // Example attack cooldown
    float exitTimer;
    float timeToExitAfterAttack = 2f; // Time to exit attack state after
    float distanceToCountExit = 3f; // Distance to player to start exit timer
    Vector3 pivotTarget; //where enemy will pivot to after attacking
<<<<<<< HEAD
    public float pivotSpeed; // determines degrees per second when pivoting
=======
    public float pivotSpeed;
>>>>>>> ScriptBreanchfixs
    float cumulativeRotation = 0f;
    float totalTargetAngle = 0f;
    float angleDirection = 1f;
    public EnemyAttack(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
<<<<<<< HEAD
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
=======
        canSeeTarget = true;
>>>>>>> ScriptBreanchfixs
    }

    public override void EnterState()
    {
        // prepare attack state (e.g., reset timers)
    }

    public override void FrameUpdate()
    {
        enemy.moveState.StopMovement(); // Stop movement during attack
<<<<<<< HEAD
        if(timer >= timeBetweenAttacks)
        {
            OnAttack();
            timer = 0;
            //Vector2 dir = (playerTransform.position - enemy.transform.position).normalized;
=======
        if(attackTimer >= timeBetweenAttacks)
        {
            OnAttack();
            attackTimer = 0;
            //Vector2 dir = (enemy.currentTarget.transform.position - enemy.transform.position).normalized;
>>>>>>> ScriptBreanchfixs
        }
        //sets for enemies that can pivot around player
        else if (enemy.doesAttackPivot)
        {
            PivotAround();
        }
        if(exitTimer >= timeToExitAfterAttack)
        {
<<<<<<< HEAD
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
=======
            enemy.stateMachine.ChangeState(enemy.moveState);            
            exitTimer = 0;
        }
        else if(Vector3.Distance(enemy.transform.position, enemy.currentTarget.transform.position) > distanceToCountExit)
        {
            exitTimer += Time.deltaTime;
        }
        if(!canSeeTarget)
        {
            enemy.currentTarget = null;
            enemy.stateMachine.ChangeState(enemy.idleState);
        }
        attackTimer += Time.deltaTime;
>>>>>>> ScriptBreanchfixs
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
<<<<<<< HEAD
        float distance = Vector3.Distance(enemy.transform.position, playerTransform.position);
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        pivotTarget = playerTransform.position + (randomOffset * distance);

        // 2. Get vectors relative to the player pivot point
        Vector3 currentDir = playerTransform.position - enemy.transform.position;
        Vector3 targetDir = playerTransform.position - pivotTarget;
=======
        float distance = Vector3.Distance(enemy.transform.position, enemy.currentTarget.transform.position);
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        pivotTarget = enemy.currentTarget.transform.position + (randomOffset * distance);

        // 2. Get vectors relative to the player pivot point
        Vector3 currentDir = enemy.currentTarget.transform.position - enemy.transform.position;
        Vector3 targetDir = enemy.currentTarget.transform.position - pivotTarget;
>>>>>>> ScriptBreanchfixs

        // 3. Calculate the shortest signed angle
        float signedAngle = Vector3.SignedAngle(currentDir, targetDir, Vector3.up);
        
        // 4. Store total angle to travel and its direction (+1 or -1)
        totalTargetAngle = Mathf.Abs(signedAngle);
        angleDirection = Mathf.Sign(signedAngle);
        cumulativeRotation = 0f; // Reset tracker for the new movement
        if (cumulativeRotation < totalTargetAngle)
        {
            float deltaAngle = pivotSpeed * Time.deltaTime;

            // Shrink the last step to perfectly hit the target
            if (cumulativeRotation + deltaAngle > totalTargetAngle)
            {
                deltaAngle = totalTargetAngle - cumulativeRotation;
            }

            // Apply direction (clockwise or counter-clockwise) to the step
            float actualRotationStep = deltaAngle * angleDirection;

            // Execute rotation around player pivot
<<<<<<< HEAD
            enemy.transform.RotateAround(playerTransform.position, Vector3.up, actualRotationStep);
=======
            enemy.transform.RotateAround(enemy.currentTarget.transform.position, Vector3.up, actualRotationStep);
>>>>>>> ScriptBreanchfixs
            
            // Track progress using absolute values
            cumulativeRotation += deltaAngle;
        }
    }
}
