using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_State_Machine
{


    public void Initialize(Enemy_State startingState)
    {
        CurrentEnemyState = startingState;
        CurrentEnemyState?.EnterState();
    }

    public void ChangeState(Enemy_State newState)
    {
        CurrentEnemyState?.ExitState();
        CurrentEnemyState = newState;
        CurrentEnemyState?.EnterState();
    }
}
