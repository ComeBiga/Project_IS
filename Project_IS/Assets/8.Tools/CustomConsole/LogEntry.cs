using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomConsole
{
    public enum ExecutionOrder
    {
        None = 0,
        Awake,
        Start,
        FixedUpdate,
        OnAnimatorMove,
        OnAnimatorIK,
        LateFixedUpdate,
        Update,
        LateUpdate
    }

    public class LogEntry
    {
        public int Id;

        public int Frame;
        public int Order;
        public LogType Type;

        public string Message;
        public string callMethod;
        public string CallFilePath;
        public int CallLineNumber;
        public string StackTrace;
        public ExecutionOrder executionOrder;

        public double Time;
    }
}