using UnityEngine;

public class PathBuilder : MonoBehaviour
{
    public void BuildPaths(int[,,] data, int width, int height, int depth,
                          Transform parent, GameObject floorPrefab)
    {
        Debug.Log("=== PATH BUILDER STARTED ===");

        // Value 2 = walkable air space (rooms + corridors)
        // Value 5 = doors (also walkable)
        // Delete any GameObjects that are in these positions

        int pathsCleared = 0;
        int doorsCleared = 0;

        // Get all instantiated objects in the dungeon
        Transform dungeonTransform = parent;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // If this position should be walkable (path or door)
                    if (data[x, y, z] == 2 || data[x, y, z] == 5)
                    {
                        Vector3 position = new Vector3(x, y, z);

                        // Find and destroy any objects at this position
                        Collider[] colliders = Physics.OverlapSphere(position, 0.3f);

                        foreach (Collider col in colliders)
                        {
                            // Don't delete the player!
                            if (col.gameObject.name == "Player") continue;

                            // Delete obstructing object
                            if (data[x, y, z] == 2)
                            {
                                pathsCleared++;
                            }
                            else if (data[x, y, z] == 5)
                            {
                                // this for testing to walk through door needs its own way to  open  or be cleared 
                                doorsCleared++;
                            }

                            Destroy(col.gameObject);
                        }
                    }
                }
            }
        }

        Debug.Log($"Cleared {pathsCleared} obstructions in paths");
        Debug.Log($"Cleared {doorsCleared} obstructions in doors");
    }
}