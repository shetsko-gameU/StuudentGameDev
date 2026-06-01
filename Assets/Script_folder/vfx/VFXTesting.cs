using UnityEngine;

public class VFXTesting : MonoBehaviour
{
    public VFXTrigger trigger;
    public float cooldown = 2f;
    private float cd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cd <0f)
        {
            trigger.TriggerVFX(transform.position);
            cd = cooldown;
        }
        cd -= Time.deltaTime;
    }
}
