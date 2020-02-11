using UnityEngine;

public class EventHandlerData : MonoBehaviour
{
    [Header("PageChange")]
    public bool pageChange;
    public string toChangePage;
    [Header("Hover")]
    public bool hover = true;  
    public Color color;
    public Color hoverColor;
    public float transitionTime;
    [Header("Scroll")]
    public bool horizontalScroll;
    public bool verticalScroll;
    public float minScroll;
    public float maxScroll;
    [Header("PopUp")]
    public bool popUp;
    public string popupTitle;
    [TextArea]
    public string popupText;
    public bool showUp;
    [Header("LevelLoad")]
    public bool levelLoad;
    public GameObject level;
}
