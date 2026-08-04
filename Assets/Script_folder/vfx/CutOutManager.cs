using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class CutOutManager : MonoBehaviour
{
    [SerializeField]
    private Transform targetObject;

    [SerializeField]
    private LayerMask obstructitons;

    private Camera mainCamera;
    private RaycastHit[] raycastHits;
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        raycastHits = new RaycastHit[0];
    }

   

    // Update is called once per frame
    void Update()
    {
        if (targetObject == null) return;

        for (int i = 0; i < raycastHits.Length; i++)
        {
            Material[] materials = raycastHits[i].transform.GetComponent<Renderer>().materials;

            for (int m = 0; m < materials.Length; m++)
            {
                materials[m].SetVector("cutOutPos", Vector4.zero);
                materials[m].SetFloat("size", 0f);
                materials[m].SetFloat("fallOffSize", 0f);
            }
            

        }
        Vector2 cutoutPos = mainCamera.WorldToViewportPoint(targetObject.position);
        cutoutPos.y /= (Screen.width / Screen.height);

        Vector3 offset = targetObject.position - transform.position;
        //raycastHits = Physics.RaycastAll(transform.position, offset, offset.magnitude, obstructitons);
        raycastHits = Physics.SphereCastAll(transform.position,1.5f, offset, offset.magnitude - 1.5f, obstructitons);
        for (int i = 0; i < raycastHits.Length; i++)
        {
            Material[] materials = raycastHits[i].transform.GetComponent<Renderer>().materials;

            for (int m = 0; m < materials.Length; m++)
            {
                materials[m].SetVector("cutOutPos", cutoutPos);
                materials[m].SetFloat("size", 0.15f);
                materials[m].SetFloat("fallOffSize", 0.05f);
            }
            Debug.Log(raycastHits[i].transform.gameObject.name);
            
        }
    }
    private void OnDrawGizmos()
    {
        if (targetObject == null) return;

        Vector3 offset = targetObject.position - transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, offset);
    }
}
