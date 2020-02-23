using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField]
    private States state = States.Patrol;

    //START OF CUSTOM CONTROLER
    [Space(6)]
    [Header("MainSwitch")]
    public bool overrideParameters;
    [Space(6)]
    public bool halt;
    [Space(6)]
    [Header("Sight")]
    public float sightBase;
    public float sightRange;
    public float sightRangeBonus;
    public int fov = 50;
    [Header("Hearing")]
    public float hearWalkBase;
    public float hearWalkRange;
    public float hearWalkRangeBonus;
    [Space(6)]
    public float hearRunBase;
    public float hearRunRange;
    public float hearRunRangeBonus;
    [Header("Light interaction")]
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
    public float alertedPathChance;
    public int alertedMaxPathRecursion;
    [Space(6)]
    public float seekingHideCheckProbability;
    public float seekingPathChance;
    public int seekingMaxPathRecursion;
    [Space(6)]
    public bool DisplayAIBehaviour;
    //END OF CUSTOM CONTROLER
    [Space(12)]
    public bool optimizePathFinding;
    public bool cachePaths;
    private struct moveTask
    {
        public moveTypes moveType;
        public Vector3 vector;
        public float time;
        public moveTask(moveTypes Movetype, Vector3 Direction)
        {
            moveType = Movetype;
            vector = Direction;
            time = 0.5f;
        }
        public moveTask(moveTypes Movetype, float Time)
        {
            moveType = Movetype;
            vector = Vector3.zero;
            time = Time;
        }
    };
    private enum moveTypes { move, rotate, hideCheck, sight, wait };
    private enum pathType { lookAround, sight, checkHide, roam, distance };
    private enum States { Patrol, Suspicious, Alerted, Hunting, Seeking };
    private GameParameters.AIStruct parameters;
    private MazeSystem maze;
    private Transform player;
    private PlayerController playerBehavior;
    private Transform playerLight;
    private Vector2Int currentPlayerPos;
    private Vector2Int prevPlayerPos;
    private Vector2Int prevPos;
    private Vector2Int currentPos;
    private Vector2Int nextPos;
    private Vector3 nextRot;
    private Vector2Int patrolPos;
    private bool newPath = false;
    private float toWaitTime = 0;
    private float currentSpeed;
    private bool sense = true;
    private Vector3 sightDifferential = new Vector3(0, 1.5f, 0);
    private List<moveTask> taskStack = new List<moveTask>();
    private List<moveTask> pushTaskStack = new List<moveTask>();
    private Vector2Int[,] toPlayerPathCache;
    void Start()
    {
        parameters = GameParameters.AI;
        player = GameObject.Find("Player").transform;
        playerLight = GameObject.Find("Torch").transform;
        playerBehavior = player.GetComponent<PlayerController>();
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        toPlayerPathCache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
        optimizePathFinding = GameParameters.settings.pathFindingUseCaching;
        Vector2Int tempPos;
        do tempPos = randomPos(GameParameters.maze.mazeSize);
        while (!maze.obstacleMatrix[tempPos.x, tempPos.y]);
        transform.position = new Vector3(tempPos.x, 0, tempPos.y);
        nextPos = tempPos;
        nextRot = transform.rotation.eulerAngles;
        UpdateVars();
        ChangeState(States.Patrol);
        StartCoroutine(Navigation());
        StartCoroutine(Sense());
    }

    private void Update()
    {
        UpdateVars();
    }

    private void UpdateVars()
    {
        Vector2Int tempPos = Vector2Int.RoundToInt(new Vector2(player.position.x, player.position.z));
        if (currentPlayerPos != tempPos)
        {         
            prevPlayerPos = currentPlayerPos;
            currentPlayerPos = tempPos;
            if (DisplayAIBehaviour)
            {
                DebugPoint(currentPlayerPos, Color.magenta, 0.75f);
                DebugPoint(prevPlayerPos, Color.cyan, 0.5f);
            }
        }

        tempPos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.z));
        if (currentPos != tempPos)
        {
            prevPos = currentPos;
            currentPos = tempPos;
            if (DisplayAIBehaviour)
            {
                DebugPoint(currentPos, Color.green, 0.75f);
                DebugPoint(prevPos, new Color(0, 0.1f, 0.25f, 1), 0.5f);
            }
        }
        if(overrideParameters) parameters = new GameParameters.AIStruct(new GameParameters.sight(sightBase, sightRange, sightRangeBonus, fov),
                                                                        new GameParameters.hearing(hearWalkBase, hearWalkRange, hearWalkRangeBonus, hearRunBase, hearRunRange, hearRunRangeBonus), 
                                                                        new GameParameters.lightSense(lightBase, lightRange, lightRangeBonus), 
                                                                        new GameParameters.speed(patrolSpeed, suspiciousSpeed, alertedSpeed, huntingSpeed, seekingSpeed), 
                                                                        new GameParameters.roaming(patrolHideCheckProbability, patrolLookAroundProbability, alertedHideCheckProbability, alertedPathChance, alertedMaxPathRecursion, seekingHideCheckProbability, seekingPathChance, seekingMaxPathRecursion));
    }

    IEnumerator Navigation()
    {
        moveTask currentTask;
        float waitTime;
        while (true)
        {
            while (halt) yield return null;
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
            currentTask = taskStack[0];
            taskStack.RemoveAt(0);
            switch (currentTask.moveType)
            {
                case moveTypes.move:
                    yield return StartCoroutine(Move(currentTask.vector, 1 / currentSpeed));
                    break;
                case moveTypes.rotate:
                    yield return StartCoroutine(Rotate(currentTask.vector, 1 / currentSpeed));
                    break;
                case moveTypes.sight:
                    waitTime = currentTask.time;
                    while(waitTime > 0 && !newPath)
                    {
                        if(!Sight(player) || playerBehavior.playerState == PlayerController.States.Hided) waitTime -= Time.deltaTime;
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
                case moveTypes.hideCheck:
                    if (maze.mazeMatrix[Mathf.RoundToInt(currentTask.vector.x), Mathf.RoundToInt(currentTask.vector.z)].GetComponent<MazeBlock>().hideWall == playerBehavior.inHideWall)
                    {
                        if (maze.debugMazePath) StartCoroutine(maze.mazeMatrix[Mathf.RoundToInt(currentTask.vector.x), Mathf.RoundToInt(currentTask.vector.z)].GetComponent<MazeBlock>().Highlight(3f, Color.green));
                        StartCoroutine(playerBehavior.Hide(playerBehavior.inHideWall, false));
                    }
                    else if (maze.debugMazePath) StartCoroutine(maze.mazeMatrix[Mathf.RoundToInt(currentTask.vector.x), Mathf.RoundToInt(currentTask.vector.z)].GetComponent<MazeBlock>().Highlight(1.5f, Color.red));
                    yield return new WaitForSeconds(1.5f);
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
                if (finishedPreviousStack) patrolPos = randomPos(GameParameters.maze.mazeSize);
                ChangeState(States.Patrol);
                return GeneratePath(nextPos, nextRot, patrolPos, new pathType[] { pathType.roam }, parameters.roaming.patrolLookAroundProbability, parameters.roaming.patrolHideCheckProbability);
            case States.Alerted:
                if (parameters.roaming.alertedMaxPathRecursion > 0)
                {
                    ChangeState(States.Seeking);
                    return PostHunt(nextPos, nextRot, prevPos, parameters.roaming.alertedHideCheckProbability, parameters.roaming.alertedMaxPathRecursion, parameters.roaming.alertedPathChance);
                }
                else return PostNavigation(States.Patrol, finishedPreviousStack);
            case States.Hunting:
                if (parameters.roaming.seekingMaxPathRecursion > 0)
                {
                    ChangeState(States.Seeking);
                    return PostHunt(nextPos, nextRot, prevPos, parameters.roaming.patrolHideCheckProbability, parameters.roaming.seekingMaxPathRecursion, parameters.roaming.seekingPathChance);
                }
                else return PostNavigation(States.Patrol, finishedPreviousStack);
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
                fromPlayerDistanceVector = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));

                //Sight
                spotted = Sight(player) && playerBehavior.playerState != PlayerController.States.Hided;
                if (spotted)
                {
                    mainSense += (parameters.sight.sightBase + Mathf.Clamp(parameters.sight.sightRangeBonus - (fromPlayerDistanceVector * (parameters.sight.sightRangeBonus / parameters.sight.sightRange)), 0, Mathf.Infinity)) / 4;
                }

                //Hearing
                heared = playerBehavior.playerState == PlayerController.States.Walking && fromPlayerDistancePoint <= parameters.hearing.hearWalkRange || playerBehavior.playerState == PlayerController.States.Running && fromPlayerDistancePoint <= parameters.hearing.hearRunRange;
                if(heared)
                {
                    if (playerBehavior.playerState == PlayerController.States.Walking) mainSense += (parameters.hearing.hearWalkBase + Mathf.Clamp(parameters.hearing.hearWalkRangeBonus - (fromPlayerDistancePoint * (parameters.hearing.hearWalkRangeBonus / parameters.hearing.hearWalkRange)), 0, Mathf.Infinity)) / 4;
                    if (playerBehavior.playerState == PlayerController.States.Running) mainSense += (parameters.hearing.hearRunBase + Mathf.Clamp(parameters.hearing.hearRunRangeBonus - (fromPlayerDistancePoint * (parameters.hearing.hearRunRangeBonus / parameters.hearing.hearRunRange)), 0, Mathf.Infinity)) / 4;
                }

                //Light interaction
                Physics.Raycast(transform.position + sightDifferential, player.position - (transform.position + sightDifferential), out RaycastHit hit);
                lighted = playerBehavior.lightEnabled && Vector3.Angle(playerLight.forward, (transform.position + sightDifferential / 2) - playerLight.position) < playerLight.GetComponent<Light>().spotAngle / 2 && hit.transform == player && fromPlayerDistanceVector <= parameters.lightSense.lightRange;
                if (lighted)
                {
                    mainSense += (parameters.lightSense.lightBase + Mathf.Clamp(parameters.lightSense.lightRangeBonus - (fromPlayerDistanceVector * (parameters.lightSense.lightRangeBonus / parameters.lightSense.lightRange)), 0, Mathf.Infinity)) / 4;
                }

                //Conclusion
                if (!spotted && !heared && !lighted) mainSense -= senseMemory / 4;
                mainSense = Mathf.Clamp(mainSense, 0, maxThreshold);
                if (DisplayAIBehaviour && false)
                {
                    Debug.Log("====================");
                    Debug.Log("STATE: " + state);
                    Debug.Log("MAIN SENSE: " + mainSense);
                    if (overrideParameters) Debug.Log("SENSE DIFFICULTY: CustomValues");
                    Debug.Log("SIGHT: " + spotted);
                    Debug.Log("HEAR: " + heared);
                    Debug.Log("LIGHT: " + lighted);
                    Debug.Log("pathDistance: " + fromPlayerDistancePoint);
                    Debug.Log("vectorDistance: " + fromPlayerDistanceVector);
                }
                if (mainSense > suspicousThreshold && (state == States.Patrol || state == States.Seeking) && (spotted || heared || lighted))
                {
                    if (DisplayAIBehaviour) Debug.Log("suspicious!");
                    ChangeState(States.Suspicious);
                    NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { pathType.sight }));
                }
                else if (mainSense > alertThreshold && state == States.Suspicious && (spotted || heared || lighted))
                {
                    if (DisplayAIBehaviour) Debug.Log("alerted!");
                    ChangeState(States.Alerted);
                    NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { pathType.lookAround }));
                }
                else if ((mainSense > huntThreshold && state == States.Alerted) || state == States.Hunting)
                {
                    if (state != States.Hunting)
                    {
                        ChangeState(States.Hunting); 
                        if (DisplayAIBehaviour) Debug.Log("Hunt!"); 
                    }
                    if (playerBehavior.playerState == PlayerController.States.Hiding && spotted || playerBehavior.playerState == PlayerController.States.Hided && lighted)
                    {
                        
                        Transform hideBlock = playerBehavior.inHideWall.parent;
                        if (DisplayAIBehaviour)
                        {
                            Debug.Log("hideCheck!");
                            Debug.Log("targetedHide: " + Vector2Int.RoundToInt(new Vector2(hideBlock.position.x, hideBlock.position.z)));
                        }
                      
                        NewPath(GeneratePath(nextPos, nextRot, Vector2Int.RoundToInt(new Vector2(hideBlock.position.x, hideBlock.position.z)), new pathType[] { pathType.checkHide } ));
                    }
                    else if (prevSpotted && !spotted && playerBehavior.playerState != PlayerController.States.Hided)
                    {
                        if(DisplayAIBehaviour) Debug.Log("lostSight!");
                        NewPath(GeneratePath(nextPos, nextRot, LostSightEstimation(currentPlayerPos, currentPlayerPos - prevPlayerPos), new pathType[] { pathType.lookAround }));
                    }
                    else if (spotted)
                    {
                        NewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathType[] { }));
                    }
                }
                prevSpotted = spotted;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private bool Sight(Transform target)
    {
        if(Physics.Raycast(transform.position + sightDifferential, target.position - (transform.position + sightDifferential), out RaycastHit hit, parameters.sight.sightRange))
        {
            if (DisplayAIBehaviour)
            {
                Debug.DrawRay(transform.position + sightDifferential, Quaternion.Euler(0, parameters.sight.fov / 2, 0) * transform.forward * parameters.sight.sightRange, Color.cyan, 0.25f);
                Debug.DrawRay(transform.position + sightDifferential, Quaternion.Euler(0, parameters.sight.fov / -2, 0) * transform.forward * parameters.sight.sightRange, Color.cyan, 0.25f);
                if (hit.transform == target && Vector2.Angle(new Vector2(target.position.x, target.position.z), new Vector2(transform.position.x, transform.position.z)) < parameters.sight.fov / 2) Debug.DrawLine(transform.position + sightDifferential, target.transform.position, Color.green, 0.25f);
                else Debug.DrawLine(transform.position + sightDifferential, target.transform.position, Color.magenta, 0.25f);
            }
            return hit.transform == target && Vector2.Angle(new Vector2((target.position - transform.position).x, (target.position - transform.position).z), new Vector2(transform.forward.x, transform.forward.z)) < parameters.sight.fov / 2;
        }
        return false;
    }

    private void NewPath(List<moveTask> NewMoveTaskStack)
    {
        newPath = true;
        pushTaskStack = NewMoveTaskStack;
        if (DisplayAIBehaviour)
        {
            Vector3 tempPos = new Vector3(nextPos.x, 15, nextPos.y);
            foreach (moveTask task in NewMoveTaskStack)
            {
                if (task.moveType == moveTypes.move) tempPos += task.vector;
            }
            //Debug.Log(tempPos);
            DebugPoint(tempPos, Color.red, 3f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Trap")
        {
            toWaitTime += 5;
            Destroy(other.gameObject);
        }
    }

    public IEnumerator GetShot()
    {
        toWaitTime += 10;
        sense = false;
        yield return new WaitForSeconds(4.5f);
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
        bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
        List<Vector2Int> pointPath = new List<Vector2Int>();
        List<moveTask> movePath = new List<moveTask>();
        bool roam = false;
        foreach (pathType type in pathTypes) if (type == pathType.roam) roam = true;
        Vector2Int orgPos = pos;
        Vector2Int alternativeTarget = Vector2Int.zero;
        if (!maze.obstacleMatrix[target.x, target.y])
        {
            if(maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall != null)
            {
                alternativeTarget = target;
                Vector3 tempVector = maze.mazeMatrix[target.x, target.y].GetComponent<MazeBlock>().hideWall.forward;
                target += Vector2Int.RoundToInt(new Vector2(tempVector.x, tempVector.z));
            }
            else
            {
                Debug.LogWarning("No hide found at " + target);
                bool randomTarget = false;
                try { target += GetDirections(target, unvisitedSpots)[0]; }
                catch (System.Exception) { target = randomPos(GameParameters.maze.mazeSize); randomTarget = true; }
                if(!randomTarget) Debug.LogWarning("Setting target to " + target + " (nearest found)");
                else Debug.LogWarning("Setting target to " + target + " (random)");
            }
        }
        //Point path
        pointPath = GenerateVectorStack(pos, target, unvisitedSpots);

        //Move path
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
                    movePath.Add(new moveTask(moveTypes.hideCheck, hide.transform.position));
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
        bool distance = false;
        foreach (pathType type in pathTypes)
        {
            switch (type)
            {
                case pathType.sight:
                    while (movePath.Count > 0 && movePath[movePath.Count - 1].moveType == moveTypes.move)
                    {
                        pos -= Vector2Int.RoundToInt(new Vector2(movePath[movePath.Count - 1].vector.x, movePath[movePath.Count - 1].vector.z));
                        movePath.RemoveAt(movePath.Count - 1);
                    }
                    movePath.Add(new moveTask(moveTypes.sight, 1.25f));
                    break;
                case pathType.checkHide:
                    if(rot != (maze.mazeMatrix[alternativeTarget.x, alternativeTarget.y].GetComponent<MazeBlock>().hideWall.rotation * Quaternion.Euler(0, 180, 0)).eulerAngles)
                    {
                        rot = (maze.mazeMatrix[alternativeTarget.x, alternativeTarget.y].GetComponent<MazeBlock>().hideWall.rotation * Quaternion.Euler(0, 180, 0)).eulerAngles;
                        movePath.Add(new moveTask(moveTypes.rotate, rot));
                    }               
                    movePath.Add(new moveTask(moveTypes.hideCheck, new Vector3(alternativeTarget.x, 0, alternativeTarget.y)));
                    Debug.Log(movePath.Count);
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
                        pos -= Vector2Int.RoundToInt(new Vector2(movePath[movePath.Count - 1].vector.x, movePath[movePath.Count - 1].vector.z));
                        movePath.RemoveAt(movePath.Count - 1);
                        movePath.Add(new moveTask(moveTypes.sight, 1.5f));
                    }
                    break;
                case pathType.distance:
                    distance = true;
                    while (movePath.Count > 0 && movePath[0].moveType != moveTypes.move) movePath.RemoveAt(0);
                    break;
            }
        }
        if (movePath.Count == 0 && !distance) movePath.Add(new moveTask(moveTypes.wait, 0.25f)); //Prevents looping in rare situations
        return movePath;
    }

    List<Vector2Int> GenerateVectorStack(Vector2Int pos, Vector2Int target, bool[,]obstacleMatrix)
    {
        bool toPlayer = target == currentPlayerPos;
        DebugPoint(target, Color.red, 1f);
        List<Vector2Int> returnList = new List<Vector2Int>();
        Vector2Int direction = Vector2Int.zero;
        Color debugColor = new Color32(255, 123, 0, 255);
        while (pos != target)
        {
            if (toPlayer && cachePaths && toPlayerPathCache[pos.x, pos.y] != Vector2Int.zero && (direction + toPlayerPathCache[pos.x, pos.y]) != Vector2Int.zero) direction = toPlayerPathCache[pos.x, pos.y];
            else direction = GetDirections(pos, obstacleMatrix, target);         
            DebugPoint(pos, debugColor, 0.2f);
            if (direction != Vector2Int.zero) //Stack forward
            {
                if (toPlayer && cachePaths) toPlayerPathCache[pos.x, pos.y] = direction;
                pos += direction;
                returnList.Add(direction);
                obstacleMatrix[pos.x, pos.y] = false;
            }
            else //Recurse back
            {     
                pos -= returnList[returnList.Count - 1];
                returnList.RemoveAt(returnList.Count - 1);
                if (toPlayer && cachePaths) toPlayerPathCache[pos.x, pos.y] = Vector2Int.zero;
            }
        }
        return returnList;
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
            List<Vector2Int> avaliableDirections = GetDirections(pos, unvisitedSpots);
            if (avaliableDirections.Count > 0 && posRecurrsion.Count < maxPathRecurrsion)
            {
                Vector2Int pickedDirection = avaliableDirections[Random.Range(0, avaliableDirections.Count)];
                unvisitedSpots[(pos + pickedDirection).x, (pos + pickedDirection).y] = false;
                if (Random.Range(0, 100) < pathChance)
                {
                    posRecurrsion.Add(pos);
                    target = LostSightEstimation(pos, pickedDirection);
                    if (DisplayAIBehaviour)
                    {
                        DebugPoint(pos + pickedDirection, Color.yellow, 5f);
                        DebugPoint(target, Color.green, 4.5f);
                    }
                    mainStack.AddRange(GeneratePath(ref pos, ref rot, target, new pathType[] { pathType.roam, pathType.lookAround }, 0, hideCheckChance));
                    prevPos = FindPreviousPosInStack(mainStack, pos);
                    if (prevPos != Vector2Int.zero) unvisitedSpots[prevPos.x, prevPos.y] = false;
                }
                else if (DisplayAIBehaviour) DebugPoint(pos + pickedDirection, Color.blue, 4f);
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
        else return currentPos - Vector2Int.RoundToInt(new Vector2(stack[index].vector.x, stack[index].vector.z));
    }

    private Vector2Int LostSightEstimation(Vector2Int pos, Vector2Int direction)
    {
        if (DisplayAIBehaviour)
        {
            DebugPoint(pos, new Color(1, 0.5f, 0, 1), 5);
            DebugPoint(pos + direction, new Color(1, 0.75f, 0, 1), 5);
           
        }
        bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
        foreach(Vector2Int avaliableDirection in GetDirections(pos, unvisitedSpots)) if(avaliableDirection != direction) unvisitedSpots[(pos + avaliableDirection).x, (pos + avaliableDirection).y] = false;
        while (true)
        {
            unvisitedSpots[pos.x, pos.y] = false;
            List<Vector2Int> directions = GetDirections(pos, unvisitedSpots);
            if (directions.Count != 1) break;
            pos += directions[0];
        }
        if (DisplayAIBehaviour)
        {
            DebugPoint(pos, Color.yellow, 5);
            Debug.Log("lostSightEstimationTarget: " + pos);
        }
        return pos;
    }

    private void ChangeState(States toChangeState)
    {
        state = toChangeState;
        switch (toChangeState)
        {          
            case States.Patrol:
                currentSpeed = parameters.speed.patrolSpeed;
                break;
            case States.Suspicious:
                currentSpeed = parameters.speed.suspiciousSpeed;
                break;
            case States.Alerted:
                currentSpeed = parameters.speed.alertedSpeed;
                break;
            case States.Hunting:
                patrolPos = Vector2Int.zero;
                currentSpeed = parameters.speed.huntingSpeed;
                break;
            case States.Seeking:
                currentSpeed = parameters.speed.seekingSpeed;
                break;
        }
        currentSpeed = Mathf.Clamp(currentSpeed, 0.25f, 20);
    }

    private IEnumerator Rotate(Vector3 targetRot, float length)
    {  
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(targetRot);
        float t = 0;
        nextRot = endRot.eulerAngles;
        while (t / length < 1)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, t / length);
            yield return null;
        }
        yield break;
    }

    private IEnumerator Move(Vector3 direction, float length)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + direction;
        float t = 0;
        nextPos = Vector2Int.RoundToInt(new Vector2(endPos.x, endPos.z));
        while (t / length < 1)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / length);
            yield return null;
        }
        yield break;
    }


    private List<Vector3> LookAround(Vector2Int pos, Quaternion rot)
    {
        List<Vector3> directions = new List<Vector3>();
        Vector3[] lookAroundRots = { new Vector3(0, 270, 0), new Vector3(0, 90, 0) };
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

    Vector2Int GetDirections(Vector2Int pos, bool[,] unvisitedSpots, Vector2Int optimizeByTarget)
    {
        List<Vector2Int> avaliableDirections = GetDirections(pos, unvisitedSpots);
        if (avaliableDirections.Count == 0) return Vector2Int.zero;
        if (avaliableDirections.Count == 1) return avaliableDirections[0];
        List<Vector2Int> optimizedOrder;
        Vector2Int dir = optimizeByTarget - pos;

        if(dir.x <= 0) //LEFT
        {
            if(dir.y <= 0) //LEFT-DOWN
            {
                if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) optimizedOrder = new List<Vector2Int> { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };
                else optimizedOrder = new List<Vector2Int> { Vector2Int.left, Vector2Int.down, Vector2Int.up, Vector2Int.right };
            }
            else //LEFT-UP
            {
                if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) optimizedOrder = new List<Vector2Int> { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down };
                else optimizedOrder = new List<Vector2Int> { Vector2Int.left, Vector2Int.up, Vector2Int.down, Vector2Int.right };
            }
        }
        else //RIGHT
        {
            if (dir.y <= 0) //RIGHT-DOWN
            {
                if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) optimizedOrder = new List<Vector2Int> { Vector2Int.down, Vector2Int.right, Vector2Int.left, Vector2Int.up };
                else optimizedOrder = new List<Vector2Int> { Vector2Int.right, Vector2Int.down, Vector2Int.up, Vector2Int.left };
            }
            else //RIGHT-UP
            {
                if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x)) optimizedOrder = new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.left, Vector2Int.down };
                else optimizedOrder = new List<Vector2Int> { Vector2Int.right, Vector2Int.up, Vector2Int.down, Vector2Int.left };
            }
        }
        foreach(Vector2Int vector in optimizedOrder)
        {
            if (avaliableDirections.Contains(vector)) return vector;
        }
        return avaliableDirections[Random.Range(0, avaliableDirections.Count)];
    }
    List<Vector2Int> GetDirections(Vector2Int pos, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (pos.x < GameParameters.maze.mazeSize && unvisiteSpots[pos.x + 1, pos.y]) directions.Add(Vector2Int.right);
        if (pos.x > 0 && unvisiteSpots[pos.x - 1, pos.y]) directions.Add(Vector2Int.left);
        if (pos.y < GameParameters.maze.mazeSize && unvisiteSpots[pos.x, pos.y + 1]) directions.Add(Vector2Int.up);
        if (pos.y > 0 && unvisiteSpots[pos.x, pos.y - 1]) directions.Add(Vector2Int.down);
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

    private void DebugPoint(Vector3 target, Color color, float length)
    {
        DebugPoint(new Vector2(target.x, target.z), color, length);
    }
    private void DebugPoint(Vector2 target, Color color, float length)
    {
        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.01f, target.y - 0.5f), new Vector3(target.x + 0.5f, 0.01f, target.y + 0.5f), color, length);
        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.01f, target.y + 0.5f), new Vector3(target.x + 0.5f, 0.01f, target.y - 0.5f), color, length);
    }
}
