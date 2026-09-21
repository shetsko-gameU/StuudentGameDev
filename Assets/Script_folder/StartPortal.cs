using UnityEngine;

/// <summary>
/// A "player pops out of a portal and drops to the ground" intro, driven by Animation
/// Events on the portal's open animation. The player GameObject starts inactive and is
/// revealed by DropPlayer(). Reuses PlayerMove's ledge-fall state machine
/// (PlayerMove.DropFromPortal()) rather than toggling rb.useGravity directly, which does
/// nothing on a kinematic body.
///
/// Setup:
///   1. Place on the portal GameObject; assign playerMove to the (initially inactive)
///      Player instance in the scene.
///   2. On the portal's open animation clip, add an Animation Event calling DropPlayer()
///      at the reveal frame, and another calling ReactivatePlayer() ~1s later.
/// </summary>
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
