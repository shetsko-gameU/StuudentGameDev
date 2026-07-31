using UnityEngine;
using System.Collections.Generic;
public class EnemyIdle : EnemyState
{
    public List<Transform> raycastPoints;

    public Vector3 targetPosition;
    public Vector3 direction;
<<<<<<< HEAD
=======
    Vector3 startPosition;
>>>>>>> ScriptBreanchfixs
    public EnemyIdle(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        raycastPoints = enemy.raycasts;
    }

    public override void EnterState()
    {
       targetPosition = GetRandomPointInRadius();
<<<<<<< HEAD
=======
       startPosition = enemy.transform.position;
>>>>>>> ScriptBreanchfixs
    }

    public override void FrameUpdate()
    {
        if(enemy.isAggroed)
        {
             enemyStateMachine.ChangeState(enemy.moveState);
             return;
        }
<<<<<<< HEAD
=======

        Vector3 offsetFromStart = enemy.transform.position - startPosition;
        if (offsetFromStart.magnitude >= enemy.randomMovementRange)
        {
            targetPosition = GetRandomPointInRadius();
            enemy.transform.position = startPosition + (offsetFromStart.normalized * enemy.randomMovementRange);
        }
        
>>>>>>> ScriptBreanchfixs
        direction = (targetPosition - enemy.transform.position).normalized;
        enemy.MoveEnemy(direction * enemy.randomMovementSpeed);
        if((enemy.transform.position - targetPosition).magnitude < 0.5f)
        {
            targetPosition = GetRandomPointInRadius();
        }
        //CheckLineOfSite();
        CheckDistance();
    }
    
    Vector3 GetRandomPointInRadius()
    {
        Vector2 randomPosition = UnityEngine.Random.insideUnitCircle * enemy.randomMovementRange;
        Vector3 adjustedPosition = new Vector3(randomPosition.x, 0, randomPosition.y);
<<<<<<< HEAD
        return enemy.transform.position + adjustedPosition;
=======
        return startPosition + adjustedPosition;
>>>>>>> ScriptBreanchfixs
    }

    void CheckLineOfSite()
    {
        RaycastHit hit;
        foreach(Transform point in raycastPoints)
        {
            //checks if enemy detects an object
            if(Physics.Raycast(point.position, point.forward, out hit, enemy.sightRange))
            {
                if(hit.collider.gameObject.CompareTag("Player") || hit.collider.gameObject.CompareTag("Dummy"))
<<<<<<< HEAD
                    enemy.isAggroed = true;
=======
                {
                    enemy.currentTarget = hit.collider.gameObject;
                    enemy.isAggroed = true;
                    return;
                }
>>>>>>> ScriptBreanchfixs
            }
        }
    }
    void CheckDistance()
    {
        Collider[] hitColliders = Physics.OverlapSphere(enemy.transform.position, enemy.sightRange);
        foreach (var hitCollider in hitColliders)
        {
            if(hitCollider.gameObject.CompareTag("Dummy") || hitCollider.gameObject.CompareTag("Player"))
            {
                enemy.currentTarget = hitCollider.gameObject;
<<<<<<< HEAD
=======
                enemy.isAggroed = true;
                return;
>>>>>>> ScriptBreanchfixs
            }
        }
    }
}
