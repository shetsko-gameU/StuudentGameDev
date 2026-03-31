using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform rotatingBone;
    public Transform targetPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void LateUpdate()
    {
        Vector3 direction = targetPoint.position - rotatingBone.position;
        rotatingBone.forward = direction;
    }
}
