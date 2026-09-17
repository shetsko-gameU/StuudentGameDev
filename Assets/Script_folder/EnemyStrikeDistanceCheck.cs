using UnityEngine;

public class EnemyStrikeDistanceCheck : MonoBehaviour
{
    public GameObject playerTarget { get; set; }
    public GameObject dummyTarget { get; set; }
    public EnemyBase enemy;

    void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        dummyTarget = GameObject.FindGameObjectWithTag("Dummy");
    }

    void Start()
    {
        TryMarkInRangeIfOverlapping(playerTarget);
        TryMarkInRangeIfOverlapping(dummyTarget);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.currentTarget = other.gameObject;
            enemy.isWithinRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.isWithinRange = false;
        }
    }

    void TryMarkInRangeIfOverlapping(GameObject target)
    {
        if (target == null || enemy == null) return;

        Collider self = GetComponent<Collider>();
        Collider other = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (self == null || other == null) return;

        if (self.bounds.Intersects(other.bounds))
        {
            enemy.currentTarget = target;
            enemy.isWithinRange = true;
        }
    }
}
