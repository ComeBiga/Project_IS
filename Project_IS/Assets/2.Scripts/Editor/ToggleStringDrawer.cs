using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static GameDebug;

[CustomPropertyDrawer(typeof(GameDebug.ToggleString))]
public class ToggleStringDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float fieldWidth = EditorGUIUtility.currentViewWidth;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float toggleWidth = 15f;

        //var value = property.FindPropertyRelative("value");
        //var active = property.FindPropertyRelative("active");
        var toggleString = (ToggleString)property.boxedValue;

        Rect fieldRect = position;
        fieldRect.width = toggleWidth;

        // value.boolValue = EditorGUI.Toggle(fieldRect, value.boolValue);
        toggleString.active = EditorGUI.Toggle(fieldRect, toggleString.active);

        fieldRect.x += toggleWidth + spacing;
        fieldRect.width = position.width - (toggleWidth + spacing);

        // value.stringValue = EditorGUI.TextField(fieldRect, value.stringValue);
        toggleString.value = EditorGUI.TextField(fieldRect, toggleString.value);

        property.boxedValue = toggleString;

        EditorGUI.EndProperty();
    }
}
