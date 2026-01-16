using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
    [Header("References")]
    public UrbanPlanner urbanPlanner;
    public WallBuilder wallBuilder;
    public RoomBuilder roomBuilder;
    public PathBuilder pathBuilder;
    public FloorBuilder floorBuilder;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject doorPrefab;
    public GameObject playerPrefab;

    [Header("Settings")]
    public bool autoBuildOnStart = true;
    public bool showGizmos = true;

    private GameObject dungeonParent;
    private GameObject playerInstance;

    void Start()
    {
        if (autoBuildOnStart)
        {
            GenerateDungeon();
        }
    }

    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        Debug.Log("=== GENERATE DUNGEON CALLED ===");

        // CRITICAL: Check if references exist
        if (urbanPlanner == null)
        {
            Debug.LogError("UrbanPlanner reference is NULL! Assign it in Inspector!");
            return;
        }

        if (roomBuilder == null)
        {
            Debug.LogError("RoomBuilder reference is NULL! Assign it in Inspector!");
            return;
        }

        if (pathBuilder == null)
        {
            Debug.LogError("PathBuilder reference is NULL! Assign it in Inspector!");
            return;
        }

        Debug.Log("All references valid, proceeding...");

        // Clear previous dungeon
        ClearDungeon();

        // Generate data
        Debug.Log("=== GENERATING DUNGEON DATA ===");
        urbanPlanner.GenerateFieldData();

        Debug.Log("=== Data generation complete, starting build ===");

        // Build physical dungeon
        Debug.Log("=== BUILDING DUNGEON GEOMETRY ===");
        BuildDungeon();

        Debug.Log("=== Build complete, spawning player ===");

        // Spawn player
        Debug.Log("=== SPAWNING PLAYER ===");
        SpawnPlayer();

        Debug.Log("=== DUNGEON GENERATION COMPLETE ===");
    }

    void BuildDungeon()
    {
        dungeonParent = new GameObject("Dungeon");

        int[,,] data = urbanPlanner.GreenFieldData;
        int width = urbanPlanner.width;
        int height = urbanPlanner.height;
        int depth = urbanPlanner.depth;

        // TEST 1: ONLY FLOORS
        Debug.Log("Building floors only...");
        floorBuilder.BuildFloors(data, width, height, depth, dungeonParent.transform,
                                 floorPrefab);
        wallBuilder.BuildWalls(data, width, height, depth, dungeonParent.transform,
                               wallPrefab);

        roomBuilder.BuildRooms(data, width, height, depth, dungeonParent.transform,
                              wallPrefab, floorPrefab, doorPrefab);

        pathBuilder.BuildPaths(data, width, height, depth, dungeonParent.transform,
                              floorPrefab);
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("No player prefab assigned!");
            return;
        }

        Vector3Int spawnPos = urbanPlanner.GetPlayerSpawnPosition();

        // Spawn at floor level - middle of the room
        Vector3 worldPos = new Vector3(spawnPos.x, 1, spawnPos.z);

        playerInstance = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        playerInstance.name = "Player";

        Debug.Log($"Player spawned at: {worldPos}");
    }

    [ContextMenu("Clear Dungeon")]
    public void ClearDungeon()
    {
        // Destroy existing dungeon
        if (dungeonParent != null)
        {
            DestroyImmediate(dungeonParent);
        }

        // Destroy player
        if (playerInstance != null)
        {
            DestroyImmediate(playerInstance);
        }
    }

}

