using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.Rendering.FilterWindow;

[CustomEditor(typeof(GameDebugLogSettings))]
public class GameDebugLogSettingsEditor : Editor
{
    private string mSearchText = string.Empty;

    private SerializedProperty mSpTags;
    private Vector2 mTagScrollPosition = Vector3.zero;
    private ReorderableList mTagROList;
    private List<GameDebug.ToggleString> mMatchedTagList = new List<GameDebug.ToggleString>();
    private List<int> mMatchedTagIndices = new List<int>();

    private void OnEnable()
    {
        mSpTags = serializedObject.FindProperty("tags");

        for (int i = 0; i < mSpTags.arraySize; ++i)
        {
            SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(i);

            var toggleString = (GameDebug.ToggleString)spTag.boxedValue;

            mMatchedTagList.Add(toggleString);
            mMatchedTagIndices.Add(i);
        }

        mTagROList = new ReorderableList(mMatchedTagList, typeof(GameDebug.ToggleString), draggable: false, displayHeader: false, displayAddButton: false, displayRemoveButton: false);
        // mTagROList.headerHeight = 0f;
        mTagROList.footerHeight = 0f;
        // mTagROList.elementHeight = EditorGUIUtility.standardVerticalSpacing;

        //mTagROList.drawHeaderCallback = (Rect rect) =>
        //{
        //    Rect labelRect = rect;
        //    labelRect.width = rect.width / 2f;

        //    EditorGUI.LabelField(rect, "Tags");

        //    Rect searchFieldRect = rect;
        //    searchFieldRect.x = rect.x + labelRect.width;
        //    searchFieldRect.width = rect.width / 2f;
        //    string newSearchText = EditorGUI.TextField(searchFieldRect, mSearchText, EditorStyles.toolbarSearchField);

        //    if(newSearchText != mSearchText)
        //    {
        //        mMatchedTagList.Clear();
        //        mMatchedTagIndices.Clear();

        //        for (int i = 0; i < mSpTags.arraySize; ++i)
        //        {
        //            SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(i);

        //            var toggleString = (GameDebug.ToggleString)spTag.boxedValue;

        //            if (toggleString.value.Contains(newSearchText, System.StringComparison.CurrentCultureIgnoreCase))
        //            {
        //                mMatchedTagList.Add(toggleString);
        //                mMatchedTagIndices.Add(i);
        //            }
        //        }

        //        if(mMatchedTagList.Count < 5)
        //        {
        //            int addCount = 5 - mMatchedTagList.Count;

        //            for (int i = 0; i < addCount; ++i)
        //            {
        //                var toggleString = new GameDebug.ToggleString();
        //                toggleString.value = "Empty";
        //                mMatchedTagList.Add(toggleString);
        //                mMatchedTagIndices.Add(-1);
        //            }
        //        }
        //    }

        //    mSearchText = newSearchText;
        //};

        mTagROList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            // EditorGUI.DrawRect(rect, Color.gray);
            if (mMatchedTagIndices[index] == -1)
                return;

            SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(mMatchedTagIndices[index]);

            var toggleString = (GameDebug.ToggleString)spTag.boxedValue;

            // if (toggleString.value.Contains(mSearchText, System.StringComparison.CurrentCultureIgnoreCase))
            {
                Rect fieldRect = rect;
                fieldRect.x = rect.x;
                fieldRect.y = rect.y + EditorGUIUtility.standardVerticalSpacing;
                fieldRect.width = rect.width;
                fieldRect.height = EditorGUIUtility.singleLineHeight;
                // fieldRect.height = EditorGUI.GetPropertyHeight(spTag, true) + 4f;

                EditorGUI.PropertyField(fieldRect, spTag);
            }
        };

        //mTagROList.elementHeightCallback = (int index) =>
        //{
        //    SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(index);

        //    var toggleString = (GameDebug.ToggleString)spTag.boxedValue;

        //    if (toggleString.value.Contains(mSearchText, System.StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        //        // return EditorGUI.GetPropertyHeight(spTag, true) + 4f;
        //    }

        //    return 0f;
        //};

        mTagROList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            //if(isFocused)
            //{
            //    Color defaultColor = new Color(.3f, .3f, .3f);

            //    EditorGUI.DrawRect(rect, defaultColor);

            //    return;
            //}

            //if (isActive)
            //{
            //    Color defaultColor = new Color(.17f, .36f, .52f);

            //    EditorGUI.DrawRect(rect, defaultColor);

            //    return;
            //}

            if (index % 2 == 0)
            {
                Color defaultColor = new Color(.3f, .3f, .3f);

                EditorGUI.DrawRect(rect, defaultColor);
            }
        };

        mTagROList.drawNoneElementCallback = (Rect rect) =>
        {
            GUI.color = Color.black;
        };
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        return;

        serializedObject.Update();

        // mSearchText = EditorGUILayout.TextField(mSearchText, EditorStyles.toolbarSearchField);

        // EditorGUILayout.PropertyField(mSpTags);

        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);

        string newSearchText = EditorGUILayout.TextField(mSearchText, EditorStyles.toolbarSearchField);

        if (newSearchText != mSearchText)
        {
            mMatchedTagList.Clear();
            mMatchedTagIndices.Clear();

            for (int i = 0; i < mSpTags.arraySize; ++i)
            {
                SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(i);

                var toggleString = (GameDebug.ToggleString)spTag.boxedValue;

                if (toggleString.value.Contains(newSearchText, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    mMatchedTagList.Add(toggleString);
                    mMatchedTagIndices.Add(i);
                }
            }

            if (mMatchedTagList.Count < 5)
            {
                int addCount = 5 - mMatchedTagList.Count;

                for (int i = 0; i < addCount; ++i)
                {
                    var toggleString = new GameDebug.ToggleString();
                    toggleString.value = "Empty";
                    mMatchedTagList.Add(toggleString);
                    mMatchedTagIndices.Add(-1);
                }
            }
        }

        mSearchText = newSearchText;

        EditorGUILayout.EndHorizontal();

        mTagScrollPosition = EditorGUILayout.BeginScrollView(mTagScrollPosition, false, true, GUILayout.Height(125));

        mTagROList.DoLayoutList();
        //for (int i = 0; i < mSpTags.arraySize; i++)
        //{
        //    SerializedProperty spTag = mSpTags.GetArrayElementAtIndex(i);

        //    GameDebug.ToggleString toggleString = (GameDebug.ToggleString) spTag.boxedValue;

        //    if (toggleString.value.Contains(mSearchText, System.StringComparison.CurrentCultureIgnoreCase))
        //        EditorGUILayout.PropertyField(spTag);
        //}
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }
}
