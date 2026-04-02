using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_State_Machine
{
  [SerializeField] Enemy_State CurrentEnemyState { get; set; }
   public void Initialize(Enemy_State startingState)
   {
        CurrentEnemyState = startingState;
        CurrentEnemyState.EnterState();
   }

    public void ChangeState(Enemy_State newState)
    {
        CurrentEnemyState.ExitStage();
        CurrentEnemyState = newState;
        CurrentEnemyState.EnterStage();
    }
}
