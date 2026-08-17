using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorHashGenerator : EditorWindow
{
    private AnimatorController controller;

    private const string OutputPath =
        "Assets/2.Scripts/Animation/AnimStateHash.cs";
    private const string AnimStatePath =
        "Assets/2.Scripts/Animation/AnimState.cs";
    private const string AnimStateNameLookUpPath =
        "Assets/2.Scripts/Animation/AnimStateNameLookUp.cs";

    [MenuItem("Tools/Animation/Generate State Hashes")]
    private static void Open()
    {
        GetWindow<AnimatorHashGenerator>(
            "Animator Hash Generator");
    }

    private void OnGUI()
    {
        controller = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller",
                                                                    controller,
                                                                    typeof(AnimatorController),
                                                                    false);

        EditorGUILayout.Space();

        if (controller == null)
            GUI.enabled = false;
        else
            GUI.enabled = true;

        if (GUILayout.Button("Generate Enum"))
        {
            GenerateAnimState(controller);
        }

        if (GUILayout.Button("Generate Hash"))
        {
            Generate(controller);
        }

        if (GUILayout.Button("Generate Name LookUp"))
        {
            GenerateAnimStateNameLookUp(controller);
        }
    }

    private static void GenerateAnimState(AnimatorController controller)
    {
        var states = new List<StateData>();

        foreach (var layer in controller.layers)
        {
            CollectStates(layer.stateMachine, layer.name, states);
        }

        GenerateAnimStateFile(states);

        AssetDatabase.Refresh();

        Debug.Log($"Generated {states.Count} animator state enum.");
    }

    private static void GenerateAnimStateNameLookUp(AnimatorController controller)
    {
        var states = new List<StateData>();

        foreach (var layer in controller.layers)
        {
            CollectStates(layer.stateMachine, layer.name, states);
        }

        GenerateAnimStateNameLookUpFile(states);

        AssetDatabase.Refresh();

        Debug.Log($"Generated {states.Count} animator state name table.");
    }

    private static void Generate(AnimatorController controller)
    {
        var states = new List<StateData>();

        foreach (var layer in controller.layers)
        {
            CollectStates(layer.stateMachine, layer.name, states);
        }

        GenerateFile(states);

        AssetDatabase.Refresh();

        Debug.Log($"Generated {states.Count} animator state hashes.");
    }

    private static void CollectStates(AnimatorStateMachine stateMachine, string path, List<StateData> states)
    {
        // ÇöÀç StateMachine ¾ÈÀÇ State
        foreach (var childState in stateMachine.states)
        {
            string fullPath = path + "." + childState.state.name;

            states.Add(new StateData
            {
                fullPath = fullPath,
                name = childState.state.name,
            });
        }

        // ÇÏÀ§ StateMachine Àç±Í Å½»ö
        foreach (var childMachine in stateMachine.stateMachines)
        {
            string childPath = path + "." + childMachine.stateMachine.name;

            CollectStates(childMachine.stateMachine, childPath, states);
        }
    }

    private static void GenerateAnimStateFile(List<StateData> states)
    {
        string directory = Path.GetDirectoryName(AnimStatePath);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine("public enum AnimState");
        builder.AppendLine("{");

        var usedNames = new HashSet<string>();

        foreach (var state in states)
        {
            string variableName = CreateVariableName(state.name);

            variableName = MakeUnique(variableName, usedNames);

            builder.Append("   ");
            builder.Append($"{variableName}");
            builder.AppendLine(",");
        }

        builder.AppendLine("}");

        File.WriteAllText(AnimStatePath, builder.ToString(), Encoding.UTF8);
    }

    private static void GenerateAnimStateNameLookUpFile(List<StateData> states)
    {
        string directory = Path.GetDirectoryName(AnimStateNameLookUpPath);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();

        builder.AppendLine("// Auto-generated. Do not edit manually.");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine();
        builder.AppendLine("public static class AnimStateNameLookUp");
        builder.AppendLine("{");

        builder.AppendLine($"   public static Dictionary<int, string> names = new()");
        builder.AppendLine("   {");

        var usedNames = new HashSet<string>();

        foreach (var state in states)
        {
            string variableName = CreateVariableName(state.name);

            builder.Append("      { ");
            builder.Append($"Animator.StringToHash(\"{state.fullPath}\"), \"{variableName}\"");
            builder.AppendLine(" },");
        }

        builder.AppendLine("   };");

        builder.AppendLine("}");

        File.WriteAllText(AnimStateNameLookUpPath, builder.ToString(), Encoding.UTF8);
    }

    private static void GenerateFile(List<StateData> states)
    {
        string directory = Path.GetDirectoryName(OutputPath);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();

        builder.AppendLine("// Auto-generated. Do not edit manually.");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine();
        builder.AppendLine("public static class AnimStateHash");
        builder.AppendLine("{");

        builder.AppendLine($"   public static Dictionary<AnimState, int> stateHashes = new()");
        builder.AppendLine("   {");

        string[] animStateLines = File.ReadAllLines(AnimStatePath);

        var usedNames = new HashSet<string>();

        foreach (var state in states)
        {
            string variableName = CreateVariableName(state.name);

            variableName = MakeUnique(variableName, usedNames);

            if (System.Enum.TryParse(variableName, out AnimState animState))
            {
                builder.Append("      { ");
                builder.Append($"AnimState.{variableName}, Animator.StringToHash(\"{state.fullPath}\")");
                builder.AppendLine(" },");
            }

            //builder.AppendLine($"    public static readonly int {variableName} =");

            //builder.AppendLine($"        Animator.StringToHash(\"{state.fullPath}\");");

            //builder.AppendLine();
        }

        builder.AppendLine("   };");

        builder.AppendLine("}");

        File.WriteAllText(OutputPath, builder.ToString(), Encoding.UTF8);
    }

    private static string CreateVariableName(string fullPath)
    {
        var builder = new StringBuilder();

        foreach (char c in fullPath)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                builder.Append(c);
            else
                builder.Append('_');
        }

        return builder.ToString();
    }

    private static string MakeUnique(string name, HashSet<string> usedNames)
    {
        if (usedNames.Add(name))
            return name;

        int index = 2;

        while (true)
        {
            string newName = name + "_" + index;

            if (usedNames.Add(newName))
                return newName;

            index++;
        }
    }

    private class StateData
    {
        public string fullPath;
        public string name;
    }
}
