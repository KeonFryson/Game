using UnityEngine;

/// <summary>
/// Manages interior spaces (values 2, 6)
/// Currently leaves interiors as empty walkable space
/// Can be extended later for furniture, props, or special floor tiles
/// </summary>
public class InteriorBuilder : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject roomInteriorPrefab; // For future use - furniture, carpets, etc.
    public GameObject hallwayInteriorPrefab; // For future use - lights, decorations, etc.

    [Header("Settings")]
    public Transform interiorParent;

    [Header("Optimization")]
    public bool makeStatic = true; // Interior objects usually don't move

    public void BuildInteriors(int[,,] fieldData, int width, int height, int depth)
    {
        int roomInteriorCount = 0;
        int hallwayInteriorCount = 0;

        // Count interiors but don't instantiate anything (yet)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int value = fieldData[x, y, z];

                    if (value == 2) // Room interior
                    {
                        roomInteriorCount++;

                        // Future implementation:
                        // if (roomInteriorPrefab != null)
                        // {
                        //     Vector3 position = new Vector3(x, y, z);
                        //     GameObject interior = Instantiate(roomInteriorPrefab, position, Quaternion.identity);
                        //     
                        //     if (interiorParent != null)
                        //     {
                        //         interior.transform.SetParent(interiorParent);
                        //     }
                        //     
                        //     if (makeStatic)
                        //     {
                        //         interior.isStatic = true;
                        //     }
                        // }
                    }
                    else if (value == 6) // Hallway interior
                    {
                        hallwayInteriorCount++;

                        // Future implementation:
                        // if (hallwayInteriorPrefab != null)
                        // {
                        //     Vector3 position = new Vector3(x, y, z);
                        //     GameObject interior = Instantiate(hallwayInteriorPrefab, position, Quaternion.identity);
                        //     
                        //     if (interiorParent != null)
                        //     {
                        //         interior.transform.SetParent(interiorParent);
                        //     }
                        //     
                        //     if (makeStatic)
                        //     {
                        //         interior.isStatic = true;
                        //     }
                        // }
                    }
                }
            }
        }

        Debug.Log($"InteriorBuilder: {roomInteriorCount} room interiors and {hallwayInteriorCount} hallway interiors left empty (walkable)");
    }

    public void ClearInteriors()
    {
        if (interiorParent != null)
        {
            foreach (Transform child in interiorParent)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("InteriorBuilder: Cleared all interiors");
        }
        else
        {
            Debug.Log("InteriorBuilder: No interiors to clear (spaces are empty)");
        }
    }
}