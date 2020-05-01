using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField]
    private States state = States.Patrol;

    /*
    //CUSTOM CONTROLER
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
    */

    //NAV CONTROLS
    public Vector2Int controlTargetVect;
    public Transform controlTargetTransform;
    public string controlModifiers;
    public bool controlSend = false;


    //ENUMS & STRUCTS
    private enum States { Patrol, Suspicious, Alerted, Hunting, Seeking };

    private struct navTask
    {
        public navTaskType type;
        public Vector2Int targetPosition;
        public Vector2Int targetDirection;
        public Transform targetTransform;
        public float targetTime;
        public navTask(navTaskType NavType, Vector2Int TargetPos, Vector2Int TargetDir)
        {
            type = NavType;
            targetPosition = TargetPos;
            targetDirection = TargetDir;
            targetTransform = null;
            targetTime = 0;
        }
        public navTask(navTaskType NavType, Transform TargetTransform, Vector2Int TargetDir)
        {
            type = NavType;
            targetPosition = Vector2Int.zero;
            targetDirection = TargetDir;
            targetTransform = TargetTransform;
            targetTime = 0;
        }
        public navTask(navTaskType NavType, Vector2Int TargetPos, Vector2Int TargetDir, float TargetTime)
        {
            type = NavType;
            targetPosition = TargetPos;
            targetDirection = TargetDir;
            targetTransform = null;
            targetTime = TargetTime;
        }
        public navTask(navTaskType NavType, Vector2Int TargetPos, Vector2Int TargetDir, float TargetTime, Transform TargetTransform)
        {
            type = NavType;
            targetPosition = TargetPos;
            targetDirection = TargetDir;
            targetTransform = TargetTransform;
            targetTime = TargetTime;
        }
    };
    private enum navTaskType { idle, startMove, move, stopMove, move180, liveMove, rotate, look, hideCheck, stun, kill };

    private struct navStackParameters
    {
        public Vector2Int targetPos;
        public Transform targetTransform;
        public List<navStackModifiers> modifiers;
        public Vector2Int[,] cache;
        public navStackParameters(Vector2Int Target, List<navStackModifiers> Modifiers)
        {
            targetPos = Target;
            modifiers = Modifiers;
            cache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
            targetTransform = null;
        }
        public navStackParameters(Transform Target, List<navStackModifiers> Modifiers)
        {
            targetTransform = Target;
            modifiers = Modifiers;
            cache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
            targetPos = Vector2Int.zero;

        }
    }
    private enum navStackModifiers { lookAroundAtEnd, sight, checkHide, liveTarget, patrolRoam, alertedRoam, seekingRoam };

    //NAVIGATION
    List<navTask> navStack = new List<navTask>();
    private navTask currentNavTask;
    private bool interuptNavigation = false;
    private Vector2 currentPos;
    private Vector2Int currentDir;

    //REFERENCES
    private GameParameters.AIStruct parameters;
    private MazeSystem maze;
    private Animator animator;

    //PATH CACHES
    private Vector2Int[,] toPlayerPathCache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
    private Vector2Int[,] toPatrolPathCache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];

    void Start()
    {
        animator = GetComponent<Animator>();
        maze = GameObject.Find("MazeSystem").GetComponent<MazeSystem>();
        currentPos = new Vector2(transform.position.x, transform.position.z);
        currentDir = new Vector2Int(0, 1);
        animator.SetInteger("MoveType", 0);
        StartCoroutine(VarsUpdater());
        StartCoroutine(Navigation());
        StartCoroutine(TransformNormalizer());
        StartCoroutine(CustomNavController());
    }


    IEnumerator CustomNavController()
    {
        while (true)
        {
            if (controlSend)
            {
                List<navStackModifiers> modfs = new List<navStackModifiers>();
                foreach (string stringModf in controlModifiers.Split(' '))
                {
                    if (System.Enum.TryParse(stringModf, out navStackModifiers enumModf)) modfs.Add(enumModf);
                    else if (stringModf != "") Debug.LogWarning(stringModf + " could not be converted into enum");
                }
                if (controlTargetTransform != null)
                {
                    Debug.Log("Stack pushed with transform");
                    PushNewNavStack(new navStackParameters(controlTargetTransform, modfs));
                }
                else if (controlTargetVect != Vector2Int.zero)
                {
                    Debug.Log("Stack pushed with vect");
                    PushNewNavStack(new navStackParameters(controlTargetVect, modfs));
                }
                else Debug.LogWarning("Stack coulndt be pushed!");
                controlSend = false;
            }
            yield return null;
        }
    }

    IEnumerator VarsUpdater()
    {
        while (true)
        {
            currentPos = new Vector2(transform.position.x, transform.position.z);
            yield return null;
        }     
    }

    IEnumerator Navigation()
    {
        navTask nextNavTask;
        while (true)
        {
            interuptNavigation = false;
            if (navStack.Count == 0) PushNewNavStack(PostNavigation(state));

            nextNavTask = navStack[0];
            navStack.RemoveAt(0);

            currentNavTask = new navTask(currentNavTask.type, nextNavTask.targetPosition, nextNavTask.targetDirection, nextNavTask.targetTime, nextNavTask.targetTransform);
            switch (nextNavTask.type)
            {
                case navTaskType.move:
                    yield return StartCoroutine(MoveNav(nextNavTask));
                    break;
                case navTaskType.rotate:
                    yield return StartCoroutine(RotateNav(nextNavTask));            
                    break;
                case navTaskType.move180:
                    yield return StartCoroutine(Moving180(nextNavTask));
                    break;
                case navTaskType.liveMove:
                    yield return StartCoroutine(LiveMove(nextNavTask));
                    break;
                case navTaskType.idle:
                    yield return StartCoroutine(Idle(nextNavTask));
                    break;
                default:
                    Debug.LogWarning(currentNavTask.type + " not implemented!");
                    break;
            }
        }
    }


    IEnumerator MoveNav(navTask task)
    {      
        if(currentNavTask.type != navTaskType.move)
        {
            currentNavTask.type = navTaskType.startMove;
            animator.SetTrigger("MoveStart");
        }
        while (true)
        {
            if (currentNavTask.type != navTaskType.move && CheckForAnimationState(new string[] { "Walk", "Sprint" })) currentNavTask.type = navTaskType.move;
            if (animator.GetInteger("MoveType") == 0 && Vector2.Distance(task.targetPosition, currentPos) < 0.9f) break;
            if (animator.GetInteger("MoveType") == 1 && Vector2.Distance(task.targetPosition, currentPos) < 0.4f) break;
            if (interuptNavigation) yield break;
            yield return null;
        }
        currentNavTask.type = navTaskType.stopMove;
        animator.SetTrigger("MoveStop");
        yield return StartCoroutine(WaitForAnimatorState("Idle"));
        currentNavTask.type = navTaskType.idle;
    }
    IEnumerator RotateNav(navTask task)
    {
        currentNavTask.type = navTaskType.rotate;
        float rotAngle = Vector2.SignedAngle(currentDir, task.targetDirection);
        if (rotAngle == 90) { animator.SetInteger("TurnType", 0); animator.SetBool("Mirror", false); } //SET RIGHT
        else if (rotAngle == -90) { animator.SetInteger("TurnType", 0); animator.SetBool("Mirror", true); } //SET LEFT
        else if (rotAngle == 180) animator.SetInteger("TurnType", 1); //SET 180
        animator.SetTrigger("Turn"); //TURN
        yield return StartCoroutine(WaitForAnimatorState("Idle"));
        currentDir = task.targetDirection;
        currentNavTask.type = navTaskType.idle;
    }
    IEnumerator Moving180(navTask task)
    {
        currentNavTask.type = navTaskType.move180;
        animator.SetTrigger("Moving180"); //TURN
        yield return StartCoroutine(WaitForAnimatorState(new string[] { "Walk", "Sprint" }));
        currentDir = task.targetDirection;
        currentNavTask.type = navTaskType.move;
    }
    IEnumerator LiveMove(navTask task)
    {
        if (currentNavTask.type != navTaskType.move)
        {
            currentNavTask.type = navTaskType.startMove;
            animator.SetTrigger("MoveStart");
        }
        Vector2 targetTransformPos;
        while (true)
        {
            targetTransformPos = new Vector2(task.targetTransform.position.x, task.targetTransform.position.z);
            if (currentNavTask.type != navTaskType.move && CheckForAnimationState(new string[] { "Walk", "Idle" })) currentNavTask.type = navTaskType.move;
            if (task.targetDirection == Vector2Int.up && (targetTransformPos - currentPos).y < 0) break;
            if (task.targetDirection == Vector2Int.down && (targetTransformPos - currentPos).y > 0) break;
            if (task.targetDirection == Vector2Int.right && (targetTransformPos - currentPos).y < 0) break;
            if (task.targetDirection == Vector2Int.left && (targetTransformPos - currentPos).y > 0) break;
            if (interuptNavigation) yield break;
            yield return null;
        }
    }
    IEnumerator Idle(navTask task)
    {
        currentNavTask.type = navTaskType.idle;
        yield return new WaitForSeconds(task.targetTime);
        yield break;
    }

 

    IEnumerator LiveTargetRefresher(Transform target)
    {
        Debug.Log("Live targeting started...");
        Vector2Int targetPos;
        navTask currentLiveTask = new navTask();
        while (true)
        {
            targetPos = Vector2Int.RoundToInt(new Vector2(target.position.x, target.position.z));
            if ((currentLiveTask.targetDirection.x == 0 && currentLiveTask.targetPosition.x != targetPos.x) || (currentLiveTask.targetDirection.y == 0 && currentLiveTask.targetPosition.y != targetPos.y))
            {
                Debug.Log("target out of liveTask trajecotry\ngenerating new one...");
                PushNewNavStack(new navStackParameters(target, new List<navStackModifiers>{navStackModifiers.liveTarget}));
                foreach (navTask task in navStack) if (task.type == navTaskType.liveMove) currentLiveTask = task;
            }
            yield return null;
        }
    }


    IEnumerator TransformNormalizer()
    {
        Vector2 diffrenceVect;
        while (true)
        {
            switch (currentNavTask.type)
            {
                case navTaskType.startMove:
                case navTaskType.move:
                case navTaskType.stopMove:
                    diffrenceVect = currentNavTask.targetPosition - currentPos;
                    transform.rotation = Quaternion.LookRotation(new Vector3(currentDir.x, 0, currentDir.y));
                    if (currentDir.x != 0) transform.position += new Vector3(0, 0, diffrenceVect.y) * Time.deltaTime;
                    if (currentDir.y != 0) transform.position += new Vector3(diffrenceVect.x, 0, 0) * Time.deltaTime;
                    break;
                case navTaskType.rotate:
                    diffrenceVect = currentNavTask.targetPosition - currentPos;
                    transform.position += new Vector3(diffrenceVect.x, 0, diffrenceVect.y) * Time.deltaTime;
                    break;

            }
            yield return null;
        }
    }

    private void PushNewNavStack(navStackParameters parameters)
    {
        List<navTask> pushingNavStack = GenerateNavStack(Vector2Int.RoundToInt(currentPos), currentDir, parameters);

        //merge with moving180
        if (pushingNavStack[0].type == navTaskType.rotate && Vector2.SignedAngle(currentDir, pushingNavStack[0].targetDirection) == 180 && currentNavTask.type == navTaskType.move)
        {
            pushingNavStack[0] = new navTask(navTaskType.move180, pushingNavStack[0].targetPosition, pushingNavStack[0].targetDirection);
            interuptNavigation = true;
        }

        //merge with currentTask
        navTask tempTask = currentNavTask;
        if (tempTask.type == navTaskType.startMove || tempTask.type == navTaskType.stopMove) tempTask.type = navTaskType.move;
        if (pushingNavStack[0].Equals(tempTask)) pushingNavStack.RemoveAt(0);

        navStack = pushingNavStack;

        if (parameters.modifiers.Contains(navStackModifiers.liveTarget)) StartCoroutine(LiveTargetRefresher(parameters.targetTransform));
    }

    private navStackParameters PostNavigation(States state)
    {
        toPatrolPathCache = new Vector2Int[GameParameters.maze.mazeSize + 1, GameParameters.maze.mazeSize + 1];
        return new navStackParameters(randomPos(GameParameters.maze.mazeSize), new List<navStackModifiers>());
    }

   /* IEnumerator Sense()
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
                fromPlayerDistancePoint = GeneratePath(currentPos, transform.eulerAngles, new Vector2Int(currentPlayerPos.x, currentPlayerPos.y), new pathModifiers[] { pathModifiers.distance }).Count;
                fromPlayerDistanceVector = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));

                //Sight
                spotted = Sight(player) && playerBehavior.playerState != PlayerController.States.Hided;
                if (spotted)
                {
                    mainSense += (parameters.sight.sightBase + Mathf.Clamp(parameters.sight.sightRangeBonus - (fromPlayerDistanceVector * (parameters.sight.sightRangeBonus / parameters.sight.sightRange)), 0, Mathf.Infinity)) / 4;
                }

                //Hearing
                if(state == States.Hunting) heared = playerBehavior.playerState == PlayerController.States.Walking && fromPlayerDistancePoint <= parameters.hearing.hearWalkRange / 3 || playerBehavior.playerState == PlayerController.States.Running && fromPlayerDistancePoint <= parameters.hearing.hearRunRange / 3;
                else heared = playerBehavior.playerState == PlayerController.States.Walking && fromPlayerDistancePoint <= parameters.hearing.hearWalkRange || playerBehavior.playerState == PlayerController.States.Running && fromPlayerDistancePoint <= parameters.hearing.hearRunRange;
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
                if (mainSense > suspicousThreshold && (state == States.Patrol || state == States.Seeking) && (spotted || heared || lighted))
                {
                    if (DisplayAIBehaviour) Debug.Log("suspicious!");
                    ChangeState(States.Suspicious);
                    PushNewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathModifiers[] { pathModifiers.sight }));
                }
                else if (mainSense > alertThreshold && state == States.Suspicious && (spotted || heared || lighted))
                {
                    if (DisplayAIBehaviour) Debug.Log("alerted!");
                    ChangeState(States.Alerted);
                    PushNewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathModifiers[] { pathModifiers.lookAround }));
                }
                else if ((mainSense > huntThreshold && state == States.Alerted && spotted) || state == States.Hunting)
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
                      
                        PushNewPath(GeneratePath(nextPos, nextRot, Vector2Int.RoundToInt(new Vector2(hideBlock.position.x, hideBlock.position.z)), new pathModifiers[] { pathModifiers.checkHide } ));
                    }
                    else if (prevSpotted && !spotted && playerBehavior.playerState != PlayerController.States.Hided)
                    {
                        if(DisplayAIBehaviour) Debug.Log("lostSight!");
                        PushNewPath(GeneratePath(nextPos, nextRot, LostSightEstimation(currentPlayerPos, currentPlayerPos - prevPlayerPos), new pathModifiers[] { pathModifiers.lookAround }));
                    }
                    else if(spotted || heared || lighted)
                    {
                        PushNewPath(GeneratePath(nextPos, nextRot, currentPlayerPos, new pathModifiers[] { }));
                    }
                }
                prevSpotted = spotted;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    //TODO REMOVE VERBOSE SIGHT AND OPTIMIZE RAYCAST ORDER
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
    }*/






    private List<navTask> GenerateNavStack(Vector2Int currentPos, Vector2Int currentDir, navStackParameters pathParameters)
    {
        return GenerateNavStack(ref currentPos, ref currentDir, pathParameters);
    }

    private List<navTask> GenerateNavStack(ref Vector2Int currentPos, ref Vector2Int currentDir, navStackParameters pathParameters)
    {
        //Pre-modifiers editing
        if (pathParameters.modifiers.Contains(navStackModifiers.checkHide)) pathParameters.targetPos = maze.hideMatrix[pathParameters.targetPos.x, pathParameters.targetPos.y].checkingPos;
        if (pathParameters.modifiers.Contains(navStackModifiers.liveTarget)) pathParameters.targetPos = Vector2Int.RoundToInt(new Vector2(pathParameters.targetTransform.position.x, pathParameters.targetTransform.position.z));

        //Generate vector path
        List<Vector2Int> vectorPath = GenerateVectorPath(currentPos, pathParameters.targetPos, (bool[,])maze.obstacleMatrix.Clone(), ref pathParameters.cache);

        //Generate task path
        List<navTask> returnNavStack = new List<navTask>();
        if (vectorPath.Count == 0)
        {
            returnNavStack.Add(new navTask(navTaskType.idle, currentPos, currentDir, 0.25f));
            return returnNavStack;
        }
        if (vectorPath[0] != currentDir)
        {
            currentDir = vectorPath[0]; 
            returnNavStack.Add(new navTask(navTaskType.rotate, currentPos, currentDir)); 
        }
        foreach (Vector2Int vector in vectorPath)
        {
            if (vector != currentDir)
            {
                returnNavStack.Add(new navTask(navTaskType.move, currentPos, currentDir));
                currentDir = vector;
                returnNavStack.Add(new navTask(navTaskType.rotate, currentPos, currentDir));
            }
            currentPos += vector;
        }
        returnNavStack.Add(new navTask(navTaskType.move, currentPos, currentDir));

        //Post-modifiers editing
        if (pathParameters.modifiers.Contains(navStackModifiers.liveTarget))
        {
            returnNavStack[returnNavStack.Count - 1] = new navTask(navTaskType.liveMove, pathParameters.targetTransform, currentDir);
        }

        if(returnNavStack.Count == 0) returnNavStack.Add(new navTask(navTaskType.idle, currentPos, currentDir, 0.25f));
        return returnNavStack;
    }


    List<Vector2Int> GenerateVectorPath(Vector2Int pos, Vector2Int target, bool[,]obstacleMatrix, ref Vector2Int[,] cache)
    {
        List<Vector2Int> returnList = new List<Vector2Int>();
        Vector2Int direction;
        while (pos != target)
        {
            obstacleMatrix[pos.x, pos.y] = true;
            //cache checking
            if (cache[pos.x, pos.y] != Vector2Int.zero && obstacleMatrix[pos.x + cache[pos.x, pos.y].x, pos.y + cache[pos.x, pos.y].y]) direction = cache[pos.x, pos.y]; 
            else direction = GetDirections(pos, obstacleMatrix, target);
            if (direction != Vector2Int.zero) //Stack forward
            {            
                cache[pos.x, pos.y] = direction;
                pos += direction;
                returnList.Add(direction);
            }
            else //Recurse back
            {
                if (returnList.Count == 0)
                {
                    Debug.LogError("INVALID VECTOR PATH PARAMETERS!\nBREAKING...");
                    break;
                }
                pos -= returnList[returnList.Count - 1];
                returnList.RemoveAt(returnList.Count - 1);
                cache[pos.x, pos.y] = Vector2Int.zero;
            }
        }
        return returnList;
    }



    /*    private List<navTask> PostHunt(Vector2Int pos, Vector3 rot, Vector2Int prevPos, float hideCheckChance, int maxPathRecurrsion, float pathChance)
        {
            List<navTask> mainStack = new List<navTask>();
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
                        mainStack.AddRange(GeneratePath(ref pos, ref rot, target, new pathModifiers[] { pathModifiers.roam, pathModifiers.lookAround }, 0, hideCheckChance));
                        prevPos = FindPreviousPosInStack(mainStack, pos);
                        if (prevPos != Vector2Int.zero) unvisitedSpots[prevPos.x, prevPos.y] = false;
                    }
                }
                else
                {
                    if (posRecurrsion.Count == 0 && avaliableDirections.Count == 0) break;    //Main break
                    mainStack.AddRange(GeneratePath(ref pos, ref rot, posRecurrsion[posRecurrsion.Count - 1], new pathModifiers[] { }));
                    posRecurrsion.RemoveAt(posRecurrsion.Count - 1);
                }
            }
            if (mainStack.Count == 0) mainStack.Add(new navTask(navType.wait, 2f));
            return mainStack;
        }



        private Vector2Int LostSightEstimation(Vector2Int pos, Vector2Int direction)
        {
            bool[,] unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
            List<Vector2Int> avaliableDirections;
            while(true)
            {
                unvisitedSpots[pos.x, pos.y] = true;
                pos += direction;
                avaliableDirections = GetDirections(pos, unvisitedSpots);
                if (avaliableDirections.Count != 1) return pos;
                direction = avaliableDirections[0];
            }
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
        }*/


    //HELP METHODS
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

        foreach(Vector2Int vector in optimizedOrder) if (avaliableDirections.Contains(vector)) return vector;
        Debug.LogWarning("Normalized direction could not be found! Returning random...");
        return avaliableDirections[UnityEngine.Random.Range(0, avaliableDirections.Count)];
    }
    List<Vector2Int> GetDirections(Vector2Int pos, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (pos.x + 1 < unvisiteSpots.GetLength(0) && !unvisiteSpots[pos.x + 1, pos.y]) directions.Add(Vector2Int.right);
        if (pos.x - 1 >= 0 && !unvisiteSpots[pos.x - 1, pos.y]) directions.Add(Vector2Int.left);
        if (pos.y + 1 < unvisiteSpots.GetLength(1) && !unvisiteSpots[pos.x, pos.y + 1]) directions.Add(Vector2Int.up);
        if (pos.y - 1 >= 0 && !unvisiteSpots[pos.x, pos.y - 1]) directions.Add(Vector2Int.down);
        return directions;
    }
    IEnumerator WaitForAnimatorState(string[] stateNames)
    {
        bool stop = false;
        while (!stop)
        {
            foreach(string stateName in stateNames)
            {
                if (animator.GetNextAnimatorStateInfo(0).IsName(stateName)) stop = true;
            }
            yield return null;
        }
        stop = false;
        while (!stop)
        {
            foreach (string stateName in stateNames)
            {
                if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) stop = true;
            }
            yield return null;
        }
    }
    IEnumerator WaitForAnimatorState(string stateName)
    {
        yield return StartCoroutine(WaitForAnimatorState(new string[] { stateName }));
    }
    bool CheckForAnimationState(string[] stateNames)
    {
        foreach(string stateName in stateNames)
        {
            if (animator.GetNextAnimatorStateInfo(0).IsName(stateName)) return true;
        }
        return false;
    }
    bool CheckForAnimationState(string stateName)
    {
        return CheckForAnimationState(new string[] { stateName });
    }
    Vector2Int randomPos(int mazeSize)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(UnityEngine.Random.Range(1, mazeSize), UnityEngine.Random.Range(1, mazeSize));
        } while (maze.obstacleMatrix[pos.x, pos.y]);
        return pos;
    }
    private void DrawPoint(Vector3 target, Color color, float length, int priority)
    {
        DrawPoint(new Vector2(target.x, target.z), color, length, priority);
    }
    private void DrawPoint(Vector2 target, Color color, float length, int priority)
    {
        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.01f + priority / 100, target.y - 0.5f), new Vector3(target.x + 0.5f, 0.01f + priority / 100, target.y + 0.5f), color, length);
        Debug.DrawLine(new Vector3(target.x - 0.5f, 0.01f + priority / 100, target.y + 0.5f), new Vector3(target.x + 0.5f, 0.01f + priority / 100, target.y - 0.5f), color, length);
    }
}
