using UnityEngine;

public class TakeDamage : MonoBehaviour
{
    public StatsManager StatsManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        



    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "EnemyAttack")
        {
            StatsManager.TakeDamage(other.GetComponent<ProjectileScript>().Damage);

        }


    }


}
