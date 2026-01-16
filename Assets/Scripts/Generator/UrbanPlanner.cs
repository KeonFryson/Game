using System.Collections.Generic;
using UnityEngine;

public class UrbanPlanner : GreenField
{
    // Tracks every door position created
    private List<Vector3Int> allExitPositions = new List<Vector3Int>();

    // Tracks room boundaries for collision detection
    private List<RoomBounds> allRooms = new List<RoomBounds>();

    //Track player spawn position
    private Vector3Int playerSpawnPosition;

    public int numberOfRooms = 10;

    // Helper struct to store room dimensions for collision checking
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

        // Check if this room overlaps with another room
        public bool Overlaps(RoomBounds other)
        {
            bool xOverlap = minX < other.maxX && maxX > other.minX;
            bool yOverlap = minY < other.maxY && maxY > other.minY;
            bool zOverlap = minZ < other.maxZ && maxZ > other.minZ;
            return xOverlap && yOverlap && zOverlap;
        }
    }

    string GetValueName(int x, int y, int z)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth)
            return "OUT_OF_BOUNDS";

        int val = GreenFieldData[x, y, z];
        switch (val)
        {
            case 0: return "Floor";
            case 1: return "Wall";
            case 2: return "Path";
            case 3: return "RoomWall";
            case 4: return "Spawn";
            case 5: return "Door";
            default: return $"Unknown({val})";
        }
    }

    // Main generation pipeline
    public override void GenerateFieldData()
    {

        // Initialize array first!
        if (GreenFieldData == null || GreenFieldData.Length == 0)
        {
            GreenFieldData = new int[width, height, depth];
            Debug.Log($"Initialized GreenFieldData array: {width}x{height}x{depth}");
        }

        SetWall();                      // 1. Fill entire array with walls
        SetFloor();                     // 2. Add floor layer at bottom
        SetRooms(numberOfRooms);        // 3. Create random rooms with doors
        SetPlayerSpawn();               // Set player spawn point
        ConnectAllDoors();              // 4. Connect all doors with corridors
        ValidateAllExits();             // 5. Validate exit connectivity

    }

    // Fill entire 3D array with walls (value 1)
    void SetWall()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < depth; k++)
                {
                    GreenFieldData[i, j, k] = 1; // All walls
                }
            }
        }
    }

    // Create floor layer at Y=0 (value 0)
    void SetFloor()
    {
        for (int i = 0; i < width; i++)
        {
            for (int k = 0; k < depth; k++)
            {
                GreenFieldData[i, 0, k] = 0; // Floor at bottom
            }
        }
    }

    // Check if a new room would collide with any existing rooms
    bool HasCollision(RoomBounds newRoom)
    {
        foreach (RoomBounds existing in allRooms)
        {
            if (newRoom.Overlaps(existing))
            {
                return true; // Collision detected
            }
        }
        return false; // No collision
    }

    // Creates ONE room at the specified position with specified size
    // Returns list of door positions that were created
    List<Vector3Int> SetRoom(int roomWidth, int roomHeight, int roomDepth,
                         int roomX, int roomY, int roomZ)
    {
        List<Vector3Int> exitPositions = new List<Vector3Int>();  // Changed from doorPositions

        Debug.Log($"Creating room: Size({roomWidth}x{roomHeight}x{roomDepth}) at ({roomX},{roomY},{roomZ})");

        // Carve the room structure (same as before)
        // Carve the room structure
        for (int x = roomX; x < roomX + roomWidth; x++)
        {
            for (int y = roomY; y < roomY + roomHeight; y++)
            {
                for (int z = roomZ; z < roomZ + roomDepth; z++)
                {
                    if (x < width && y < height && z < depth)
                    {
                        bool isWall = (x == roomX || x == roomX + roomWidth - 1 ||
                                       z == roomZ || z == roomZ + roomDepth - 1 ||
                                       y == roomY + roomHeight - 1);

                        if (isWall)
                        {
                            GreenFieldData[x, y, z] = 3;  // Room walls
                        }
                        else
                        {
                            GreenFieldData[x, y, z] = 2;  // Open air 
                        }
                    }
                }
            }
        }

        // Randomly choose 1-4 doors for this room
        int numDoors = Random.Range(1, 5);
        bool[] wallsWithDoors = new bool[4];
        int doorsPlaced = 0;

        while (doorsPlaced < numDoors)
        {
            int wallIndex = Random.Range(0, 4);
            if (!wallsWithDoors[wallIndex])
            {
                wallsWithDoors[wallIndex] = true;
                doorsPlaced++;
            }
        }

        // Place doors and track EXTERIOR positions
        int doorX, doorZ;

        if (wallsWithDoors[0]) // North wall
        {
            doorX = roomX + roomWidth / 2;
            doorZ = roomZ + roomDepth - 1;
            GreenFieldData[doorX, roomY, doorZ] = 5; // Door

            // CARVE EXIT (outside room) and TRACK IT
            if (doorZ + 1 < depth)
            {
                GreenFieldData[doorX, roomY, doorZ + 1] = 2;
                exitPositions.Add(new Vector3Int(doorX, roomY, doorZ + 1));  // Track EXIT position
            }

            // CARVE ENTRY (inside room)
            if (doorZ - 1 >= 0)
                GreenFieldData[doorX, roomY, doorZ - 1] = 2;
        }

        if (wallsWithDoors[1]) // South wall
        {
            doorX = roomX + roomWidth / 2;
            doorZ = roomZ;
            GreenFieldData[doorX, roomY, doorZ] = 5;

            // CARVE EXIT and TRACK IT
            if (doorZ - 1 >= 0)
            {
                GreenFieldData[doorX, roomY, doorZ - 1] = 2;
                exitPositions.Add(new Vector3Int(doorX, roomY, doorZ - 1));
            }

            // CARVE ENTRY
            if (doorZ + 1 < depth)
                GreenFieldData[doorX, roomY, doorZ + 1] = 2;
        }

        if (wallsWithDoors[2]) // East wall
        {
            doorX = roomX + roomWidth - 1;
            doorZ = roomZ + roomDepth / 2;
            GreenFieldData[doorX, roomY, doorZ] = 5;

            // CARVE EXIT and TRACK IT
            if (doorX + 1 < width)
            {
                GreenFieldData[doorX + 1, roomY, doorZ] = 2;
                exitPositions.Add(new Vector3Int(doorX + 1, roomY, doorZ));
            }

            // CARVE ENTRY
            if (doorX - 1 >= 0)
                GreenFieldData[doorX - 1, roomY, doorZ] = 2;
        }

        if (wallsWithDoors[3]) // West wall
        {
            doorX = roomX;
            doorZ = roomZ + roomDepth / 2;
            GreenFieldData[doorX, roomY, doorZ] = 5;

            // CARVE EXIT and TRACK IT
            if (doorX - 1 >= 0)
            {
                GreenFieldData[doorX - 1, roomY, doorZ] = 2;
                exitPositions.Add(new Vector3Int(doorX - 1, roomY, doorZ));
            }

            // CARVE ENTRY
            if (doorX + 1 < width)
                GreenFieldData[doorX + 1, roomY, doorZ] = 2;
        }

        return exitPositions; // Return EXIT positions
    }

    // Attempts to place multiple random rooms
    // Each room tries up to 50 times to find a valid non-overlapping position
    void SetRooms(int numberOfRooms)
    {
        for (int i = 0; i < numberOfRooms; i++)
        {
            int maxAttempts = 50;
            bool placed = false;

            for (int attempt = 0; attempt < maxAttempts && !placed; attempt++)
            {
                // Generate random size for this placement attempt
                int roomWidth = Random.Range(5, 9);
                int roomHeight = Random.Range(3, 6);
                int roomDepth = Random.Range(5, 9);

                // Check if room can fit within world bounds
                if (roomWidth >= width - 2 || roomDepth >= depth - 2)
                {
                    continue; // Room too big, try again
                }

                // Generate random position for this placement attempt
                int roomX = Random.Range(1, width - roomWidth - 1);
                int roomY = 1; // All rooms at Y=1 for now
                int roomZ = Random.Range(1, depth - roomDepth - 1);

                // Create bounds for collision checking
                RoomBounds newRoom = new RoomBounds(roomX, roomY, roomZ, roomWidth, roomHeight, roomDepth);

                // Check if this position collides with existing rooms
                if (!HasCollision(newRoom))
                {
                    // Get EXIT positions from SetRoom
                    List<Vector3Int> exits = SetRoom(roomWidth, roomHeight, roomDepth, roomX, roomY, roomZ);

                    allRooms.Add(newRoom);

                    // Store all EXIT positions (not door positions)
                    allExitPositions.AddRange(exits); 

                    placed = true;
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"Couldn't place room {i} after {maxAttempts} attempts");
            }
        }
    }


    // Carves TWO L-shaped corridors between two points
    // One goes X-first, one goes Z-first 
    void DrawLineBetween(Vector3Int start, Vector3Int end)
    {
        Debug.Log($"Drawing double L-path from ({start.x},{start.z}) to ({end.x},{end.z})");

        // PATH 1: X-first, then Z
        DrawL_XFirst(start, end);

        // PATH 2: Z-first, then X
        DrawL_ZFirst(start, end);
    }

    // Helper: X direction first, then Z
    void DrawL_XFirst(Vector3Int start, Vector3Int end)
    {
        int x = start.x;
        int z = start.z;
        int y = start.y;

        // Move along X axis
        while (x != end.x)
        {
            if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
            {
                if (GreenFieldData[x, y, z] != 3 &&
                    GreenFieldData[x, y, z] != 5 &&
                    GreenFieldData[x, y, z] != 4)
                {
                    GreenFieldData[x, y, z] = 2;
                }
            }
            x += (end.x > x) ? 1 : -1;
        }

        // Move along Z axis
        while (z != end.z)
        {
            if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
            {
                if (GreenFieldData[x, y, z] != 3 &&
                    GreenFieldData[x, y, z] != 5 &&
                    GreenFieldData[x, y, z] != 4)
                {
                    GreenFieldData[x, y, z] = 2;
                }
            }
            z += (end.z > z) ? 1 : -1;
        }

        // Final position
        if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
        {
            if (GreenFieldData[x, y, z] != 3 &&
                GreenFieldData[x, y, z] != 5 &&
                GreenFieldData[x, y, z] != 4)
            {
                GreenFieldData[x, y, z] = 2;
            }
        }
    }

    // Helper: Z direction first, then X
    void DrawL_ZFirst(Vector3Int start, Vector3Int end)
    {
        int x = start.x;
        int z = start.z;
        int y = start.y;

        // Move along Z axis FIRST
        while (z != end.z)
        {
            if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
            {
                if (GreenFieldData[x, y, z] != 3 &&
                    GreenFieldData[x, y, z] != 5 &&
                    GreenFieldData[x, y, z] != 4)
                {
                    GreenFieldData[x, y, z] = 2;
                }
            }
            z += (end.z > z) ? 1 : -1;
        }

        // Move along X axis SECOND
        while (x != end.x)
        {
            if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
            {
                if (GreenFieldData[x, y, z] != 3 &&
                    GreenFieldData[x, y, z] != 5 &&
                    GreenFieldData[x, y, z] != 4)
                {
                    GreenFieldData[x, y, z] = 2;
                }
            }
            x += (end.x > x) ? 1 : -1;
        }

        // Final position
        if (x >= 0 && x < width && z >= 0 && z < depth && y >= 0 && y < height)
        {
            if (GreenFieldData[x, y, z] != 3 &&
                GreenFieldData[x, y, z] != 5 &&
                GreenFieldData[x, y, z] != 4)
            {
                GreenFieldData[x, y, z] = 2;
            }
        }
    }


    // Connects all doors using Minimum Spanning Tree (efficient connectivity)
    void ConnectAllDoors()
    {
        if (allExitPositions.Count == 0) return;

        Debug.Log($"Connecting {allExitPositions.Count} doors using MST...");

        List<Vector3Int> connected = new List<Vector3Int>();
        List<Vector3Int> unconnected = new List<Vector3Int>(allExitPositions);

        // Start with first door
        connected.Add(unconnected[0]);
        unconnected.RemoveAt(0);

        int pathsCreated = 0;

        // Keep connecting until all doors are connected
        while (unconnected.Count > 0)
        {
            float shortestDistance = float.MaxValue;
            int bestConnectedIndex = -1;
            int bestUnconnectedIndex = -1;

            // Find closest unconnected door to ANY connected door
            for (int i = 0; i < connected.Count; i++)
            {
                for (int j = 0; j < unconnected.Count; j++)
                {
                    float distance = Vector3Int.Distance(connected[i], unconnected[j]);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        bestConnectedIndex = i;
                        bestUnconnectedIndex = j;
                    }
                }
            }

            // Connect the closest pair
            DrawLineBetween(connected[bestConnectedIndex], unconnected[bestUnconnectedIndex]);

            // Move door from unconnected to connected
            connected.Add(unconnected[bestUnconnectedIndex]);
            unconnected.RemoveAt(bestUnconnectedIndex);

            pathsCreated++;
        }

        Debug.Log($"Created {pathsCreated} connections (MST)");
    }

    // Call this at the end of GenerateFieldData() to diagnose unreachable exits
    void ValidateAllExits()
    {
        Debug.Log("=== VALIDATING EXIT POSITIONS ===");

        for (int i = 0; i < allExitPositions.Count; i++)
        {
            Vector3Int exit = allExitPositions[i];
            int x = exit.x;
            int y = exit.y;
            int z = exit.z;

            // Check if exit position is actually in bounds
            if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth)
            {
                Debug.LogError($"Exit {i} at {exit} is OUT OF BOUNDS! (World size: {width}x{height}x{depth})");
                continue;
            }

            // Check what value the exit position has
            int value = GreenFieldData[x, y, z];
            Debug.Log($"Exit {i} at {exit}: value = {value} (should be 2 for path)");

            if (value != 2)
            {
                Debug.LogWarning($"Exit {i} at {exit} has wrong value! Expected 2 (path), got {value}");
            }

            // Check neighbors (are there any paths nearby?)
            int pathNeighbors = 0;
            Vector3Int[] neighbors = new Vector3Int[]
            {
            new Vector3Int(x+1, y, z),
            new Vector3Int(x-1, y, z),
            new Vector3Int(x, y, z+1),
            new Vector3Int(x, y, z-1)
            };

            foreach (Vector3Int neighbor in neighbors)
            {
                if (neighbor.x >= 0 && neighbor.x < width &&
                    neighbor.z >= 0 && neighbor.z < depth &&
                    neighbor.y >= 0 && neighbor.y < height)
                {
                    if (GreenFieldData[neighbor.x, neighbor.y, neighbor.z] == 2)
                    {
                        pathNeighbors++;
                    }
                }
            }

            Debug.Log($"Exit {i} has {pathNeighbors} path neighbors (connections)");

            if (pathNeighbors == 0)
            {
                Debug.LogError($"EXIT {i} AT {exit} IS ISOLATED! No path neighbors!");

                // Show what's around it
                Debug.Log($"  North ({x},{z + 1}): {GetValueName(x, y, z + 1)}");
                Debug.Log($"  South ({x},{z - 1}): {GetValueName(x, y, z - 1)}");
                Debug.Log($"  East ({x + 1},{z}): {GetValueName(x + 1, y, z)}");
                Debug.Log($"  West ({x - 1},{z}): {GetValueName(x - 1, y, z)}");
            }
        }
    }

    void SetPlayerSpawn()
    {
        if (allRooms.Count == 0) return;

        // Use first room
        RoomBounds spawnRoom = allRooms[0];

        // Middle of the room, floor level
        int spawnX = (spawnRoom.minX + spawnRoom.maxX) / 2;
        int spawnY = spawnRoom.minY; // Floor level
        int spawnZ = (spawnRoom.minZ + spawnRoom.maxZ) / 2;

        // Store position
        playerSpawnPosition = new Vector3Int(spawnX, spawnY, spawnZ);

        Debug.Log($"Player spawn: ({spawnX},{spawnY},{spawnZ})");
    }

    public Vector3Int GetPlayerSpawnPosition()
    {
        return playerSpawnPosition;
    }
    // Real-time visualization in Scene view (no GameObject instantiation)
    void OnDrawGizmos()
    {
        if (GreenFieldData == null) return;

        // Draw grid data
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);

                    switch (GreenFieldData[x, y, z])
                    {
                        case 0: // Floor (hidden)
                            //Gizmos.color = Color.teal;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.5f);
                            break;

                        case 1: // Wall (hidden)
                            break;

                        case 2: // Path/Walkable space
                           // Gizmos.color = Color.red;
                          //  Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;

                        case 3: // Room walls
                           // Gizmos.color = Color.yellow;
                          //  Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;

                        case 4: // Player spawn point (hidden for now)
                            break;

                        case 5: // Door
                           // Gizmos.color = Color.magenta;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.7f);
                            break;
                    }
                }
            }
        }

        // Draw EXIT POSITIONS (where MST connects)
        /*
        if (allExitPositions != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3Int exitPos in allExitPositions)
            {
                Vector3 pos = new Vector3(exitPos.x, exitPos.y, exitPos.z);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);  // Cyan wireframe cube
            }
        }
        */
    }


}