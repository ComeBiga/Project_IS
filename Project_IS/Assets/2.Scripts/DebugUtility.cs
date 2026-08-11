using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugUtility
{
    public static void Log(string tag, object message, bool frameCount = true)
    {
        string tagPart = tag + " - ";
        Debug.Log($"[{Time.frameCount}] " + tagPart + message);
    }
}
