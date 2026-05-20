using UnityEngine;


public class EnemyBase : MonoBehaviour



{
   [Header("Stats")]
    public StatsManager stats;
    public StatsManager Player_Stats;
    public Animator animator;
    



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
        if (other.gameObject.tag == "PlayerAttack")
        {
            stats.currentHealth -= Player_Stats.Attack;
        }
    }
    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }

}
