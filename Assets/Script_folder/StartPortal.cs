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
        playerMove.gameObject.SetActive(true);
        playerMove.rb.useGravity = true;

    }
    public void ReactivatePlayer()
    {
        playerMove.enabled = true;
        playerMove.rb.useGravity = false;
        Destroy(gameObject);
    }



}
