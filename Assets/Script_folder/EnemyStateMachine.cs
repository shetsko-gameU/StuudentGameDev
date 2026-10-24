using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
   public EnemyState CurrentEnemyState { get; set; }
   public void Initialize(EnemyState startingState)
   {
        CurrentEnemyState = startingState;
        CurrentEnemyState?.EnterState();
   }

    public void ChangeState(EnemyState newState)
    {
        Debug.Log($"EnemyStateMachine: Changing state from {CurrentEnemyState?.GetType().Name} to {newState?.GetType().Name}");
        CurrentEnemyState?.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState?.EnterState();
    }
}
