using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject Projectile;
    public float timer , timerMax;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
        {
            Instantiate(Projectile,transform.position,Projectile.transform.rotation);
            timer = timerMax;
        }
        


    }
}
