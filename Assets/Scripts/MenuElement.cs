using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System;

public class MenuElement : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IPointerDownHandler
{
    public enum Action { changePage, scroll, popUp, setPopUpOutcome, loadLevel, play, dropDownSetAndClose, dropDownChange, buttonSliderChange, exit };
    public List<Action> actions;
    //ChangePage
    public string toChangePage;
    //Scroll
    public enum ScrollDirection { horizontal, vertical }
    public ScrollDirection scrollDir;
    public float minPos;
    public float maxPos;
    //Hover
    public bool hover;
    public bool customHover = false;
    [Serializable]
    public struct HoverElementConstructor
    {
        public HoverElementConstructor(GameObject Element, MenuNavigation.hoverColorsEnum Color)
        {
            element = Element;
            color = Color;
        }
        public GameObject element;
        public MenuNavigation.hoverColorsEnum color;
    }
    public List<HoverElementConstructor> hoverElementsConstructors;
    struct HoverElement
    {
        public HoverElement(HoverElementConstructor constructor)
        {
            element = constructor.element;
            if (element.GetComponent<Image>() != null) normalColor = element.GetComponent<Image>().color;
            else if (element.GetComponent<Text>() != null) normalColor = element.GetComponent<Text>().color;
            else
            {
                normalColor = Color.white;
                Debug.LogWarning("Couldn't find Text component nor Image component on " + element);
            }
            hoverColor = GameObject.Find("MenuNavigation").GetComponent<MenuNavigation>().hoverColors[(int)constructor.color];
        }
        public GameObject element;
        public Color normalColor;
        public Color hoverColor;

    }
    List<HoverElement> hoverElements = new List<HoverElement>();
    //PopUp
    public string popUpTitle;
    public string popUpText;
    //SetPopUpOutcome
    public int popUpOutcome;
    //LoadLevel
    public Level level;
    //DropDown
    public bool dropDownState;
    public MenuNavigation.Properties dropdownProperty;
    public float dropDownValue;
    //ButtonSlider
    public float sliderChangeValue;
    public float minimalSliderValue;
    public float maximalSliderValue;
    public float maxSliderChangesPerSeconds;
    public MenuNavigation.Properties sliderProperty;
    public bool numericOutput;
    //Other
    public Transform transformHolder;
    private RectTransform rectTransform;
    public MenuNavigation menuNavigation;

    void Start()
    {
        foreach (HoverElementConstructor hoverItem in hoverElementsConstructors)
        {
            hoverElements.Add(new HoverElement(hoverItem));
        }
        menuNavigation = GameObject.Find("MenuNavigation").GetComponent<MenuNavigation>();
        rectTransform = GetComponent<RectTransform>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (actions.Contains(Action.changePage)) menuNavigation.ChangePage(toChangePage);
        if (actions.Contains(Action.popUp)) StartCoroutine(menuNavigation.Popup(popUpTitle, popUpText));
        if (actions.Contains(Action.setPopUpOutcome)) menuNavigation.popUpOutcome = popUpOutcome;
        if (actions.Contains(Action.dropDownChange)) menuNavigation.DropDownSet(transformHolder, dropDownState);
        if (actions.Contains(Action.dropDownSetAndClose)) menuNavigation.DropDownSet(transformHolder, dropdownProperty, dropDownValue, transform.Find("Value").GetComponent<Text>().text);
        if (actions.Contains(Action.buttonSliderChange)) menuNavigation.SliderStop();
        if (actions.Contains(Action.loadLevel)) menuNavigation.Load(level);
        if (actions.Contains(Action.play)) menuNavigation.Play();
        if (actions.Contains(Action.exit)) Application.Quit();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hover) StartCoroutine(Hover(hoverElements, true, false));
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hover) StartCoroutine(Hover(hoverElements, false, false));
        if (actions.Contains(Action.buttonSliderChange)) menuNavigation.SliderStop();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (actions.Contains(Action.scroll))
        {
            if (scrollDir == ScrollDirection.horizontal)
            {
                rectTransform.anchoredPosition += new Vector2(eventData.delta.x, 0);
                rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(rectTransform.anchoredPosition.x, minPos, maxPos), rectTransform.anchoredPosition.y);
            }
            if (scrollDir == ScrollDirection.vertical)
            {
                rectTransform.anchoredPosition += new Vector2(0, eventData.delta.y);
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, Mathf.Clamp(rectTransform.anchoredPosition.y, minPos, maxPos));
            }
            
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (actions.Contains(Action.buttonSliderChange)) menuNavigation.SliderDown(transformHolder, sliderProperty, sliderChangeValue, minimalSliderValue, maximalSliderValue, maxSliderChangesPerSeconds, numericOutput);
    }

    public void OnDisable()
    {
        if (hover)
        {
            foreach (HoverElement hoverElement in hoverElements)
            {
                if (hoverElement.element.GetComponent<Text>() != null) hoverElement.element.GetComponent<Text>().color = hoverElement.normalColor;
                if (hoverElement.element.GetComponent<Image>() != null) hoverElement.element.GetComponent<Image>().color = hoverElement.normalColor;
            }
        }
    }

    IEnumerator Hover(List<HoverElement> hoverElements, bool hover, bool instant)
    {
        List<HoverElement> images = new List<HoverElement>();
        List<HoverElement> texts = new List<HoverElement>();
        foreach (HoverElement hoverElement in hoverElements)
        {
            if (hoverElement.element.activeSelf && hoverElement.element.GetComponent<Image>() != null) images.Add(hoverElement);
            if (hoverElement.element.activeSelf && hoverElement.element.GetComponent<Text>() != null) texts.Add(hoverElement);
        }
        if (images.Count == 0 && texts.Count == 0) yield break;
        if(instant)
        {
            foreach(HoverElement image in images)
            {
                if (hover) image.element.GetComponent<Image>().color = image.hoverColor;
                else image.element.GetComponent<Image>().color = image.normalColor;
            }
            foreach (HoverElement text in texts)
            {
                if (hover) text.element.GetComponent<Text>().color = text.hoverColor;
                else text.element.GetComponent<Text>().color = text.normalColor;
            }
        }
        else
        {
            float t = 0;
            while (t / menuNavigation.hoverTime < 1)
            {
                t += Time.deltaTime;
                foreach (HoverElement image in images)
                {
                    if (hover) image.element.GetComponent<Image>().color = Color.Lerp(image.normalColor, image.hoverColor, t / menuNavigation.hoverTime);
                    else image.element.GetComponent<Image>().color = Color.Lerp(image.hoverColor, image.normalColor, t / menuNavigation.hoverTime);
                }
                foreach (HoverElement text in texts)
                {
                    if (hover) text.element.GetComponent<Text>().color = Color.Lerp(text.normalColor, text.hoverColor, t / menuNavigation.hoverTime);
                    else text.element.GetComponent<Text>().color = Color.Lerp(text.hoverColor, text.normalColor, t / menuNavigation.hoverTime);
                }
                yield return null;
            }
        }
    }
}

