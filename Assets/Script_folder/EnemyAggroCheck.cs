using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject playerTarget{get; set;}
    public GameObject dummyTarget{get; set;}
    public EnemyBase enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //adjust FindGameObject to incorporate dummy as target
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        dummyTarget = GameObject.FindGameObjectWithTag("Dummy");
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
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.isAggroed = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerTarget && other.gameObject == dummyTarget)
        {
            enemy.isAggroed = false;
        }
    }
}
