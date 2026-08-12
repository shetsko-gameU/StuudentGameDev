using UnityEngine;

public class StartPortal : MonoBehaviour
{
    public PlayerMove playerMove;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMove.gameObject.SetActive(false);
       // playerMove.enabled = false;   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void DropPlayer()
    {
        // Toggling rb.useGravity directly doesn't do anything — the Rigidbody is kinematic
        // while agent-driven (PlayerMove.Start() forces this), and kinematic bodies ignore
        // gravity regardless of that flag. DropFromPortal() routes through PlayerMove's own
        // fall/land state machine instead, the same one used for walking off a ledge.
        playerMove.gameObject.SetActive(true);
        playerMove.DropFromPortal();
    }
    public void ReactivatePlayer()
    {
        // PlayerMove.Land() already re-enables the agent and re-kinematics the Rigidbody the
        // moment the player actually touches down — nothing left to do here but clean up.
        Destroy(gameObject);
    }



}
