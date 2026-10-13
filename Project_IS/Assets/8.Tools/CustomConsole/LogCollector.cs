using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CustomConsole
{
    [InitializeOnLoad]
    public static class LogCollector
    {
        public static readonly List<LogEntry> Logs = new();

        public static event Action<LogEntry> OnLogAdded;

        private static int mNextId = 1;
        private static int _lastFrame = -1;
        private static int _order;

        static LogCollector()
        {
            // 중복 등록 방지
            Application.logMessageReceived -= HandleLog;
            Application.logMessageReceived += HandleLog;
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            int frame = Time.frameCount;

            if (_lastFrame != frame)
            {
                _lastFrame = frame;
                _order = 0;
            }



            string callMethod = getCallMethod(stackTrace, out string callFilePath, out int callLineNumber);

            var newLogEntry = new LogEntry
            {
                Id = mNextId++,

                Frame = frame,
                Order = _order++,
                Type = type,

                Message = removeTags(condition),
                callMethod = callMethod,
                CallFilePath = callFilePath,
                CallLineNumber = callLineNumber,
                StackTrace = stackTrace,
                executionOrder = getExecutionOrder(stackTrace),

                Time = Time.timeAsDouble
            };

            Logs.Add(newLogEntry);

            OnLogAdded?.Invoke(newLogEntry);
        }

        public static void Clear()
        {
            Logs.Clear();

            _lastFrame = -1;
            _order = 0;

            OnLogAdded?.Invoke(null);
        }

        private static string removeTags(string message)
        {
            string pattern = @"\[[^\]]*\]";
            string messageWithoutTags = message;

            if (Regex.IsMatch(message, pattern))
            {
                messageWithoutTags = Regex.Replace(message, pattern, "");
            }

            return messageWithoutTags;
        }

        private static string getCallMethod(string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            string[] stackTraceLines = stackTrace.Split('\n');

            if (stackTraceLines.Length <= 2)
                return string.Empty;

            // The third line is the caller after Debug.Log and GameDebug.Log.
            string callMethodLine = stackTraceLines[2].Trim();
            int sourceLocationIndex = callMethodLine.IndexOf(" (at ", StringComparison.Ordinal);

            if (sourceLocationIndex >= 0)
            {
                int pathStartIndex = sourceLocationIndex + " (at ".Length;
                int lineSeparatorIndex = callMethodLine.LastIndexOf(':');
                int locationEndIndex = callMethodLine.LastIndexOf(')');

                if (lineSeparatorIndex > pathStartIndex && locationEndIndex > lineSeparatorIndex
                    && int.TryParse(callMethodLine.Substring(lineSeparatorIndex + 1,
                        locationEndIndex - lineSeparatorIndex - 1), out int parsedLineNumber)
                    && parsedLineNumber > 0)
                {
                    filePath = callMethodLine.Substring(pathStartIndex,
                        lineSeparatorIndex - pathStartIndex).Replace('\\', '/');
                    lineNumber = parsedLineNumber;
                }

                callMethodLine = callMethodLine.Substring(0, sourceLocationIndex).TrimEnd();
            }

            return callMethodLine;
        }

        private static ExecutionOrder getExecutionOrder(string stackTrace)
        {
            ExecutionOrder order = ExecutionOrder.None;

            if (stackTrace.Contains("Update ()"))
            {
                order = ExecutionOrder.Update;
            }
            else if (stackTrace.Contains("OnAnimatorIK ()"))
            {
                order = ExecutionOrder.OnAnimatorIK;
            }
            else if (stackTrace.Contains("OnAnimatorMove ()"))
            {
                order = ExecutionOrder.OnAnimatorMove;
            }
            else if (stackTrace.Contains("FixedUpdate ()"))
            {
                order = ExecutionOrder.FixedUpdate;
            }
            else if (stackTrace.Contains("Start ()"))
            {
                order = ExecutionOrder.Start;
            }
            else if (stackTrace.Contains("Awake ()"))
            {
                order = ExecutionOrder.Awake;
            }

            return order;
        }
    }
}