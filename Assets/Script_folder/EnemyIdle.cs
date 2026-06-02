using UnityEngine;
using System.Collections.Generic;
public class EnemyIdle : EnemyState
{
    public List<Transform> raycastPoints;
    public float sightRange;
    public Vector3 targetPosition;
    public Vector3 direction;
    public EnemyIdle(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
        raycastPoints = enemy.raycasts;
    }

    public override void EnterState()
    {
       targetPosition = GetRandomPointInRadius();
    }

    public override void FrameUpdate()
    {
        if(enemy.isAggroed)
        {
             enemyStateMachine.ChangeState(enemy.moveState);
             return;
        }
        direction = (targetPosition - enemy.transform.position).normalized;
        enemy.MoveEnemy(direction * enemy.randomMovementSpeed);
        if((enemy.transform.position - targetPosition).magnitude < 0.5f)
        {
            targetPosition = GetRandomPointInRadius();
        }
        CheckLineOfSite();
    }
    
    Vector3 GetRandomPointInRadius()
    {
        Vector2 randomPosition = UnityEngine.Random.insideUnitCircle * enemy.randomMovementRange;
        Vector3 adjustedPosition = new Vector3(randomPosition.x, 0, randomPosition.y);
        return enemy.transform.position + adjustedPosition;
    }

    void CheckLineOfSite()
    {
        RaycastHit hit;
        foreach(Transform point in raycastPoints)
        {
            //checks if enemy detects an object
            if(Physics.Raycast(point.position, point.forward, out hit, sightRange))
            {
                if(hit.collider.gameObject.CompareTag("Player") || hit.collider.gameObject.CompareTag("Dummy"))
                {
                    enemy.currentTarget = hit.collider.gameObject;
                    enemy.isAggroed = true;
                    return;
                }
            }
        }
    }
}
