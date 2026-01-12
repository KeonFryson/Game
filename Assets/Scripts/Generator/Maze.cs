using UnityEngine;

public class Maze : MonoBehaviour
{
    public GameObject floor; 
    public GameObject wall; 
    public GameObject path;


    public int width = 10;
    public int height = 10;
    public int depth = 10;
    public int[,,] mazeData;
    
    void Start()
    {
        InitializeArray();
        GenerateMazeData();
        InstantiateMaze();
    }

    void InitializeArray()
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);

        mazeData = new int[w, h, d];  // Just create it

        // Fill everything with floors (0)
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                for (int z = 0; z < d; z++)
                    mazeData[x, y, z] = 0;  // All floors by default
    }

    public virtual void GenerateMazeData()
    {
        int w = width;
        int h = height;
        int d = depth;

        // Now randomly change some to walls or paths
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < d; z++)
                {
                    int roll = Random.Range(0, 100);
                    if (roll < 35)  // 35% walls
                    {
                        mazeData[x, y, z] = 1;
                    }
                    else if (roll < 55)  // 20% paths
                    {
                        mazeData[x, y, z] = 2;
                    }
                    // else stays 0 (floor)
                }
            }
        }
    }

    void InstantiateMaze()
    {
        // Ensure dimensions are at least 1
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);

        // Instantiate GameObjects based on mazeData
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int z = 0; z < d; z++)
                {
                    Vector3 position = new Vector3(x, y, z);
                    if (mazeData[x, y, z] == 0)
                    {
                        if (mazeData[x, y, z] == 0)  // Floor
                        {
                            //position.y -= 1;  // Subtract 1 from y
                            Instantiate(floor, position, Quaternion.identity);
                        }
                        //Instantiate(floor, floorPosition, Quaternion.identity);
                    }
                    else if (mazeData[x, y, z] == 1)
                    {
                        // Optionally instantiate walls or other objects
                        Instantiate(wall, position, Quaternion.identity);
                    }
                    else if (mazeData[x, y, z] == 2)
                    {
                        // Optionally instantiate paths or other objects
                        Instantiate(path, position, Quaternion.identity);
                    }
                }
            }
        }
    }

    void Update()
    {
        
    }
}