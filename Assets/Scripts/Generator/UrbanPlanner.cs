using System.Collections.Generic;
using UnityEngine;

public class UrbanPlanner : GreenField
{
    // ===================================================================
    // CONFIGURATION
    // ===================================================================
    [Header("Player Spawn")]
    public Vector3 playerSpawnPosition = Vector3.zero;

    [Header("Generation Settings")]
    public int targetRooms = 20;
    public int targetHallways = 15;
    // Add more structure types here:
    // public int targetGrandHalls = 3;
    // public int targetBossRooms = 1;

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

    [Header("Connection Settings")]
    public int connectionBuffer = 2;

    // ===================================================================
    // DATA STRUCTURES
    // ===================================================================

    private class Structure
    {
        public int id;
        public string type; // "Room", "Hallway", "GrandHall", "BossRoom", etc.
        public int width, height, depth;
        public Vector3Int position;
        public List<int> connectedTo = new List<int>();
        public List<DoorData> doors = new List<DoorData>();

        // Customizable values for different structure types
        public int wallValue = 3;
        public int interiorValue = 2;
        public bool isBossRoom = false;
        public bool isTreasureRoom = false;

        public Structure(int id, bool isHallway, int w, int h, int d)
        {
            this.id = id;
            this.type = isHallway ? "Hallway" : "Room";
            this.width = w;
            this.height = h;
            this.depth = d;

            if (isHallway)
            {
                wallValue = 7;
                interiorValue = 6;
            }
        }

        // Constructor for custom types
        public Structure(int id, string type, int w, int h, int d)
        {
            this.id = id;
            this.type = type;
            this.width = w;
            this.height = h;
            this.depth = d;
        }
    }

    private class DoorData
    {
        public int direction; // 0=North, 1=South, 2=East, 3=West
        public int connectsToStructureId; // Which structure this door leads to
    }

    private List<Structure> allStructures = new List<Structure>();
    private int roomCount = 0;
    private int hallwayCount = 0;

    // ===================================================================
    // MAIN GENERATION PIPELINE
    // ===================================================================

    public override void GenerateFieldData()
    {
        Debug.Log("=== URBAN PLANNER: Graph-based generation starting ===");

        // Clear previous data
        allStructures.Clear();
        roomCount = 0;
        hallwayCount = 0;

        // Step 1: Fill world with walls
        SetWall();

        // Step 2: Add floor layer
        SetFloor();

        // Step 3: Generate connected structures
        GenerateConnectedStructures();

        // Step 4: Find player spawn
        FindPlayerSpawnPosition();

        Debug.Log($"Generation complete: {allStructures.Count} structures placed");
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
    // GENERATION PIPELINE
    // ===================================================================

    void GenerateConnectedStructures()
    {
        // STEP 1: Create all structures (each type separately)
        CreateRooms();
        CreateHallways();
        // Easy to add more types:
        // CreateGrandHalls();
        // CreateBossRooms();
        // CreateTreasureRooms();

        // STEP 2: Build the connection graph (tree structure)
        BuildConnectionGraph();

        // STEP 3: Place structures spatially based on connections
        PlaceStructuresSpatially();

        // STEP 4: Add doors between connected structures
        AddDoorsToConnections();

        // STEP 5: Carve everything into the world
        CarveAllStructures();

        Debug.Log($"Generated {allStructures.Count} structures (Rooms: {roomCount}, Hallways: {hallwayCount})");
    }

    // ===================================================================
    // STRUCTURE CREATION (One function per type)
    // ===================================================================

    void CreateRooms()
    {
        Debug.Log($"=== Creating {targetRooms} rooms ===");

        for (int i = 0; i < targetRooms; i++)
        {
            int w = Random.Range(roomWidthRange.x, roomWidthRange.y);
            int h = Random.Range(roomHeightRange.x, roomHeightRange.y);
            int d = Random.Range(roomDepthRange.x, roomDepthRange.y);

            Structure room = new Structure(allStructures.Count, false, w, h, d);
            allStructures.Add(room);
            roomCount++;
        }

        Debug.Log($"Created {roomCount} rooms");
    }

    void CreateHallways()
    {
        Debug.Log($"=== Creating {targetHallways} hallways ===");

        for (int i = 0; i < targetHallways; i++)
        {
            int w = Random.Range(hallWidthRange.x, hallWidthRange.y);
            int h = Random.Range(hallHeightRange.x, hallHeightRange.y);
            int d = Random.Range(hallDepthRange.x, hallDepthRange.y);

            Structure hallway = new Structure(allStructures.Count, true, w, h, d);
            allStructures.Add(hallway);
            hallwayCount++;
        }

        Debug.Log($"Created {hallwayCount} hallways");
    }

    // Example: Add more structure types following this pattern
    /*
    void CreateGrandHalls()
    {
        Debug.Log($"=== Creating {targetGrandHalls} grand halls ===");
        
        for (int i = 0; i < targetGrandHalls; i++)
        {
            int w = 12; // Larger fixed size
            int h = 8;
            int d = 12;
            
            Structure grandHall = new Structure(allStructures.Count, "GrandHall", w, h, d);
            grandHall.wallValue = 9;
            grandHall.interiorValue = 10;
            allStructures.Add(grandHall);
        }
    }

    void CreateBossRooms()
    {
        Debug.Log($"=== Creating {targetBossRooms} boss rooms ===");
        
        for (int i = 0; i < targetBossRooms; i++)
        {
            int w = 15;
            int h = 10;
            int d = 15;
            
            Structure bossRoom = new Structure(allStructures.Count, "BossRoom", w, h, d);
            bossRoom.wallValue = 11;
            bossRoom.interiorValue = 12;
            bossRoom.isBossRoom = true;
            allStructures.Add(bossRoom);
        }
    }
    */

    // ===================================================================
    // CONNECTION GRAPH (Tree Structure)
    // ===================================================================

    void BuildConnectionGraph()
    {
        Debug.Log("=== Building connection graph ===");

        if (allStructures.Count == 0) return;

        List<Structure> unconnected = new List<Structure>(allStructures);
        List<Structure> connected = new List<Structure>();

        // Start with first structure as root
        Structure root = unconnected[0];
        unconnected.RemoveAt(0);
        connected.Add(root);

        // Connect remaining structures in a branching tree pattern
        while (unconnected.Count > 0)
        {
            // Pick a random already-connected structure to branch from
            Structure parent = connected[Random.Range(0, connected.Count)];

            // Pick a random unconnected structure to attach
            Structure child = unconnected[0];
            unconnected.RemoveAt(0);

            // Create bidirectional connection
            parent.connectedTo.Add(child.id);
            child.connectedTo.Add(parent.id);

            connected.Add(child);

            Debug.Log($"Connected structure {child.id} ({child.type}) to {parent.id} ({parent.type})");
        }
    }

    // ===================================================================
    // SPATIAL PLACEMENT
    // ===================================================================

    void PlaceStructuresSpatially()
    {
        Debug.Log("=== Placing structures spatially ===");

        if (allStructures.Count == 0) return;

        // Start first structure at world center
        Structure root = allStructures[0];
        root.position = new Vector3Int(
            width / 2 - root.width / 2,
            1,
            depth / 2 - root.depth / 2
        );

        HashSet<int> placed = new HashSet<int> { 0 };
        Queue<int> toPlace = new Queue<int>();
        toPlace.Enqueue(0);

        // Place structures breadth-first from connections
        while (toPlace.Count > 0)
        {
            int currentId = toPlace.Dequeue();
            Structure current = allStructures[currentId];

            foreach (int connectedId in current.connectedTo)
            {
                if (placed.Contains(connectedId)) continue;

                Structure child = allStructures[connectedId];

                // Try to place child next to current
                if (TryPlaceNextTo(current, child))
                {
                    placed.Add(connectedId);
                    toPlace.Enqueue(connectedId);
                    Debug.Log($"Placed structure {child.id} ({child.type}) at {child.position}");
                }
                else
                {
                    Debug.LogWarning($"Failed to place structure {child.id} ({child.type})");
                }
            }
        }

        Debug.Log($"Placed {placed.Count}/{allStructures.Count} structures");
    }

    bool TryPlaceNextTo(Structure parent, Structure child)
    {
        // Try all 4 directions
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        ShuffleList(directions);

        foreach (int direction in directions)
        {
            Vector3Int pos = CalculatePositionNextTo(parent, direction, child.width, child.depth);

            if (IsPositionValidForStructure(pos, child.width, child.height, child.depth))
            {
                child.position = pos;
                return true;
            }
        }

        return false;
    }

    Vector3Int CalculatePositionNextTo(Structure parent, int direction, int childW, int childD)
    {
        int spacing = 2 + connectionBuffer;
        int x = 0, y = parent.position.y, z = 0;

        switch (direction)
        {
            case 0: // North
                x = parent.position.x + parent.width / 2 - childW / 2;
                z = parent.position.z + parent.depth + spacing;
                break;
            case 1: // South
                x = parent.position.x + parent.width / 2 - childW / 2;
                z = parent.position.z - childD - spacing;
                break;
            case 2: // East
                x = parent.position.x + parent.width + spacing;
                z = parent.position.z + parent.depth / 2 - childD / 2;
                break;
            case 3: // West
                x = parent.position.x - childW - spacing;
                z = parent.position.z + parent.depth / 2 - childD / 2;
                break;
        }

        return new Vector3Int(x, y, z);
    }

    bool IsPositionValidForStructure(Vector3Int pos, int w, int h, int d)
    {
        // Check bounds
        if (pos.x < 1 || pos.x + w >= width - 1) return false;
        if (pos.z < 1 || pos.z + d >= depth - 1) return false;
        if (pos.y < 1 || pos.y + h >= height - 1) return false;

        // Check overlap with already-placed structures
        foreach (Structure s in allStructures)
        {
            if (s.position == Vector3Int.zero) continue; // Not placed yet

            bool overlap = !(pos.x + w + connectionBuffer < s.position.x ||
                            pos.x > s.position.x + s.width + connectionBuffer ||
                            pos.z + d + connectionBuffer < s.position.z ||
                            pos.z > s.position.z + s.depth + connectionBuffer);

            if (overlap) return false;
        }

        return true;
    }

    // ===================================================================
    // DOOR PLACEMENT
    // ===================================================================

    void AddDoorsToConnections()
    {
        Debug.Log("=== Adding doors ===");

        foreach (Structure s1 in allStructures)
        {
            if (s1.position == Vector3Int.zero) continue; // Skip unplaced

            foreach (int connectedId in s1.connectedTo)
            {
                Structure s2 = allStructures[connectedId];
                if (s2.position == Vector3Int.zero) continue;

                // Check if we already added a door for this connection
                bool alreadyHasDoor = false;
                foreach (DoorData door in s1.doors)
                {
                    if (door.connectsToStructureId == s2.id)
                    {
                        alreadyHasDoor = true;
                        break;
                    }
                }

                if (alreadyHasDoor) continue;

                // Determine direction from s1 to s2
                int direction = GetDirectionBetween(s1, s2);
                if (direction == -1) continue;

                // Add door to s1
                s1.doors.Add(new DoorData { direction = direction, connectsToStructureId = s2.id });
                Debug.Log($"Added door: Structure {s1.id} -> {s2.id} (direction {direction})");
            }
        }
    }

    int GetDirectionBetween(Structure from, Structure to)
    {
        int fromCenterX = from.position.x + from.width / 2;
        int fromCenterZ = from.position.z + from.depth / 2;
        int toCenterX = to.position.x + to.width / 2;
        int toCenterZ = to.position.z + to.depth / 2;

        int deltaX = toCenterX - fromCenterX;
        int deltaZ = toCenterZ - fromCenterZ;

        if (Mathf.Abs(deltaZ) > Mathf.Abs(deltaX))
        {
            return deltaZ > 0 ? 0 : 1; // North or South
        }
        else
        {
            return deltaX > 0 ? 2 : 3; // East or West
        }
    }

    // ===================================================================
    // CARVING INTO WORLD
    // ===================================================================

    void CarveAllStructures()
    {
        Debug.Log("=== Carving structures ===");

        foreach (Structure s in allStructures)
        {
            if (s.position == Vector3Int.zero) continue; // Skip unplaced

            // Carve the structure
            Carve(s.position.x, s.position.y, s.position.z, s.width, s.height, s.depth,
                  s.wallValue, s.interiorValue);

            // Carve doors and corridors
            foreach (DoorData door in s.doors)
            {
                CarveDoor(s.position.x, s.position.y, s.position.z, s.width, s.depth, door.direction);

                Structure connected = allStructures[door.connectsToStructureId];
                if (connected.position == Vector3Int.zero) continue;

                Vector3Int doorPos1 = GetDoorPosition(s.position.x, s.position.y, s.position.z, s.width, s.depth, door.direction);
                int oppDir = GetOppositeDirection(door.direction);
                Vector3Int doorPos2 = GetDoorPosition(connected.position.x, connected.position.y, connected.position.z, connected.width, connected.depth, oppDir);

                CarveDirectConnection(doorPos1, doorPos2);
            }
        }
    }

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

    void CarveDirectConnection(Vector3Int from, Vector3Int to)
    {
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
    // UTILITY FUNCTIONS
    // ===================================================================

    void FindPlayerSpawnPosition()
    {
        if (allStructures.Count == 0)
        {
            playerSpawnPosition = new Vector3(width / 2, 2, depth / 2);
            return;
        }

        Structure spawnRoom = allStructures[0]; // Spawn in first structure
        playerSpawnPosition = new Vector3(
            spawnRoom.position.x + spawnRoom.width / 2f,
            spawnRoom.position.y + 0.5f,
            spawnRoom.position.z + spawnRoom.depth / 2f
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
    // GIZMO VISUALIZATION (Debug)
    // ===================================================================

    void OnDrawGizmos()
    {
        if (GreenFieldData == null) return;

        // Draw voxel data
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
                           //; Gizmos.color = new Color(1, 0, 0, 0.3f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;
                        case 3: // Room walls
                           // Gizmos.color = new Color(1, 1, 0, 0.5f);
                            //Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;
                        case 5: // Doors
                           // Gizmos.color = Color.magenta;
                            //Gizmos.DrawCube(pos, Vector3.one * 0.7f);
                            break;
                        case 6: // Hallway interior
                           // Gizmos.color = Color.cyan;
                           // Gizmos.DrawCube(pos, Vector3.one * 0.6f);
                            break;
                        case 7: // Hallway walls
                           // Gizmos.color = new Color(0, 0.5f, 1, 0.5f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.85f);
                            break;
                        case 8: // Corridors
                           // Gizmos.color = new Color(0, 1, 0, 0.6f);
                           // Gizmos.DrawCube(pos, Vector3.one * 0.5f);
                            break;
                    }
                }
            }
        }

        // Draw structure bounds and connections
        if (allStructures != null)
        {
            foreach (Structure s in allStructures)
            {
                if (s.position == Vector3Int.zero) continue;

                // Draw structure bounding box
                Vector3 center = new Vector3(
                    s.position.x + s.width / 2f,
                    s.position.y + s.height / 2f,
                    s.position.z + s.depth / 2f
                );
                Vector3 size = new Vector3(s.width, s.height, s.depth);

                // Different colors for different types
                if (s.type == "Room")
                    Gizmos.color = new Color(1, 0, 0, 0.3f);
                else if (s.type == "Hallway")
                    Gizmos.color = new Color(0, 1, 1, 0.3f);
                else if (s.type == "BossRoom")
                    Gizmos.color = new Color(1, 0, 1, 0.3f);
                else
                    Gizmos.color = new Color(0, 1, 0, 0.3f);

                Gizmos.DrawWireCube(center, size);

                // Draw connection lines
                Gizmos.color = Color.yellow;
                foreach (int connectedId in s.connectedTo)
                {
                    if (connectedId < allStructures.Count)
                    {
                        Structure connected = allStructures[connectedId];
                        if (connected.position == Vector3Int.zero) continue;

                        Vector3 connectedCenter = new Vector3(
                            connected.position.x + connected.width / 2f,
                            connected.position.y + connected.height / 2f,
                            connected.position.z + connected.depth / 2f
                        );

                        Gizmos.DrawLine(center, connectedCenter);
                    }
                }

                // Draw doors as spheres
                Gizmos.color = Color.magenta;
                foreach (DoorData door in s.doors)
                {
                    Vector3Int doorPos = GetDoorPosition(s.position.x, s.position.y, s.position.z, s.width, s.depth, door.direction);
                    Gizmos.DrawSphere(doorPos, 0.5f);
                }
            }
        }
    }
}
