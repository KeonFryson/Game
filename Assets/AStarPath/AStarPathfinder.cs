using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private Vector3 gridWorldSize = new Vector3(50f, 10f, 50f);
    [SerializeField] private float nodeRadius = 0.2f;
    [SerializeField] private LayerMask unwalkableMask;
    [SerializeField] private LayerMask groundLayerMask; // Renamed: Ground layer to detect
    [SerializeField] private float maxRaycastDistance = 100f; // Max distance to raycast down for ground
    [SerializeField] private float heightAboveGround = 5f; // How many nodes to create above ground level
    [SerializeField] private float nodeHeightOffset = 0.5f; // How far above ground to place nodes

    [Header("Performance")]
    [SerializeField] private int maxIterations = 1000; // Prevent infinite loops
    [SerializeField] private bool disableAutoGridUpdate = false; // Option to disable periodic updates

    [Header("Debug")]
    [SerializeField] private bool displayGridGizmos = false;
    [SerializeField][Range(0f, 1f)] private float gridAlpha = 0.3f;
    [SerializeField][Range(0f, 1f)] private float redGridAlpha = 0.3f;

    private PathNode[,,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY, gridSizeZ;
    private float gridUpdateTimer = 0f;
    private const float GRID_UPDATE_INTERVAL = 5f;
    private float minGroundHeight;
    private float maxGroundHeight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);
        CreateGrid();
    }

    private void Update()
    {
        if (disableAutoGridUpdate) return;

        gridUpdateTimer += Time.deltaTime;

        if (gridUpdateTimer >= GRID_UPDATE_INTERVAL)
        {
            CreateGrid();
            gridUpdateTimer = 0f;
        }
    }

    private void CreateGrid()
    {
        grid = new PathNode[gridSizeX, gridSizeY, gridSizeZ];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2 - Vector3.forward * gridWorldSize.z / 2;

        minGroundHeight = float.MaxValue;
        maxGroundHeight = float.MinValue;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                // Calculate horizontal position
                Vector3 horizontalPos = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);

                // Raycast down from high above to find ground
                Vector3 rayStart = new Vector3(horizontalPos.x, transform.position.y + gridWorldSize.y, horizontalPos.z);
                RaycastHit hit;

                if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
                {
                    // Found ground, create nodes starting from above ground level
                    Vector3 groundPoint = hit.point + Vector3.up * nodeHeightOffset;

                    // Track min/max heights for gizmo
                    if (groundPoint.y < minGroundHeight) minGroundHeight = groundPoint.y;
                    if (groundPoint.y > maxGroundHeight) maxGroundHeight = groundPoint.y;

                    // Create nodes from ground level upward
                    for (int y = 0; y < gridSizeY; y++)
                    {
                        Vector3 worldPoint = groundPoint + Vector3.up * (y * nodeDiameter);

                        // Only create nodes up to the specified height above ground
                        if (y * nodeDiameter <= heightAboveGround)
                        {
                            // Check if this position is walkable (not blocked by unwalkable objects)
                            // Use a slightly larger radius for more conservative pathfinding
                            bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius * 1.1f, unwalkableMask);
                            grid[x, y, z] = new PathNode(walkable, worldPoint, x, y, z, true); // Has ground support
                        }
                        else
                        {
                            // Above the height limit, mark as unwalkable
                            grid[x, y, z] = new PathNode(false, worldPoint, x, y, z, false); // No ground support
                        }
                    }
                }
                else
                {
                    // No ground found, create unwalkable nodes at grid positions
                    for (int y = 0; y < gridSizeY; y++)
                    {
                        Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
                        grid[x, y, z] = new PathNode(false, worldPoint, x, y, z, false); // No ground support
                    }
                }
            }
        }
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        PathNode startNode = NodeFromWorldPoint(startPos);
        PathNode targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !targetNode.walkable)
        {
            return null;
        }

        // Use a min-heap (SortedSet with custom comparer) for better performance
        SortedSet<PathNode> openSet = new SortedSet<PathNode>(new PathNodeComparer());
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        // Reset node costs from previous searches
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        startNode.parent = null;

        openSet.Add(startNode);

        int iterations = 0;

        while (openSet.Count > 0)
        {
            // Check iteration limit to prevent freezing
            if (++iterations > maxIterations)
            {
                Debug.LogWarning($"Pathfinding exceeded max iterations ({maxIterations}). Path may be incomplete.");
                return null;
            }

            // Get node with lowest fCost (first in sorted set)
            PathNode currentNode = openSet.Min;
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (PathNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                bool isInOpenSet = openSet.Contains(neighbor);

                if (newMovementCostToNeighbor < neighbor.gCost || !isInOpenSet)
                {
                    // Remove from open set to update (SortedSet doesn't allow updates in place)
                    if (isInOpenSet)
                    {
                        openSet.Remove(neighbor);
                    }

                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<PathNode> path = new List<PathNode>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        List<Vector3> waypoints = new List<Vector3>();
        for (int i = path.Count - 1; i >= 0; i--)
        {
            waypoints.Add(path[i].worldPosition);
        }

        return SimplifyPath(waypoints);
    }

    private List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path.Count <= 2)
            return path;

        List<Vector3> simplifiedPath = new List<Vector3>();
        simplifiedPath.Add(path[0]);

        Vector3 oldDirection = Vector3.zero;

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 newDirection = (path[i] - path[i - 1]).normalized;

            // Check if there's a direct line of sight to skip intermediate points
            bool hasLineOfSight = i < path.Count - 1 && HasLineOfSight(simplifiedPath[simplifiedPath.Count - 1], path[i + 1]);

            if (newDirection != oldDirection || !hasLineOfSight)
            {
                simplifiedPath.Add(path[i - 1]);
            }
            oldDirection = newDirection;
        }

        simplifiedPath.Add(path[path.Count - 1]);
        return simplifiedPath;
    }

    private bool HasLineOfSight(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        // Check if there's a clear path between points
        return !Physics.SphereCast(start, nodeRadius * 0.9f, direction.normalized, out RaycastHit hit, distance, unwalkableMask);
    }

    private int GetDistance(PathNode nodeA, PathNode nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        int dstZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        // 3D distance calculation using Chebyshev distance approximation
        int[] distances = { dstX, dstY, dstZ };
        System.Array.Sort(distances);

        return 17 * distances[0] + 14 * (distances[1] - distances[0]) + 10 * (distances[2] - distances[1]);
    }

    private List<PathNode> GetNeighbors(PathNode node)
    {
        List<PathNode> neighbors = new List<PathNode>(26); // Pre-allocate capacity

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    int checkZ = node.gridZ + z;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY && checkZ >= 0 && checkZ < gridSizeZ)
                    {
                        PathNode neighbor = grid[checkX, checkY, checkZ];

                        // Skip diagonal movement if it would cut through walls
                        bool isDiagonal = (x != 0 && z != 0) || (x != 0 && y != 0) || (y != 0 && z != 0);

                        if (isDiagonal)
                        {
                            // Check if axis-aligned neighbors are walkable (prevent corner cutting)
                            bool xBlocked = x != 0 && !grid[node.gridX + x, node.gridY, node.gridZ].walkable;
                            bool yBlocked = y != 0 && !grid[node.gridX, node.gridY + y, node.gridZ].walkable;
                            bool zBlocked = z != 0 && !grid[node.gridX, node.gridY, node.gridZ + z].walkable;

                            if (xBlocked || yBlocked || zBlocked)
                                continue; // Skip this diagonal neighbor
                        }

                        neighbors.Add(neighbor);
                    }
                }
            }
        }

        return neighbors;
    }

    private PathNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2 - Vector3.forward * gridWorldSize.z / 2;
        Vector3 relativePos = worldPosition - worldBottomLeft;

        float percentX = relativePos.x / gridWorldSize.x;
        float percentY = relativePos.y / gridWorldSize.y;
        float percentZ = relativePos.z / gridWorldSize.z;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        percentZ = Mathf.Clamp01(percentZ);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);

        if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
        {
            // First, try the calculated node
            if (y >= 0 && y < gridSizeY && grid[x, y, z] != null && grid[x, y, z].walkable)
            {
                return grid[x, y, z];
            }

            // If calculated node is unwalkable or out of bounds, search for nearest walkable node vertically
            for (int yOffset = 0; yOffset < gridSizeY; yOffset++)
            {
                // Check below first
                int checkY = y - yOffset;
                if (checkY >= 0 && checkY < gridSizeY && grid[x, checkY, z] != null && grid[x, checkY, z].walkable && grid[x, checkY, z].hasGroundSupport)
                {
                    return grid[x, checkY, z];
                }

                // Then check above
                checkY = y + yOffset;
                if (checkY >= 0 && checkY < gridSizeY && grid[x, checkY, z] != null && grid[x, checkY, z].walkable && grid[x, checkY, z].hasGroundSupport)
                {
                    return grid[x, checkY, z];
                }
            }

            // Fallback: return the originally calculated node even if unwalkable (pathfinding will handle it)
            if (y >= 0 && y < gridSizeY)
            {
                return grid[x, y, z];
            }
        }

        return null;
    }



    private void OnDrawGizmos()
    {
        // Draw grid box that matches the single layer of nodes
        if (grid != null && minGroundHeight != float.MaxValue && maxGroundHeight != float.MinValue)
        {
            // Calculate the actual height of the node layer
            float layerHeight = nodeDiameter;
            float centerY = (minGroundHeight + maxGroundHeight) / 2f;

            Vector3 boxSize = new Vector3(gridWorldSize.x, layerHeight, gridWorldSize.z);
            Vector3 boxCenter = new Vector3(transform.position.x, centerY, transform.position.z);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
        else
        {
            // Fallback to original box if grid not yet created
            Gizmos.DrawWireCube(transform.position, gridWorldSize);
        }

        if (grid != null && displayGridGizmos)
        {
            foreach (PathNode node in grid)
            {
                if (node == null || !node.hasGroundSupport) continue; // Skip nodes without ground support

                Gizmos.color = node.walkable ? new Color(1f, 1f, 1f, gridAlpha) : new Color(1f, 0f, 0f, redGridAlpha);
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }


    [ContextMenu("Regenerate Grid")]

    // Manual grid refresh method
    public void RefreshGrid()
    {
        CreateGrid();
    }
}

// Comparer for PathNode to use in SortedSet (min-heap behavior)
public class PathNodeComparer : IComparer<PathNode>
{
    public int Compare(PathNode a, PathNode b)
    {
        int compare = a.fCost.CompareTo(b.fCost);
        if (compare == 0)
        {
            compare = a.hCost.CompareTo(b.hCost);
        }
        // If costs are equal, compare by grid position to maintain uniqueness in SortedSet
        if (compare == 0)
        {
            compare = a.gridX.CompareTo(b.gridX);
            if (compare == 0)
            {
                compare = a.gridY.CompareTo(b.gridY);
                if (compare == 0)
                {
                    compare = a.gridZ.CompareTo(b.gridZ);
                }
            }
        }
        return compare;
    }


}

public class PathNode
{
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;
    public int gridZ;
    public bool hasGroundSupport; // Tracks if this node is above actual ground

    public int gCost;
    public int hCost;
    public PathNode parent;

    public PathNode(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gridZ, bool hasGroundSupport)
    {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
        this.gridZ = gridZ;
        this.hasGroundSupport = hasGroundSupport;
    }

    public int fCost
    {
        get { return gCost + hCost; }
    }

}