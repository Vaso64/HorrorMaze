using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    AudioSource audioSource;
    [SerializeField]
    Transform camera;
    [SerializeField]
    AudioClip[] footsteps = new AudioClip[6];
    [SerializeField]
    float speed = 50;
    [HideInInspector]
    public bool lightEnabled = true;
    public enum States{Idle, Walking, Running, Hiding, Hided };
    public States playerState = States.Idle;
    [HideInInspector]
    public Transform inHideWall;
    public GameObject trapPrefab;
    public GameObject decoyPrefab;
    public GameObject physicDecoyPrefab;
    
    private Torch torch;
    private GameObject torchObject;
    private MazeSystem maze;
    public Chest.Items[] inventory = { Chest.Items.Empty, Chest.Items.Empty, Chest.Items.Empty, Chest.Items.Empty, Chest.Items.Empty };
    private enum InteractTypes { Chest, Hide, TrapGround, DecoyGround, DecoyThrow, Unhide, Fire, Tracker, Door, Null}
    private InteractTypes interactType;
    [HideInInspector]
    public Transform interactObject;
    private bool trackerRunning = false;
    private bool[] keys = new bool[3];
    private UIHandler UI;
    private int inventoryCapacity = 2;
    private int selected = 4;
    public Touch[] permTouches;
    public int moveTouchID = -1;
    public int viewTouchID = -1;   
    private float viewBeginTime;
    private float moveBeginTime;
    private Vector2 moveStartPos;
    private Vector2 viewStartPos;
    private bool sprint;

    void Start()
    { 
        torchObject = GameObject.Find("Torch");
        torch = torchObject.GetComponent<Torch>();
        audioSource = gameObject.GetComponent<AudioSource>();
        maze = GameObject.FindGameObjectWithTag("MazeSystem").GetComponent<MazeSystem>();
        rb = gameObject.GetComponent<Rigidbody>();
        UI = GameObject.Find("Canvas").GetComponent<UIHandler>();
        UI.UpdateUI(keys, inventory, Chest.Items.Empty, 4);
        camera.GetComponent<Camera>().backgroundColor = GameParameters.maze.fogColor;
        StartCoroutine(InteractCast());
    }

    void Update()
    {
        //Touch
        permTouches = new Touch[10];
        foreach (Touch touch in Input.touches)
        {
            permTouches[touch.fingerId] = touch;
            if (touch.position.x < Screen.width / 2 && touch.phase == TouchPhase.Began) moveTouchID = touch.fingerId;
            if (touch.position.x > Screen.width / 2 && touch.phase == TouchPhase.Began) viewTouchID = touch.fingerId;
        }
        if (moveTouchID != -1) Move(permTouches[moveTouchID]);
        if (viewTouchID != -1) View(permTouches[viewTouchID]);

        //KBM + Joystick
        if (Application.isEditor)
        {
            Navigation(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")),
            new Vector3(Input.GetAxis("Mouse Y") * -2, Input.GetAxis("Mouse X") * 2, 0),

            Input.GetKey(KeyCode.LeftShift));
            if (Input.GetKeyDown(KeyCode.LeftShift)) audioSource.clip = footsteps[1];
            if (Input.GetKeyUp(KeyCode.LeftShift)) audioSource.clip = footsteps[0];
            if (Input.GetKeyDown(KeyCode.E)) Interact(interactType, interactObject);
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (torchObject.GetComponent<Light>().enabled)
                {
                    torchObject.GetComponent<Light>().enabled = false;
                    lightEnabled = false;
                }
                else
                {
                    torchObject.GetComponent<Light>().enabled = true;
                    lightEnabled = true;
                }
            }
        }
    }

    public void Move(Touch touch)
    {
        UI.MoveUI(touch);
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (Time.time < moveBeginTime + 0.3f) sprint = true;
                else moveBeginTime = Time.time;
                moveStartPos = touch.position;
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                Vector3 direction = Vector3.ClampMagnitude(new Vector3((moveStartPos.x - touch.position.x) * -1, 0, (moveStartPos.y - touch.position.y) * -1), 100);
                direction /= 100;
                Navigation(direction, Vector3.zero, sprint);
                break;
            case TouchPhase.Canceled:
            case TouchPhase.Ended:
                moveTouchID = -1;
                sprint = false;
                break;
        }
    }

    public void View(Touch touch)
    {  
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (Time.time < viewBeginTime + 0.3f) Interact(interactType, interactObject);
                else viewBeginTime = Time.time;
                viewStartPos = touch.position;
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                Navigation(Vector3.zero, new Vector3(touch.deltaPosition.y / -5, touch.deltaPosition.x / 5, 0), false);
                break;
            case TouchPhase.Canceled:
            case TouchPhase.Ended: 
                viewTouchID = -1;
                break;
        }
    }

    public void Navigation(Vector3 move, Vector3 view, bool sprint)
    {
        //Move
        if(playerState != States.Hiding && playerState != States.Hided)
        {

            if (move == Vector3.zero) playerState = States.Idle;
            else
            {
                if (sprint) { playerState = States.Running; speed = 100; }
                else { playerState = States.Walking; speed = 50; }
            }  
            rb.AddRelativeForce(move * Time.deltaTime * speed, ForceMode.Impulse);
            if (!audioSource.isPlaying && move != Vector3.zero) audioSource.Play();
            else if (audioSource.isPlaying && move == Vector3.zero) audioSource.Pause();
        }  
        if(audioSource.isPlaying && playerState == States.Hided ) audioSource.Pause();

        //View
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            torch.cameraDifferentialInput -= view;
            camera.Rotate(new Vector3(view.x, 0, 0));
            transform.Rotate(new Vector3(0, view.y, 0));
        }
    }

    public void SelectItem(int input)
    {
            
            if(inventory[selected] == Chest.Items.Decoy || inventory[selected] == Chest.Items.Trap) Destroy(interactObject.gameObject);
            interactObject = null;
            if (selected != input) selected = input;
            else selected = 4;
            UI.UpdateUI(keys, inventory, Chest.Items.Empty, selected);
            switch (inventory[selected])
            {
                case Chest.Items.Decoy:
                case Chest.Items.Trap:
                    if(inventory[selected] == Chest.Items.Decoy) interactObject = Instantiate(decoyPrefab, Vector3.zero, Quaternion.identity).transform;
                    else if (inventory[selected] == Chest.Items.Trap) interactObject = Instantiate(trapPrefab, Vector3.zero, Quaternion.identity).transform;
                    interactObject.tag = "Untagged";
                    Color tempColor = interactObject.GetComponent<Renderer>().material.color;
                    interactObject.GetComponent<Renderer>().material.color = new Color(tempColor.r, tempColor.g, tempColor.b, 0.5f);
                    break;
            }
    }

    private IEnumerator InteractCast()
    {
        while (true)
        {
            interactType = InteractCheck(ref interactObject);
            yield return new WaitForSeconds(0.15f);
        }
    }

    private InteractTypes InteractCheck(ref Transform interactObject)
    {
        InteractTypes interact = InteractTypes.Null;
        if (playerState == States.Hiding) interact = InteractTypes.Null;
        else if (playerState == States.Hided)
        {
            interact = InteractTypes.Unhide;
            interactObject = inHideWall;
        }
        else if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, Mathf.Infinity))
        {
            switch (inventory[selected])
            {
                case Chest.Items.Trap:
                    //Trap
                    if (hit.transform.tag == "Ground" && Vector3.Distance(hit.transform.position, transform.position) < 2)
                    {
                        interactObject.position = hit.transform.position + new Vector3(0, 0.075f, 0);
                        interact = InteractTypes.TrapGround;
                    }
                    else
                    {
                        interact = InteractTypes.Null;
                        interactObject.position = Vector3.zero;
                    }
                    break;
                case Chest.Items.Decoy:
                    //Decoy (place)
                    if (hit.transform.tag == "Ground" && Vector3.Distance(hit.transform.position, transform.position) < 2.5f)
                    {
                        interactObject.position = hit.transform.position + new Vector3(0, 0.075f, 0);
                        interact = InteractTypes.DecoyGround;
                    }
                    //Decoy (throw)
                    else
                    {
                        interact = InteractTypes.DecoyThrow;
                        interactObject.position = Vector3.zero;
                    }
                    break;
                case Chest.Items.Gun:
                    //Fire
                    if (hit.transform.tag == "AI" && Vector3.Distance(hit.transform.position, transform.position) < 8f)
                    {
                        interact = InteractTypes.Fire;
                        interactObject = hit.transform;
                    }
                    break;
                case Chest.Items.Tracker:
                    //Activate tracker
                    if (!trackerRunning) interact = InteractTypes.Tracker;
                    break;
                case Chest.Items.Empty:
                    //Chest
                    if (hit.transform.tag == "Chest" && Vector3.Distance(hit.transform.position, transform.position) < 1.8f)
                    {
                        interact = InteractTypes.Chest;
                        interactObject = hit.transform;
                    }
                    //Hide
                    else if (hit.transform.tag == "HideWall" && Vector3.Distance(hit.transform.position, transform.position) < 1.65f)
                    {
                        interact = InteractTypes.Hide;
                        interactObject = hit.transform;
                    }
                    //Door
                    else if (hit.transform.tag == "ExitWall" && Vector3.Distance(hit.transform.position, transform.position) < 2f)
                    {
                        bool temp = true;
                        foreach (bool key in keys) if (!key) temp = false;
                        if (temp)
                        {
                            interact = InteractTypes.Door;
                            interactObject = hit.transform;
                        }
                    }
                    break;
            }
        }
        return interact;
    }

    private void Interact(InteractTypes interactType, Transform interactObject)
    {
        switch (interactType)
        {
            case InteractTypes.Chest:
                PickupItem(interactObject.GetComponent<Chest>());
                break;
            case InteractTypes.Hide:
                StartCoroutine(Hide(interactObject, true));
                break;
            case InteractTypes.Unhide:
                StartCoroutine(Hide(interactObject, false));
                break;
            case InteractTypes.TrapGround:
                Instantiate(trapPrefab, interactObject.position, interactObject.rotation);
                Destroy(interactObject.gameObject);
                inventory[selected] = Chest.Items.Empty;
                SelectItem(4);       
                break;
            case InteractTypes.DecoyGround:
                Instantiate(decoyPrefab, interactObject.position, interactObject.rotation);
                Destroy(interactObject.gameObject);
                inventory[selected] = Chest.Items.Empty;
                SelectItem(4);
                break;
            case InteractTypes.DecoyThrow:
                GameObject temp = Instantiate(physicDecoyPrefab, camera.position + camera.transform.forward / 8, Quaternion.identity);
                temp.GetComponent<Rigidbody>().AddForce((camera.transform.forward + camera.transform.up / 2) * 0.5f, ForceMode.Impulse);
                temp.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), Random.Range(-90, 90)), ForceMode.Impulse);
                foreach (Transform AI in maze.AIs) StartCoroutine(AI.GetComponent<AI>().HearObject(temp.transform, 30, 5));
                Destroy(interactObject.gameObject);
                inventory[selected] = Chest.Items.Empty;
                SelectItem(4);
                break;
            case InteractTypes.Fire:
                StartCoroutine(AimAndShoot(interactObject));
                break;
            case InteractTypes.Tracker:
                StartCoroutine(Tracker());
                break;
            case InteractTypes.Door:
                //TODO open door
                break;
        }
    }

    private IEnumerator AimAndShoot(Transform target)
    {
        float aimTime = 2;
        while (aimTime > 0)
        {
            if(Physics.Raycast(camera.position, target.position - camera.position, out RaycastHit hit, 8))
            {
                Debug.Log(hit.transform.position);
                Debug.Log(hit.transform.tag);
                if (hit.transform == target) aimTime -= Time.deltaTime;
                else yield break;
            }
            yield return null;
        }
        StartCoroutine(target.GetComponent<AI>().GetShot());
        foreach (Transform AI in maze.AIs) AI.GetComponent<AI>().HearObject(transform, 30, 0);
        inventory[selected] = Chest.Items.Empty;
        SelectItem(4);
    }

    private IEnumerator Tracker()
    {
        trackerRunning = true;
        float time = 600;
        float lowest;
        float delay;
        
        Transform[] AIs = maze.AIs;
        bool[,] unvisitedSpots;
        Vector2Int pos;
        Vector2Int direction;
        List<Vector2Int> path = new List<Vector2Int>();
        while (time > 0)
        {
            path.Add(Vector2Int.zero);
            lowest = 30;
            unvisitedSpots = (bool[,])maze.obstacleMatrix.Clone();
            pos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.z));
            if (playerState == States.Hiding || playerState == States.Hided) pos += Vector2Int.RoundToInt(new Vector2(inHideWall.transform.forward.x, inHideWall.transform.forward.z));
            do
            {
                direction = PickDirection(pos, unvisitedSpots);
                unvisitedSpots[pos.x, pos.y] = false;
                if (direction != Vector2Int.zero && path.Count < lowest)
                {           
                    foreach (Transform AI in AIs) if (pos == Vector2Int.RoundToInt(new Vector2(AI.position.x, AI.position.z))) lowest = path.Count;
                    pos += direction;
                    path.Add(direction);
                }
                else
                {
                    pos -= path[path.Count - 1];
                    path.RemoveAt(path.Count - 1);
                }
            } while (path.Count != 0);
            Debug.Log("BEEP!");
            delay = 0.15f + Mathf.Pow(lowest / 13, 2f);
            //
            //BEEP!
            //
            yield return new WaitForSeconds(delay);          
        }
        trackerRunning = false;
        inventory[selected] = Chest.Items.Empty;
        SelectItem(4);
    }

    private Vector2Int PickDirection(Vector2Int pos, bool[,] unvisiteSpots)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (unvisiteSpots[pos.x + 1, pos.y]) directions.Add(Vector2Int.right);
        if (unvisiteSpots[pos.x - 1, pos.y]) directions.Add(Vector2Int.left);
        if (unvisiteSpots[pos.x, pos.y + 1]) directions.Add(Vector2Int.up);
        if (unvisiteSpots[pos.x, pos.y - 1]) directions.Add(Vector2Int.down);
        if (directions.Count == 0) return Vector2Int.zero;
        else return directions[Random.Range(0, directions.Count)];
    }

    public IEnumerator Hide(Transform hideWall, bool hide)
    {
        float t = 0;
        //Hide
        if (hide)
        {
            playerState = States.Hiding;
            inHideWall = hideWall;
            if (maze.debugMazePath) StartCoroutine(hideWall.parent.GetComponent<MazeBlock>().Highlight(3f, Color.blue));
            Vector3 startPos = new Vector3(transform.position.x, 1, transform.position.z);
            Vector3 lockPos = new Vector3(hideWall.GetComponent<HideWall>().camLockPos.x, 1, hideWall.GetComponent<HideWall>().camLockPos.z);           
            Vector3 startRot = transform.rotation.eulerAngles;
            float camLockHeight = hideWall.GetComponent<HideWall>().camLockPos.y;
            float camStartHeight = camera.position.y;
            rb.isKinematic = true;
            hideWall.GetComponent<Collider>().enabled = false;     
            while (true)
            {
                transform.position = Vector3.Lerp(startPos, lockPos, t);
                transform.rotation = Quaternion.Euler(Vector3.Lerp(startRot, hideWall.rotation.eulerAngles, t * 2));
                camera.transform.position = new Vector3(camera.position.x, Mathf.Lerp(camStartHeight, camLockHeight, t * 3), camera.position.z);
                t += 1f * Time.deltaTime;
                if (t > 1) break;
                else yield return null;
            }
            playerState = States.Hided;
        }
        //Unhide
        else
        {
            Vector3 startPos = new Vector3(transform.position.x, 1, transform.position.z);
            Vector3 lockPos = transform.position + hideWall.transform.forward / 3;
            float camStartPos = camera.localPosition.y;
            float camLockHieght = 0.5f;
            
            while (true)
            {
                transform.position = Vector3.Lerp(startPos, lockPos, t * 3);
                camera.transform.localPosition = new Vector3(0, Mathf.Lerp(camStartPos, camLockHieght, t), 0);
                t += 1f * Time.deltaTime;
                if (t > 1) break;
                else yield return null;
            }
            playerState = States.Idle;
            rb.isKinematic = false;
            hideWall.GetComponent<Collider>().enabled = true;
        }
    }

    private void PickupItem(Chest targetChest)
    {
        bool picked = true;
        if (targetChest.chestContent != Chest.Items.Empty)
        {
            Chest.Items pickedItem = targetChest.chestContent;
            if ((int)pickedItem < 3) keys[(int)pickedItem] = true;
            else
            {
                if (pickedItem == Chest.Items.Backpack && inventoryCapacity >= 4) pickedItem = (Chest.Items)Random.Range(3, 8); //Replace backpack if max invenotry
                //Add new inventory slot
                if (pickedItem == Chest.Items.Backpack)
                {
                    GameObject.Find("Inventory").GetComponent<Animator>().SetTrigger("AddSlot");
                    inventoryCapacity++;
                }
                //Add item to inventory stack
                else for (int inventoryIndex = 0; inventoryIndex < inventoryCapacity; inventoryIndex++)
                {
                    if (inventory[inventoryIndex] == Chest.Items.Empty)
                    {
                        inventory[inventoryIndex] = pickedItem;
                        picked = true;
                        break;
                    }
                    else picked = false;
                }
            }
            if (picked)
            {
                targetChest.chestContent = Chest.Items.Empty;
                targetChest.GetComponent<Animator>().SetTrigger("Open");
                UI.UpdateUI(keys, inventory, pickedItem, selected);
            }
        }
    }
}
