using UnityEngine;

public class EnemyStrikeDistanceCheck : MonoBehaviour
{ 
    public GameObject playerTarget{get; set;}
    public GameObject dummyTarget{get; set;}
    public EnemyBase enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        dummyTarget = GameObject.FindGameObjectWithTag("Dummy");
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //Enemy determines trigger based on spotting the player
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.isWithinRange = true;
        }
    }
    void  OnTriggerExit(Collider other)
    {
<<<<<<< HEAD
        if (other.gameObject == playerTarget && other.gameObject == dummyTarget)
=======
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
>>>>>>> ScriptBreanchfixs
        {
            enemy.isWithinRange = false;
        }
    }
}
