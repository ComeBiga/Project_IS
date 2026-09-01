using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PlayerCharacterUtility
{
    public static GameObject FindActivePlayerCharacterObject()
    {
        GameObject[] playerCharacterObjects = GameObject.FindGameObjectsWithTag("Player");

        for (int i = 0; i < playerCharacterObjects.Length; i++)
        {
            if (playerCharacterObjects[i].activeSelf)
                return playerCharacterObjects[i];
        }

        return null;
    }

#if UNITY_EDITOR

    [MenuItem("Tools/Player Character/Select Player Character #p")]
    private static void SelectPlayerCharacter()
    {
        GameObject target = GameObject.FindWithTag("Player");
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        // EditorUtility.FocusProjectWindow();
    }

    [MenuItem("Tools/Player Character/Select Player State #s")]
    private static void SelectPlayerState()
    {
        GameObject target = GameObject.Find("PlayerState");
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        // EditorUtility.FocusProjectWindow();
    }

#endif
}
