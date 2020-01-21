using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField]
    private States state = States.Patrol;
    [Header("Sight")]
    public bool sightEnabled;
    [Space(6)]
    public float sightBase;
    public float sightRange;
    public float sightRangeBonus;
    public int fov = 50;

    [Header("Hearing")]
    public bool hearingEnabled;
    [Space(6)]
    public float hearWalkBase;
    public float hearWalkRange;
    public float hearWalkRangeBonus;
    [Space(6)]
    public float hearRunBase;
    public float hearRunRange;
    public float hearRunRangeBonus;

    [Header("Light interaction")]
    public bool lightSenseEnabled;
    [Space(6)]
    public float lightBase;
    public float lightRange;
    public float lightRangeBonus;

    [Header("Speeds")]
    public float patrolSpeed;
    public float suspiciousSpeed;
    public float alertedSpeed;
    public float huntingSpeed;
    public float seekingSpeed;

    [Header("Thresholds")]
    public float suspicousThreshold;
    public float alertThreshold;
    public float huntThreshold;
    public float maxThreshold;
    public float senseMemory;

    [Header("Roaming")]
    public float patrolHideCheckProbability;
    public float patrolLookAroundProbability;
    [Space(6)]
    public float alertedHideCheckProbability;
    public float alertedLookAroundProbability;
    public int alertedMaxPathRecursion;
    [Space(6)]
    public float seekingHideCheckProbability;
    public float seekingLookAroundProbability;
    public int seekingMaxPathRecursion;
    [Space(6)]   
    public bool DisplayAIBehaviour;

    private MazeSystem maze;
    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private Quaternion endRot;
    private float t = 0;
    private float tMdf;
    private float nextTMdf;
    private Transform player;
    private PlayerController playerBehavior;
    private Vector2Int currentPlayerPos;
    private Vector2Int prevPlayerPos;
    private Vector2Int prevPos;
    private Vector2Int currentPos;
    private Vector2Int nextPos;
    private Vector3 nextRot;
    private Vector2Int patrolPos;
    private bool newPath = false; 
    private enum moveTypes { move, rotate, hideCheck, sight, wait };
    private struct moveTask
    {
        public moveTypes moveType;
        public Vector3 direction;
        public float time;
        public moveTask(moveTypes Movetype, Vector3 Direction)
        {
            moveType = Movetype;
            direction = Direction;
            time = 0.5f;
        }
        public moveTask(moveTypes Movetype, float Time)
        {
            moveType = Movetype;
            direction = Vector3.zero;
            time = Time;
        }
        public moveTask(moveTypes MoveType)
        {
            moveType = MoveType;
            direction = Vector3.zero;
            time = 0.5f;
        }
    };
    private enum pathType { lookAround, sight, checkHide, roam, distance };
    private enum Movements { Moving, Rotating, Idle };
    private Movements movement = Movements.Idle;   
    private enum States { Patrol, Suspicious, Alerted, Hunting, Seeking };
    private float toWaitTime = 0;
    private Renderer rend;
    private bool sense = true;
    private List<moveTask> taskStack = new List<moveTask>();
    private List<moveTask> pushTaskStack = new List<moveTask>();
    void Start()
    { 
        rend = gameObject.GetComponent<Renderer>();
        player = GameObject.Find("Player").transform;
        playerBehavior = GameObject.Find("Player").GetComponent<PlayerController>();
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        Vector2Int tempPos;
        do tempPos = new Vector2Int(Random.Range(8, GameParameters.size - 2), Random.Range(8, GameParameters.size));
        while (!maze.obstacleMatrix[tempPos.x, tempPos.y]);
        transform.position = new Vector3(tempPos.x, 0, tempPos.y);
        prevPos = tempPos;
        nextPos = tempPos;
        currentPos = tempPos;
        patrolPos = randomPos(GameParameters.size);
        ChangeState(States.Patrol);
        StartCoroutine(Navigation());
        StartCoroutine(Sense());
    }

    private void Update()
    {
        UpdateVars();
        Move(movement);
    }

    private void UpdateVars()
    {
        Vector2Int tempPos = Vector2Int.RoundToInt(new Vector2(player.transform.position.x, player.transform.position.z));
        if (currentPlayerPos != tempPos)
        {
            prevPlayerPos = currentPlayerPos;
            currentPlayerPos = tempPos;
            Debug.Log("current: " + currentPlayerPos);
            Debug.Log("prev: " + prevPlayerPos);
            Vector3 temp3 = new Vector3(currentPlayerPos.x, 5, currentPlayerPos.y);
            Debug.DrawLine(temp3 + new Vector3(-0.5f, 0, -0.5f), temp3 + new Vector3(+0.5f, 0, +0.5f), Color.green, 0.5f);
            Debug.DrawLine(temp3 + new Vector3(-0.5f, 0, +0.5f), temp3 + new Vector3(+0.5f, 0, -0.5f), Color.green, 0.5f);
            temp3 = new Vector3(prevPlayerPos.x, 5, prevPlayerPos.y);
            Debug.DrawLine(temp3 + new Vector3(-0.5f, 0, -0.5f), temp3 + new Vector3(+0.5f, 0, +0.5f), Color.cyan, 0.5f);
            Debug.DrawLine(temp3 + new Vector3(-0.5f, 0, +0.5f), temp3 + new Vector3(+0.5f, 0, -0.5f), Color.cyan, 0.5f);
            Debug.Log("================");
        }

        tempPos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.z));
        if (currentPos != tempPos)
        {
            prevPos = currentPos;
            currentPos = tempPos;
        }
    }

    private void Move(Movements moveType)
    {
        switch (moveType)
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

    IEnumerator Navigation()
    {
        moveTask currentTask;
        float waitTime;
        while (true)
        {
            if (toWaitTime != 0)
            {
                yield return new WaitForSeconds(toWaitTime);
                toWaitTime = 0;
            }
            if (taskStack.Count == 0 && !newPath) NewPath(PostNavigation(state, taskStack.Count == 0));
            if (newPath)
            {
                newPath = false;
                taskStack = pushTaskStack;
            }
            tMdf = nextTMdf;
            currentTask = taskStack[0];
            taskStack.RemoveAt(0);
            switch (currentTask.moveType)
            {
                case moveTypes.move:
                    yield return StartCoroutine(Move(currentTask.direction));
                    transform.position = Vector3Int.RoundToInt(transform.position);
                    break;
                case moveTypes.rotate:
                    yield return StartCoroutine(Rotate(currentTask.direction));
                    transform.eulerAngles = new Vector3(Mathf.Round(transform.eulerAngles.x / 90) * 90, Mathf.Round(transform.eulerAngles.y / 90) * 90, Mathf.Round(transform.eulerAngles.z / 90) * 90);
                    break;
                case moveTypes.sight:
                    waitTime = currentTask.time;
                    while(waitTime > 0 && !newPath)
                    {
                        if(!Sight(player)) waitTime -= Time.deltaTime;
                        yield return null;
                    }
                    break;
                case moveTypes.wait:
                    waitTime = currentTask.time;
                    while (waitTime > 0 && !newPath)
                    { 
                        waitTime -= Time.deltaTime;
                        yield return null;
                    }
                    break;
                case moveTypes.hideCheck: //TODO
                    //Debug.Log("Checking hide...");
                    yield return new WaitForSeconds(2f);
                    break;
            }
        }
    }

    List<moveTask> PostNavigation(States state, bool finishedPreviousStack)
    {
        switch (state)
        {
            default:
            case States.Patrol:
            case States.Suspicious:
            case States.Seeking:
                if (finishedPreviousStack) patrolPos = randomPos(GameParameters.size);
                ChangeState(States.Patrol);
                return GeneratePath(nextPos, nextRot, patrolPos, new pathType[] { pathType.roam }, patrolLookAroundProbability, patrolHideCheckProbability);
            case States.Alerted:
            case States.Hunting:
                ChangeState(States.Seeking);
                return PostHunt(nextPos, nextRot, prevPos, 35, 3, 75);
        }
    }

    IEnumerator Sense()
    {
        float mainSense = 0;
        bool heared;
        bool spotted;
        bool lighted;
        bool prevSpotted = false;
        float fromPlayerDistancePoint;
        float fromPlayerDistanceVector;
        while (true)
        {
            if (sense)
            {
                fromPlayerDistancePoint = GeneratePath(currentPos, transform.eulerAngles, new Vector2Int(currentPlayerPos.x, currentPlayerPos.y), new pathType[] { pathType.distance }).Count;
                fromPlayerDistanceVector = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), currentPlayerPos);
                //Hearing
                heared = hearingEnabled && (playerBehavior.playerState == PlayerController.States.Walking && fromPlayerDistancePoint < hearWalkRange || playerBehavior.playerState == PlayerController.States.Running && fromPlayerDistancePoint < hearRunRange);
                if(heared)
                {
                    if (playerBehavior.playerState == PlayerController.States.Walking) mainSense += (hearWalkBase + Mathf.Clamp(hearWalkRangeBonus - (fromPlayerDistancePoint * (hearWalkRangeBonus / hearWalkRange)), 0, Mathf.Infinity)) / 4;
                    if (playerBehavior.playerState == PlayerController.States.Running) mainSense += (hearRunBase + Mathf.Clamp(hearRunRangeBonus - (fromPlayerDistancePoint * (hearRunRangeBonus / hearRunRange)), 0, Mathf.Infinity)) / 4;
                }                  

                //Light interaction
                lighted = lightSenseEnabled && playerBehavior.lightEnabled && Vector3.Angle(player.transform.forward, transform.position - player.position) < 30 && Physics.Raycast(transform.position, player.position - transform.position, out RaycastHit hit) && hit.transform.tag == "Player" && fromPlayerDistanceVector < lightRange;
                if (lighted)
                {
                    mainSense += (lightBase + Mathf.Clamp(lightRangeBonus - (fromPlayerDistanceVector * (lightRangeBonus / lightRange)), 0, Mathf.Infinity)) / 4;
                }

                //Sight
                spotted = Sight(player) && playerBehavior.playerState != PlayerController.States.Hided;
                if (spotted)
                {
                    mainSense += (sightBase + Mathf.Clamp(sightRangeBonus - (fromPlayerDistanceVector * (sightRangeBonus / sightRange)), 0, Mathf.Infinity)) / 4;
                }

                //Conclusion
                if (!spotted && !heared && !lighted) mainSense -= senseMemory / 4;

                mainSense = Mathf.Clamp(mainSense, 0, maxThreshold);

                if (mainSense > suspicousThreshold && (state == States.Patrol || state == States.Seeking) && (spotted || heared || lighted))
                {
                    //Debug.Log("suspicious");
                    ChangeState(States.Suspicious);
                    NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { pathType.sight }));
                }
                else if (mainSense > alertThreshold && state == States.Suspicious && (spotted || heared || lighted))
                {
                    //Debug.Log("alerted");
                    ChangeState(States.Alerted);
                    NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { pathType.lookAround }));
                }
                else if ((mainSense > huntThreshold && state == States.Alerted && spotted) || state == States.Hunting)
                {
                    if (state != States.Hunting) ChangeState(States.Hunting);
                    if (playerBehavior.playerState == PlayerController.States.Hiding && spotted)
                    {
                        Transform hideBlock = playerBehavior.inHideWall.parent;
                        NewPath(GeneratePath(nextPos, nextRot, Vector2Int.RoundToInt(new Vector2(hideBlock.position.x, hideBlock.position.z)), new pathType[] { pathType.checkHide } ));
                    }
                    else if (prevSpotted && !spotted)
                    {
                        NewPath(GeneratePath(nextPos, nextRot, LostSightEstimation(currentPlayerPos, currentPlayerPos - prevPlayerPos), new pathType[] { pathType.lookAround }));
                    }
                    else if (spotted)
                    {
                        NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { }));
                    }
                }              
                prevSpotted = spotted;

                //Debug
                /*if(spotted) Debug.Log("+sight: " + ((sightBase + Mathf.Clamp(sightRangeBonus - (fromPlayerDistanceVector * (sightRangeBonus / sightRange)), 0, Mathf.Infinity)) / 4));
                else Debug.Log("+sight: 0");
                if (heared)
                {
                    if (playerBehavior.playerState == PlayerController.States.Walking) Debug.Log("+hearWalk: " + ((hearRunBase + Mathf.Clamp(hearRunRangeBonus - (fromPlayerDistancePoint * (hearRunRangeBonus / hearRunRange)), 0, Mathf.Infinity)) / 4));
                    else Debug.Log("+hearWalk: 0");
                    if (playerBehavior.playerState == PlayerController.States.Running) Debug.Log("+hearRun: " + ((hearRunBase + Mathf.Clamp(hearRunRangeBonus - (fromPlayerDistancePoint * (hearRunRangeBonus / hearRunRange)), 0, Mathf.Infinity)) / 4));
                    else Debug.Log("+hearRun: 0");
                }
                else Debug.Log("+hearWalk: 0"); Debug.Log("+hearRun: 0");
                if (lighted) Debug.Log("+light: " + ((lightBase + Mathf.Clamp(lightRangeBonus - (fromPlayerDistanceVector * (lightRangeBonus / lightRange)), 0, Mathf.Infinity)) / 4));
                else Debug.Log("+light: 0");
                Debug.Log("totalSense: " + senseDelayer);*/
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void NewPath(List<moveTask> NewMoveTaskStack)
    {
        newPath = true;
        pushTaskStack = NewMoveTaskStack;
        //DEBUGGING
        if (true)
        {
            Vector3 tempPos = new Vector3(nextPos.x, 15, nextPos.y);
            foreach(moveTask task in NewMoveTaskStack)
            {
                if(task.moveType == moveTypes.move) tempPos += task.direction;
            }
            Debug.Log(tempPos);
            Debug.DrawLine(tempPos + new Vector3(-0.5f, 0, -0.5f), tempPos + new Vector3(+0.5f, 0, +0.5f), Color.red, 3f);
            Debug.DrawLine(tempPos + new Vector3(-0.5f, 0, +0.5f), tempPos + new Vector3(+0.5f, 0, -0.5f), Color.red, 3f);
        }
    }

    private bool Sight(Transform target)
    {
        if(sightEnabled && Physics.Raycast(transform.position, target.position - transform.position, out RaycastHit hit, sightRange))
        {
            if (DisplayAIBehaviour)
            {
                Debug.DrawRay(transform.position, Quaternion.Euler(0, fov / 2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                Debug.DrawRay(transform.position, Quaternion.Euler(0, fov / -2, 0) * transform.forward * sightRange, Color.cyan, 0.25f);
                if (hit.transform == target && Vector3.Angle(target.transform.position - transform.position, transform.forward) < fov / 2) Debug.DrawLine(transform.position, target.transform.position, Color.green, 0.25f);
                else Debug.DrawLine(transform.position, target.transform.position, Color.magenta, 0.25f);
            }
            return hit.transform == target && Vector3.Angle(target.transform.position - transform.position, transform.forward) < fov / 2;
        }
        return false;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Trap")
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
        List<moveTask> tempList = GeneratePath(currentPos, transform.eulerAngles, Vector2Int.RoundToInt(new Vector2(hearedObject.position.x, hearedObject.position.z)), new pathType[] { pathType.distance });
        if (tempList.Count < range && state != States.Hunting)
        {
            ChangeState(States.Alerted);
            NewPath(GeneratePath(nextPos, nextRot, Vector2Int.RoundToInt(new Vector2(hearedObject.position.x, hearedObject.position.z)), new pathType[] { pathType.lookAround }));
        }
    }

    private List<moveTask> GeneratePath(Vector2Int pos, Vector3 rot, Vector2Int target, pathType[] pathTypes)
    {
        return GeneratePath(ref pos, ref rot, target, pathTypes, 0, 0);
    }
    private List<moveTask> GeneratePath(ref Vector2Int pos, ref Vector3 rot, Vector2Int target, pathType[] pathTypes)
    {
        return GeneratePath(ref pos, ref rot, target, pathTypes, 0, 0);
    }
    private List<moveTask> GeneratePath(Vector2Int pos, Vector3 rot, Vector2Int target, pathType[] pathTypes, float lookAroundChance, float hideCheckChance)
    {
        return GeneratePath(ref pos, ref rot, target, pathTypes, lookAroundChance, hideCheckChance);
    }
    private List<moveTask> GeneratePath(ref Vector2Int pos, ref Vector3 rot, Vector2Int target, pathType[] pathTypes, float lookAroundChance, float hideCheckChance)
    {
        Vector3 hideWallRotation = Vector3.zero;
        bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
        List<Vector2Int> pointPath = new List<Vector2Int>();
        List<moveTask> movePath = new List<moveTask>();
        bool roam = false;
        foreach (pathType type in pathTypes) if (type == pathType.roam) roam = true;
        Vector2Int orgPos = pos;
        List<Vector2Int> tempDirections;

        //TODO remove and use hide struct form MazeSystem as pathParameter
        if (!maze.obstacleMatrix[target.x, target.y])
        {
            if (maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall != null)
            {
                Transform hideWall = maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall;
                target += Vector2Int.RoundToInt(new Vector2(hideWall.forward.x, hideWall.forward.z));
                hideWallRotation = hideWall.rotation * Quaternion.Euler(0, 180, 0).eulerAngles;
            }
            else
            {
                tempDirections = GetDirections(target, unvisitedSpots);
                if(tempDirections.Count > 0) target += tempDirections[0];
                else target = randomPos(GameParameters.size);
            }
        }

        //Point path

        while (pos != target)
        {
            unvisitedSpots[pos.x, pos.y] = false;
            tempDirections = GetDirections(pos, unvisitedSpots);      
            if (tempDirections.Count != 0) //Stack forward
            {
                int randIndex = Random.Range(0, tempDirections.Count);
                pos += tempDirections[randIndex];
                pointPath.Add(tempDirections[randIndex]);
            }
            else //Recurse back
            {
                pos -= pointPath[pointPath.Count - 1];
                pointPath.RemoveAt(pointPath.Count - 1);
            }
        }

        //Move path
        if (pointPath.Count == 0) movePath.Add(new moveTask(moveTypes.wait, 0.25f)); //Prevents looping in rare situations
        pos = orgPos;
        Vector3 nextRot;    
        foreach (Vector2Int vector in pointPath)
        {
            //Prepare rot
            if (vector == Vector2Int.up && rot != new Vector3(0, 0, 0)) nextRot = new Vector3(0, 0, 0);
            else if (vector == Vector2Int.right && rot != new Vector3(0, 90, 0)) nextRot = new Vector3(0, 90, 0);
            else if (vector == Vector2Int.down && rot != new Vector3(0, 180, 0)) nextRot = new Vector3(0, 180, 0);
            else if (vector == Vector2Int.left && rot != new Vector3(0, 270, 0)) nextRot = new Vector3(0, 270, 0);
            else nextRot = Vector3.one; //No rot

            //Roam
            if (roam && GetDirections(pos, maze.obstacleMatrix).Count >= 3) foreach (Vector3 direction in LookAround(pos, Quaternion.Euler(rot)))
            {
                if (lookAroundChance > Random.Range(0, 100) && direction != nextRot)
                {
                    movePath.Add(new moveTask(moveTypes.rotate, direction));
                    movePath.Add(new moveTask(moveTypes.sight, 1));
                    if (nextRot == Vector3.one) nextRot = rot; //Rotate back if not already rotating
                }
            }
            if(roam && maze.hideMatrix[pos.x, pos.y] != null) foreach (MazeSystem.hide hide in maze.hideMatrix[pos.x, pos.y])
            {
                if (hideCheckChance > Random.Range(0, 100))
                {
                    if (hide.checkingRot != rot) movePath.Add(new moveTask(moveTypes.rotate, hide.checkingRot));
                    movePath.Add(new moveTask(moveTypes.hideCheck));
                    if (nextRot == Vector3.one) nextRot = rot;
                }
            }

            //Rotate (if neccessary) and Move
            if (nextRot != Vector3.one)
            {
                movePath.Add(new moveTask(moveTypes.rotate, nextRot));
                rot = nextRot;
            }
            movePath.Add(new moveTask(moveTypes.move, Quaternion.Euler(rot) * Vector3.forward));
            pos += vector;
        }

        //Final edits of path
        foreach (pathType type in pathTypes)
        {
            switch (type)
            {
                case pathType.sight:
                    while (movePath.Count > 0 && movePath[movePath.Count - 1].moveType == moveTypes.move)
                    {
                        pos -= Vector2Int.RoundToInt(new Vector2(movePath[movePath.Count - 1].direction.x, movePath[movePath.Count - 1].direction.z));
                        movePath.RemoveAt(movePath.Count - 1);
                    }
                    movePath.Add(new moveTask(moveTypes.sight, 1.25f));
                    break;
                case pathType.checkHide:
                    movePath.Add(new moveTask(moveTypes.rotate, hideWallRotation));
                    rot = hideWallRotation;
                    movePath.Add(new moveTask(moveTypes.hideCheck));
                    break;
                case pathType.lookAround:
                    movePath.Add(new moveTask(moveTypes.wait, 0.25f));
                    foreach (Vector3 rotation in LookAround(pos, Quaternion.Euler(rot)))
                    {
                        movePath.Add(new moveTask(moveTypes.rotate, rotation));
                        rot = rotation;
                        movePath.Add(new moveTask(moveTypes.sight, 0.75f));
                    }
                    movePath.Add(new moveTask(moveTypes.wait, 0.75f));
                    break;
                case pathType.roam:
                    while (movePath.Count > 0 && GetDirections(pos, maze.obstacleMatrix).Count < 3 && movePath[movePath.Count - 1].moveType == moveTypes.move)
                    {
                        pos -= Vector2Int.RoundToInt(new Vector2(movePath[movePath.Count - 1].direction.x, movePath[movePath.Count - 1].direction.z));
                        movePath.RemoveAt(movePath.Count - 1);
                        movePath.Add(new moveTask(moveTypes.sight, 1.5f));
                    }
                    break;
            }
        }
        return movePath;
    }

    private List<moveTask> PostHunt(Vector2Int pos, Vector3 rot, Vector2Int prevPos, float hideCheckChance, int maxPathRecurrsion, float pathChance)
    {
        List<moveTask> mainStack = new List<moveTask>();
        List<Vector2Int> posRecurrsion = new List<Vector2Int>();
        Vector2Int target;
        bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
        unvisitedSpots[prevPos.x, prevPos.y] = false;
        while (true)
        {
            if (DisplayAIBehaviour) Debug.Log("currentPos: " + pos);
            List<Vector2Int> avaliableDirections = GetDirections(pos, unvisitedSpots);
            if(avaliableDirections.Count > 0 && posRecurrsion.Count < maxPathRecurrsion)
            {
                Vector2Int pickedDirection = avaliableDirections[Random.Range(0, avaliableDirections.Count)];               
                unvisitedSpots[(pos + pickedDirection).x, (pos + pickedDirection).y] = false;
                if(Random.Range(0,100) < pathChance)
                {
                    posRecurrsion.Add(pos);
                    target = LostSightEstimation(pos, pickedDirection);
                    if (DisplayAIBehaviour)
                    {
                        Debug.Log("pickedDirection: " + (pos + pickedDirection));
                        Debug.DrawLine(new Vector3((pos + pickedDirection).x - 0.5f, 0.1f, (pos + pickedDirection).y - 0.5f), new Vector3((pos + pickedDirection).x + 0.5f, 0.1f, (pos + pickedDirection).y + 0.5f), Color.yellow, 4f);
                        Debug.DrawLine(new Vector3((pos + pickedDirection).x - 0.5f, 0.1f, (pos + pickedDirection).y + 0.5f), new Vector3((pos + pickedDirection).x + 0.5f, 0.1f, (pos + pickedDirection).y - 0.5f), Color.yellow, 4f);
                        Debug.Log("goingTo: " + target);
                        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.1f, target.y - 0.5f), new Vector3(target.x + 0.5f, 0.1f, target.y + 0.5f), Color.green, 4f);
                        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.1f, target.y + 0.5f), new Vector3(target.x + 0.5f, 0.1f, target.y - 0.5f), Color.green, 4f);               
                    }
                    mainStack.AddRange(GeneratePath(ref pos, ref rot, target, new pathType[] { pathType.roam, pathType.lookAround }, 0, hideCheckChance));
                    prevPos = FindPreviousPosInStack(mainStack, pos);
                    if(prevPos != Vector2Int.zero) unvisitedSpots[prevPos.x, prevPos.y] = false;
                }
                else if (DisplayAIBehaviour)
                {
                    Debug.Log("ignoredDirection: " + (pos + pickedDirection));
                    Vector3 targetPos = new Vector3((pos + pickedDirection).x, 0.1f, (pos + pickedDirection).y);
                    Debug.DrawLine(new Vector3(targetPos.x - 0.5f, targetPos.y, targetPos.z - 0.5f), new Vector3(targetPos.x + 0.5f, targetPos.y, targetPos.z + 0.5f), Color.blue, 4f);
                    Debug.DrawLine(new Vector3(targetPos.x - 0.5f, targetPos.y, targetPos.z + 0.5f), new Vector3(targetPos.x + 0.5f, targetPos.y, targetPos.z - 0.5f), Color.blue, 4f);
                }
            }
            else
            {
                if (posRecurrsion.Count == 0 && avaliableDirections.Count == 0) break;    //Main break
                mainStack.AddRange(GeneratePath(ref pos, ref rot, posRecurrsion[posRecurrsion.Count - 1], new pathType[] { }));
                posRecurrsion.RemoveAt(posRecurrsion.Count - 1);
            }
        }
        if (mainStack.Count == 0) mainStack.Add(new moveTask(moveTypes.wait, 2f));
        return mainStack;
    }

    private Vector2Int FindPreviousPosInStack(List<moveTask> stack, Vector2Int currentPos)
    {
        int index = stack.Count - 1;
        while (index >= 0 && stack[index].moveType != moveTypes.move) index--;
        if (index == -1) return Vector2Int.zero;
        else return currentPos - Vector2Int.RoundToInt(new Vector2(stack[index].direction.x, stack[index].direction.z));
    }

    private Vector2Int LostSightEstimation(Vector2Int pos, Vector2Int direction)
    {
        Debug.ClearDeveloperConsole();
        Debug.Log("LostSight Triggered!");
        Debug.Log("pos: " + pos);
        Debug.Log("dir: " + direction);
        bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
        foreach(Vector2Int avaliableDirection in GetDirections(pos, unvisitedSpots)) if(avaliableDirection != direction) unvisitedSpots[(pos + avaliableDirection).x, (pos + avaliableDirection).y] = false;
        while (true)
        {
            unvisitedSpots[pos.x, pos.y] = false;
            List<Vector2Int> directions = GetDirections(pos, unvisitedSpots);
            if (directions.Count != 1) break;
            pos += directions[0];
        }
        Debug.Log("target: " + pos);
        return pos;
    }

    private void ChangeState(States toChangeState)
    {
        state = toChangeState;
        switch (toChangeState)
        {
            
            case States.Patrol:
                nextTMdf = 1 / patrolSpeed;
                rend.material.color = Color.green;
                break;
            case States.Suspicious:
                nextTMdf = 1 / suspiciousSpeed;
                rend.material.color = Color.yellow;
                break;
            case States.Alerted:
                nextTMdf = 1 / alertedSpeed;
                rend.material.color = new Color(1, 0.392f, 0);
                break;
            case States.Hunting:
                patrolPos = Vector2Int.zero;
                nextTMdf = 1 / huntingSpeed;
                rend.material.color = Color.red;
                break;
            case States.Seeking:
                nextTMdf = 1 / seekingSpeed;
                rend.material.color = Color.magenta;
                break;
        }
    }

    private IEnumerator Rotate(Vector3 targetRot)
    {  
        startRot = transform.rotation;
        endRot = Quaternion.Euler(targetRot);
        nextRot = endRot.eulerAngles;
        t = 0;
        movement = Movements.Rotating;
        while (t / tMdf < 1) yield return null;
        movement = Movements.Idle;
        yield break;
    }

    private IEnumerator Move(Vector3 targetMove)
    {
        startPos = transform.position;
        endPos = transform.position + targetMove;
        nextPos = Vector2Int.RoundToInt(new Vector2(endPos.x, endPos.z));
        t = 0;
        movement = Movements.Moving;
        while (t / tMdf < 1) yield return null;
        movement = Movements.Idle;
        yield break;
    }


    private List<Vector3> LookAround(Vector2Int pos, Quaternion rot)
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] lookAroundRots = { new Vector3(0, 270, 0), new Vector3(0, 90, 0), /*new Vector3(0, 0, 0)*/ };
        foreach(Vector3 lookRot in lookAroundRots)
        {
            Quaternion tempRot = rot * Quaternion.Euler(lookRot);
            if (tempRot.eulerAngles == new Vector3(0, 90, 0) && maze.obstacleMatrix[pos.x + 1, pos.y]) directions.Add(new Vector3(0, 90, 0));
            if (tempRot.eulerAngles == new Vector3(0, 270, 0) && maze.obstacleMatrix[pos.x - 1, pos.y]) directions.Add(new Vector3(0, 270, 0));
            if (tempRot.eulerAngles == new Vector3(0, 0, 0) && maze.obstacleMatrix[pos.x, pos.y + 1]) directions.Add(new Vector3(0, 0, 0));
            if (tempRot.eulerAngles == new Vector3(0, 180, 0) && maze.obstacleMatrix[pos.x, pos.y - 1]) directions.Add(new Vector3(0, 180, 0));
        }
        return directions;
    }

    List<Vector2Int> GetDirections(Vector2Int pos, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        try
        {
            if (pos.x < GameParameters.size && unvisiteSpots[pos.x + 1, pos.y]) directions.Add(Vector2Int.right);
            if (pos.x > 0 && unvisiteSpots[pos.x - 1, pos.y]) directions.Add(Vector2Int.left);
            if (pos.y < GameParameters.size && unvisiteSpots[pos.x, pos.y + 1]) directions.Add(Vector2Int.up);
            if (pos.y > 0 && unvisiteSpots[pos.x, pos.y - 1]) directions.Add(Vector2Int.down);
        }
        catch (System.Exception)
        {
            Debug.Log(pos);
            throw;
        }
        return directions;
    }

    Vector2Int randomPos(int mazeSize)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(Random.Range(1, mazeSize), Random.Range(1, mazeSize));
        } while (!maze.obstacleMatrix[pos.x, pos.y]);
        return pos;
    }
}
