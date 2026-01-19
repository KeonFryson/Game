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

    [Header("Room Settings")]
    public int numberOfRooms = 5;
    public Vector2Int roomWidthRange = new Vector2Int(5, 9);
    public Vector2Int roomHeightRange = new Vector2Int(4, 6);
    public Vector2Int roomDepthRange = new Vector2Int(5, 9);

    [Header("Hall Settings")]
    public int numberOfHalls = 5;
    public Vector2Int HallWidthRange = new Vector2Int(5, 9);
    public Vector2Int HallHeightRange = new Vector2Int(4, 6);
    public Vector2Int HllDepthRange = new Vector2Int(5, 9);

    [Header("Door Settings")]
    public int doorWidth = 2;   // How wide the door is (X or Z)
    public int doorHeight = 3;  // How tall the door is (Y)

    [Header("Corridor Settings")]
    public int corridorWidth = 2;  // Should be at least doorWidth + 2
    public int corridorHeight = 3; // Should match doorHeight

    // ===================================================================
    // DATA STRUCTURES
    // ===================================================================

    // Tracks all placed structures (rooms and hallways)
    private List<RoomBounds> allRooms = new List<RoomBounds>();

    // Tracks all doors for connection logic
    private List<DoorInfo> allDoors = new List<DoorInfo>();

    // Represents the 3D bounds of a structure
    private struct RoomBounds
    {
        public int minX, maxX;
        public int minY, maxY;
        public int minZ, maxZ;

        public RoomBounds(int x, int y, int z, int width, int height, int depth)
        {
            minX = x;
            maxX = x + width;
            minY = y;
            maxY = y + height;
            minZ = z;
            maxZ = z + depth;
        }

        // Check if this structure overlaps with another
        public bool Overlaps(RoomBounds other)
        {
            bool xOverlap = minX < other.maxX && maxX > other.minX;
            bool yOverlap = minY < other.maxY && maxY > other.minY;
            bool zOverlap = minZ < other.maxZ && maxZ > other.minZ;

            return xOverlap && yOverlap && zOverlap;
        }
    }

    // Represents a door and its properties
    private struct DoorInfo
    {
        public Vector3Int position;      // World position of door center
        public int direction;            // 0=North, 1=South, 2=East, 3=West
        public int structureIndex;       // Which structure this door belongs to
    }

    // ===================================================================
    // UTILITY FUNCTIONS
    // ===================================================================

    // Check if a position is within the world bounds
    bool IsInBounds(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height &&
               pos.z >= 0 && pos.z < depth;
    }

    // ===================================================================
    // MAIN GENERATION PIPELINE
    // ===================================================================

    public override void GenerateFieldData()
    {
        Debug.Log("=== URBAN PLANNER: Starting generation ===");

        // Clear previous data
        allRooms.Clear();
        allDoors.Clear();

        // Step 1: Fill entire world with solid walls
        SetWall();

        // Step 2: Add floor layer at bottom
        SetFloor();

        // Step 3: Place all rooms randomly
        PlaceRooms();

        // Step 4: Place all hallways randomly
        PlaceHallWays();

        // Step 5: Connect all structures with corridors
        ConnectDoors();

        // Step 6: Determine player spawn position
        FindPlayerSpawnPosition();

        Debug.Log($"Generation complete: {allRooms.Count} structures placed");
    }

    // ===================================================================
    // WORLD INITIALIZATION
    // ===================================================================


    void FindPlayerSpawnPosition()
    {
        if (allRooms.Count == 0)
        {
            Debug.LogWarning("No rooms placed! Using world center as fallback.");
            playerSpawnPosition = new Vector3(width / 2, 2, depth / 2);
            return;
        }

        // Pick a random room
        RoomBounds randomRoom = allRooms[Random.Range(0, allRooms.Count)];

        // Calculate center of the room
        float centerX = (randomRoom.minX + randomRoom.maxX) / 2f;
        float centerY = randomRoom.minY + 0.5f; // Just above ground (y=1.5 instead of y=2)
        float centerZ = (randomRoom.minZ + randomRoom.maxZ) / 2f;

        playerSpawnPosition = new Vector3(centerX, centerY, centerZ);

        Debug.Log($"Player spawn set to center of random room at: {playerSpawnPosition}");
    }
    // Fill the entire 3D grid with solid walls
    void SetWall()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    GreenFieldData[x, y, z] = 1; // 1 = solid wall
                }
            }
        }

        Debug.Log("Filled world with walls");
    }

    // Create a floor layer at y=0
    void SetFloor()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                GreenFieldData[x, 0, z] = 0; // 0 = floor
            }
        }

        Debug.Log("Added floor layer");
    }

    // ===================================================================
    // ROOM PLACEMENT
    // ===================================================================

    // Attempt to place a single room
    void PlaceRoom()
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random dimensions
            int roomWidth = Random.Range(roomWidthRange.x, roomWidthRange.y);
            int roomHeight = Random.Range(roomHeightRange.x, roomHeightRange.y);
            int roomDepth = Random.Range(roomDepthRange.x, roomDepthRange.y);

            // Check if room fits in world
            if (roomWidth >= width - 2 || roomDepth >= depth - 2)
            {
                continue; // Too big, try again
            }

            // Generate random position (with edge padding)
            int roomX = Random.Range(1, width - roomWidth - 1);
            int roomY = 1; // Always place at ground level
            int roomZ = Random.Range(1, depth - roomDepth - 1);

            RoomBounds newRoom = new RoomBounds(roomX, roomY, roomZ, roomWidth, roomHeight, roomDepth);

            // Check for overlaps with existing structures
            bool canPlace = true;
            foreach (RoomBounds existing in allRooms)
            {
                if (newRoom.Overlaps(existing))
                {
                    canPlace = false;
                    break;
                }
            }

            if (canPlace)
            {
                // Carve room: 3=walls, 2=interior
                Carve(roomX, roomY, roomZ, roomWidth, roomHeight, roomDepth, 3, 2);
                allRooms.Add(newRoom);

                // Place doors on room walls
                PlaceDoors(roomX, roomY, roomZ, roomWidth, roomDepth);

                Debug.Log($"Room {allRooms.Count}: Size({roomWidth}x{roomHeight}x{roomDepth}) at ({roomX},{roomY},{roomZ})");
                return;
            }
        }

        Debug.LogWarning($"Couldn't place room after {maxAttempts} attempts");
    }

    // Place all configured rooms
    public void PlaceRooms()
    {
        for (int i = 0; i < numberOfRooms; i++)
        {
            PlaceRoom();
        }
    }

    // ===================================================================
    // HALLWAY PLACEMENT
    // ===================================================================

    // Attempt to place a single hallway
    public void PlaceHallWay()
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random dimensions
            int hallWidth = Random.Range(HallWidthRange.x, HallWidthRange.y);
            int hallHeight = Random.Range(HallHeightRange.x, HallHeightRange.y);
            int hallDepth = Random.Range(HllDepthRange.x, HllDepthRange.y);

            // Check if hallway fits in world
            if (hallWidth >= width - 2 || hallDepth >= depth - 2)
            {
                continue; // Too big, try again
            }

            // Generate random position (with edge padding)
            int hallX = Random.Range(1, width - hallWidth - 1);
            int hallY = 1; // Always place at ground level
            int hallZ = Random.Range(1, depth - hallDepth - 1);

            RoomBounds newHall = new RoomBounds(hallX, hallY, hallZ, hallWidth, hallHeight, hallDepth);

            // Check for overlaps with existing structures
            bool canPlace = true;
            foreach (RoomBounds existing in allRooms)
            {
                if (newHall.Overlaps(existing))
                {
                    canPlace = false;
                    break;
                }
            }

            if (canPlace)
            {
                // Carve hallway: 7=walls, 6=interior
                Carve(hallX, hallY, hallZ, hallWidth, hallHeight, hallDepth, 7, 6);
                allRooms.Add(newHall);

                // Place doors on hallway walls
                PlaceDoors(hallX, hallY, hallZ, hallWidth, hallDepth);

                Debug.Log($"Hallway {allRooms.Count}: Size({hallWidth}x{hallHeight}x{hallDepth}) at ({hallX},{hallY},{hallZ})");
                return;
            }
        }

        Debug.LogWarning($"Couldn't place hallway after {maxAttempts} attempts");
    }

    // Place all configured hallways
    public void PlaceHallWays()
    {
        for (int i = 0; i < numberOfHalls; i++)
        {
            PlaceHallWay();
        }
    }

    // ===================================================================
    // STRUCTURE CARVING
    // ===================================================================

    // Carve a hollow box structure into the grid
    // Creates walls on edges and empty interior
    void Carve(int x, int y, int z, int w, int h, int d, int wallValue = 3, int interiorValue = 2)
    {
        for (int ix = x; ix < x + w; ix++)
        {
            for (int iy = y; iy < y + h; iy++)
            {
                for (int iz = z; iz < z + d; iz++)
                {
                    if (ix < width && iy < height && iz < depth)
                    {
                        // Determine if this position is a wall or interior
                        bool isWall = (ix == x || ix == x + w - 1 ||       // Left/Right walls
                                       iz == z || iz == z + d - 1 ||       // Front/Back walls
                                       iy == y + h - 1);                   // Ceiling

                        if (isWall)
                        {
                            GreenFieldData[ix, iy, iz] = wallValue;
                        }
                        else
                        {
                            GreenFieldData[ix, iy, iz] = interiorValue;
                        }
                    }
                }
            }
        }
    }

    // ===================================================================
    // DOOR PLACEMENT
    // ===================================================================

    // Place doors on structure walls, avoiding edges and facing center
    void PlaceDoors(int x, int y, int z, int w, int d)
    {
        bool[] wallsWithDoors = new bool[4];
        bool[] wallsValid = new bool[4];

        int mapCenterX = width / 2;
        int mapCenterZ = depth / 2;

        // Validate each wall: ensure enough space for corridor
        wallsValid[0] = (z + d + corridorWidth + 2 < depth);  // North
        wallsValid[1] = (z - corridorWidth - 2 > 0);          // South
        wallsValid[2] = (x + w + corridorWidth + 2 < width);  // East
        wallsValid[3] = (x - corridorWidth - 2 > 0);          // West

        // Count how many walls are valid
        int validWallCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (wallsValid[i]) validWallCount++;
        }

        if (validWallCount == 0)
        {
            Debug.LogError($"Structure at ({x},{y},{z}) has NO valid walls for doors!");
            return;
        }

        // Determine number of doors (1-4, capped by valid walls)
        int numDoors = Mathf.Min(Random.Range(1, 5), validWallCount);
        int doorsPlaced = 0;

        // Prioritize walls facing toward center of map
        List<int> preferredWalls = new List<int>();
        List<int> otherWalls = new List<int>();

        int structCenterX = x + w / 2;
        int structCenterZ = z + d / 2;

        for (int i = 0; i < 4; i++)
        {
            if (!wallsValid[i]) continue;

            bool facingCenter = false;

            switch (i)
            {
                case 0: facingCenter = (structCenterZ < mapCenterZ); break; // North
                case 1: facingCenter = (structCenterZ > mapCenterZ); break; // South
                case 2: facingCenter = (structCenterX < mapCenterX); break; // East
                case 3: facingCenter = (structCenterX > mapCenterX); break; // West
            }

            if (facingCenter)
                preferredWalls.Add(i);
            else
                otherWalls.Add(i);
        }

        // Select walls for doors (prefer center-facing)
        while (doorsPlaced < numDoors)
        {
            int wallIndex;

            if (preferredWalls.Count > 0)
            {
                int idx = Random.Range(0, preferredWalls.Count);
                wallIndex = preferredWalls[idx];
                preferredWalls.RemoveAt(idx);
            }
            else if (otherWalls.Count > 0)
            {
                int idx = Random.Range(0, otherWalls.Count);
                wallIndex = otherWalls[idx];
                otherWalls.RemoveAt(idx);
            }
            else
            {
                break;
            }

            if (!wallsWithDoors[wallIndex])
            {
                wallsWithDoors[wallIndex] = true;
                doorsPlaced++;
            }
        }

        // Carve the actual door openings
        if (wallsWithDoors[0]) CarveDoor(x, y, z, w, d, 0);
        if (wallsWithDoors[1]) CarveDoor(x, y, z, w, d, 1);
        if (wallsWithDoors[2]) CarveDoor(x, y, z, w, d, 2);
        if (wallsWithDoors[3]) CarveDoor(x, y, z, w, d, 3);

        // Record door positions for connection logic
        RecordDoors(x, y, z, w, d, wallsWithDoors);

        Debug.Log($"Placed {doorsPlaced} smart doors (valid walls: {validWallCount})");
    }

    // Carve a door opening in a wall
    void CarveDoor(int roomX, int roomY, int roomZ, int roomW, int roomD, int wall)
    {
        // wall: 0=North, 1=South, 2=East, 3=West

        if (wall == 0 || wall == 1) // North or South walls (door spans X axis)
        {
            int doorZ = (wall == 0) ? roomZ + roomD - 1 : roomZ;
            int doorStartX = roomX + (roomW / 2) - (doorWidth / 2);

            for (int dx = 0; dx < doorWidth; dx++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    int px = doorStartX + dx;
                    int py = roomY + dy;

                    if (px >= 0 && px < width && py >= 0 && py < height && doorZ >= 0 && doorZ < depth)
                    {
                        GreenFieldData[px, py, doorZ] = 5; // 5 = door
                    }
                }
            }
        }
        else // East or West walls (door spans Z axis)
        {
            int doorX = (wall == 2) ? roomX + roomW - 1 : roomX;
            int doorStartZ = roomZ + (roomD / 2) - (doorWidth / 2);

            for (int dz = 0; dz < doorWidth; dz++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    int pz = doorStartZ + dz;
                    int py = roomY + dy;

                    if (doorX >= 0 && doorX < width && py >= 0 && py < height && pz >= 0 && pz < depth)
                    {
                        GreenFieldData[doorX, py, pz] = 5; // 5 = door
                    }
                }
            }
        }
    }

    // Calculate the center position of a door (used for pathfinding)
    Vector3Int GetDoorOuterPosition(int x, int y, int z, int w, int d, int direction)
    {
        int offset = corridorWidth / 2 + 1; // Extend beyond door

        switch (direction)
        {
            case 0: // North - extend outward in +Z direction
                return new Vector3Int(x + w / 2, y, z + d + offset);
            case 1: // South - extend outward in -Z direction
                return new Vector3Int(x + w / 2, y, z - offset);
            case 2: // East - extend outward in +X direction
                return new Vector3Int(x + w + offset, y, z + d / 2);
            case 3: // West - extend outward in -X direction
                return new Vector3Int(x - offset, y, z + d / 2);
            default:
                return Vector3Int.zero;
        }
    }

    // Store door information for later connection
    void RecordDoors(int x, int y, int z, int w, int d, bool[] wallsWithDoors)
    {
        for (int i = 0; i < 4; i++)
        {
            if (wallsWithDoors[i])
            {
                Vector3Int doorPos = GetDoorOuterPosition(x, y, z, w, d, i);
                allDoors.Add(new DoorInfo
                {
                    position = doorPos,
                    direction = i,
                    structureIndex = allRooms.Count - 1
                });
            }
        }
    }


    // ===================================================================
    // CORRIDOR CONNECTION SYSTEM
    // ===================================================================

    // Connect all structures with corridors
    public void ConnectDoors()
    {
        HashSet<int> connected = new HashSet<int> { 0 }; // Seed structure 0

        // Group doors by structure
        Dictionary<int, List<DoorInfo>> doorsByStructure = new Dictionary<int, List<DoorInfo>>();
        foreach (DoorInfo door in allDoors)
        {
            if (!doorsByStructure.ContainsKey(door.structureIndex))
                doorsByStructure[door.structureIndex] = new List<DoorInfo>();
            doorsByStructure[door.structureIndex].Add(door);
        }

        // Connect each structure
        foreach (var kvp in doorsByStructure)
        {
            int structureID = kvp.Key;
            if (connected.Contains(structureID)) continue;

            List<DoorInfo> myDoors = kvp.Value;
            HashSet<int> alreadyConnectedTo = new HashSet<int>(); // Track which structures we've already targeted

            // Each door connects to a DIFFERENT structure
            foreach (DoorInfo myDoor in myDoors)
            {
                DoorInfo target = FindNearestConnectedDoor(myDoor, connected, alreadyConnectedTo);

                if (target.structureIndex != -1)
                {
                    CarveLShapedCorridor(myDoor.position, target.position);
                    alreadyConnectedTo.Add(target.structureIndex); // Mark this structure as used
                    Debug.Log($"Connected structure {structureID} door to structure {target.structureIndex}");
                }
            }

            // Mark as connected if we made at least one connection
            if (alreadyConnectedTo.Count > 0)
            {
                connected.Add(structureID);
            }
        }

        Debug.Log($"Connected structures: {connected.Count} out of {allRooms.Count}");
        foreach (int id in Enumerable.Range(0, allRooms.Count))
        {
            if (!connected.Contains(id))
            {
                Debug.LogWarning($"Structure {id} is ORPHANED (not connected)!");
            }
        }
    }

    // Find best door connection that has a clear path
    // Tries all doors sorted by distance until finding one with clean path
    DoorInfo FindBestCleanConnection(DoorInfo fromDoor, HashSet<int> connectedStructures, bool allowConnectedTargets = false)
    {
        List<(DoorInfo door, float distance)> candidates = new List<(DoorInfo, float)>();

        foreach (DoorInfo door in allDoors)
        {
            // Skip doors from same structure
            if (door.structureIndex == fromDoor.structureIndex)
                continue;

            // Allow or disallow already-connected structures as targets
            if (!allowConnectedTargets && connectedStructures.Contains(door.structureIndex))
                continue;

            float distance = Vector3Int.Distance(fromDoor.position, door.position);
            candidates.Add((door, distance));
        }

        // Sort by distance (try nearest first)
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Test each candidate for clean path
        foreach (var candidate in candidates)
        {
            if (IsPathClear(fromDoor.position, candidate.door.position))
            {
                Debug.Log($"Found clean path from structure {fromDoor.structureIndex} to {candidate.door.structureIndex}");
                return candidate.door;
            }
            else
            {
                Debug.Log($"Path blocked from structure {fromDoor.structureIndex} to {candidate.door.structureIndex}");
            }
        }

        return new DoorInfo { structureIndex = -1 };
    }

    // Find nearest door, excluding structures we've already connected to
    DoorInfo FindNearestConnectedDoor(DoorInfo fromDoor, HashSet<int> connected, HashSet<int> exclude)
    {
        float minDist = float.MaxValue;
        DoorInfo nearest = new DoorInfo { structureIndex = -1 };

        foreach (DoorInfo door in allDoors)
        {
            // Skip same structure
            if (door.structureIndex == fromDoor.structureIndex) continue;

            // Must be in connected network
            if (!connected.Contains(door.structureIndex)) continue;

            // Skip if we already connected to this structure
            if (exclude.Contains(door.structureIndex)) continue;

            float dist = Vector3.Distance(fromDoor.position, door.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = door;
            }
        }

        return nearest;
    }

    // ===================================================================
    // PATH VALIDATION
    // ===================================================================

    // Check if an L-shaped path between two points is clear
    // Tries both X-first and Z-first orientations
    bool IsPathClear(Vector3Int start, Vector3Int end)
    {
        // Try L-shape going X-first
        Vector3Int corner1 = new Vector3Int(end.x, start.y, start.z);
        bool path1Clear = IsLineClear(start, corner1) && IsLineClear(corner1, end);

        if (path1Clear) return true;

        // Try L-shape going Z-first
        Vector3Int corner2 = new Vector3Int(start.x, start.y, end.z);
        bool path2Clear = IsLineClear(start, corner2) && IsLineClear(corner2, end);

        return path2Clear;
    }

    // Check if a straight line path is clear
    // Tests the full corridor footprint (width x height) along the line
    bool IsLineClear(Vector3Int from, Vector3Int to)
    {
        int steps = Mathf.Max(
            Mathf.Abs(to.x - from.x),
            Mathf.Abs(to.y - from.y),
            Mathf.Abs(to.z - from.z)
        );

        if (steps == 0) return true;

        int halfWidth = corridorWidth / 2;

        // Check each point along the line
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3Int pos = Vector3Int.RoundToInt(Vector3.Lerp(from, to, t));

            // Check the corridor footprint at this position
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                for (int dy = 0; dy < corridorHeight; dy++)
                {
                    for (int dz = -halfWidth; dz <= halfWidth; dz++)
                    {
                        Vector3Int p = pos + new Vector3Int(dx, dy, dz);

                        if (!IsInBounds(p))
                            return false;

                        int value = GreenFieldData[p.x, p.y, p.z];

                        // Block on actual room/hallway structures
                        // Allow: floor(0), walls(1), doors(5), corridors(8)
                        // Block: room interior(2), room walls(3), hall interior(6), hall walls(7)
                        if (value == 2 || value == 3 || value == 6 || value == 7)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    // ===================================================================
    // CORRIDOR CARVING
    // ===================================================================

    // Carve an L-shaped corridor between two points
    // Tests both orientations and uses the one that was validated as clear
    void CarveLShapedCorridor(Vector3Int start, Vector3Int end)
    {
        // Always use X-first path for consistency
        Vector3Int corner = new Vector3Int(end.x, start.y, start.z);

        CarveLineCorridor(start, corner);
        CarveLineCorridor(corner, end);

        // Carve extra connection zone at start and end to ensure no gaps
        CarveConnectionZone(start);
        CarveConnectionZone(end);
    }

    // Carve a small zone around door connection points to ensure smooth transitions
    void CarveConnectionZone(Vector3Int center)
    {
        int halfWidth = corridorWidth / 2;

        for (int dx = -halfWidth; dx <= halfWidth; dx++)
        {
            for (int dy = 0; dy < corridorHeight; dy++)
            {
                for (int dz = -halfWidth; dz <= halfWidth; dz++)
                {
                    Vector3Int p = center + new Vector3Int(dx, dy, dz);

                    if (IsInBounds(p))
                    {
                        int currentValue = GreenFieldData[p.x, p.y, p.z];

                        // Carve through walls, preserve doors and interiors
                        if (currentValue == 1) // Only solid walls
                        {
                            GreenFieldData[p.x, p.y, p.z] = 8; // Corridor
                        }
                    }
                }
            }
        }
    }

    // Improved line corridor carving with better edge handling
    void CarveLineCorridor(Vector3Int from, Vector3Int to)
    {
        // Calculate direction vector
        Vector3Int delta = to - from;
        int steps = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));

        if (steps == 0) return;

        int halfWidth = corridorWidth / 2;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3Int pos = Vector3Int.RoundToInt(Vector3.Lerp(from, to, t));

            // Carve corridor cross-section
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                for (int dy = 0; dy < corridorHeight; dy++)
                {
                    for (int dz = -halfWidth; dz <= halfWidth; dz++)
                    {
                        Vector3Int p = pos + new Vector3Int(dx, dy, dz);

                        if (IsInBounds(p))
                        {
                            int currentValue = GreenFieldData[p.x, p.y, p.z];

                            // Only carve through solid walls (1)
                            // Preserve: floors(0), doors(5), room/hall interiors(2,6), existing corridors(8)
                            if (currentValue == 1)
                            {
                                GreenFieldData[p.x, p.y, p.z] = 8; // Corridor
                            }
                        }
                    }
                }
            }
        }
    }

    // ===================================================================
    // GIZMO VISUALIZATION
    // ===================================================================

    // Draw colored cubes in the editor to visualize the level structure
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
                        case 0: // Floor
                            //Gizmos.color = new Color(0, 0, 1, 0.1f);
                           // Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
                            break;

                        case 1: // Solid walls (hidden for clarity)
                            break;

                        case 2: // Room interior
                            //Gizmos.color = new Color(1, 0, 0, 0.3f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;

                        case 3: // Room walls
                           // Gizmos.color = new Color(1, 1, 0, 0.5f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;

                        case 5: // Doors
                            //Gizmos.color = Color.magenta;
                            //Gizmos.DrawCube(pos, Vector3.one * 0.7f);
                            break;

                        case 6: // Hallway interior
                           // Gizmos.color = Color.cyan;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;

                        case 7: // Hallway walls
                            //Gizmos.color = new Color(0, 0.5f, 1, 0.5f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;

                        case 8: // Corridor connectors
                            //Gizmos.color = new Color(0, 1, 0, 0.4f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.5f);
                            break;
                    }
                }
            }
        }
    }
}
