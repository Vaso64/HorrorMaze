using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MenuElement))]
public class MenuElementEditor : Editor
{
    MenuElement menuElement;
    bool displayHoverOptions;
    SerializedProperty hoverStruct;
    SerializedProperty actionsList;
    MenuNavigation.hoverColorsEnum hoverColor;
    public void OnEnable()
    {
        menuElement = (MenuElement)target;
        hoverStruct = serializedObject.FindProperty("hoverElementsConstructors");
        actionsList = serializedObject.FindProperty("actions");
        hoverColor = MenuNavigation.hoverColorsEnum.red;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(actionsList, new GUIContent("Actions"));

        if (menuElement.actions != null)
        {
            if (CheckForDuplicates(menuElement.actions))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Duplicate action detected!", EditorStyles.boldLabel);
                EditorGUILayout.Space();
            }

            displayHoverOptions = false;
            foreach (MenuElement.Action action in menuElement.actions)
            {
                EditorGUILayout.Space();
                switch (action)
                {
                    case MenuElement.Action.changePage:
                        EditorGUILayout.LabelField("Change Page", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.toChangePage = EditorGUILayout.TextField("Page", menuElement.toChangePage);
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.scroll:
                        EditorGUILayout.LabelField("Scroll", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.scrollDir = (MenuElement.ScrollDirection)EditorGUILayout.EnumPopup("Scroll direction", menuElement.scrollDir);
                        menuElement.minPos = EditorGUILayout.FloatField("Minimal position", menuElement.minPos);
                        menuElement.maxPos = EditorGUILayout.FloatField("Maximal position", menuElement.maxPos);
                        break;
                    case MenuElement.Action.popUp:
                        EditorGUILayout.LabelField("PopUp", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.popUpTitle = EditorGUILayout.TextField("Title", menuElement.popUpTitle);
                        menuElement.popUpText = EditorGUILayout.TextField("Text", menuElement.popUpText);
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.setPopUpOutcome:
                        EditorGUILayout.LabelField("PopUpOutcome", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.popUpOutcome = EditorGUILayout.IntField("Outcome", menuElement.popUpOutcome);
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.loadLevel:
                        if (menuElement.GetComponent<Level>() != null) menuElement.level = menuElement.GetComponent<Level>();
                        else
                        {
                            EditorGUILayout.LabelField("LoadLevel", EditorStyles.boldLabel);
                            EditorGUI.indentLevel = 1;
                            menuElement.level = (Level)EditorGUILayout.ObjectField("Level", menuElement.level, typeof(Level), true);
                        }
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.play:
                    case MenuElement.Action.exit:
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.dropDownChange:
                        EditorGUILayout.LabelField("Dropdown change", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.transformHolder = (Transform)EditorGUILayout.ObjectField("Dropdown", menuElement.transformHolder, typeof(Transform), true);
                        menuElement.dropDownState = EditorGUILayout.Toggle("Dropdown state", menuElement.dropDownState);
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.dropDownSetAndClose:
                        EditorGUILayout.LabelField("Dropdown set", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.transformHolder = (Transform)EditorGUILayout.ObjectField("Dropdown", menuElement.transformHolder, typeof(Transform), true);
                        menuElement.dropdownProperty = (MenuNavigation.Properties)EditorGUILayout.EnumPopup("Property", menuElement.dropdownProperty);
                        menuElement.dropDownValue = EditorGUILayout.FloatField("Value", menuElement.dropDownValue);
                        displayHoverOptions = true;
                        break;
                    case MenuElement.Action.buttonSliderChange:
                        EditorGUILayout.LabelField("ButtonSlider change", EditorStyles.boldLabel);
                        EditorGUI.indentLevel = 1;
                        menuElement.transformHolder = (Transform)EditorGUILayout.ObjectField("ButtonSlider", menuElement.transformHolder, typeof(Transform), true);
                        menuElement.sliderProperty = (MenuNavigation.Properties)EditorGUILayout.EnumPopup("Property", menuElement.sliderProperty);
                        menuElement.numericOutput = EditorGUILayout.Toggle("Numeric output", menuElement.numericOutput);
                        if (menuElement.numericOutput) menuElement.sliderChangeValue = EditorGUILayout.FloatField("Value", menuElement.sliderChangeValue);
                        else if (menuElement.transform.name == "Minus") menuElement.sliderChangeValue = -1;
                        else if (menuElement.transform.name == "Plus") menuElement.sliderChangeValue = 1;
                        menuElement.minimalSliderValue = EditorGUILayout.FloatField("Minimal value", Mathf.Clamp(menuElement.minimalSliderValue, Mathf.NegativeInfinity, menuElement.maximalSliderValue));
                        menuElement.maximalSliderValue = EditorGUILayout.FloatField("Maxinam value", Mathf.Clamp(menuElement.maximalSliderValue, menuElement.minimalSliderValue, Mathf.Infinity));
                        menuElement.maxSliderChangesPerSeconds = EditorGUILayout.FloatField("Max changes per second", menuElement.maxSliderChangesPerSeconds);
                        displayHoverOptions = true;
                        break;

                }
                EditorGUI.indentLevel = 0;
            }

            if (displayHoverOptions) menuElement.hover = EditorGUILayout.Toggle("Hover", menuElement.hover);
            else menuElement.hover = false;

            if (menuElement.hover)
            {
                menuElement.customHover = EditorGUILayout.Toggle("Custom hover", menuElement.customHover);
                if (menuElement.customHover) EditorGUILayout.PropertyField(hoverStruct, new GUIContent("Hover elements"));
                else
                {
                    menuElement.hoverElementsConstructors = new List<MenuElement.HoverElementConstructor>();
                    hoverColor = (MenuNavigation.hoverColorsEnum)EditorGUILayout.EnumPopup("Color", hoverColor);
                    menuElement.hoverElementsConstructors.Add(new MenuElement.HoverElementConstructor(menuElement.gameObject, hoverColor));
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private bool CheckForDuplicates(List<MenuElement.Action> actions)
    {
        for (int left = 0; left < actions.Count; left++)
        {
            for (int right = 0; right < actions.Count; right++)
            {
                if (left != right && actions[left] == actions[right]) return true;
            }
        }
        return false;
    }
}
