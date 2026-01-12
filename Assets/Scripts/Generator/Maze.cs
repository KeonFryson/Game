using UnityEngine;

public class Maze : MonoBehaviour
{
    public GameObject Cube; 
    public int width = 10;
    public int height = 10;
    public int depth = 10;
    private int[,,] mazeData;
    
    void Start()
    {
        InitializMap();
        GenerateMazeData();
        InstantiateMaze();
    }
    
    void InitializMap()
    {
        // Ensure at least 1 in each dimension
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);
        
        mazeData = new int[w, h, d];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < d; z++)
                {
                    mazeData[x, y, z] = 0; //0= floor, 1=wall, 2=path,
                }
            }
        }
    }
    
    void GenerateMazeData()
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);
        
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < d; z++)
                {
                    mazeData[x, y, z] = 0; //0= floor, 1=wall, 2=path,
                }
            }
        }
    }
    
    void InstantiateMaze()
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);
        
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < d; z++)
                {
                    Vector3 position = new Vector3(x, y, z);
                    if (mazeData[x, y, z] == 1)
                    {
                        Instantiate(Cube, position, Quaternion.identity);
                    }
                }
            }
        }
    }
    
    void Update()
    {
        
    }
}