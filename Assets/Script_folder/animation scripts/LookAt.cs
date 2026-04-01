using Unity.Mathematics;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform rotatingBone;
    public Transform targetPoint;
    public float maxYaw = 60f;
    public float maxPitch = 40f;
    public IntrestManager im;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        im = GetComponent<IntrestManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void LateUpdate()
    {
        
        targetPoint = im.lookPoint;
        Vector3 direction = targetPoint.position - rotatingBone.position;
        Vector3 local = rotatingBone.parent.InverseTransformDirection(direction.normalized);
        float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(local.y) * Mathf.Rad2Deg;
        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0);
        if (direction.magnitude< 0.1f)
        {
            return;
        }
        rotatingBone.localRotation = Quaternion.Slerp(rotatingBone.localRotation, targetRot, Time.deltaTime*8);
    }
}
