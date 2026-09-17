using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optional look-at interest tracker used by LookAt. Off by default — enable
/// <see cref="enableTracking"/> only when a scene actually needs it. Keeps Update quiet.
/// </summary>
public class IntrestManager : MonoBehaviour
{
    [Tooltip("When false (default), Update is a no-op so play scenes stay quiet.")]
    public bool enableTracking;

    public List<Transform> transforms;
    public Transform lookPoint;
    public float maxRange;
    public Transform origin;
    public Transform target;

    void Update()
    {
        if (!enableTracking) return;
        if (transforms == null || transforms.Count == 0 || origin == null)
            return;

        float best = float.MaxValue;
        Transform closest = transforms[0];
        for (int i = 0; i < transforms.Count; i++)
        {
            if (transforms[i] == null) continue;
            float d = (transforms[i].position - origin.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                closest = transforms[i];
            }
        }

        if (closest != null && best <= maxRange * maxRange)
            lookPoint = closest;
    }
}
