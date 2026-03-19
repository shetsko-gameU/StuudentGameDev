using System.Numerics;
using UnityEngine;


public class Enemy_Base : MonoBehaviour



{
   [Header("Stats")]
    public StatManager stats;
    public StatManager Player_Stats;
    public Animator animator;
    Vector2 movevalue;
    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player_Attack")
        {
            stats.currentHealth -= Player_Stats.Attack;
        }
    }
    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }
    /*public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {
        }
    }*/
}
