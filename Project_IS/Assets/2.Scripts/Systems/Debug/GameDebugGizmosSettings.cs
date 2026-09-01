using System.Collections.Generic;
using UnityEngine;
using static GameDebug;

[CreateAssetMenu(menuName = "Debug/Game Debug Gizmos Settings")]
public class GameDebugGizmosSettings : ScriptableObject
{
    public bool enabled = true;

    public LogCategory categories;

    public List<ToggleString> tags;

    public List<ToggleString> sources;
}
