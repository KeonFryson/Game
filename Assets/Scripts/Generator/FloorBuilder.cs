using UnityEngine;

/// <summary>
/// Builds floor blocks from the GreenFieldData array
/// Instantiates floor prefabs for all positions marked as floor (value 0)
/// </summary>
public class FloorBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject floorPrefab;

    [Header("Settings")]
    public Transform floorParent; // Optional: parent object to organize hierarchy

    [Header("Optimization")]
    public bool makeStatic = true; 

    /// <summary>
    /// Build all floors from the field data
    /// </summary>
    public void BuildFloors(int[,,] fieldData, int width, int height, int depth)
    {
        if (floorPrefab == null)
        {
            Debug.LogError("FloorBuilder: No floor prefab assigned!");
            return;
        }

        int floorCount = 0;

        // Iterate through entire 3D grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // Check if this position is a floor
                    if (fieldData[x, y, z] == 0)
                    {
                        Vector3 position = new Vector3(x, y, z);
                        GameObject floor = Instantiate(floorPrefab, position, Quaternion.identity);

                        // Organize in hierarchy if parent is set
                        if (floorParent != null)
                        {
                            floor.transform.SetParent(floorParent);
                        }

                        // Mark as static for optimization
                        if (makeStatic)
                        {
                            floor.isStatic = true;
                        }

                        floorCount++;
                    }
                }
            }
        }

        Debug.Log($"FloorBuilder: Built {floorCount} floor blocks");
    }

    /// <summary>
    /// Clear all existing floor blocks
    /// </summary>
    public void ClearFloors()
    {
        if (floorParent != null)
        {
            // Destroy all children
            foreach (Transform child in floorParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("FloorBuilder: Cleared all floors");
        }
    }
}