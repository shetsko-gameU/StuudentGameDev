using UnityEngine;
using UnityEditor;

public class SnapToTerrainTool
{
    [MenuItem("Tools/Snap Selected to Terrain #&d")] // Hotkey: Shift + Ctrl + D
    public static void SnapToTerrain()
    {
        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms.Length == 0)
        {
            Debug.LogWarning("No objects selected to drop.");
            return;
        }

        Undo.RecordObjects(selectedTransforms, "Snap Objects to Terrain");

        foreach (Transform obj in selectedTransforms)
        {
            // Start raycast from 100 meters above the object to catch any terrain peaks
            Vector3 rayStart = obj.position + new Vector3(0, 100f, 0);
            Ray ray = new Ray(rayStart, Vector3.down);

            // Cast downwards
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                // Verify we hit a Terrain
                if (hit.collider is TerrainCollider)
                {
                    // Snap the object to the hit point
                    Vector3 newPosition = new Vector3(obj.position.x, hit.point.y, obj.position.z);
                    obj.position = newPosition;
                }
            }
        }
        
        Debug.Log($"Snapped {selectedTransforms.Length} objects to the terrain.");
    }
}