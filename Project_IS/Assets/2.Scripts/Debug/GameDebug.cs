using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using static GameDebug;

public static class GameDebug
{
    public enum LogCategory
    {
        None = 0,
        Uncategory = 1 << 0,
        State = 1 << 1,
        Animation = 1 << 2,
        Movement = 1 << 3,
        Physics = 1 << 4,
        Input = 1 << 5,
        Editor = 1 << 6,

        All = ~0
    }

    public enum LogLevel
    {
        None = 0,
        Verbose = 1 << 1,
        Info = 1 << 2,
        Warning = 1 << 3,
        Error = 1 << 4,

        All = ~0
    }

    public static class LogTag
    {
        public static string Transition = nameof(Transition);
    }

    [System.Serializable]
    public struct ToggleString
    {
        public string value;
        public bool active;
    }

    public struct GizmosInfo
    {
        public bool drawOnlyPlaying;
        public LogCategory category;
        public string tag;

        public static GizmosInfo normal = new GizmosInfo()
        {
            drawOnlyPlaying = true,
            category = LogCategory.Uncategory,
            tag = ""
        };
    }

    public static GameDebugLogSettings logSettings;
    public static GameDebugLogSettings LogSettings
    {
        get
        {
            if (logSettings == null)
            {
                string[] guids =
                AssetDatabase.FindAssets("t:GameDebugLogSettings");

                if (guids.Length > 0)
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(guids[0]);

                    logSettings =
                        AssetDatabase.LoadAssetAtPath<GameDebugLogSettings>(path);
                }
            }

            return logSettings;
        }
    }

    public static GameDebugGizmosSettings gizmosSettings;
    public static GameDebugGizmosSettings GizmosSettings
    {
        get
        {
            if (gizmosSettings == null)
            {
                string[] guids =
                AssetDatabase.FindAssets("t:GameDebugGizmosSettings");

                if (guids.Length > 0)
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(guids[0]);

                    gizmosSettings = AssetDatabase.LoadAssetAtPath<GameDebugGizmosSettings>(path);
                }
            }

            return gizmosSettings;
        }
    }

    // public static HashSet<string> disabledTags_Log = new();
    //public static HashSet<string> disabledClasses_Log = new();

    //public static HashSet<string> disabledTags_Gizmos = new();
    //public static HashSet<string> disabledClasses_Gizmos = new();

    public static void Initialize(GameDebugLogSettings logSettings, GameDebugGizmosSettings gizmosSettings)
    {
        GameDebug.logSettings = logSettings;
        // GameDebug.disabledTags_Log = new HashSet<string>(logSettings.disabledTags);
        //GameDebug.disabledTags_Log = new HashSet<ToggleString>(logSettings.disabledTags);
        //GameDebug.disabledClasses_Log = new HashSet<string>(logSettings.disabledSources);

        GameDebug.gizmosSettings = gizmosSettings;
        //GameDebug.disabledTags_Gizmos = new HashSet<string>(gizmosSettings.disabledTags);
        //GameDebug.disabledClasses_Gizmos = new HashSet<string>(gizmosSettings.disabledSources);
    }

    private static string filePathToClassName(string filePath)
    {
        string[] split = filePath.Split("\\");
        string className = split[split.Length - 1];
        className = className.Replace(".cs", "");

        return className;
    }

    public static void Log(object message, string tag = "", LogCategory category = LogCategory.Uncategory, LogLevel level = LogLevel.Info, [CallerFilePath] string filePath = "")
    {
        if (!LogSettings.enabled)
            return;

        registerTag(tag, logSettings.tags);

        // if (disabledTags_Log.Contains(tag))
        if(!isEnabledToggleString(tag, logSettings.tags))
            return;

        if ((LogSettings.categories & category) == 0)
            return;

        if((LogSettings.level & level) == 0) 
            return;

        string className = filePathToClassName(filePath);

        // if (disabledClasses_Log.Contains(className))
        if(!isEnabledToggleString(className, logSettings.sources))
            return;

        var builder = new StringBuilder();

        if (LogSettings.frameCount)
            builder.Append($"[{Time.frameCount}]");

        if (LogSettings.showLevel)
            builder.Append($"[{level}]");

        if (LogSettings.showCategory && category != LogCategory.Uncategory)
            builder.Append($"[{category}]");

        if (LogSettings.showTag && !string.IsNullOrEmpty(tag))
            builder.Append($"[{tag}]");

        if (LogSettings.showClass)
            builder.Append($"[{className}]");

        builder.Append(" ");
        builder.Append(message);

        Debug.Log(builder.ToString());
    }

    public static void LogAndPause(object message, LogCategory category, LogLevel level)
    {
        Log(message, "", category, level);

        EditorApplication.isPaused = true;
    }

    private static void registerTag(string tag, List<ToggleString> toggleStringList)
    {
        if (string.IsNullOrEmpty(tag))
            return;

        foreach(ToggleString toggleString in toggleStringList)
        {
            if (toggleString.value == tag)
                return;
        }

        var newTag = new ToggleString();
        newTag.value = tag;
        newTag.active = true;

        toggleStringList.Add(newTag);
    }

    private static bool isEnabledToggleString(string value, List<ToggleString> toggleStringList)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        ToggleString toggleString = new ToggleString();
        toggleString.active = true;
        toggleString.value = value;

        // bool bContains = disabledTags_Log.Contains(toggleString);
        bool bContains = toggleStringList.Contains(toggleString);

        if (bContains)
            return true;

        toggleString.active = false;
        bContains = toggleStringList.Contains(toggleString);

        if (bContains)
            return false;

        return true;
    }

    #region Gizmos

    private static bool gizmosFilter(GizmosInfo gizmosInfo, string filePath)
    {
        if (!GizmosSettings.enabled)
            return false;

        if (gizmosInfo.drawOnlyPlaying && !EditorApplication.isPlaying)
            return false;

        if ((gizmosInfo.category & GizmosSettings.categories) == 0)
            return false;

        // if(disabledTags_Gizmos.Contains(gizmosInfo.tag))
        if(!isEnabledToggleString(gizmosInfo.tag, gizmosSettings.tags))
            return false;

        string className = filePathToClassName(filePath);

        // if (disabledClasses_Gizmos.Contains(className))
        if(!isEnabledToggleString(className, gizmosSettings.sources))
            return false;

        return true;
    }

    public static void DrawRay(Vector3 origin, Vector3 direction, GizmosInfo gizmosInfo = new GizmosInfo(), [CallerFilePath] string filePath = "")
    {
        if (!gizmosFilter(gizmosInfo, filePath))
            return;

        Gizmos.DrawRay(origin, direction);
    }

    public static void DrawGizmos(GizmosInfo info, Action drawAction, [CallerFilePath] string filePath = "")
    {
        if (!gizmosFilter(info, filePath))
            return;

        drawAction.Invoke();
    }

    #endregion
}
