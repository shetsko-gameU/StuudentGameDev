using UnityEngine;

public class DirkEvents : MonoBehaviour
{

    public ParticleSystem swing1;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwingAnim()
    {
        swing1.Emit(1);
    }
}
