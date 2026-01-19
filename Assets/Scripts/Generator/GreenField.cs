using UnityEngine;

public class GreenField : MonoBehaviour
{
    public GameObject floor;
    public GameObject wall;
    public GameObject path;
    public int width = 10;
    public int height = 10;
    public int depth = 10;
    public int[,,] GreenFieldData;

    void Start()
    {
        //InitializeFieldArray();
       // GenerateFieldData();
       // DrawField();  // Shows random data for debugging
    }

    public void InitializeFieldArray()
    {
        int w = Mathf.Max(1, width);
        int h = Mathf.Max(1, height);
        int d = Mathf.Max(1, depth);

        GreenFieldData = new int[w, h, d];

        // Fill everything with floors by default
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                for (int z = 0; z < d; z++)
                    GreenFieldData[x, y, z] = 0;
    }

    public virtual void GenerateFieldData()
    {
        // Generate RANDOM field data (the raw land)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int roll = Random.Range(0, 100);

                    if (roll < 35)  // 35% walls
                    {
                        GreenFieldData[x, y, z] = 1;
                    }
                    else if (roll < 55)  // 20% paths
                    {
                        GreenFieldData[x, y, z] = 2;
                    }
                    // else stays 0 (floor)
                }
            }
        }
    }

    void DrawField()
    {
        // Visual debugging - shows what's in the array
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

                    if (GreenFieldData[x, y, z] == 0) // Floor
                    {
                        Instantiate(floor, position, Quaternion.identity);
                    }
                    else if (GreenFieldData[x, y, z] == 1) // Wall
                    {
                        Instantiate(wall, position, Quaternion.identity);
                    }
                    else if (GreenFieldData[x, y, z] == 2) // Path
                    {
                        Instantiate(path, position, Quaternion.identity);
                    }
                }
            }
        }
    }
}
