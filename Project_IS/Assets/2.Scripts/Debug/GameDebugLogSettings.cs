using System.Collections.Generic;
using UnityEngine;
using static GameDebug;

[CreateAssetMenu(menuName = "Debug/Game Debug Log Settings")]
public class GameDebugLogSettings : ScriptableObject
{
    public bool enabled = true;

    [Header("Labels")]
    public bool frameCount = true;
    public bool showCategory = true;
    public bool showLevel = true;
    public bool showTag = true;
    public bool showClass = true;

    [Header("Toggles")]
    public LogCategory categories;
    public LogLevel level;

    public List<string> disabledTags;

    public List<string> disabledSources;
}