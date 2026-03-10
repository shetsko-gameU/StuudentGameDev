using UnityEngine;


public class Enemy_Base : MonoBehaviour



{
   [Header("Stats")]
    public StatManager stats;
    public StatManager Player_Stats;
    



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


}
