using UnityEngine;


public class ProjectileScript : MonoBehaviour
{
    public float Speed, Damage;
    public Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rb.MovePosition(transform.position + transform.forward * Speed);
    }
}
