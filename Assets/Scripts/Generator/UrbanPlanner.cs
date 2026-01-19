using System.Collections.Generic;
using UnityEngine;

public class UrbanPlanner : GreenField
{
    private Vector3Int startingDoorPosition;

    // Track all room boundaries
    private List<RoomBounds> allRooms = new List<RoomBounds>();

    // Helper struct to store room dimensions
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

        // Check if this room overlaps with another
        public bool Overlaps(RoomBounds other)
        {
            bool xOverlap = minX < other.maxX && maxX > other.minX;
            bool yOverlap = minY < other.maxY && maxY > other.minY;
            bool zOverlap = minZ < other.maxZ && maxZ > other.minZ;

            return xOverlap && yOverlap && zOverlap;
        }
    }

    public override void GenerateFieldData()
    {
        SetWall();                              //  Fill with walls
        SetFloor();                             //  Add floor
        SetPath();                            //  Carve main path
        //PlayerStartingRoom();                 //  Create starting room (sets startingDoorPosition)
        PathFromDoor(startingDoorPosition);   //  Connect door to main path
        
        // Create 5 additional rooms
        for (int i = 0; i < 5; i++)
        {
            SetRoom();                          //  Attempt to add a room
        }

                                            
    }

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

    void SetRoom()
    {
        int maxAttempts = 50;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            attempts++;

            // Generate NEW size for each attempt
            int roomWidth = Random.Range(5, 9);
            int roomHeight = Random.Range(3, 6);
            int roomDepth = Random.Range(5, 9);

            // CHECK IF ROOM CAN FIT IN BOUNDS
            if (roomWidth >= width - 2 || roomDepth >= depth - 2)
            {
                continue; // Room too big, try again
            }

            // Generate NEW position for each attempt
            int roomX = Random.Range(1, width - roomWidth - 1);
            int roomY = 1;
            int roomZ = Random.Range(1, depth - roomDepth - 1);

            RoomBounds newRoom = new RoomBounds(roomX, roomY, roomZ, roomWidth, roomHeight, roomDepth);

            // Check overlap
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
                Debug.Log($"Room {allRooms.Count} placed: Size({roomWidth}x{roomHeight}x{roomDepth}) at ({roomX},{roomY},{roomZ})");
                allRooms.Add(newRoom);

                // Carve room
                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        for (int z = roomZ; z < roomZ + roomDepth; z++)
                        {
                            if (x < width && y < height && z < depth)
                            {
                                bool isWall = (x == roomX || x == roomX + roomWidth - 1 ||      // Left/Right walls
                                               z == roomZ || z == roomZ + roomDepth - 1 ||      // Front/Back walls
                                                             y == roomY + roomHeight - 1);      // Ceiling ONLY (removed floor)

                                if (isWall)
                                {
                                    GreenFieldData[x, y, z] = 3;  // Room wall (yellow)
                                }
                                else
                                {
                                    GreenFieldData[x, y, z] = 2;  // Empty space / Path (red)
                                }
                            }
                        }
                    }
                }

                // Add 1-4 doors
                int numDoors = Random.Range(1, 5);
                Debug.Log($"Room will have {numDoors} door(s)");

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

                // Place doors
                int doorX, doorZ;

                if (wallsWithDoors[0]) // North
                {
                    doorX = roomX + roomWidth / 2;
                    doorZ = roomZ + roomDepth - 1;
                    GreenFieldData[doorX, roomY, doorZ] = 5;
                }

                if (wallsWithDoors[1]) // South
                {
                    doorX = roomX + roomWidth / 2;
                    doorZ = roomZ;
                    GreenFieldData[doorX, roomY, doorZ] = 5;
                }

                if (wallsWithDoors[2]) // East
                {
                    doorX = roomX + roomWidth - 1;
                    doorZ = roomZ + roomDepth / 2;
                    GreenFieldData[doorX, roomY, doorZ] = 5;
                }

                if (wallsWithDoors[3]) // West
                {
                    doorX = roomX;
                    doorZ = roomZ + roomDepth / 2;
                    GreenFieldData[doorX, roomY, doorZ] = 5;
                }

                return; // Success!
            }
        }

        Debug.LogWarning($"Couldn't place room after {maxAttempts} attempts");
    }


    void PlayerStartingRoom()
    {
        // GreenFieldData[x, y, z] = 4; // Spawn marker

        // Fixed size starting room (or make it random?)
        int roomWidth = 5;
        int roomHeight = 3;
        int roomDepth = 5;

        // fixed starting position will randomize later
        int roomX = 2;
        int roomY = 1;
        int roomZ = 2;

        Debug.Log($"Creating starting room at ({roomX},{roomY},{roomZ})");

        // Store bounds FIRST
        allRooms.Add(new RoomBounds(roomX, roomY, roomZ, roomWidth, roomHeight, roomDepth));

        // Carve out room
        for (int x = roomX; x < roomX + roomWidth; x++)
        {
            for (int y = roomY; y < roomY + roomHeight; y++)
            {
                for (int z = roomZ; z < roomZ + roomDepth; z++)
                {
                    if (x < width && y < height && z < depth)
                    {
                        GreenFieldData[x, y, z] = 3; // Room interior
                    }
                }
            }
        }

        // Mark player spawn point (center of room)
        int spawnX = roomX + roomWidth / 2;
        int spawnY = roomY;
        int spawnZ = roomZ + roomDepth / 2;
        GreenFieldData[spawnX, spawnY, spawnZ] = 4; // Cyan spawn marker

        // Add exactly ONE door (random wall)
        int wallChoice = Random.Range(0, 4);
        int doorX = 0, doorZ = 0;

        switch (wallChoice)
        {
            case 0: doorX = roomX + roomWidth / 2; doorZ = roomZ + roomDepth - 1; 
                break;
            case 1: doorX = roomX + roomWidth / 2; doorZ = roomZ; 
                break;
            case 2: doorX = roomX + roomWidth - 1; doorZ = roomZ + roomDepth / 2; 
                break;
            case 3: doorX = roomX; doorZ = roomZ + roomDepth / 2; 
                break;
        }

        GreenFieldData[doorX, roomY, doorZ] = 5; // Door

        // Store door position for Path to use
        startingDoorPosition = new Vector3Int(doorX, roomY, doorZ);
        Debug.Log($"Starting room door at: {startingDoorPosition}");

    }

    void SetPath()
    {
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        int x = width / 2;
        int y = 1;
        int z = depth / 2;

        Debug.Log($"Starting path carver at: ({x}, {y}, {z})");
        GreenFieldData[x, y, z] = 2;

        while (true)
        {
            Vector3Int move = directions[Random.Range(0, directions.Length)];
            int newX = x + move.x;
            int newZ = z + move.z;

            if (newX < 0 || newX >= width || newZ < 0 || newZ >= depth)
            {
                Debug.Log($"Path reached edge at: ({x}, {y}, {z})");
                break;
            }

            x = newX;
            z = newZ;
            GreenFieldData[x, y, z] = 2;
        }

        Debug.Log("Path carving complete!");
    }

    void PathFromDoor(Vector3Int doorPos)
    {
        int x = doorPos.x;
        int y = doorPos.y;
        int z = doorPos.z;

        Debug.Log($"Finding nearest path to door at ({x},{y},{z})");

        // Find the nearest path cell (value 2)
        Vector3Int nearestPath = FindNearestPath(doorPos);

        if (nearestPath == Vector3Int.zero)
        {
            Debug.LogWarning("No path found nearby!");
            return;
        }

        Debug.Log($"Nearest path found at ({nearestPath.x},{nearestPath.y},{nearestPath.z})");

        // Draw straight line from door to nearest path
        DrawLineBetween(doorPos, nearestPath);
    }

    Vector3Int FindNearestPath(Vector3Int start)
    {
        float shortestDistance = float.MaxValue;
        Vector3Int nearestPath = Vector3Int.zero;

        // Search the entire array for path cells (value 2)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (GreenFieldData[x, start.y, z] == 2) // Found a path cell
                {
                    // Calculate distance
                    float distance = Vector3Int.Distance(start, new Vector3Int(x, start.y, z));

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        nearestPath = new Vector3Int(x, start.y, z);
                    }
                }
            }
        }

        return nearestPath;
    }

    void DrawLineBetween(Vector3Int start, Vector3Int end)
    {
        int x = start.x;
        int z = start.z;
        int y = start.y;

        // Move toward target on X axis
        while (x != end.x)
        {
            // Only carve if it's a wall (value 1) or already floor (value 0)
            if (GreenFieldData[x, y, z] == 1 || GreenFieldData[x, y, z] == 0)
            {
                GreenFieldData[x, y, z] = 2; // Carve path
            }
            // If it's a room (3), door (5), or spawn (4), leave it alone!

            x += (end.x > x) ? 1 : -1;
        }

        // Move toward target on Z axis
        while (z != end.z)
        {
            // Only carve if it's a wall or floor
            if (GreenFieldData[x, y, z] == 1 || GreenFieldData[x, y, z] == 0)
            {
                GreenFieldData[x, y, z] = 2; // Carve path
            }

            z += (end.z > z) ? 1 : -1;
        }

        // Mark final position (only if wall/floor)
        if (GreenFieldData[x, y, z] == 1 || GreenFieldData[x, y, z] == 0)
        {
            GreenFieldData[x, y, z] = 2;
        }

        Debug.Log($"Line drawn from ({start.x},{start.z}) to ({end.x},{end.z})");
    }

    // VISUALIZATION
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
                            Gizmos.color = Color.blue;
                            Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
                            break;

                        case 1: // Wall
                           // Gizmos.color = Color.green;
                            //Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
                            break;
                        case 2: // Path
                            Gizmos.color = Color.red;
                            Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;

                        case 3: // Room
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;

                        case 4: // Player Spawn
                            Gizmos.color = Color.cyan;
                            Gizmos.DrawSphere(pos, 0.5f);
                            break;

                        case 5: // Door
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawCube(pos, Vector3.one * 0.7f);
                            break;
                       
                    }
                }
            }
        }
    }
}