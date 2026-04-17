using UnityEngine;

public class EnemyStrikeDistanceCheck : MonoBehaviour
{ 
    public GameObject target{get; set;}
    public Enemy_Base enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        enemy = GetComponentInParent<Enemy_Base>();
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
        if (other.gameObject == target)
        {
            enemy.isWithinRange = true;
        }
    }
    void  OnTriggerExit(Collider other)
    {
        if (other.gameObject == target)
        {
            enemy.isWithinRange = false;
        }
    }
}
