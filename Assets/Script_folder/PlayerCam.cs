using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orbit camera rig: keeps CamTarget following this object's position and slowly rotates
/// it around the Y axis based on input, independent of player-model facing.
///
/// Setup:
///   1. Add to the Player (or a dedicated camera-rig object).
///   2. Assign CamTarget to the actual Camera's parent/pivot object.
///   3. Wire OnCamMove to an Input System action (see Player.prefab's CamMove binding).
///   4. Tune CameraMoveSpeed to taste.
/// </summary>
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
