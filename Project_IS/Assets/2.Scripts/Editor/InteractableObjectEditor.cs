using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableObject), editorForChildClasses: true)]
[CanEditMultipleObjects]
public class InteractableObjectEditor : Editor
{
    private Type mTargetType;
    private Type mInteractableObjectType;
    private List<SerializedProperty> mChildClassProperties;
    private List<SerializedProperty> mInteractableObjectProperties;

    private void OnEnable()
    {
        mTargetType = target.GetType();
        mInteractableObjectType = mTargetType.BaseType;

        mChildClassProperties = FindClassProperties(mTargetType);
        mInteractableObjectProperties = FindClassProperties(mInteractableObjectType);
    }

    public override void OnInspectorGUI()
    {
        // DrawDefaultInspector();

        // 기본 Inspector의 Script 필드
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("m_Script")
        );
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        serializedObject.Update();

        EditorGUILayout.LabelField($"[{mInteractableObjectType.Name}] Properties", EditorStyles.boldLabel);

        foreach (var property in mInteractableObjectProperties)
            EditorGUILayout.PropertyField(property, true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"[{mTargetType.Name}] Properties", EditorStyles.boldLabel);

        foreach (var property in mChildClassProperties)
            EditorGUILayout.PropertyField(property, true);

        serializedObject.ApplyModifiedProperties();
    }

    private List<SerializedProperty> FindClassProperties(Type type)
    {
        // var targetType = target.GetType();

        var fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly);

        var serializedProperties = new List<SerializedProperty>();

        foreach (var field in fields)
        {
            if (!IsSerializedField(field))
                continue;

            var property = serializedObject.FindProperty(field.Name);

            if (property != null)
                serializedProperties.Add(property);
        }

        return serializedProperties;
    }

    private bool IsSerializedField(FieldInfo field)
    {
        // public 필드는 기본적으로 Serialize됨
        if (field.IsPublic &&
            !field.IsDefined(typeof(System.NonSerializedAttribute)))
        {
            return true;
        }

        // private/protected + [SerializeField]
        if (field.IsDefined(typeof(SerializeField)))
        {
            return true;
        }

        return false;
    }
}
