using UnityEngine;

public class WallBuilder : MonoBehaviour
{
    public void BuildWalls(int[,,] data, int width, int height, int depth,
                          Transform parent, GameObject wallPrefab)
    {
        Debug.Log("=== WALL BUILDER STARTED ===");

        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.parent = parent;

        int wallCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (data[x, y, z] == 1) // Solid walls (fill material)
                    {
                        Vector3 position = new Vector3(x, y, z);

                        if (wallPrefab != null)
                        {
                            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, wallsParent.transform);
                            wall.name = $"Wall_{x}_{y}_{z}";
                            wallCount++;
                        }
                    }
                }
            }
        }

        Debug.Log($"Built {wallCount} solid walls under {wallsParent.name}");
    }
}
