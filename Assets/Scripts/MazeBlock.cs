using System.Collections.Generic;
using UnityEngine;

public class MazeBlock : MonoBehaviour
{
    public GameObject[] wallPrefabs;
    public Transform hideWall;
    public Vector3[,][] hideArray;
    public List<GameObject> walls = new List<GameObject>();
    public MazeSystem maze;
    public GameObject debugWall;

    public void Build(bool[,] obstacleMemory, bool debugMazePath)
    {
        bool hideAllowed = true;
        Vector2Int pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));

        for (int x = Mathf.Clamp(pos.x - 1, 0, GameParameters.size); x <= Mathf.Clamp(pos.x + 1, 0, GameParameters.size); x++)
        {
            for (int y = Mathf.Clamp(pos.y - 1, 0, GameParameters.size); y <= Mathf.Clamp(pos.y + 1, 0, GameParameters.size); y++)
            {
                if (maze.mazeMatrix[x, y].GetComponent<MazeBlock>().hideWall != null) hideAllowed = false;
            }
        }

        foreach (Vector3 direction in AllowedDirection(obstacleMemory, pos))
        {
            if (direction != Vector3.zero) //Spawn wall / hide
            {
                if (Random.Range(0f, 100f) < GameParameters.hideDensity && hideAllowed && hideWall == null) //Spawn hide
                {
                    walls.Add(Instantiate(wallPrefabs[Random.Range(1, 8)], transform.position + direction / 2, Quaternion.LookRotation(direction), transform));
                    hideWall = walls[walls.Count - 1].transform;
                    Vector2Int hideWallPos = Vector2Int.RoundToInt(new Vector2(hideWall.position.x, hideWall.position.z)) + Vector2Int.RoundToInt(new Vector2(hideWall.transform.forward.x, hideWall.transform.forward.z));
                    if (maze.hideMatrix[hideWallPos.x, hideWallPos.y] == null) maze.hideMatrix[hideWallPos.x, hideWallPos.y] = new List<MazeSystem.hide>();
                    maze.hideMatrix[hideWallPos.x, hideWallPos.y].Add(new MazeSystem.hide(Quaternion.LookRotation(direction).eulerAngles * -1, transform));
                }
                else walls.Add(Instantiate(wallPrefabs[0], transform.position + direction / 2, Quaternion.LookRotation(direction), transform)); //Spawn wall
            }
            else walls.Add(null); //No wall / hide
        }
        if (Application.isEditor && debugMazePath && !obstacleMemory[pos.x, pos.y]) Instantiate(debugWall, transform.position + new Vector3(0, 5, 0), Quaternion.Euler(0, 0, 0), transform);
    }

    List<Vector3> AllowedDirection(bool[,] obstacleMemory, Vector2Int pos)
    {
        List<Vector3> allowedDirection = new List<Vector3>();
        if (pos.x - 1 > 0 && obstacleMemory[pos.x - 1, pos.y]) allowedDirection.Add(Vector3.left);
        else allowedDirection.Add(Vector3.zero);       
        if (pos.x + 1 < obstacleMemory.GetLength(0) && obstacleMemory[pos.x + 1, pos.y]) allowedDirection.Add(Vector3.right);
        else allowedDirection.Add(Vector3.zero);
        if (pos.y - 1 > 0 && obstacleMemory[pos.x, pos.y - 1]) allowedDirection.Add(Vector3.back);
        else allowedDirection.Add(Vector3.zero);
        if (pos.y + 1 < obstacleMemory.GetLength(1) && obstacleMemory[pos.x, pos.y + 1]) allowedDirection.Add(Vector3.forward);
        else allowedDirection.Add(Vector3.zero);
        return allowedDirection;
    }
}
