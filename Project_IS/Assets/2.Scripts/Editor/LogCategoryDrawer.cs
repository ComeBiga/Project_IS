using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GameDebug;

[CustomPropertyDrawer(typeof(LogCategory))]
public class LogCategoryDrawer : PropertyDrawer
{
    private static readonly LogCategory[] Categories = Enum.GetValues(typeof(LogCategory))
                                                        .Cast<LogCategory>()
                                                        .Where(x =>
                                                        {
                                                            int value = (int)x;

                                                            // None, All 제외
                                                            if (x == LogCategory.None ||
                                                                x == LogCategory.All)
                                                                return false;

                                                            // 단일 비트 값만
                                                            return (value & (value - 1)) == 0;
                                                        })
                                                        .ToArray();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        int mask = property.intValue;

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect rect = position;
        rect.height = line;

        EditorGUI.LabelField(rect, label);

        rect.y += line + spacing;
        EditorGUI.indentLevel++;

        // None
        bool none = mask == 0;
        bool newNone = EditorGUI.ToggleLeft(rect, "None", none);

        if (newNone && !none)
            mask = 0;

        rect.y += line + spacing;

        // Categories
        foreach (LogCategory category in Categories)
        {
            int value = (int)category;

            bool enabled = (mask & value) != 0;

            bool newEnabled = EditorGUI.ToggleLeft(rect, category.ToString(), enabled);

            if (newEnabled != enabled)
            {
                if (newEnabled)
                    mask |= value;
                else
                    mask &= ~value;
            }

            rect.y += line + spacing;
        }

        // All
        bool all = mask == (int)LogCategory.All;
        bool newAll = EditorGUI.ToggleLeft(rect, "All", all);

        if (newAll && !all)
            mask = (int)LogCategory.All;

        property.intValue = mask;

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines =
            1 +                 // label
            1 +                 // None
            Categories.Length +
            1;                  // All

        return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
    }
}