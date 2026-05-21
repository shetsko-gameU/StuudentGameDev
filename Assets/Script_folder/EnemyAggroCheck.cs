using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject target{get; set;}
    public EnemyBase enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        enemy = GetComponentInParent<EnemyBase>();
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
            enemy.isAggroed = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == target)
        {
            enemy.isAggroed = false;
        }
    }
}
