using UnityEngine;

/// <summary>
/// Spawns the Projectile prefab on a fixed timer (timerMax interval) - a standalone
/// repeating emitter, separate from the wizard's on-demand AttackHitbox/WizardProjectiles
/// firing path. Useful for a stationary hazard (e.g. a turret or trap) rather than a
/// player-triggered attack. Instantiates whatever prefab is assigned with no component
/// requirement - pair it with WizardProjectiles (or any script with its own movement/
/// damage logic) for the spawned object to actually do something.
///
/// Setup:
///   1. Add to a stationary emitter GameObject, oriented the direction it should fire.
///   2. Assign Projectile (the prefab to spawn) and timerMax.
/// </summary>
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
