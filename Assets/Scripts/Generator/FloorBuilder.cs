using UnityEngine;


public class FloorBuilder : MonoBehaviour
{
    public void BuildFloors(int[,,] data, int width, int height, int depth,
                        Transform parent, GameObject floorPrefab)
    {
        Debug.Log("=== FLOOR BUILDER STARTED ===");

        GameObject floorsParent = new GameObject("Floors");
        floorsParent.transform.parent = parent;

        int floorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (data[x, y, z] == 0) // Floor tiles (inside rooms)
                    {
                        Vector3 position = new Vector3(x, y, z);  // Direct position

                        if (floorPrefab != null)
                        {
                            GameObject floor = Instantiate(floorPrefab, position, Quaternion.identity, floorsParent.transform);
                            floor.name = $"Floor_{x}_{y}_{z}";
                            floorCount++;
                        }
                    }
                }
            }
        }

        Debug.Log($"Built {floorCount} floor tiles under {floorsParent.name}");
    }
}