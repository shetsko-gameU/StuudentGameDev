using UnityEngine;

/// <summary>
/// Superseded by ComboRunner.cs — see that file's own doc comment ("Replaces
/// PlayerAttack"). This is the old single-trigger attack script, kept around but not wired
/// into the current Player prefab; ComboRunner + AttackHitbox handle combo sequencing,
/// damage, and hit detection instead.
///
/// Setup: none — not currently used on any prefab in this project.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    public Animator animator;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
 
    }

    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }



}
