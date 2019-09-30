using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField]
    Transform player;
    [SerializeField]
    Transform camera;
    [SerializeField]
    GameObject outerJoystick;
    [SerializeField]
    GameObject innerJoystick;
    [SerializeField]
    GameObject FPS;
    RectTransform innerJoystickRect;
    Player playerBehaviour;
    Touch movementTouch;
    Vector2 movementStartPos;
    Touch viewTouch;
    Vector2 viewStartPos;
    private int currentFPS;
    public GameObject[] keysUI;
    public Sprite[] Sprites;
    public GameObject[] InvenotryUI;
    public GameObject ItemPopUp;
    private int inventorySlots = 2;

    private void Start()
    {
        Application.targetFrameRate = 999;
        playerBehaviour = player.GetComponent<Player>();
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        movementTouch.phase = TouchPhase.Ended;
        viewTouch.phase = TouchPhase.Ended;
        innerJoystickRect = innerJoystick.GetComponent<RectTransform>();
        StartCoroutine(FPSCounter());
    }

    void Update()
    {
        currentFPS = (int)(1f / Time.unscaledDeltaTime);
        foreach (Touch touch in Input.touches)
        {
            if (touch.position.x < Screen.width / 2) Move(touch);
            if (touch.position.x > Screen.width / 2) View(touch);
        }      
    }

    public void Move(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                outerJoystick.SetActive(true);
                outerJoystick.GetComponentInChildren<RectTransform>().anchoredPosition = touch.position;
                movementStartPos = touch.position;
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                Vector3 direction = Vector3.ClampMagnitude(new Vector3((movementStartPos.x - touch.position.x) * -1, 0, (movementStartPos.y - touch.position.y) * -1), 100);
                innerJoystickRect.anchoredPosition = new Vector2(direction.x, direction.z);
                direction /= 100;
                playerBehaviour.Navigation(direction, Vector3.zero, false);
                break;
            case TouchPhase.Ended:
                outerJoystick.SetActive(false);
                break;
        }
    }

    public void View(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                playerBehaviour.Navigation(Vector3.zero, new Vector3(touch.deltaPosition.y / -5, touch.deltaPosition.x / 5, 0), false);
                break;
        }
    } 

    IEnumerator FPSCounter()
    {
        while(true)
        {
            FPS.GetComponent<Text>().text = currentFPS.ToString();
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateUI(bool[] keys, Chest.Items[] inventory, Chest.Items pickedItem, int selectedItem)
    {

        transform.Find("Inventory").GetComponent<Animator>().SetTrigger("FadeIn");
        transform.Find("Inventory").GetComponent<Animator>().SetInteger("Selected", selectedItem);
        if (pickedItem != Chest.Items.Empty)
        {
            ItemPopUp.transform.Find("Text").GetComponent<Text>().text = pickedItem.ToString();
            ItemPopUp.transform.Find("Image").GetComponent<Image>().sprite = Sprites[(int)pickedItem];
            ItemPopUp.GetComponent<Animator>().SetTrigger("Display");
        }
        for (int x = 0; x < 3; x ++)
        {
            if (keys[x]) keysUI[x].GetComponent<Image>().sprite = Sprites[x];
            InvenotryUI[x].GetComponent<Image>().sprite = Sprites[(int)inventory[x]];
        }
    }
}
