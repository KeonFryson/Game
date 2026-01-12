using UnityEngine;
using UnityEngine.UIElements;

public class Crawler : Maze
{
    public override void GenerateMazeData()
    {
        //bool done = false;

        // Start in the center
        int x = width / 2;
        int y = height / 2;
        int z = depth / 2;

        // Make center a path
        mazeData[x, y, z] = 2;

   
    }
}
