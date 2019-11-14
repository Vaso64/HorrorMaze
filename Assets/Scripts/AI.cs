using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public int Fov = 50;
    public float patrolSpeed;
    public float suspiciousSpeed;
    public float alertedSpeed;
    public float huntingSpeed;
    public float sightRange;
    public float hearRunningRange;
    public float hearWalkRange;
    public bool DisplayAIBehaviour;

    private bool interuptNavigation;
    private MazeSystem maze;
    private bool[,] obstacleMemory;
    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private Quaternion endRot;
    private float t = 0;
    private float tMdf;
    private float nextTMdf;
    private float toWaitTime = 0;
    private Transform player;
    private PlayerController playerBehavior;
    private Vector2Int playerPos = new Vector2Int(1,1);
    private Vector2Int prevPlayerPos;
    private enum Movements { Moving, Rotating, Idle };
    private Movements movement = Movements.Idle;   
    private enum States { Patrol, Suspicious, Alerted, Hunting };
    [SerializeField]
    private States state = States.Patrol;
    private Vector2Int targetPos;
    private Renderer rend;
    private bool sense = true;

    void Start()
    { 
        rend = gameObject.GetComponent<Renderer>();
        player = GameObject.Find("Player").transform;
        playerBehavior = GameObject.Find("Player").GetComponent<PlayerController>();
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        obstacleMemory = maze.obstacleMemory;
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(Random.Range(8, maze.size - 2), Random.Range(8, maze.size));
        } while (!obstacleMemory[pos.x, pos.y]);
        transform.position = new Vector3(pos.x, 1, pos.y);
        ChangeState(States.Patrol, randomPos(maze.size));
        StartCoroutine(PathNavigation());
        StartCoroutine(Sense());
    }

    private void Update()
    {
        Vector2Int pos = Vector2Int.RoundToInt(new Vector2(player.transform.position.x, player.transform.position.z));
        if (playerPos != pos) prevPlayerPos = playerPos;
        playerPos = pos;
        switch (movement)
        {
            case Movements.Idle:
                break;
            case Movements.Moving:
                transform.position = Vector3.Lerp(startPos, endPos, t / tMdf);
                break;
            case Movements.Rotating:
                transform.rotation = Quaternion.Slerp(startRot, endRot, t / tMdf);
                break;
        }
        t += Time.deltaTime;
    }

    IEnumerator PathNavigation()
    {
        while (true)
        {
            if (DisplayAIBehaviour)
            {
                Debug.DrawLine(new Vector3(targetPos.x - 0.5f, 0, targetPos.y - 0.5f), new Vector3(targetPos.x + 0.5f, 0, targetPos.y + 0.5f), Color.red, 4f);
                Debug.DrawLine(new Vector3(targetPos.x - 0.5f, 0, targetPos.y + 0.5f), new Vector3(targetPos.x + 0.5f, 0, targetPos.y - 0.5f), Color.red, 4f);
            }
            List<Vector3> moveOrder = GeneratePath(targetPos);
            tMdf = nextTMdf;
            while (moveOrder.Count > 0)
            {
                if (toWaitTime != 0)
                {
                    yield return new WaitForSeconds(toWaitTime);
                    toWaitTime = 0;
                }
                if (moveOrder[moveOrder.Count - 1] == Vector3.forward) yield return new WaitForSeconds(Move(moveOrder[moveOrder.Count - 1], tMdf));
                else yield return new WaitForSeconds(Rotate(moveOrder[moveOrder.Count - 1], tMdf));
                transform.eulerAngles = new Vector3(Mathf.Round(transform.eulerAngles.x / 90) * 90, Mathf.Round(transform.eulerAngles.y / 90) * 90, Mathf.Round(transform.eulerAngles.z / 90) * 90);
                transform.position = Vector3Int.RoundToInt(transform.position);
                moveOrder.RemoveAt(moveOrder.Count - 1);
                if (interuptNavigation) break;
            }
            if (!interuptNavigation) 
            {
                switch (state)
                {
                    case States.Hunting:
                        foreach (Vector3 rotation in LookAround(targetPos, transform.rotation))
                        {
                            yield return new WaitForSeconds(Rotate(rotation, tMdf) + 0.45f);
                            while (Sight() && !interuptNavigation) yield return new WaitForSeconds(0.2f);
                            if (interuptNavigation) break;
                        }
                        if (interuptNavigation) break;
                        yield return new WaitForSeconds(1.5f);
                        ChangeState(States.Patrol, randomPos(maze.size));
                        toWaitTime = 1f;
                        break;
                    case States.Alerted:
                        foreach (Vector3 rotation in LookAround(targetPos, transform.rotation))
                        {
                            if (rotation != transform.eulerAngles)
                            {
                                yield return new WaitForSeconds(Rotate(rotation, tMdf) + 0.75f);
                                while (Sight() && !interuptNavigation) yield return new WaitForSeconds(0.2f);
                            }
                            if (interuptNavigation) break;
                        }
                        if (interuptNavigation) break;
                        yield return new WaitForSeconds(1f);
                        ChangeState(States.Patrol, randomPos(maze.size));
                        toWaitTime = 1f;
                        break;
                    case States.Suspicious:           
                        while (Sight() && !interuptNavigation) yield return new WaitForSeconds(0.2f);
                        if (interuptNavigation) break;
                        yield return new WaitForSeconds(1f);
                        ChangeState(States.Patrol, randomPos(maze.size));        
                        break;
                    case States.Patrol:
                        ChangeState(States.Patrol, randomPos(maze.size));
                        break;
                }          
            }
            interuptNavigation = false;
        }
    }

    IEnumerator Sense()
    {
        int hearingDelayer = 0;
        int lightDelayer = 0;
        int sightDelayer = 0;
        bool heared = false;
        bool spotted = false;
        bool lighted = false; ;
        bool lostSight = false;
        while (sense)
        {
            //Hearing
            if (playerBehavior.playerState != PlayerController.States.Hiding && obstacleMemory[playerPos.x, playerPos.y])
            {
                List<Vector3> tempList = GeneratePath(new Vector2Int(playerPos.x, playerPos.y));
                while (tempList.Count > 0 && tempList[tempList.Count - 1] != Vector3.forward) tempList.RemoveAt(tempList.Count - 1);
                heared = playerBehavior.playerState == PlayerController.States.Walking && tempList.Count < hearWalkRange || playerBehavior.playerState == PlayerController.States.Running && tempList.Count < hearRunningRange;
            }
            if (heared) hearingDelayer++;
            else hearingDelayer--;
            hearingDelayer = Mathf.Clamp(hearingDelayer, 0, 6);
            if (hearingDelayer > 2 && state == States.Patrol && heared) ChangeState(States.Suspicious, playerPos);
            if (hearingDelayer > 5 && state == States.Suspicious && heared) ChangeState(States.Alerted, playerPos);
                   
            //Light Interaction
            if(Physics.Raycast(transform.position, player.position - transform.position, out RaycastHit hitTest, 5))
            {
                lighted = playerBehavior.light && Vector3.Angle(player.transform.forward, transform.position - player.position) < 30 && hitTest.transform.tag == "Player";
            }
            if (lighted) lightDelayer++;
            else lightDelayer--;
            lightDelayer = Mathf.Clamp(lightDelayer, 0, 5);
            if (lightDelayer > 2 && state == States.Patrol && lighted) ChangeState(States.Suspicious, playerPos);
            if (lightDelayer > 4 && state == States.Suspicious && lighted) ChangeState(States.Alerted, playerPos);
            
            //Sight
            spotted = Sight();
            if (spotted) { sightDelayer++; lostSight = false; }
            else sightDelayer--;
            sightDelayer = Mathf.Clamp(sightDelayer, 0, 5);
            if (spotted)
            {
                if ((sightDelayer > 4 && state == States.Alerted) || state == States.Hunting)
                {
                    if (playerBehavior.playerState == PlayerController.States.Hiding)
                    {
                        Transform hideBlock = player.GetComponent<PlayerController>().inHideWall.parent;
                        ChangeState(States.Hunting, Vector2Int.FloorToInt(new Vector2(hideBlock.position.x, hideBlock.position.z)));
                    }
                    else ChangeState(States.Hunting, playerPos);
                }
                else if (playerBehavior.playerState != PlayerController.States.Hiding)
                {               
                    if (sightDelayer > 3 && (state == States.Suspicious || state == States.Alerted)) ChangeState(States.Alerted, playerPos);
                    else if (sightDelayer > 2 && (state == States.Patrol || state == States.Suspicious)) ChangeState(States.Suspicious, playerPos);
                }

            }      
            else if (state == States.Hunting && !lostSight && playerBehavior.playerState != PlayerController.States.Hiding) { lostSight = true; ChangeState(States.Hunting, LostSightEstimation(playerPos, playerPos - prevPlayerPos)); }
            
            yield return new WaitForSeconds(0.25f);
        }
    }

    private List<Vector3> GeneratePath(Vector2Int target)
    {
        Vector3 checkHideWallRot = Vector3.zero;
        if (!obstacleMemory[target.x, target.y] && maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall != null)
        {
            Transform hideWall = maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall;
            target += Vector2Int.RoundToInt(new Vector2(hideWall.forward.x, hideWall.forward.z));
            checkHideWallRot = (hideWall.transform.rotation * Quaternion.Euler(0, 180, 0)).eulerAngles;
        }
        bool[,] unvisitedSpots = (bool[,])obstacleMemory.Clone();
        List<Vector2Int> pointPath = new List<Vector2Int>();
        Vector2Int pos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.z));
        while (pos != target)
        {
            List<Vector2Int> direction = PickDirection(pos, unvisitedSpots);
            unvisitedSpots[pos.x, pos.y] = false;
            if (direction.Count == 0)
            {
                if (pointPath.Count <= 0) break;
                pos -= pointPath[pointPath.Count - 1];
                pointPath.RemoveAt(pointPath.Count - 1);
            }
            else
            {
                int randIndex = Random.Range(0, direction.Count);
                pos += direction[randIndex];
                pointPath.Add(direction[randIndex]);
            }
        }
        List<Vector3> movePath = new List<Vector3>();
        Vector3 oreintation = transform.eulerAngles;
        for (int index = 0; index < pointPath.Count; index++)
        {
            if (pointPath[index] == Vector2Int.up && oreintation != new Vector3(0, 0, 0)) { movePath.Add(new Vector3(0, 0, 0)); oreintation = new Vector3(0, 0, 0); }
            else if (pointPath[index] == Vector2Int.down && oreintation != new Vector3(0, 180, 0)) { movePath.Add(new Vector3(0, 180, 0)); oreintation = new Vector3(0, 180, 0); }
            else if (pointPath[index] == Vector2Int.left && oreintation != new Vector3(0, 270, 0)) { movePath.Add(new Vector3(0, 270, 0)); oreintation = new Vector3(0, 270, 0); }
            else if (pointPath[index] == Vector2Int.right && oreintation != new Vector3(0, 90, 0)) { movePath.Add(new Vector3(0, 90, 0)); oreintation = new Vector3(0, 90, 0); }
            movePath.Add(Vector3.forward);
        }
        if (checkHideWallRot != Vector3.zero && oreintation != checkHideWallRot)
        {
            movePath.Add(checkHideWallRot);
        }
        movePath.Reverse();
        if (state == States.Suspicious) while (movePath.Count > 0 && movePath[0] == Vector3.forward) movePath.RemoveAt(0);
        return movePath;
    }

    private void NewPath(Vector2Int target)
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Trap")
        {
            toWaitTime += 5;
            Destroy(other.gameObject);
            rend.material.color = Color.blue;
        }
    }

    public IEnumerator GetShot()
    {     
        toWaitTime += 10;       
        sense = false;
        Color prevColor = rend.material.color;
        rend.material.color = Color.gray;
        yield return new WaitForSeconds(4.5f);
        rend.material.color = prevColor;
        sense = true;
    }

    public IEnumerator HearObject(Transform hearedObject, int range, float delay)
    {      
        yield return new WaitForSeconds(delay);
        List<Vector3> tempList = GeneratePath(Vector2Int.RoundToInt(new Vector2(hearedObject.position.x, hearedObject.position.z)));
        while (tempList.Count > 0 && tempList[tempList.Count - 1] != Vector3.forward) tempList.RemoveAt(tempList.Count - 1);
        if (tempList.Count < range && state != States.Hunting) ChangeState(States.Alerted, Vector2Int.RoundToInt(new Vector2(hearedObject.position.x, hearedObject.position.z)));
    }

    private bool Sight()
    {
        if (Physics.Raycast(transform.position + transform.forward / 3, (player.position - transform.position) * sightRange, out RaycastHit hit, sightRange))
        {
            if (DisplayAIBehaviour)
            {
                Debug.DrawRay(transform.position + transform.forward / 3, Quaternion.Euler(0, Fov / 2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                Debug.DrawRay(transform.position + transform.forward / 3, Quaternion.Euler(0, Fov / -2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                if (hit.transform.tag == "Player") Debug.DrawLine(transform.position, player.transform.position, Color.green, 0.25f);
                else Debug.DrawLine(transform.position, player.transform.position, Color.magenta, 0.25f);
            }
            return hit.transform.tag == "Player" && Vector3.Angle(transform.position - player.transform.position, transform.forward) > Fov / 2;
        }
        return false;
    }

    private Vector2Int LostSightEstimation(Vector2Int pos, Vector2Int direction)
    {
        bool openCorridor = false;
        while (obstacleMemory[pos.x, pos.y])
        {
            if (PickDirection(pos, obstacleMemory).Count > 2)
            {
                openCorridor = true;
                break;
            }
            pos += direction;
        }
        if(!openCorridor) pos -= direction;
        Vector2Int prevPos = pos - direction;
        List<Vector2Int> directions = PickDirection(pos, obstacleMemory);
        while(directions.Count == 2 && !openCorridor)
        {
            if (directions[0] != prevPos - pos)
            {
                prevPos = pos;
                pos += directions[0];
            }
            else
            {
                prevPos = pos;
                pos += directions[1];
            }
            directions = PickDirection(pos, obstacleMemory);
        }
        return pos;
    }

    private void ChangeState(States toChangeState, Vector2Int lastSeen)
    {
        interuptNavigation = true;
        targetPos = lastSeen;
        switch (toChangeState)
        {
            case States.Patrol:
                state = States.Patrol;
                nextTMdf = 1 / patrolSpeed;
                rend.material.color = Color.green;
                break;
            case States.Suspicious:
                state = States.Suspicious;
                nextTMdf = 1 / suspiciousSpeed;
                rend.material.color = Color.yellow;
                break;
            case States.Alerted:
                toWaitTime += 1f;
                state = States.Alerted;
                nextTMdf = 1 / alertedSpeed;
                rend.material.color = new Color(1, 0.392f, 0);
                break;
            case States.Hunting:
                state = States.Hunting;
                nextTMdf = 1 / huntingSpeed;
                rend.material.color = Color.red;
                break;
        }
    }

    private float Rotate(Vector3 targetRot, float time)
    {
        movement = Movements.Rotating;
        startRot = transform.rotation;
        endRot = Quaternion.Euler(targetRot);
        t = 0;
        return time;
    }

    private float Move(Vector3 targetMove, float time)
    {
        movement = Movements.Moving;
        startPos = transform.position;
        endPos = transform.position + transform.forward;
        t = 0;
        return time;
    }


    private List<Vector3> LookAround(Vector2Int pos, Quaternion rot)
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] lookAroundRots = { new Vector3(0, 270, 0), new Vector3(0, 90, 0), new Vector3(0, 0, 0) };
        foreach(Vector3 lookRot in lookAroundRots)
        {
            Quaternion tempRot = rot * Quaternion.Euler(lookRot);
            if (tempRot.eulerAngles == new Vector3(0, 90, 0) && obstacleMemory[pos.x + 1, pos.y]) directions.Add(new Vector3(0, 90, 0));
            if (tempRot.eulerAngles == new Vector3(0, 270, 0) && obstacleMemory[pos.x - 1, pos.y]) directions.Add(new Vector3(0, 270, 0));
            if (tempRot.eulerAngles == new Vector3(0, 0, 0) && obstacleMemory[pos.x, pos.y + 1]) directions.Add(new Vector3(0, 0, 0));
            if (tempRot.eulerAngles == new Vector3(0, 180, 0) && obstacleMemory[pos.x, pos.y - 1]) directions.Add(new Vector3(0, 180, 0));
        }
        return directions;
    }

    List<Vector2Int> PickDirection(Vector2Int pos, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (unvisiteSpots[pos.x + 1, pos.y]) directions.Add(Vector2Int.right);
        if (unvisiteSpots[pos.x - 1, pos.y]) directions.Add(Vector2Int.left);
        if (unvisiteSpots[pos.x, pos.y + 1]) directions.Add(Vector2Int.up);
        if (unvisiteSpots[pos.x, pos.y - 1]) directions.Add(Vector2Int.down);
        return directions;
    }

    Vector2Int randomPos(int mazeSize)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(Random.Range(1, mazeSize), Random.Range(1, mazeSize));
        } while (!obstacleMemory[pos.x, pos.y]);
        return pos;
    }
}
