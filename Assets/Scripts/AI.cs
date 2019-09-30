using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public int Fov = 50;
    public float patrolSpeed;
    public float huntSpeed;
    public float alertedSpeed;
    public float sightRange;
    public float hearRange;
    public float hearPathRange;
    public float AlertTimeout;
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
    private Player playerBehavior;
    private Vector2Int playerPos = new Vector2Int(1,1);
    private Vector2Int prevPlayerPos;
    private bool visualContact;
    private enum Movements { Moving, Rotating, Idle };
    private Movements movement = Movements.Idle;   
    private enum States { Patrol, Alerted, Hunting };
    [SerializeField]
    private States state = States.Patrol;
    private Vector2Int targetPos;

    private Renderer rend;
    private Light light;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    { 
        light = gameObject.GetComponent<Light>();
        rend = gameObject.GetComponent<Renderer>();
        player = GameObject.Find("Player").transform;
        playerBehavior = GameObject.Find("Player").GetComponent<Player>();
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        obstacleMemory = maze.obstacleMemory;
        while (true)
        {
            int x = Random.Range(10, maze.size - 2);
            int y = Random.Range(10, maze.size - 2);
            x -= (x + 1) % 2;
            y -= (y + 1) % 2;
            if (obstacleMemory[x, y] == true)
            {
                transform.position = new Vector3(x, 1, y);
                break;
            }
        }
        nextTMdf = 1 / patrolSpeed;
        ChangeState(States.Patrol, Vector2Int.zero);
        StartCoroutine(PathNavigation());
        StartCoroutine(Sense());
    }

    private void Update()
    {
        Vector2Int pos = new Vector2Int(Mathf.RoundToInt(player.transform.position.x), Mathf.RoundToInt(player.transform.position.z));
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
                transform.rotation = Quaternion.Lerp(startRot, endRot, t / tMdf);
                break;
        }
        t += Time.deltaTime;
        if (Vector3.Distance(player.transform.position, transform.position) > 10f && rend.enabled) rend.enabled = false;
        else if (Vector3.Distance(player.transform.position, transform.position) <= 10f && !rend.enabled) rend.enabled = true;
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
            List<Vector2Int> moveOrder = GeneratePath(Mathf.Clamp(targetPos.x - (targetPos.x + 1) % 2, 1, maze.size - 2), Mathf.Clamp(targetPos.y - (targetPos.y + 1) % 2, 1, maze.size - 2));
            tMdf = nextTMdf;
            yield return new WaitForSeconds(toWaitTime);
            toWaitTime = 0;
            while (moveOrder.Count != 0)
            {
                if (moveOrder[moveOrder.Count - 1] == Vector2Int.up && transform.eulerAngles != new Vector3(0, 0, 0)) yield return new WaitForSeconds(Rotate(new Vector3(0, 0, 0), tMdf));
                else if (moveOrder[moveOrder.Count - 1] == Vector2Int.down && transform.eulerAngles != new Vector3(0, 180, 0)) yield return new WaitForSeconds(Rotate(new Vector3(0, 180, 0), tMdf));
                else if (moveOrder[moveOrder.Count - 1] == Vector2Int.left && transform.eulerAngles != new Vector3(0, 270, 0)) yield return new WaitForSeconds(Rotate(new Vector3(0, 270, 0), tMdf));
                else if (moveOrder[moveOrder.Count - 1] == Vector2Int.right && transform.eulerAngles != new Vector3(0, 90, 0)) yield return new WaitForSeconds(Rotate(new Vector3(0, 90, 0), tMdf));
                transform.eulerAngles = new Vector3(Mathf.Round(transform.eulerAngles.x / 90) * 90, Mathf.Round(transform.eulerAngles.y / 90) * 90, Mathf.Round(transform.eulerAngles.z / 90) * 90);
                if (interuptNavigation) break;
                yield return new WaitForSeconds(Move(transform.position + new Vector3(moveOrder[moveOrder.Count - 1].x, 0, moveOrder[moveOrder.Count - 1].y), tMdf));
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), Mathf.Round(transform.position.z));
                moveOrder.RemoveAt(moveOrder.Count - 1);
                if (interuptNavigation) break;
            }
            if (!interuptNavigation)
            {
                switch (state)
                {
                    case States.Hunting:
                        foreach (Vector3 rotation in LookAround(targetPos)) if (rotation != transform.eulerAngles)
                        {
                             yield return new WaitForSeconds(Rotate(rotation, tMdf) + 0.25f);
                             while (visualContact && !interuptNavigation) yield return new WaitForSeconds(0.10f);
                             if (interuptNavigation) break;
                        }
                        if (interuptNavigation) break;
                        ChangeState(States.Alerted, new Vector2Int(Mathf.Clamp(Random.Range(playerPos.x - 4, playerPos.x + 4), 1, maze.size - 2), Mathf.Clamp(Random.Range(playerPos.y - 4, playerPos.y + 4), 1, maze.size - 2)));
                        toWaitTime = 1f;
                        break;
                    case States.Alerted:
                        foreach (Vector3 rotation in LookAround(targetPos)) if (rotation != transform.eulerAngles)
                        {
                            yield return new WaitForSeconds(Rotate(rotation, tMdf) + 0.75f);
                            while (visualContact && !interuptNavigation) yield return new WaitForSeconds(0.25f);
                            if (interuptNavigation) break;
                        }
                        if (interuptNavigation) break;
                        ChangeState(States.Patrol, Vector2Int.zero);
                        break;
                    case States.Patrol:
                        ChangeState(States.Patrol, Vector2Int.zero);
                        break;
                }
            }
            interuptNavigation = false;
        }
    }

    IEnumerator Sense()
    {
        int hearingDelayer = 0;
        int sightDelayer = 0;
        bool lostSight = false;
        
        while (true)
        {
            Vector2Int pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
            //Hearing
            if (Vector2Int.Distance(pos, playerPos) < hearRange || GeneratePath(playerPos.x, playerPos.y).Count < hearPathRange && playerBehavior.playerState == Player.States.Running
                || playerBehavior.playerState == Player.States.Walking && Vector2Int.Distance(pos, playerPos) < 1)
            {
                hearingDelayer++;
            }
            else hearingDelayer = 0;
            if (hearingDelayer >= 3 && state != States.Hunting) ChangeState(States.Alerted, playerPos);

            //Sight
            if(Physics.Raycast(transform.position + transform.forward / 3, (player.transform.position - transform.position) * sightRange, out RaycastHit hit, sightRange))
            {
                if (DisplayAIBehaviour) Debug.DrawRay(transform.position + transform.forward / 3, Quaternion.Euler(0, Fov / 2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                if (DisplayAIBehaviour) Debug.DrawRay(transform.position + transform.forward / 3, Quaternion.Euler(0, Fov / -2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                if (DisplayAIBehaviour && hit.transform.tag == "Player") Debug.DrawLine(transform.position, player.transform.position, Color.green, 0.25f);
                else if (DisplayAIBehaviour) Debug.DrawLine(transform.position, player.transform.position, Color.magenta, 0.25f);
                visualContact = hit.transform.tag == "Player" && Vector3.Angle(transform.position - player.transform.position, transform.forward) > Fov / 2;
                if (visualContact)
                {   
                    if (sightDelayer < 0) sightDelayer = 0;
                    sightDelayer++;
                    if (sightDelayer == 2 && state != States.Alerted)
                    {
                        sightDelayer = 0;
                        toWaitTime = 1f;
                        ChangeState(States.Alerted, playerPos);
                    }
                    if (sightDelayer == 4 || state == States.Hunting)
                    {
                        lostSight = true;
                        ChangeState(States.Hunting, playerPos);
                    }
                }
                else if(state == States.Hunting && lostSight)
                {
                    lostSight = false;
                    ChangeState(States.Hunting, LostSightEstimation(playerPos, playerPos - prevPlayerPos));
                }
                else if(state != States.Hunting)
                {
                    if (sightDelayer > 0) sightDelayer = 0;
                    sightDelayer--;
                    if (sightDelayer == -4 * AlertTimeout) ChangeState(States.Patrol, Vector2Int.zero);
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private Vector2Int LostSightEstimation(Vector2Int pos, Vector2Int direction)
    {
        bool openCorridor = false;
        while (obstacleMemory[pos.x, pos.y])
        {
            if (PickDirection(pos.x, pos.y, obstacleMemory).Count > 2)
            {
                openCorridor = true;
                break;
            }
            pos += direction;
        }
        if(!openCorridor) pos -= direction;
        Vector2Int prevPos = pos - direction;
        List<Vector2Int> directions = PickDirection(pos.x, pos.y, obstacleMemory);
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
            directions = PickDirection(pos.x, pos.y, obstacleMemory);
        }
        return pos;
    }

    private void ChangeState(States toChangeState, Vector2Int lastSeen)
    {
        //Debug.Log(lastSeen);
        switch (toChangeState)
        {
            case States.Patrol:
                state = States.Patrol;
                nextTMdf = 1 / patrolSpeed;
                targetPos = new Vector2Int(Random.Range(1, maze.size - 2), Random.Range(1, maze.size - 2));
                rend.material.color = Color.green;
                //light.color = Color.green;
                break;
            case States.Alerted:
                state = States.Alerted;
                nextTMdf = 1 / alertedSpeed;
                targetPos = lastSeen;
                interuptNavigation = true;
                rend.material.color = new Color(1, 0.392f, 0);
                //light.color = new Color(1, 0.392f, 0);
                break;
            case States.Hunting:
                state = States.Hunting;
                nextTMdf = 1 / huntSpeed;
                targetPos = lastSeen;
                interuptNavigation = true;
                rend.material.color = Color.red;
                //light.color = Color.red;
                break;
        }
        if (targetPos.x == 0 || targetPos.y == 0) Debug.Log("target outside of maze! - " + targetPos);
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
        endPos = targetMove;
        t = 0;
        return time;
    }

    private List<Vector2Int> GeneratePath(int targetX, int targetY)
    {
        bool[,] unvisitedSpots = (bool[,])obstacleMemory.Clone();      
        List<Vector2Int> path = new List<Vector2Int>();
        List<Vector2Int> debugPath = new List<Vector2Int>();
        Vector2Int pos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        while (pos != new Vector2Int(targetX, targetY))
        {
            List<Vector2Int> direction = PickDirection(pos.x, pos.y, unvisitedSpots);
            unvisitedSpots[pos.x, pos.y] = false;
            if (direction.Count == 0)
            {
                pos -= path[path.Count - 1];
                if (path.Count - 1 < 0) Debug.Break();
                path.RemoveAt(path.Count - 1);
            }
            else
            {
                int randIndex = Random.Range(0, direction.Count);
                pos += direction[randIndex];
                path.Add(direction[randIndex]);
                debugPath.Add(direction[randIndex]);
            }
        }
        path.Reverse();
        return path;
    }

    private List<Vector3> LookAround(Vector2Int pos)
    {
        List<Vector3> direction = new List<Vector3>();
        if (obstacleMemory[pos.x + 1, pos.y]) direction.Add(new Vector3(0, 90, 0));
        if (obstacleMemory[pos.x - 1, pos.y]) direction.Add(new Vector3(0, 270, 0));
        if (obstacleMemory[pos.x, pos.y + 1]) direction.Add(new Vector3(0, 0, 0));
        if (obstacleMemory[pos.x, pos.y - 1]) direction.Add(new Vector3(0, 180, 0));
        return direction;
    }

    List<Vector2Int> PickDirection(int x, int y, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (unvisiteSpots[x + 1, y]) directions.Add(Vector2Int.right);
        if (unvisiteSpots[x - 1, y]) directions.Add(Vector2Int.left);
        if (unvisiteSpots[x, y + 1]) directions.Add(Vector2Int.up);
        if (unvisiteSpots[x, y - 1]) directions.Add(Vector2Int.down);
        return directions;
    }
}
