using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    public void BuildRooms(int[,,] data, int width, int height, int depth,
                      Transform parent, GameObject wallPrefab,
                      GameObject floorPrefab, GameObject doorPrefab)
    {
        Debug.Log("=== ROOM BUILDER STARTED ===");
      
        GameObject roomsParent = new GameObject("Rooms");
        roomsParent.transform.parent = parent;

        int wallCount = 0;
        int doorCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = new Vector3(x, y, z);

                    switch (data[x, y, z])
                    {
                        case 3: // Room walls
                            if (wallPrefab != null)
                            {
                                GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, roomsParent.transform);
                                wall.name = $"RoomWall_{x}_{y}_{z}";
                                wallCount++;
                            }
                            break;

                        case 5: // Doors
                            if (doorPrefab != null)
                            {
                                GameObject door = Instantiate(doorPrefab, position, Quaternion.identity, roomsParent.transform);
                                door.name = $"Door_{x}_{y}_{z}";
                                doorCount++;
                            }
                            break;
                    }
                }
            }
        }

        Debug.Log($"Built {wallCount} walls and {doorCount} doors under {roomsParent.name}");
    }
}