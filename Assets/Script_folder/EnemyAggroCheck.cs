using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    public GameObject playerTarget { get; set; }
    public GameObject dummyTarget { get; set; }
    public EnemyBase enemy;

    void Awake()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        dummyTarget = GameObject.FindGameObjectWithTag("Dummy");
        enemy = GetComponentInParent<EnemyBase>();
    }

    void Start()
    {
        // Enter-only triggers miss targets already overlapping at spawn / wave start.
        TryAggroIfOverlapping(playerTarget);
        TryAggroIfOverlapping(dummyTarget);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.currentTarget = other.gameObject;
            enemy.isAggroed = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerTarget || other.gameObject == dummyTarget)
        {
            enemy.isAggroed = false;
        }
    }

    void TryAggroIfOverlapping(GameObject target)
    {
        if (target == null || enemy == null) return;

        Collider self = GetComponent<Collider>();
        Collider other = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (self == null || other == null) return;

        if (self.bounds.Intersects(other.bounds))
        {
            enemy.currentTarget = target;
            enemy.isAggroed = true;
        }
    }
}
