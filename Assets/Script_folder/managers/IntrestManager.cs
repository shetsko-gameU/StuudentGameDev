
using UnityEngine;
using System.Collections.Generic;
public class IntrestManager : MonoBehaviour
{

    public List<Transform> transforms;
    public Transform lookPoint;
    public float maxRange;
    public Transform origin;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float distance = float.MaxValue;
        Transform closest = transforms[0];
        for (int i = 0; i < transforms.Count; i++)
        {
            float d = (transforms[i].position - origin.position).sqrMagnitude;
            if (d<distance*distance)
            {
                distance = d;
                closest = transforms[i];
            }
        }
        lookPoint.position = closest.position;
    }
}
