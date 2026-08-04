using UnityEngine;

public class CutOutManager : MonoBehaviour
{
    [SerializeField]
    private Transform targetObject;

    [SerializeField]
    private LayerMask obstructitons;

    private Camera mainCamera;
    private RaycastHit[] raycastHits;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        raycastHits = new RaycastHit[0];
        propertyBlock = new MaterialPropertyBlock();
    }

    // Update is called once per frame
    void Update()
    {
        if (targetObject == null) return;

        // Clear last frame's cutout before this frame's raycast replaces the hit list —
        // anything no longer hit just stays cleared and isn't touched again.
        for (int i = 0; i < raycastHits.Length; i++)
        {
            ApplyCutout(raycastHits[i].transform, Vector4.zero, 0f, 0f);
        }

        Vector2 cutoutPos = mainCamera.WorldToViewportPoint(targetObject.position);
        cutoutPos.y /= ((float)Screen.width / Screen.height);

        Vector3 offset = targetObject.position - transform.position;
        raycastHits = Physics.SphereCastAll(transform.position, 1.5f, offset, offset.magnitude - 1.5f, obstructitons);

        for (int i = 0; i < raycastHits.Length; i++)
        {
            ApplyCutout(raycastHits[i].transform, cutoutPos, 0.15f, 0.05f);
        }
    }

    // Writes the cutout properties via a MaterialPropertyBlock instead of Renderer.materials —
    // .materials silently instances every material it's touched on and never releases them,
    // leaking a growing pool of orphaned GPU-resident materials for as long as this runs.
    // A property block changes the same shader properties per-renderer with no instancing
    // and no risk of one object's cutout bleeding onto every other object sharing its material.
    private void ApplyCutout(Transform hitTransform, Vector4 cutOutPos, float size, float fallOffSize)
    {
        if (hitTransform == null) return;

        Renderer targetRenderer = hitTransform.GetComponent<Renderer>();
        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector("cutOutPos", cutOutPos);
        propertyBlock.SetFloat("size", size);
        propertyBlock.SetFloat("fallOffSize", fallOffSize);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmos()
    {
        if (targetObject == null) return;

        Vector3 offset = targetObject.position - transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, offset);
    }
}
