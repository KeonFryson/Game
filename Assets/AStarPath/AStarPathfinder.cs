using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] private Vector3 gridWorldSize = new Vector3(50f, 10f, 50f);
    [SerializeField] private float nodeRadius = 0.2f;
    [SerializeField] private LayerMask unwalkableMask;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private float heightAboveGround = 5f;

    [Header("Performance")]
    [SerializeField] private int maxIterations = 1000;
    [SerializeField] private bool disableAutoGridUpdate = false;
    [SerializeField] private bool useIncrementalUpdates = true;
    [SerializeField] private int nodesPerFrameUpdate = 100; // Spread updates across frames

    [Header("Debug")]
    [SerializeField] private bool displayGridGizmos = false;
    [SerializeField][Range(0f, 1f)] private float gridAlpha = 0.3f;
    [SerializeField][Range(0f, 1f)] private float redGridAlpha = 0.3f;

    private PathNode[,,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY, gridSizeZ;
    private float gridUpdateTimer = 0f;
    private const float GRID_UPDATE_INTERVAL = 5f;

    // Optimization: Cache ground heights to avoid repeated raycasts
    private Dictionary<Vector2Int, float> groundHeightCache;
    private Vector3 worldBottomLeft;
    
    // Incremental update tracking
    private bool isUpdatingGrid = false;
    private int updateXIndex = 0;
    private int updateZIndex = 0;

    // Cached values to avoid repeated calculations
    private float nodeRadiusExpanded;

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
        nodeRadiusExpanded = nodeRadius * 1.1f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);
        
        groundHeightCache = new Dictionary<Vector2Int, float>(gridSizeX * gridSizeZ);
        worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2 - Vector3.forward * gridWorldSize.z / 2;
        
        CreateGrid();
    }

    private void Update()
    {
        if (disableAutoGridUpdate) return;

        if (useIncrementalUpdates && isUpdatingGrid)
        {
            UpdateGridIncremental();
        }
        else
        {
            gridUpdateTimer += Time.deltaTime;

            if (gridUpdateTimer >= GRID_UPDATE_INTERVAL)
            {
                if (useIncrementalUpdates)
                {
                    StartIncrementalUpdate();
                }
                else
                {
                    CreateGrid();
                }
                gridUpdateTimer = 0f;
            }
        }
    }

    private void StartIncrementalUpdate()
    {
        isUpdatingGrid = true;
        updateXIndex = 0;
        updateZIndex = 0;
        groundHeightCache.Clear();
    }

    private void UpdateGridIncremental()
    {
        int nodesProcessed = 0;

        while (nodesProcessed < nodesPerFrameUpdate && updateXIndex < gridSizeX)
        {
            UpdateGridColumn(updateXIndex, updateZIndex);
            nodesProcessed++;

            updateZIndex++;
            if (updateZIndex >= gridSizeZ)
            {
                updateZIndex = 0;
                updateXIndex++;
            }
        }

        if (updateXIndex >= gridSizeX)
        {
            isUpdatingGrid = false;
        }
    }

    private void UpdateGridColumn(int x, int z)
    {
        Vector3 horizontalPos = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
        Vector2Int gridPos = new Vector2Int(x, z);

        // Check cache first
        float groundHeight;
        if (!groundHeightCache.TryGetValue(gridPos, out groundHeight))
        {
            Vector3 rayStart = new Vector3(horizontalPos.x, transform.position.y + gridWorldSize.y, horizontalPos.z);
            
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, maxRaycastDistance, groundLayerMask))
            {
                groundHeight = hit.point.y;
                groundHeightCache[gridPos] = groundHeight;
            }
            else
            {
                groundHeight = float.MinValue; // Mark as no ground
                groundHeightCache[gridPos] = groundHeight;
            }
        }

        // Create nodes for this column
        if (groundHeight > float.MinValue)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = new Vector3(horizontalPos.x, groundHeight + y * nodeDiameter, horizontalPos.z);

                if (y * nodeDiameter <= heightAboveGround)
                {
                    bool walkable = !Physics.CheckSphere(worldPoint + Vector3.up * nodeRadius, nodeRadiusExpanded, unwalkableMask);
                    grid[x, y, z] = new PathNode(walkable, worldPoint, x, y, z, true);
                }
                else
                {
                    grid[x, y, z] = new PathNode(false, worldPoint, x, y, z, false);
                }
            }
        }
        else
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
                grid[x, y, z] = new PathNode(false, worldPoint, x, y, z, false);
            }
        }
    }

    private void CreateGrid()
    {
        grid = new PathNode[gridSizeX, gridSizeY, gridSizeZ];
        groundHeightCache.Clear();

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                UpdateGridColumn(x, z);
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

        // 3D distance calculation using optimized Chebyshev distance
        if (dstX > dstY)
        {
            if (dstX > dstZ)
            {
                // X is largest
                if (dstY > dstZ)
                    return 17 * dstZ + 14 * (dstY - dstZ) + 10 * (dstX - dstY);
                else
                    return 17 * dstY + 14 * (dstZ - dstY) + 10 * (dstX - dstY);
            }
            else
            {
                // Z is largest
                if (dstX > dstY)
                    return 17 * dstY + 14 * (dstX - dstY) + 10 * (dstZ - dstX);
                else
                    return 17 * dstX + 14 * (dstY - dstX) + 10 * (dstZ - dstY);
            }
        }
        else
        {
            if (dstY > dstZ)
            {
                // Y is largest
                if (dstX > dstZ)
                    return 17 * dstZ + 14 * (dstX - dstZ) + 10 * (dstY - dstX);
                else
                    return 17 * dstX + 14 * (dstZ - dstX) + 10 * (dstY - dstZ);
            }
            else
            {
                // Z is largest
                return 17 * dstX + 14 * (dstY - dstX) + 10 * (dstZ - dstY);
            }
        }
    }

    private List<PathNode> GetNeighbors(PathNode node)
    {
        List<PathNode> neighbors = new List<PathNode>(26);

        int nodeX = node.gridX;
        int nodeY = node.gridY;
        int nodeZ = node.gridZ;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    int checkX = nodeX + x;
                    int checkY = nodeY + y;
                    int checkZ = nodeZ + z;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY && checkZ >= 0 && checkZ < gridSizeZ)
                    {
                        PathNode neighbor = grid[checkX, checkY, checkZ];

                        // Skip diagonal movement if it would cut through walls
                        if ((x != 0 && z != 0) || (x != 0 && y != 0) || (y != 0 && z != 0))
                        {
                            // Check if axis-aligned neighbors are walkable (prevent corner cutting)
                            if ((x != 0 && !grid[nodeX + x, nodeY, nodeZ].walkable) ||
                                (y != 0 && !grid[nodeX, nodeY + y, nodeZ].walkable) ||
                                (z != 0 && !grid[nodeX, nodeY, nodeZ + z].walkable))
                            {
                                continue;
                            }
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
        Vector3 relativePos = worldPosition - worldBottomLeft;

        float percentX = Mathf.Clamp01(relativePos.x / gridWorldSize.x);
        float percentY = Mathf.Clamp01(relativePos.y / gridWorldSize.y);
        float percentZ = Mathf.Clamp01(relativePos.z / gridWorldSize.z);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);

        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY && z >= 0 && z < gridSizeZ)
        {
            return grid[x, y, z];
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, gridWorldSize);

        if (grid != null && displayGridGizmos)
        {
            foreach (PathNode node in grid)
            {
                if (node == null || !node.hasGroundSupport) continue;

                Gizmos.color = node.walkable ? new Color(1f, 1f, 1f, gridAlpha) : new Color(1f, 0f, 0f, redGridAlpha);
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }

    [ContextMenu("Regenerate Grid")]
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
    public bool hasGroundSupport;

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