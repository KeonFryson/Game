using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Manages door spaces (value 5)
/// Currently leaves doors as empty walkable openings
/// Can be extended later for door frames, animations, or logic
/// </summary>
public class DoorBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject doorPrefab; // For future use - door frames, etc.

    [Header("Settings")]
    public Transform doorParent;

    [Header("Optimization")]
    public bool makeStatic = false; // Doors usually animate, so default to false

    public void BuildDoors(int[,,] fieldData, int width, int height, int depth)
    {
        int doorCount = 0;

        // Count doors but don't instantiate anything (yet)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (fieldData[x, y, z] == 5)
                    {
                        doorCount++;

                        // Future implementation:
                        // if (doorPrefab != null)
                        // {
                        //     Vector3 position = new Vector3(x, y, z);
                        //     GameObject door = Instantiate(doorPrefab, position, Quaternion.identity);
                        //     
                        //     if (doorParent != null)
                        //     {
                        //         door.transform.SetParent(doorParent);
                        //     }
                        //     
                        //     // Only make static if doors won't animate
                        //     if (makeStatic)
                        //     {
                        //         door.isStatic = true;
                        //     }
                        // }
                    }
                }
            }
        }

        Debug.Log($"DoorBuilder: {doorCount} door spaces left empty (walkable)");
    }

    public void ClearDoors()
    {
        if (doorParent != null)
        {
            foreach (Transform child in doorParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("DoorBuilder: Cleared all doors");
        }
        else
        {
            Debug.Log("DoorBuilder: No doors to clear (spaces are empty)");
        }
    }
}