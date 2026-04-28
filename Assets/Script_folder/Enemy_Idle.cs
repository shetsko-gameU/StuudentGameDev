using UnityEngine;

public class Enemy_Idle : Enemy_State
{
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
    }
    
    Vector3 GetRandomPointInRadius()
    {
        return enemy.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * enemy.randomMovementRange;
    }
}
