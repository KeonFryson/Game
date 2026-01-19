using UnityEngine;

/// <summary>
/// Builds solid wall blocks (value 1) - uncarved/unused walls
/// </summary>
public class SolidWallBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject solidWallPrefab;

    [Header("Settings")]
    public Transform wallParent;

    [Header("Optimization")]
    public bool makeStatic = true; 

    public void BuildWalls(int[,,] fieldData, int width, int height, int depth)
    {
        if (solidWallPrefab == null)
        {
            Debug.LogError("SolidWallBuilder: No prefab assigned!");
            return;
        }

        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (fieldData[x, y, z] == 1) // Solid wall
                    {
                        Vector3 position = new Vector3(x, y, z);
                        GameObject wall = Instantiate(solidWallPrefab, position, Quaternion.identity);

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
        Debug.Log($"SolidWallBuilder: Built {count} solid walls");
    }

    public void ClearWalls()
    {
        if (wallParent != null)
        {
            foreach (Transform child in wallParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("SolidWallBuilder: Cleared all walls");
        }
    }
}