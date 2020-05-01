using System.Collections.Generic;
using UnityEngine;

public class MazeSystem : MonoBehaviour
{
    public bool[,] obstacleMatrix;
    public struct hide
    {
        public bool exist;
        public Vector3 checkingRot;
        public Vector2Int checkingPos;
        public Transform wallTransform;
        public bool containsPlayer;
        public hide(Vector2Int CheckingPos, Vector3 CheckingRot, Transform Transform)
        {
            checkingRot = CheckingRot;
            checkingPos = CheckingPos;
            wallTransform = Transform;
            containsPlayer = false;
            exist = true;
        }
    }
    public hide[,] hideMatrix;
    public List<Renderer>[,] rendMatrix;
    public List<Transform> AIs;

    //Prefabs
    public GameObject wallBlockPrefab;
    public GameObject floorPrefab;
    public GameObject AIPrefab; 
    public GameObject chestPrefab;
    public GameObject[] wallPrefabs;
    public GameObject debugPrefab;

    public bool debugMazePath;

    void Start()
    {
        GameParameters.maze.mazeSize += GameParameters.maze.mazeSize % 2;
        hideMatrix = new hide[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
        rendMatrix = new List<Renderer>[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
        for (int x = 0; x <= GameParameters.maze.mazeSize; x++) for (int y = 0; y <= GameParameters.maze.mazeSize; y++) rendMatrix[x, y] = new List<Renderer>();
        obstacleMatrix = GenerateBoolMaze(GameParameters.maze.mazeSize);
        BuildMaze(obstacleMatrix, GameParameters.maze.mazeSize);
        SpawnChests(FindDeadEnds(obstacleMatrix), GameParameters.maze.chestCount);
        SpawnAIs(GameParameters.maze.aiCount);
    }

    private void BuildMaze(bool[,] obstacleMatrix, int mazeSize)
    {
        GameObject Floor = Instantiate(floorPrefab, new Vector3((mazeSize - 1) / 2, 0, (mazeSize - 1) / 2), Quaternion.identity);
        Floor.transform.localScale = new Vector3(mazeSize / 1f, 1, mazeSize / 1f);
        Floor.GetComponent<Renderer>().material.mainTextureScale = new Vector2(mazeSize * 1.2f, mazeSize * 1.2f);

        //BUILD MAZE
        for (int x = 0; x <= mazeSize; x++) for (int y = 0; y <= mazeSize; y++)
        {
            Instantiate(wallBlockPrefab, new Vector3(x, 0, y), Quaternion.identity, transform).transform.GetComponent<MazeBlock>().Build(obstacleMatrix, debugMazePath, this);
        }
    }

    private bool[,] GenerateBoolMaze(int size)
    {

        List<Vector2Int> nearbyBlocks = new List<Vector2Int>();
        bool[,] returnMaze = new bool[size + 1, size + 1];
        for (int x = 0; x <= size; x++) for (int y = 0; y <= size; y++) returnMaze[x, y] = true;
        Vector2Int pos = new Vector2Int(Random.Range(0, size), Random.Range(0, size));
        pos = new Vector2Int(pos.x + (pos.x + 1) % 2, pos.y + (pos.y + 1) % 2);    
        returnMaze[pos.x, pos.y] = false;
        foreach (Vector2Int checkedDir in CheckNearbyBlocks(pos, returnMaze, true, size, 2)) if (!nearbyBlocks.Contains(pos + checkedDir * 2)) nearbyBlocks.Add(pos + checkedDir * 2);
        while (nearbyBlocks.Count > 0)
        {
            pos = nearbyBlocks[Random.Range(0, nearbyBlocks.Count)];
            nearbyBlocks.Remove(pos);
            returnMaze[pos.x, pos.y] = false;
            List<Vector2Int> tempSurround = CheckNearbyBlocks(pos, returnMaze, false, size, 2);
            Vector2Int pickedTempSurround = tempSurround[Random.Range(0, tempSurround.Count)];
            returnMaze[pos.x + pickedTempSurround.x, pos.y + pickedTempSurround.y] = false;
            foreach (Vector2Int checkedDir in CheckNearbyBlocks(pos, returnMaze, true, size, 2)) if (!nearbyBlocks.Contains(pos + checkedDir * 2)) nearbyBlocks.Add(pos + checkedDir * 2);
        }
        return returnMaze;
    }

    private List<Vector2Int> CheckNearbyBlocks(Vector2Int pos, bool[,] checkMatrix, bool searchFor, int size, int range)
    {
        List<Vector2Int> returnList = new List<Vector2Int>();
        if (pos.x - range >= 0 && checkMatrix[pos.x - range, pos.y] == searchFor) returnList.Add(Vector2Int.left);
        if (pos.y - range >= 0 && checkMatrix[pos.x, pos.y - range] == searchFor) returnList.Add(Vector2Int.down);
        if (pos.x + range < size && checkMatrix[pos.x + range, pos.y] == searchFor) returnList.Add(Vector2Int.right);
        if (pos.y + range < size && checkMatrix[pos.x, pos.y + range] == searchFor) returnList.Add(Vector2Int.up);
        return returnList;
    }

    private List<Vector2Int> FindDeadEnds(bool[,] obstacleMatrix)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int y = 1; y < GameParameters.maze.mazeSize; y++)
        {
            for (int x = 1; x < GameParameters.maze.mazeSize; x++)
            {
                if (!obstacleMatrix[x, y] && CheckNearbyBlocks(new Vector2Int(x,y), obstacleMatrix, false, GameParameters.maze.mazeSize, 1).Count == 1) deadEnds.Add(new Vector2Int(x, y));
            }
        }
        deadEnds.Remove(new Vector2Int(1,1));
        return deadEnds;
    }

    private void SpawnChests(List<Vector2Int> deadEnds, int numberOfChests)
    {
        int randIndex;
        for (int i = 0; i < 3; i++)
        {
            if (deadEnds.Count == 0) break;
            randIndex = Random.Range(0, deadEnds.Count);
            CreateChest(deadEnds[randIndex], (Chest.Items)i);
            deadEnds.RemoveAt(randIndex);            
        }
        for (int i = 3; i < numberOfChests; i++)
        {
            if (deadEnds.Count == 0) break;
            randIndex = Random.Range(0, deadEnds.Count);
            CreateChest(deadEnds[randIndex], (Chest.Items)Random.Range(3,9));
            deadEnds.RemoveAt(randIndex);         
        }
    }

    private void CreateChest(Vector2Int pos, Chest.Items item)
    {
        Transform chest;
        Vector2Int dir = CheckNearbyBlocks(pos, obstacleMatrix, false, GameParameters.maze.mazeSize, 1)[0];
        chest = Instantiate(chestPrefab, new Vector3(pos.x, 0, pos.y), Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up) * Quaternion.Euler(0,180,0), transform).transform;
        chest.GetComponent<Chest>().chestContent = item;
        rendMatrix[pos.x, pos.y].AddRange(chest.GetComponentsInChildren<Renderer>());
        obstacleMatrix[pos.x, pos.y] = true;
    }

    private void SpawnAIs(int numberOfAIs)
    {
        for (int x = 0; x < numberOfAIs; x++)
        {
            AIs.Add(Instantiate(AIPrefab, randomPos(GameParameters.maze.mazeSize, obstacleMatrix), Quaternion.identity).transform);
        }
    }

    Vector3 randomPos(int mazeSize, bool[,] obstacleMatrix)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(Random.Range(1, mazeSize), Random.Range(1, mazeSize));
        } while (obstacleMatrix[pos.x, pos.y]);
        return new Vector3(pos.x, 0, pos.y);
    }
}
