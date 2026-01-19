using UnityEngine;

/// <summary>
/// Builds hallway wall blocks (value 7)
/// </summary>
public class HallwayWallBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject hallwayWallPrefab;

    [Header("Settings")]
    public Transform wallParent;

    [Header("Optimization")]
    public bool makeStatic = true; // Toggle for static batching

    public void BuildWalls(int[,,] fieldData, int width, int height, int depth)
    {
        if (hallwayWallPrefab == null)
        {
            Debug.LogError("HallwayWallBuilder: No prefab assigned!");
            return;
        }

        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (fieldData[x, y, z] == 7) // Hallway wall
                    {
                        Vector3 position = new Vector3(x, y, z);
                        GameObject wall = Instantiate(hallwayWallPrefab, position, Quaternion.identity);

                        if (wallParent != null)
                        {
                            wall.transform.SetParent(wallParent);
                        }

                        // Mark as static for optimization
                        if (makeStatic)
                        {
                            wall.isStatic = true;
                        }

                        count++;
                    }
                }
            }
        }
        Debug.Log($"HallwayWallBuilder: Built {count} hallway walls");
    }

    public void ClearWalls()
    {
        if (wallParent != null)
        {
            foreach (Transform child in wallParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("HallwayWallBuilder: Cleared all walls");
        }
    }
}