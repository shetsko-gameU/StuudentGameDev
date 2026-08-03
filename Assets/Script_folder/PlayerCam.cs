using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerCam : MonoBehaviour
{
    public GameObject CamTarget;

    public float CameraMoveSpeed;

    public float CameraDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CamTarget.transform.position = transform.position;

        CamTarget.transform.Rotate(Vector3.up * CameraMoveSpeed * CameraDirection * Time.deltaTime);


    }
    public void OnCamMove(InputAction.CallbackContext context)
    {
       CameraDirection = context.ReadValue<float>();
    }
}
