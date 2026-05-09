using UnityEngine;
using System.Collections.Generic;
public class Enemy_Idle : Enemy_State
{
    public List<Transform> raycastPoints;
    public float sightRange;
    public Vector3 targetPosition;
    public Vector3 direction;
    public Enemy_Idle(Enemy_Base enemy, Enemy_State_Machine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
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
        return enemy.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * enemy.randomMovementRange;
    }

    void CheckLineOfSite()
    {
        return;
        RaycastHit hit;
        foreach(Transform point in raycastPoints)
        {
            //checks if enemy detects an object
            if(Physics.Raycast(point.position, point.forward, out hit, sightRange))
            {
                if(hit.collider.gameObject.tag == "Player")
                    enemy.isAggroed = true;
            }
        }
    }
}
