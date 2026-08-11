using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using static TransitionTable;

public class TransitionTableCsvImporter : EditorWindow
{
    private TextAsset csvFile;
    private TransitionTable transitionTable;

    [MenuItem("Tools/Animation/Import Transition CSV")]
    private static void Open()
    {
        GetWindow<TransitionTableCsvImporter>("Transition CSV Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Transition CSV Importer", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File",
                                                        csvFile,
                                                        typeof(TextAsset),
                                                        false);

        transitionTable = (TransitionTable)EditorGUILayout.ObjectField("Transition Table",
                                                                        transitionTable,
                                                                        typeof(TransitionTable),
                                                                        false);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(csvFile == null || transitionTable == null))
        {
            if (GUILayout.Button("Import"))
            {
                Import();
            }
        }
    }

    private void Import()
    {
        try
        {
            List<TransitionData> transitions = ParseCsv(csvFile.text);

            Undo.RecordObject(transitionTable, "Import Transition CSV");

            transitionTable.SetTransitions(transitions);

            EditorUtility.SetDirty(transitionTable);
            AssetDatabase.SaveAssets();

            Debug.Log($"Transition CSV Import 완료: " + $"{transitions.Count}개");
        }
        catch (Exception e)
        {
            Debug.LogError($"Transition CSV Import 실패\n{e}");
        }
    }

    private static List<TransitionData> ParseCsv(string csv)
    {
        var result = new List<TransitionData>();

        using var reader = new StringReader(csv);

        // Header
        string header = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(header))
            throw new Exception("CSV가 비어있습니다.");

        int lineNumber = 1;

        while (true)
        {
            string line = reader.ReadLine();

            if (line == null)
                break;

            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (string.Equals(line, ",,,,"))
                continue;

            string[] columns = ParseCsvLine(line);

            if (columns.Length < 5)
            {
                throw new Exception($"{lineNumber}행의 Column 수가 부족합니다.");
            }

            string fromText = columns[0].Trim();
            string toText = columns[1].Trim();

            string transitionName = fromText + " -> " + toText;
            bool anyFrom = fromText.Equals("Any", StringComparison.OrdinalIgnoreCase);

            AnimState from = default;

            if (!anyFrom)
            {
                if (!Enum.TryParse(fromText, true, out from))
                {
                    throw new Exception($"{lineNumber}행: " + $"From '{fromText}'을 " + $"AnimState에서 찾을 수 없습니다.");
                }
            }

            if (!Enum.TryParse(toText, true, out AnimState to))
            {
                throw new Exception($"{lineNumber}행: " + $"To '{toText}'을 " + $"AnimState에서 찾을 수 없습니다.");
            }

            if (!bool.TryParse(columns[2].Trim(), out bool fixedDuration))
            {
                throw new Exception($"{lineNumber}행: " + $"FixedDuration 값이 잘못되었습니다.");
            }

            if (!float.TryParse(columns[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
            {
                throw new Exception($"{lineNumber}행: " + $"Duration 값이 잘못되었습니다.");
            }

            if (!float.TryParse(columns[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float offset))
            {
                throw new Exception($"{lineNumber}행: " + $"Offset 값이 잘못되었습니다.");
            }

            result.Add(new TransitionData
            {
                name = transitionName,
                anyFrom = anyFrom,
                from = from,
                to = to,
                fixedDuration = fixedDuration,
                duration = duration,
                offset = offset
            });
        }

        return result;
    }

    // "문자열,안에,쉼표" 같은 CSV도 처리
    private static string[] ParseCsvLine(string line)
    {
        var columns = new List<string>();
        var current = new System.Text.StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (c == ',' && !insideQuotes)
            {
                columns.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        columns.Add(current.ToString());

        return columns.ToArray();
    }
}