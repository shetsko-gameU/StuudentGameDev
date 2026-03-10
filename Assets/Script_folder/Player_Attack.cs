using UnityEngine;

public class Player_Attack : MonoBehaviour
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
