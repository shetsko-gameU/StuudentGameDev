using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeath : EnemyState
{
    public EnemyDeath(EnemyBase enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public override void EnterState()
    {
        // prepare attack state (e.g., reset timers)
    }

    public override void FrameUpdate()
    {
        enemy.animator.SetTrigger("Death");
        float deathAnimationLength = GetCurrentAnimationLength(enemy.animator, "Death");
        UnityEngine.Object.Destroy(enemy.gameObject, deathAnimationLength); // Destroy the enemy after the death animation completes
    }

    float GetCurrentAnimationLength(Animator animator, string stateName)
    {
        // Get all clips currently in the animator controller
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        float length = 2f; // Default fallback time in seconds

        if (ac != null)
        {
            foreach (AnimationClip clip in ac.animationClips)
            {
                // Match the clip name (Unity names them after the state or file name)
                if (clip.name.Contains(stateName))
                {
                    length = clip.length;
                    break;
                }
            }
        }

        return length;
    }

}
