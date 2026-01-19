using UnityEngine;

/// <summary>
/// Master coordinator for all level construction
/// Manages all builders and provides centralized control over level building/clearing
/// </summary>
public class ConstructionManager : MonoBehaviour
{
    [Header("Data Source")]
    public UrbanPlanner urbanPlanner; // Reference to the level generator

    [Header("Builder References")]
    public FloorBuilder floorBuilder;
    public SolidWallBuilder solidWallBuilder;
    public RoomWallBuilder roomWallBuilder;
    public HallwayWallBuilder hallwayWallBuilder;
    public InteriorBuilder interiorBuilder;
    public DoorBuilder doorBuilder;
    public CorridorBuilder corridorBuilder;


    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public bool spawnPlayerOnStart = true;
    private GameObject spawnedPlayer;

    [Header("Build Options")]
    public bool buildFloorsOnStart = true;
    public bool buildSolidWallsOnStart = true;
    public bool buildRoomWallsOnStart = true;
    public bool buildHallwayWallsOnStart = true;
    public bool buildInteriorsOnStart = true;
    public bool buildDoorsOnStart = true;
    public bool buildCorridorsOnStart = true;

    [Header("Debug Options")]
    public bool showBuildLogs = true;

    void Start()
    {
        if (urbanPlanner != null)
        {
            // Step 0: Initialize the array first!
            urbanPlanner.InitializeFieldArray();

            // Step 1: Generate the level data
            urbanPlanner.GenerateFieldData();

            // Step 2: Build the world
            BuildLevel();

            // Step 3: Spawn player
            if (spawnPlayerOnStart && playerPrefab != null)
            {
                SpawnPlayer();
            }
        }
        else
        {
            Debug.LogWarning("ConstructionManager: No UrbanPlanner assigned.");
        }
    }

    /// <summary>
    /// Build the entire level from the GreenFieldData
    /// Calls all builders in the correct order
    /// </summary>
    public void BuildLevel()
    {
        if (urbanPlanner == null || urbanPlanner.GreenFieldData == null)
        {
            Debug.LogError("ConstructionManager: No valid GreenFieldData to build from!");
            return;
        }

        if (showBuildLogs) Debug.Log("=== CONSTRUCTION MANAGER: Starting construction ===");

        // Get field dimensions
        int width = urbanPlanner.width;
        int height = urbanPlanner.height;
        int depth = urbanPlanner.depth;
        int[,,] fieldData = urbanPlanner.GreenFieldData;

        // Build in order: Floors ? Walls ? Interiors ? Doors ? Corridors

        // Build floors
        if (buildFloorsOnStart && floorBuilder != null)
        {
            floorBuilder.BuildFloors(fieldData, width, height, depth);
        }

        // Build solid walls
        if (buildSolidWallsOnStart && solidWallBuilder != null)
        {
            solidWallBuilder.BuildWalls(fieldData, width, height, depth);
        }

        // Build room walls
        if (buildRoomWallsOnStart && roomWallBuilder != null)
        {
            roomWallBuilder.BuildWalls(fieldData, width, height, depth);
        }

        // Build hallway walls
        if (buildHallwayWallsOnStart && hallwayWallBuilder != null)
        {
            hallwayWallBuilder.BuildWalls(fieldData, width, height, depth);
        }


        // 3. Build interiors (values 2, 6)
        if (buildInteriorsOnStart && interiorBuilder != null)
        {
            interiorBuilder.BuildInteriors(fieldData, width, height, depth);
        }
        else if (buildInteriorsOnStart)
        {
            Debug.LogWarning("ConstructionManager: InteriorBuilder not assigned!");
        }

        // 4. Build doors (value 5)
        if (buildDoorsOnStart && doorBuilder != null)
        {
            doorBuilder.BuildDoors(fieldData, width, height, depth);
        }
        else if (buildDoorsOnStart)
        {
            Debug.LogWarning("ConstructionManager: DoorBuilder not assigned!");
        }

        // 5. Build corridors (value 8)
        if (buildCorridorsOnStart && corridorBuilder != null)
        {
            corridorBuilder.BuildCorridors(fieldData, width, height, depth);
        }
        else if (buildCorridorsOnStart)
        {
            Debug.LogWarning("ConstructionManager: CorridorBuilder not assigned!");
        }

        if (showBuildLogs) Debug.Log("=== CONSTRUCTION MANAGER: Construction complete ===");
    }

    /// <summary>
    /// Clear and rebuild the entire level
    /// Useful for regeneration or testing
    /// </summary>
    public void RebuildLevel()
    {
        ClearLevel();
        
        // Regenerate the data
        if (urbanPlanner != null)
        {
            urbanPlanner.GenerateFieldData();
        }
        
        BuildLevel();
    }

    /// <summary>
    /// Clear all constructed elements
    /// </summary>
    public void ClearLevel()
    {
        if (showBuildLogs) Debug.Log("=== CONSTRUCTION MANAGER: Clearing level ===");

        if (floorBuilder != null)
            floorBuilder.ClearFloors();

        if (solidWallBuilder != null)
            solidWallBuilder.ClearWalls();

        if (interiorBuilder != null)
            interiorBuilder.ClearInteriors();

        if (doorBuilder != null)
            doorBuilder.ClearDoors();

        if (corridorBuilder != null)
            corridorBuilder.ClearCorridors();
    }

    /// <summary>
    /// Build only specific components (useful for debugging/optimization)
    /// </summary>
    public void BuildFloorsOnly()
    {
        if (floorBuilder != null && urbanPlanner != null)
        {
            floorBuilder.BuildFloors(urbanPlanner.GreenFieldData, urbanPlanner.width, urbanPlanner.height, urbanPlanner.depth);
        }
    }

    public void BuildWallsOnly()
    {
        if (solidWallBuilder != null && urbanPlanner != null)
        {
            solidWallBuilder.BuildWalls(urbanPlanner.GreenFieldData, urbanPlanner.width, urbanPlanner.height, urbanPlanner.depth);
        }
    }

    public void BuildInteriorsOnly()
    {
        if (interiorBuilder != null && urbanPlanner != null)
        {
            interiorBuilder.BuildInteriors(urbanPlanner.GreenFieldData, urbanPlanner.width, urbanPlanner.height, urbanPlanner.depth);
        }
    }

    public void BuildDoorsOnly()
    {
        if (doorBuilder != null && urbanPlanner != null)
        {
            doorBuilder.BuildDoors(urbanPlanner.GreenFieldData, urbanPlanner.width, urbanPlanner.height, urbanPlanner.depth);
        }
    }

    public void BuildCorridorsOnly()
    {
        if (corridorBuilder != null && urbanPlanner != null)
        {
            corridorBuilder.BuildCorridors(urbanPlanner.GreenFieldData, urbanPlanner.width, urbanPlanner.height, urbanPlanner.depth);
        }
    }

    /// <summary>
    /// Get total block count (useful for optimization checks)
    /// </summary>
    public void PrintBuildStatistics()
    {
        if (urbanPlanner == null || urbanPlanner.GreenFieldData == null) return;

        int[] counts = new int[9]; // Counts for values 0-8

        for (int x = 0; x < urbanPlanner.width; x++)
        {
            for (int y = 0; y < urbanPlanner.height; y++)
            {
                for (int z = 0; z < urbanPlanner.depth; z++)
                {
                    int value = urbanPlanner.GreenFieldData[x, y, z];
                    if (value >= 0 && value <= 8)
                    {
                        counts[value]++;
                    }
                }
            }
        }

        Debug.Log("=== BUILD STATISTICS ===");
        Debug.Log($"Floors (0): {counts[0]}");
        Debug.Log($"Solid Walls (1): {counts[1]}");
        Debug.Log($"Room Interiors (2): {counts[2]}");
        Debug.Log($"Room Walls (3): {counts[3]}");
        Debug.Log($"Doors (5): {counts[5]}");
        Debug.Log($"Hallway Interiors (6): {counts[6]}");
        Debug.Log($"Hallway Walls (7): {counts[7]}");
        Debug.Log($"Corridors (8): {counts[8]}");
        Debug.Log($"Total Blocks: {counts[0] + counts[1] + counts[2] + counts[3] + counts[5] + counts[6] + counts[7] + counts[8]}");
    }

    //spawn player at designated spawn point
    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("ConstructionManager: No player prefab assigned!");
            return;
        }

        if (spawnedPlayer != null)
            Destroy(spawnedPlayer);

        spawnedPlayer = Instantiate(playerPrefab, urbanPlanner.playerSpawnPosition, Quaternion.identity);

        Debug.Log($"Player spawned at: {urbanPlanner.playerSpawnPosition}");
    }
}