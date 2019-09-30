using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody rb;
    AudioSource audioSource;
    [SerializeField]
    Transform camera;
    [SerializeField]
    AudioClip[] footsteps = new AudioClip[6];
    [SerializeField]
    float speed = 50;
    public enum States{Idle, Walking, Running, Hiding };
    public States playerState;
    private Torch torch;
    private GameObject torchObject;
    [SerializeField]
    private Chest.Items[] inventory = { Chest.Items.Empty, Chest.Items.Empty, Chest.Items.Empty };
    [SerializeField]
    private bool[] keys = new bool[3];
    private UIHandler UI;
    private int inventoryCapacity = 2;
    private int selected;
    // Start is called before the first frame update
    void Start()
    { 
        torchObject = GameObject.Find("Torch");
        torch = torchObject.GetComponent<Torch>();
        playerState = States.Idle;
        audioSource = gameObject.GetComponent<AudioSource>();
        rb = gameObject.GetComponent<Rigidbody>();
        UI = GameObject.Find("Canvas").GetComponent<UIHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isEditor)
        {
            Navigation(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")),
            new Vector3(Input.GetAxis("Mouse Y") * -3, Input.GetAxis("Mouse X") * 3, 0),
            Input.GetKey(KeyCode.LeftShift));
            if (Input.GetKeyDown(KeyCode.LeftShift)) audioSource.clip = footsteps[1];
            if (Input.GetKeyUp(KeyCode.LeftShift)) audioSource.clip = footsteps[0];
            if (Input.GetKeyDown(KeyCode.E)) Interact();
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (torchObject.GetComponent<Light>().enabled) torchObject.GetComponent<Light>().enabled = false;
                else torchObject.GetComponent<Light>().enabled = true;
            }
        }
    }

    public void Navigation(Vector3 move, Vector3 view, bool sprint)
    {
        torch.cameraDifferentialInput -= view;
        if (move == Vector3.zero) playerState = States.Idle;
        else
        {
            if (sprint) playerState = States.Running;
            else playerState = States.Walking;
        }
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            camera.Rotate(new Vector3(view.x, 0, 0));
            transform.Rotate(new Vector3(0, view.y, 0));
        }
        if (sprint) speed = 100;
        else speed = 50;
        rb.AddRelativeForce(move * Time.deltaTime * speed, ForceMode.Impulse);

        if (!audioSource.isPlaying && move != Vector3.zero) audioSource.Play();
        else if (audioSource.isPlaying && move == Vector3.zero)
        {
            audioSource.Pause();
        }
    }

    public void SelectItem(int input)
    {
        if (inventory[input] != Chest.Items.Empty)
        {
            if (selected != input) selected = input;
            else selected = 0;
            UI.UpdateUI(keys, inventory, Chest.Items.Empty, selected);
        }

    }

    private void Interact()
    {
        if(Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 2))
        {
            switch (hit.transform.tag)
            {
                case "Chest":
                    Chest targetChest = hit.transform.GetComponent<Chest>();
                    if (targetChest.chestContent != Chest.Items.Empty)
                    {
                        Chest.Items pickedItem = targetChest.chestContent;
                        targetChest.GetComponent<Animator>().SetTrigger("Open");
                        if ((int)pickedItem < 3)
                        {
                            keys[(int)pickedItem] = true;
                            targetChest.chestContent = Chest.Items.Empty;
                            UI.UpdateUI(keys, inventory, pickedItem, selected);
                        }
                        else
                        {
                            if (pickedItem == Chest.Items.Backpack && inventoryCapacity >= 4) pickedItem = (Chest.Items)Random.Range(3, 8);
                            if (pickedItem == Chest.Items.Backpack)
                            {
                                GameObject.Find("Inventory").GetComponent<Animator>().SetTrigger("AddSlot");
                                inventoryCapacity++;
                                targetChest.chestContent = Chest.Items.Empty;
                                UI.UpdateUI(keys, inventory, pickedItem, selected);
                                pickedItem = Chest.Items.Empty;
                            }
                            else for (int inventoryIndex = 0; inventoryIndex < inventoryCapacity; inventoryIndex++) if (inventory[inventoryIndex] == Chest.Items.Empty)
                            {
                                inventory[inventoryIndex] = targetChest.chestContent;
                                targetChest.chestContent = Chest.Items.Empty;
                                UI.UpdateUI(keys, inventory, pickedItem, selected);
                            }
                            
                        }
                    }
                    break;
                case "HideWall":
                    break;
            }
        }
        
    }
}
