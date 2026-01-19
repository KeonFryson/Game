using UnityEngine;



/// <summary>
/// Manages corridor spaces (value 8)
/// Currently leaves corridors as empty walkable space
/// Can be extended later for special corridor floor tiles or effects
/// </summary>
public class CorridorBuilder : MonoBehaviour
{
    [Header("Settings")]
    public Transform corridorParent;

    public void BuildCorridors(int[,,] fieldData, int width, int height, int depth)
    {
        int corridorCount = 0;

        // Count corridors but don't instantiate anything
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (fieldData[x, y, z] == 8)
                    {
                        corridorCount++;
                        // Future: Could place special floor tiles, particles, etc.
                    }
                }
            }
        }

        Debug.Log($"CorridorBuilder: {corridorCount} corridor spaces left empty (walkable)");
    }

    public void ClearCorridors()
    {
        // Nothing to clear since we don't spawn anything
        Debug.Log("CorridorBuilder: No corridors to clear (spaces are empty)");
    }
}