using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CustomHierarchy : MonoBehaviour
{
    static CustomHierarchy()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        Object obj = EditorUtility.InstanceIDToObject(instanceID);

        if (obj != null)
        {
            string componentName = string.Empty;

            var gameObject = obj as GameObject;
            //bool activeFlag = false;

            //List<Texture> iconTexture = new List<Texture>();

            //if (gameObject.GetComponent<Animator>())
            //{
            //    activeFlag = true;
            //    iconTexture.Add(EditorGUIUtility.IconContent("UnityEditor.Graphs.AnimatorControllerTool@2x").image);
            //}

            //if (gameObject.GetComponent<Animation>())
            //{
            //    activeFlag = true;
            //    iconTexture.Add(EditorGUIUtility.IconContent("Animation Icon").image);
            //}

            //if (activeFlag)
            //{
            //    if (iconTexture.Count == 0)
            //        iconTexture.Add(PrefabUtility.GetIconForGameObject(gameObject));

            //    float iconWidth = 16f;
            //    int temp_iconsDrawedCount = iconTexture.Count;
            //    float lockAndMoreGap = (iconWidth * temp_iconsDrawedCount) - 2;
            //    for (int i = 0; i < temp_iconsDrawedCount; ++i)
            //    {
            //        GUI.DrawTexture(new Rect(selectionRect.xMax - lockAndMoreGap + (iconWidth * i), selectionRect.yMin, 16, 16), iconTexture[i]);
            //    }
            //}

            gameObject.SetActive(GUI.Toggle(new Rect(selectionRect.xMax, selectionRect.yMin, 16, 16), gameObject.activeSelf, ""));
        }
    }
}
