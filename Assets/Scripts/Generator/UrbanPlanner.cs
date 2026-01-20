using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UrbanPlanner : GreenField
{
    // ===================================================================
    // CONFIGURATION
    // ===================================================================
    [Header("Player Spawn")]
    public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("Generation Settings")]
    public int maxStructures = 10;  // Total rooms + hallways to generate
    public float hallwayChance = 0.3f;  // 30% chance to spawn hallway instead of room

    [Header("Room Settings")]
    public Vector2Int roomWidthRange = new Vector2Int(5, 9);
    public Vector2Int roomHeightRange = new Vector2Int(4, 6);
    public Vector2Int roomDepthRange = new Vector2Int(5, 9);

    [Header("Hall Settings")]
    public Vector2Int hallWidthRange = new Vector2Int(5, 9);
    public Vector2Int hallHeightRange = new Vector2Int(4, 6);
    public Vector2Int hallDepthRange = new Vector2Int(5, 9);

    [Header("Door Settings")]
    public int doorWidth = 2;
    public int doorHeight = 3;
    public int minDoorsPerRoom = 1;
    public int maxDoorsPerRoom = 4;

    [Header("Connection Settings")]
    public int connectionBuffer = 2;  // Space between structures

    // ===================================================================
    // DATA STRUCTURES
    // ===================================================================

    private List<RoomBounds> allRooms = new List<RoomBounds>();
    private Queue<DoorNode> availableDoors = new Queue<DoorNode>();
    private List<DoorNode> allDoors = new List<DoorNode>();
    private int structureCounter = 0;

    private struct RoomBounds
    {
        public int minX, maxX;
        public int minY, maxY;
        public int minZ, maxZ;
        public int structureIndex;

        public RoomBounds(int x, int y, int z, int width, int height, int depth, int index)
        {
            minX = x;
            maxX = x + width;
            minY = y;
            maxY = y + height;
            minZ = z;
            maxZ = z + depth;
            structureIndex = index;
        }

        public bool Overlaps(RoomBounds other, int buffer = 0)
        {
            bool xOverlap = minX - buffer < other.maxX + buffer && maxX + buffer > other.minX - buffer;
            bool yOverlap = minY - buffer < other.maxY + buffer && maxY + buffer > other.minY - buffer;
            bool zOverlap = minZ - buffer < other.maxZ + buffer && maxZ + buffer > other.minZ - buffer;

            return xOverlap && yOverlap && zOverlap;
        }
    }

    private struct DoorNode
    {
        public Vector3Int position;      // Position at the door opening
        public int direction;            // 0=North(+Z), 1=South(-Z), 2=East(+X), 3=West(-X)
        public int parentStructureIndex; // Which structure this door belongs to
        public bool isUsed;              // Has this door spawned a child room?
    }

    // ===================================================================
    // MAIN GENERATION PIPELINE
    // ===================================================================

    public override void GenerateFieldData()
    {
        Debug.Log("=== URBAN PLANNER: Graph-based generation starting ===");

        // Clear previous data
        allRooms.Clear();
        availableDoors.Clear();
        allDoors.Clear();
        structureCounter = 0;

        // Step 1: Fill world with walls
        SetWall();

        // Step 2: Add floor layer
        SetFloor();

        // Step 3: Generate connected structures
        GenerateConnectedStructures();

        // Step 4: Find player spawn
        FindPlayerSpawnPosition();

        Debug.Log($"Generation complete: {allRooms.Count} structures placed");
    }

    // ===================================================================
    // WORLD INITIALIZATION
    // ===================================================================

    void SetWall()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                for (int z = 0; z < depth; z++)
                    GreenFieldData[x, y, z] = 1; // Solid wall

        Debug.Log("Filled world with walls");
    }

    void SetFloor()
    {
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                GreenFieldData[x, 0, z] = 0; // Floor

        Debug.Log("Added floor layer");
    }

    // ===================================================================
    // GRAPH-BASED GENERATION
    // ===================================================================

    void GenerateConnectedStructures()
    {
        // STEP 1: Place seed structure at world center
        int seedX = width / 2;
        int seedZ = depth / 2;

        PlaceSeedStructure(seedX, 1, seedZ);

        // STEP 2: Process door queue (breadth-first generation)
        while (availableDoors.Count > 0 && structureCounter < maxStructures)
        {
            DoorNode parentDoor = availableDoors.Dequeue();

            // Try to spawn a new structure from this door
            TrySpawnStructureFromDoor(parentDoor);
        }

        Debug.Log($"Queue processed. Final structure count: {structureCounter}");
    }
    // Im going to break this up not a fan of  this doing too much in one function  so heres the seed structure placement
    // well do a store thr  infor in a object or struct later

    void PlaceSeedStructure(int x, int y, int z)
    {
        // Decide if seed is room or hallway
        bool isHallway = Random.value < hallwayChance;

        int w = Random.Range(roomWidthRange.x, roomWidthRange.y);
        int h = Random.Range(roomHeightRange.x, roomHeightRange.y);
        int d = Random.Range(roomDepthRange.x, roomDepthRange.y);

        // Center the seed structure
        x -= w / 2;
        z -= d / 2;

        // Ensure in bounds
        x = Mathf.Clamp(x, 1, width - w - 1);
        z = Mathf.Clamp(z, 1, depth - d - 1);

        RoomBounds bounds = new RoomBounds(x, y, z, w, h, d, structureCounter);
        allRooms.Add(bounds);

        if (isHallway)
        {
            Carve(x, y, z, w, h, d, 7, 6); // Hallway
            Debug.Log($"Seed Hallway 0: Size({w}x{h}x{d}) at ({x},{y},{z})");
        }
        else
        {
            Carve(x, y, z, w, h, d, 3, 2); // Room
            Debug.Log($"Seed Room 0: Size({w}x{h}x{d}) at ({x},{y},{z})");
        }

        // Add seed doors to queue
        AddDoorsToStructure(x, y, z, w, h, d, structureCounter);
        structureCounter++;
    }

    void TrySpawnStructureFromDoor(DoorNode parentDoor)
    {
        // Decide structure type
        bool isHallway = Random.value < hallwayChance;

        // Generate random dimensions
        int w, h, d;
        if (isHallway)
        {
            w = Random.Range(hallWidthRange.x, hallWidthRange.y);
            h = Random.Range(hallHeightRange.x, hallHeightRange.y);
            d = Random.Range(hallDepthRange.x, hallDepthRange.y);
        }
        else
        {
            w = Random.Range(roomWidthRange.x, roomWidthRange.y);
            h = Random.Range(roomHeightRange.x, roomHeightRange.y);
            d = Random.Range(roomDepthRange.x, roomDepthRange.y);
        }

        // Calculate new structure position based on parent door
        Vector3Int newPos = CalculateStructurePosition(parentDoor, w, d);

        // Check if position is valid
        if (!IsPositionValid(newPos, w, h, d))
        {
            Debug.Log($"Failed to place structure from door at {parentDoor.position} (out of bounds or overlap)");
            return;
        }

        // Place the structure
        RoomBounds bounds = new RoomBounds(newPos.x, newPos.y, newPos.z, w, h, d, structureCounter);
        allRooms.Add(bounds);

        if (isHallway)
        {
            Carve(newPos.x, newPos.y, newPos.z, w, h, d, 7, 6);
            Debug.Log($"Hallway {structureCounter}: Size({w}x{h}x{d}) at ({newPos.x},{newPos.y},{newPos.z})");
        }
        else
        {
            Carve(newPos.x, newPos.y, newPos.z, w, h, d, 3, 2);
            Debug.Log($"Room {structureCounter}: Size({w}x{h}x{d}) at ({newPos.x},{newPos.y},{newPos.z})");
        }

        // Create connection between parent door and new structure's matching door
        ConnectDoorToStructure(parentDoor, newPos, w, d);

        // Add new structure's OTHER doors to queue (not the one we just used)
        AddDoorsToStructure(newPos.x, newPos.y, newPos.z, w, h, d, structureCounter, parentDoor.direction);

        structureCounter++;
    }

    // ===================================================================
    // POSITION CALCULATION
    // ===================================================================

    Vector3Int CalculateStructurePosition(DoorNode door, int structureWidth, int structureDepth)
    {
        int x = door.position.x;
        int y = door.position.y;
        int z = door.position.z;

        // Need extra space: +1 for the wall itself, +buffer for gap, +1 for child's wall
        int spacing = 2 + connectionBuffer;

        switch (door.direction)
        {
            case 0: // North (+Z) - new structure extends further north
                x -= structureWidth / 2;
                z += spacing; // Move beyond parent's wall + buffer
                break;

            case 1: // South (-Z) - new structure extends further south
                x -= structureWidth / 2;
                z -= (structureDepth + spacing); // Move beyond parent's wall + buffer
                break;

            case 2: // East (+X) - new structure extends further east
                x += spacing; // Move beyond parent's wall + buffer
                z -= structureDepth / 2;
                break;

            case 3: // West (-X) - new structure extends further west
                x -= (structureWidth + spacing); // Move beyond parent's wall + buffer
                z -= structureDepth / 2;
                break;
        }

        return new Vector3Int(x, y, z);
    }

    bool IsPositionValid(Vector3Int pos, int w, int h, int d)
    {
        // Check world bounds
        if (pos.x < 1 || pos.x + w >= width - 1) return false;
        if (pos.z < 1 || pos.z + d >= depth - 1) return false;
        if (pos.y < 1 || pos.y + h >= height - 1) return false;

        // Check overlaps with existing structures
        RoomBounds newBounds = new RoomBounds(pos.x, pos.y, pos.z, w, h, d, -1);
        foreach (RoomBounds existing in allRooms)
        {
            if (newBounds.Overlaps(existing, connectionBuffer))
                return false;
        }

        return true;
    }

    // ===================================================================
    // DOOR MANAGEMENT
    // ===================================================================

    void AddDoorsToStructure(int x, int y, int z, int w, int h, int d, int structureIndex, int excludeDirection = -1)
    {
        List<int> doorDirections = new List<int>();

        // STEP 1: If this is a child structure, FORCE a door on the connecting side
        if (excludeDirection != -1)
        {
            int connectionDirection = GetOppositeDirection(excludeDirection);
            doorDirections.Add(connectionDirection); // This door connects to parent
            Debug.Log($"Structure {structureIndex}: Forced connection door in direction {connectionDirection} (parent was {excludeDirection})");
        }

        // STEP 2: Add additional random doors
        List<int> availableDirections = new List<int> { 0, 1, 2, 3 };

        // Remove the direction we already added (if any)
        if (excludeDirection != -1)
        {
            int connectionDirection = GetOppositeDirection(excludeDirection);
            availableDirections.Remove(connectionDirection);
        }

        // Determine how many ADDITIONAL doors to add (0 to maxDoorsPerRoom-1)
        int additionalDoors = Random.Range(0, maxDoorsPerRoom);

        // Shuffle and pick random additional doors
        ShuffleList(availableDirections);
        int numAdditional = Mathf.Min(additionalDoors, availableDirections.Count);

        for (int i = 0; i < numAdditional; i++)
        {
            doorDirections.Add(availableDirections[i]);
        }

        // STEP 3: Carve all doors
        foreach (int direction in doorDirections)
        {
            CarveDoor(x, y, z, w, d, direction);

            Vector3Int doorPos = GetDoorPosition(x, y, z, w, d, direction);
            DoorNode door = new DoorNode
            {
                position = doorPos,
                direction = direction,
                parentStructureIndex = structureIndex,
                isUsed = (excludeDirection != -1 && direction == GetOppositeDirection(excludeDirection))
            };

            allDoors.Add(door);

            // Only add to queue if it's not the connection door we just used
            if (!(excludeDirection != -1 && direction == GetOppositeDirection(excludeDirection)))
            {
                availableDoors.Enqueue(door);
            }
        }

        Debug.Log($"Added {doorDirections.Count} doors to structure {structureIndex} (directions: {string.Join(",", doorDirections)})");
    }

    Vector3Int GetDoorPosition(int x, int y, int z, int w, int d, int direction)
    {
        switch (direction)
        {
            case 0: return new Vector3Int(x + w / 2, y, z + d - 1); // North
            case 1: return new Vector3Int(x + w / 2, y, z);          // South
            case 2: return new Vector3Int(x + w - 1, y, z + d / 2); // East
            case 3: return new Vector3Int(x, y, z + d / 2);          // West
            default: return Vector3Int.zero;
        }
    }

    void CarveDoor(int roomX, int roomY, int roomZ, int roomW, int roomD, int direction)
    {
        if (direction == 0 || direction == 1) // North/South (door spans X)
        {
            int doorZ = (direction == 0) ? roomZ + roomD - 1 : roomZ;
            int doorStartX = roomX + (roomW / 2) - (doorWidth / 2);

            for (int dx = 0; dx < doorWidth; dx++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    int px = doorStartX + dx;
                    int py = roomY + dy;

                    if (IsInBounds(new Vector3Int(px, py, doorZ)))
                        GreenFieldData[px, py, doorZ] = 5; // Door
                }
            }
        }
        else // East/West (door spans Z)
        {
            int doorX = (direction == 2) ? roomX + roomW - 1 : roomX;
            int doorStartZ = roomZ + (roomD / 2) - (doorWidth / 2);

            for (int dz = 0; dz < doorWidth; dz++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    int pz = doorStartZ + dz;
                    int py = roomY + dy;

                    if (IsInBounds(new Vector3Int(doorX, py, pz)))
                        GreenFieldData[doorX, py, pz] = 5; // Door
                }
            }
        }
    }

    // ===================================================================
    // DOOR CONNECTION (NO PATHFINDING!)
    // ===================================================================

    void ConnectDoorToStructure(DoorNode parentDoor, Vector3Int childPos, int childW, int childD)
    {
        // Find the matching door on the child structure
        int childDirection = GetOppositeDirection(parentDoor.direction);
        Vector3Int childDoor = GetDoorPosition(childPos.x, childPos.y, childPos.z, childW, childD, childDirection);

        // Carve direct connection
        CarveDirectConnection(parentDoor.position, childDoor);

        Debug.Log($"Connected door at {parentDoor.position} to {childDoor}");
    }

    void CarveDirectConnection(Vector3Int from, Vector3Int to)
    {
        // This should be a very short distance (just the connectionBuffer)
        int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.z - from.z));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)Mathf.Max(steps, 1);
            Vector3Int pos = Vector3Int.RoundToInt(Vector3.Lerp(from, to, t));

            // Carve simple corridor
            for (int dx = -doorWidth / 2; dx <= doorWidth / 2; dx++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    for (int dz = -doorWidth / 2; dz <= doorWidth / 2; dz++)
                    {
                        Vector3Int p = pos + new Vector3Int(dx, dy, dz);

                        if (IsInBounds(p) && GreenFieldData[p.x, p.y, p.z] == 1)
                            GreenFieldData[p.x, p.y, p.z] = 8; // Corridor
                    }
                }
            }
        }
    }

    // ===================================================================
    // STRUCTURE CARVING
    // ===================================================================

    void Carve(int x, int y, int z, int w, int h, int d, int wallValue, int interiorValue)
    {
        for (int ix = x; ix < x + w; ix++)
        {
            for (int iy = y; iy < y + h; iy++)
            {
                for (int iz = z; iz < z + d; iz++)
                {
                    if (IsInBounds(new Vector3Int(ix, iy, iz)))
                    {
                        bool isWall = (ix == x || ix == x + w - 1 ||
                                       iz == z || iz == z + d - 1 ||
                                       iy == y + h - 1);

                        GreenFieldData[ix, iy, iz] = isWall ? wallValue : interiorValue;
                    }
                }
            }
        }
    }

    // ===================================================================
    // UTILITY FUNCTIONS
    // ===================================================================

    void FindPlayerSpawnPosition()
    {
        if (allRooms.Count == 0)
        {
            playerSpawnPosition = new Vector3(width / 2, 2, depth / 2);
            return;
        }

        RoomBounds spawnRoom = allRooms[0]; // Spawn in seed room
        playerSpawnPosition = new Vector3(
            (spawnRoom.minX + spawnRoom.maxX) / 2f,
            spawnRoom.minY + 0.5f,
            (spawnRoom.minZ + spawnRoom.maxZ) / 2f
        );

        Debug.Log($"Player spawn: {playerSpawnPosition}");
    }

    bool IsInBounds(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height &&
               pos.z >= 0 && pos.z < depth;
    }

    int GetOppositeDirection(int direction)
    {
        // 0=North ↔ 1=South, 2=East ↔ 3=West
        return direction ^ 1;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ===================================================================
    // GIZMO VISUALIZATION
    // ===================================================================

    void OnDrawGizmos()
    {
        if (GreenFieldData == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);

                    switch (GreenFieldData[x, y, z])
                    {
                        case 2: // Room interior
                            //Gizmos.color = new Color(1, 0, 0, 0.3f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;
                        case 3: // Room walls
                          //  Gizmos.color = new Color(1, 1, 0, 0.5f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;
                        case 5: // Doors
                           // Gizmos.color = Color.magenta;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.7f);
                            break;
                        case 6: // Hallway interior
                           // Gizmos.color = Color.cyan;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;
                        case 7: // Hallway walls
                            //Gizmos.color = new Color(0, 0.5f, 1, 0.5f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;
                        case 8: // Corridors
                           // Gizmos.color = new Color(0, 1, 0, 0.6f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.5f);
                            break;
                    }
                }
            }
        }

        // Draw door nodes
        foreach (DoorNode door in allDoors)
        {
           // Gizmos.color = door.isUsed ? Color.red : Color.green;
           // Gizmos.DrawSphere(door.position, 0.3f);
        }
    }
}