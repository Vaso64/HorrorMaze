using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MazeBlock : MonoBehaviour
{
    private MazeSystem maze;
    private Renderer debugRend;
    private Vector2Int pos;
    private bool hideAllowed;

    public void Build(bool[,] obstacleMemory, bool debugMazePath, MazeSystem mazeRef)
    {
        maze = mazeRef;
        pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        if (obstacleMemory[pos.x, pos.y])
        {
            //Create ground
            transform.Find("Ground").gameObject.SetActive(false);

            //Check for nearby hides
            hideAllowed = CheckNearbyHides(pos, 1);

            //Spawn walls / hides
            foreach (Vector3 direction in AllowedDirection(obstacleMemory, pos)) SpawnWall(direction, Random.Range(0,100) < GameParameters.maze.hideDensity && hideAllowed);

            //Spawn debug wall
            if (Application.isEditor && debugMazePath) debugRend = Instantiate(maze.debugPrefab, transform.position + new Vector3(0, 2, 0), Quaternion.Euler(0, 0, 0), transform).GetComponent<Renderer>();
        }            
    }

    List<Vector3> AllowedDirection(bool[,] obstacleMemory, Vector2Int pos)
    {
        List<Vector3> allowedDirection = new List<Vector3>();
        if (pos.x - 1 >= 0 && !obstacleMemory[pos.x - 1, pos.y]) allowedDirection.Add(Vector3.left);
        if (pos.x + 1 < obstacleMemory.GetLength(0) && !obstacleMemory[pos.x + 1, pos.y]) allowedDirection.Add(Vector3.right);
        if (pos.y - 1 >= 0 && !obstacleMemory[pos.x, pos.y - 1]) allowedDirection.Add(Vector3.back);
        if (pos.y + 1 < obstacleMemory.GetLength(1) && !obstacleMemory[pos.x, pos.y + 1]) allowedDirection.Add(Vector3.forward);
        return allowedDirection;
    }

    private void SpawnWall(Vector3 dir, bool hideWall)
    {
        Transform wall;
        if (hideWall)
        {
            wall = Instantiate(maze.wallPrefabs[Random.Range(1, 8)], transform.position + dir / 2, Quaternion.LookRotation(dir), transform).transform;
            maze.hideMatrix[pos.x, pos.y] = new MazeSystem.hide(pos + Vector2Int.RoundToInt(new Vector2(dir.x, dir.y)), (Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180, 0)).eulerAngles, wall);
            hideAllowed = false;
        }
        else
        {
            wall = Instantiate(maze.wallPrefabs[0], transform.position + dir / 2, Quaternion.LookRotation(dir), transform).transform;
        }
        maze.rendMatrix[pos.x, pos.y].AddRange(wall.GetComponentsInChildren<Renderer>());    
    }

    private bool CheckNearbyHides(Vector2Int pos, int range)
    {
        for (int x = Mathf.Clamp(pos.x - range, 0, GameParameters.maze.mazeSize); x <= Mathf.Clamp(pos.x + range, 0, GameParameters.maze.mazeSize); x++)
        {
            for (int y = Mathf.Clamp(pos.y - range, 0, GameParameters.maze.mazeSize); y <= Mathf.Clamp(pos.y + range, 0, GameParameters.maze.mazeSize); y++)
            {
                if (maze.hideMatrix[x, y].exist) return false;
            }
        }
        return true;
    }

    public IEnumerator Highlight(float time, Color color)
    {
        debugRend.material.color = color;
        yield return new WaitForSeconds(time);
        debugRend.material.color = Color.white;
    }
}
