using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EventHandler : EventTrigger
{
    EventHandlerData eventHandlerData;
    RectTransform scroll;
    private void Start()
    {
        eventHandlerData = gameObject.GetComponent<EventHandlerData>();
        if (eventHandlerData.horizontalScroll || eventHandlerData.verticalScroll) scroll = gameObject.GetComponent<RectTransform>();
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if(eventHandlerData.hover) StartCoroutine(ColorTransition(eventHandlerData.color, eventHandlerData.hoverColor));
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if(eventHandlerData.hover) StartCoroutine(ColorTransition(eventHandlerData.hoverColor, eventHandlerData.color));
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if(eventHandlerData.hover) StartCoroutine(ColorTransition(eventHandlerData.hoverColor, eventHandlerData.color));
        if(eventHandlerData.pageChange) GameObject.Find("MenuNavigation").GetComponent<MenuNavigation>().ChangePage(eventHandlerData.toChangePage);
        if(eventHandlerData.popUp) GameObject.Find("MenuNavigation").GetComponent<MenuNavigation>().PopUp(eventHandlerData.popupTitle, eventHandlerData.popupText, eventHandlerData.showUp);
        if (eventHandlerData.levelLoad) GetComponent<Level>().Load();
        base.OnPointerClick(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (eventHandlerData.horizontalScroll)
        {
            scroll.anchoredPosition += new Vector2(eventData.delta.x, 0);
            scroll.anchoredPosition = new Vector2(Mathf.Clamp(scroll.anchoredPosition.x, eventHandlerData.minScroll, eventHandlerData.maxScroll), scroll.anchoredPosition.y);
        }
        if (eventHandlerData.verticalScroll)
        {
            Debug.Log("tseest");
            scroll.anchoredPosition += new Vector2(0, eventData.delta.y);
            scroll.anchoredPosition = new Vector2(scroll.anchoredPosition.x, Mathf.Clamp(scroll.anchoredPosition.y, eventHandlerData.minScroll, eventHandlerData.maxScroll));
        }
        base.OnDrag(eventData);
    }
    private IEnumerator ColorTransition(Color fromColor, Color toColor)
    {
        float t = 0;
        while(t <= 1)
        {
            t += Time.deltaTime / eventHandlerData.transitionTime;
            foreach (Image image in GetComponentsInChildren<Image>()) image.color = Color.Lerp(fromColor, toColor, t);
            foreach (Text text in GetComponentsInChildren<Text>()) text.color = Color.Lerp(fromColor, toColor, t);
            yield return null;
        }
        yield break;
    }
}
