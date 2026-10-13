using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CustomConsole
{
    public class CustomConsoleWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState mTreeViewState;

        private ConsoleTreeView mTreeView;

        private bool mbDirty = false;
        private List<LogEntry> mPendingLogEntries = new List<LogEntry>();
        private bool mAutoScroll = true;
        private double mNextLogRefreshTime;

        private MultiColumnHeader mColumnHeader;

        private int mInfoLogCount = 0;
        private int mWarningLogCount = 0;
        private int mErrorLogCount = 0;

        private bool mbShowInfoLog = true;
        private bool mbShowWarningLog = true;
        private bool mbShowErrorLog = true;

        private LogEntry mSelectedLog;

        private Vector2 mLogListScroll;
        private Vector2 mDetailScroll;

        private float mLogListViewRectHeight = 0f;
        private float mDetailViewRectHeight = 0f;

        private float mDetailHeight;

        private bool mIsResizing;
        private float mResizeOffset;

        private GUIStyle mStackTraceStyle;
        private GUIStyle mStackTraceLinkStyle;

        private static readonly Regex mStackTraceRegex = new Regex(@"\(at (.+):(\d+)\)$");

        private float Height_Text => EditorGUIUtility.singleLineHeight;
        private float Height_Spacing => EditorGUIUtility.standardVerticalSpacing;
        private float Height_Log => Height_Text + Height_Spacing;

        private const double LOG_REFRESH_INTERVAL = 0.2d;
        private const float LOG_TOGGLE_MIN_WIDTH = 30f;
        private const float TOOLBAR_HEIGHT = 20f;
        private const float DETAIL_HEIGHT = 140f;
        private const float SPLITTER_HEIGHT = 4f;
        private const float MIN_DETAIL_HEIGHT = 60f;
        private const float MIN_LOG_LIST_HEIGHT = 100f;

        private readonly Color Color_TextWarning = new Color(1f, 0.7f, 0.2f);
        private readonly Color Color_TextError = new Color(1f, 0.3f, 0.3f);
        private readonly Color Color_LogBackground = new Color(0.18f, 0.18f, 0.18f);
        private readonly Color Color_LogBackgroundAlt = new Color(0.23f, 0.23f, 0.23f);
        private readonly Color Color_SelectedLog = new Color(.25f, .45f, .70f, .8f);
        private readonly Color Color_Separator = new Color(0.1f, 0.1f, 0.1f);
        private readonly Color Color_DetailBackground = new Color(0.15f, 0.15f, 0.15f);


        [MenuItem("Tools/Debug/Custom Console")]
        public static void Open()
        {
            GetWindow<CustomConsoleWindow>("Custom Console");
        }

        private void OnEnable()
        {
            if (mTreeViewState == null)
                mTreeViewState = new TreeViewState();

            mTreeView = new ConsoleTreeView(mTreeViewState);

            mTreeView.LogSelected += OnLogSelected;

            createColumnHeader();

            mPendingLogEntries.Clear();
            mNextLogRefreshTime = EditorApplication.timeSinceStartup + LOG_REFRESH_INTERVAL;

            LogCollector.OnLogAdded += onLogAdded;
            EditorApplication.update += onEditorUpdate;
        }

        private void OnDisable()
        {
            if (mTreeViewState != null)
                mTreeView.LogSelected -= OnLogSelected;

            LogCollector.OnLogAdded -= onLogAdded;
            EditorApplication.update -= onEditorUpdate;
        }

        private void OnLogSelected(LogEntry log)
        {
            mSelectedLog = log;

            mDetailScroll = Vector2.zero;

            Repaint();
        }

        private void onLogAdded(LogEntry addedLogEntry)
        {
            // mbDirty = true;
            mPendingLogEntries.Add(addedLogEntry);
        }

        private void onEditorUpdate()
        {
            // if (!mbDirty)
            if (mPendingLogEntries.Count == 0)
                return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime < mNextLogRefreshTime)
                return;

            mNextLogRefreshTime = currentTime + LOG_REFRESH_INTERVAL;

            bool bShouldAutoScroll = mTreeView.IsNearBottom();

            LogEntry lastLog = null;

            // mbDirty = false;
            foreach (LogEntry logEntry in mPendingLogEntries)
            {
                mTreeView.AddLogEntry(logEntry);
                lastLog = logEntry;
            }

            mPendingLogEntries.Clear();

            if (bShouldAutoScroll && lastLog != null)
            {
                mTreeView.ScrollToLog(lastLog);
            }

            // mTreeView.Reload();
            Repaint();
        }

        private void OnGUI()
        {
            Rect toolbarRect = new Rect(
                x: 0,
                y: 0,
                width: position.width,
                TOOLBAR_HEIGHT);

            Rect contentRect = new Rect(
                x: 0f,
                y: TOOLBAR_HEIGHT,
                width: position.width,
                height: position.height - TOOLBAR_HEIGHT);

            DrawToolbar();

            // DrawColumnHeader();

            //Rect contentRect = GUILayoutUtility.GetRect(0,
            //                                            position.height,//float.MaxValue,
            //                                            GUILayout.ExpandWidth(true),
            //                                            GUILayout.ExpandHeight(true));

            //// GUI.Label(new Rect(0, 0, 300f, 50f), "GUI Label Test");

            //float maxDetailHeight = contentRect.height - MIN_LOG_LIST_HEIGHT - SPLITTER_HEIGHT;

            //mDetailHeight = Mathf.Clamp(mDetailHeight, MIN_DETAIL_HEIGHT, maxDetailHeight);

            //Rect logListRect = new Rect(
            //    x: contentRect.x,
            //    y: contentRect.y,
            //    width: contentRect.width,
            //    height: contentRect.height - mDetailHeight - SPLITTER_HEIGHT);

            //Rect splitterRect = new Rect(
            //    x: contentRect.x,
            //    y: logListRect.yMax,
            //    width: contentRect.width,
            //    height: SPLITTER_HEIGHT);

            //Rect detailRect = new Rect(
            //    x: contentRect.x,
            //    y: splitterRect.yMax,
            //    width: contentRect.width,
            //    height: mDetailHeight);

            float maxDetailHeight = contentRect.height - MIN_LOG_LIST_HEIGHT - SPLITTER_HEIGHT;

            maxDetailHeight = Mathf.Max(MIN_DETAIL_HEIGHT, maxDetailHeight);
            mDetailHeight = Mathf.Clamp(mDetailHeight, MIN_DETAIL_HEIGHT, maxDetailHeight);

            Rect logListRect = new Rect(
                x: contentRect.x,
                y: contentRect.y,
                width: contentRect.width,
                height: contentRect.height - mDetailHeight - SPLITTER_HEIGHT);

            Rect splitterRect = new Rect(
                x: contentRect.x,
                y: logListRect.yMax,
                width: contentRect.width,
                height: SPLITTER_HEIGHT);

            Rect detailRect = new Rect(
                x: contentRect.x,
                y: splitterRect.yMax,
                width: contentRect.width,
                height: mDetailHeight);

            // DrawLogList();
            DrawLogList(logListRect);

            DrawSplitter(splitterRect, contentRect);

            // DrawDetail();
            DrawDetail(detailRect);

            //Rect testRect = GUILayoutUtility.GetRect(300f, 300f);
            ////EditorGUI.DrawRect(testRect, Color.blue);
            ////EditorGUI.LabelField(testRect, "EditorGUI Label Test");

            //// GUI.BeginGroup(testRect);
            //Rect testRect2 = GUILayoutUtility.GetRect(400f, 400f);
            //EditorGUI.DrawRect(testRect2, Color.red);
            //// GUI.EndGroup();

            //// GUILayout.BeginArea(testRect2);
            //EditorGUILayout.BeginScrollView(mLogListScroll, true, true);
            //EditorGUI.DrawRect(testRect2, Color.red);
            //EditorGUILayout.EndScrollView();
            //// GUILayout.EndArea();

            //Rect testRect3 = GUILayoutUtility.GetRect(400f, 400f);
            //EditorGUI.DrawRect(testRect3, Color.green);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                LogCollector.Clear();
                mPendingLogEntries.Clear();
                mNextLogRefreshTime = EditorApplication.timeSinceStartup + LOG_REFRESH_INTERVAL;
                mTreeView.Clear();
                mSelectedLog = null;
                Repaint();
            }

            GUILayout.FlexibleSpace();

            bool bLogTypeEnableChanged = false;

            ConsoleTreeView.LogTypeSet infoLogTypeSet = mTreeView.GetLogTypeSet(LogType.Log);
            int infoLogCount = infoLogTypeSet.count;
            //GUIContent infoIconContent = mInfoLogCount > 0 ?
            GUIContent infoIconContent = infoLogCount > 0 ?
                EditorGUIUtility.IconContent("d_console.infoicon") : EditorGUIUtility.IconContent("d_console.infoicon.inactive.sml@2x");
            // infoIconContent.text = $"{mInfoLogCount}";
            infoIconContent.text = $"{infoLogCount}";
            GUIStyle infoLogToggle = EditorStyles.toolbarButton;
            infoLogToggle.stretchWidth = true;

            //mbShowInfoLog = GUILayout.Toggle(mbShowInfoLog, 
            bool bInfoLogEnable = GUILayout.Toggle(infoLogTypeSet.enable,
                infoIconContent,
                infoLogToggle,
                GUILayout.MinWidth(LOG_TOGGLE_MIN_WIDTH));

            bLogTypeEnableChanged = mTreeView.SetLogTypeEnable(LogType.Log, bInfoLogEnable);

            mInfoLogCount = 0;

            ConsoleTreeView.LogTypeSet warningLogTypeSet = mTreeView.GetLogTypeSet(LogType.Warning);
            int warningLogCount = warningLogTypeSet.count;
            GUIContent warningIconContent = warningLogCount > 0 ?
                EditorGUIUtility.IconContent("d_console.warnicon") : EditorGUIUtility.IconContent("d_console.warnicon.inactive.sml@2x");
            warningIconContent.text = $"{warningLogCount}";
            GUIStyle warningLogToggle = EditorStyles.toolbarButton;
            warningLogToggle.stretchWidth = true;

            //mbShowWarningLog = GUILayout.Toggle(mbShowWarningLog,
            bool bWarningLogEnable = GUILayout.Toggle(warningLogTypeSet.enable,
                warningIconContent,
                warningLogToggle,
                GUILayout.MinWidth(LOG_TOGGLE_MIN_WIDTH));

            bLogTypeEnableChanged = mTreeView.SetLogTypeEnable(LogType.Warning, bWarningLogEnable);

            mWarningLogCount = 0;

            ConsoleTreeView.LogTypeSet errorLogTypeSet = mTreeView.GetLogTypeSet(LogType.Error);
            int errorLogCount = errorLogTypeSet.count;
            GUIContent errorIconContent = errorLogCount > 0 ?
                EditorGUIUtility.IconContent("console.erroricon") : EditorGUIUtility.IconContent("console.erroricon.inactive.sml@2x");
            errorIconContent.text = $"{errorLogCount}";
            GUIStyle errorLogToggle = EditorStyles.toolbarButton;
            errorLogToggle.stretchWidth = true;

            //mbShowErrorLog = GUILayout.Toggle(mbShowErrorLog,
            bool bErrorLogEnable = GUILayout.Toggle(errorLogTypeSet.enable,
                errorIconContent,
                errorLogToggle,
                GUILayout.MinWidth(LOG_TOGGLE_MIN_WIDTH));

            bLogTypeEnableChanged = mTreeView.SetLogTypeEnable(LogType.Error, bErrorLogEnable);

            mErrorLogCount = 0;

            //if(bLogTypeEnableChanged)
            //{
            //    Repaint();
            //}

            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false);
            mColumnHeader.OnGUI(headerRect, 0f);
        }

        private void createColumnHeader()
        {
            float foldoutWidth = 20f;
            float frameWidth = 50f;
            float executionOrderWidth = 160f;
            float callFuntionWidth = 300f;
            float messageWidth = position.width - foldoutWidth
                - frameWidth - executionOrderWidth - callFuntionWidth;

            var columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(""),
                    width = foldoutWidth,
                    maxWidth = foldoutWidth,
                    minWidth = foldoutWidth,
                    autoResize = false,
                    canSort = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Frame"),
                    width = frameWidth,
                    minWidth = 50f,
                    autoResize = false,
                    canSort = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Execution Order"),
                    width = executionOrderWidth,
                    minWidth = 160f,
                    autoResize = false,
                    canSort = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Message"),
                    width = messageWidth,
                    minWidth = 300f,
                    autoResize = true,
                    canSort = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Call Function"),
                    width = callFuntionWidth,
                    minWidth = 160f,
                    autoResize = false,
                    canSort = false
                }
            };

            var state = new MultiColumnHeaderState(columns);

            mColumnHeader = new MultiColumnHeader(state);

            //mColumnHeader.columnSettingsChanged += (index) =>
            //{
            //    if (index == 1)
            //        return;

            //    var messageColumn = mColumnHeader.GetColumn(index);
            //    var callFuntionColumn = mColumnHeader.GetColumn(index + 1);

            //    float callFuntionWidth = position.width - messageColumn.width;
            //    callFuntionWidth = Mathf.Clamp(callFuntionWidth, callFuntionColumn.minWidth, position.width);
            //    callFuntionColumn.width = callFuntionWidth;
            //};

            // mTreeView.multiColumnHeader = mColumnHeader;
            mTreeView.SetColumnHeader(mColumnHeader);
        }

        private void DrawLogList(Rect rect)
        {
            mTreeView.OnGUI(rect);

            return;

            Rect viewRect = new Rect(
                x: rect.x + 6f,
                y: rect.y + 6f,
                width: rect.width - 12f,
                height: mLogListViewRectHeight); // rect.height - 12f);

            mLogListScroll = GUI.BeginScrollView(rect, mLogListScroll, viewRect);

            //float lineHeight = EditorGUIUtility.singleLineHeight;
            //float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect frameGroupRect = new Rect(
                x: viewRect.x,
                y: viewRect.y,
                width: viewRect.width,
                height: Height_Text);

            // int lastFrame = -1;
            bool alternate = false;

            int index = 0;

            float totalHeight = 0f;

            while (index < LogCollector.Logs.Count)
            {
                int frame = LogCollector.Logs[index].Frame;

                int startIndex = index;
                int frameGroupLogCount = 0;

                while (index < LogCollector.Logs.Count && LogCollector.Logs[index].Frame == frame)
                {
                    LogType logType = LogCollector.Logs[index].Type;

                    switch (logType)
                    {
                        case LogType.Log:
                            mInfoLogCount++;
                            if (mbShowInfoLog)
                                frameGroupLogCount++;
                            break;

                        case LogType.Warning:
                            mWarningLogCount++;
                            if (mbShowWarningLog)
                                frameGroupLogCount++;
                            break;

                        case LogType.Exception:
                        case LogType.Assert:
                        case LogType.Error:
                            mErrorLogCount++;
                            if (mbShowErrorLog)
                                frameGroupLogCount++;
                            break;
                    }

                    index++;
                }

                int endIndex = index;

                if (frameGroupLogCount < 1)
                    continue;

                float frameGroupHeight = DrawFrameGroup(frameGroupRect, frame, startIndex, endIndex, alternate);

                frameGroupRect.y += frameGroupHeight;
                totalHeight += frameGroupHeight;

                alternate = !alternate;
            }

            mLogListViewRectHeight = totalHeight;

            GUI.EndScrollView();
        }

        private void DrawSplitter(Rect splitterRect, Rect contentRect)
        {
            EditorGUI.DrawRect(splitterRect, Color_Separator);

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            Event e = Event.current;

            switch (e.type)
            {
                case EventType.MouseDown:
                    {
                        if (e.button == 0 && splitterRect.Contains(e.mousePosition))
                        {
                            mIsResizing = true;

                            float currentHeight = contentRect.yMax - e.mousePosition.y;

                            mResizeOffset = currentHeight - mDetailHeight;

                            e.Use();
                        }

                        break;
                    }
                case EventType.MouseDrag:
                    {
                        if (mIsResizing)
                        {
                            float height = contentRect.yMax - e.mousePosition.y - mResizeOffset;

                            float maxHeight = contentRect.height - MIN_LOG_LIST_HEIGHT - SPLITTER_HEIGHT;

                            mDetailHeight = Mathf.Clamp(height, MIN_DETAIL_HEIGHT, maxHeight);

                            Repaint();
                            e.Use();
                        }

                        break;
                    }

                case EventType.MouseUp:
                    {
                        if (mIsResizing)
                        {
                            mIsResizing = false;

                            e.Use();
                        }

                        break;
                    }
            }
        }

        private void DrawDetail(Rect rect)
        {
            //Rect separatorRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));

            //EditorGUI.DrawRect(separatorRect, Color_Separator);

            // Rect detailRect = GUILayoutUtility.GetRect(0, DETAIL_HEIGHT, GUILayout.ExpandWidth(true));

            // EditorGUI.DrawRect(rect, Color_DetailBackground);

            if (!mTreeView.TryGetSelectedLogEntry(out LogEntry selectedLogEntry))
            {
                // drawStackTrace(rect, selectedLogEntry);
                return;
            }

            //return;

            //if (mSelectedLog == null)
            //    return;

            //Rect areaRect = new Rect(x: rect.x + 6,
            //                              y: rect.y + 6,
            //                          width: rect.width - 12,
            //                         height: rect.height - 12);

            //GUILayout.BeginArea(areaRect);

            //EditorGUILayout.BeginVertical(GUILayout.Height(DETAIL_HEIGHT));
            //EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            {
                Rect viewRect = new Rect(
                    x: rect.x + 6f,
                    y: rect.y + 6f,
                    width: rect.width - 12f,
                    height: mDetailViewRectHeight);

                mDetailScroll = GUI.BeginScrollView(rect, mDetailScroll, viewRect);
                // mDetailScroll = EditorGUILayout.BeginScrollView(mDetailScroll, GUILayout.ExpandHeight(true));
                {
                    // Rect detailRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    // Rect detailRect = GUILayoutUtility.GetRect(50f, 50f);
                    // detailRect.y -= 50f;
                    // EditorGUI.DrawRect(viewRect, Color_DetailBackground);
                    // EditorGUILayout.LabelField($"x: {detailRect.x}, y: {detailRect.y}");
                    // GUI.Label(detailRect, $"x: {detailRect.x}, y: {detailRect.y}");

                    drawStackTrace(viewRect, selectedLogEntry);
                    //if (mSelectedLog != null)
                    //{
                    //    string detail = mSelectedLog.Message + "\n\n" + mSelectedLog.StackTrace;

                    //    int lineCount = 0;

                    //    foreach (char c in detail)
                    //    {
                    //        if (c == '\n')
                    //            lineCount++;
                    //    }

                    //    float height = Height_Text * lineCount + Height_Spacing * lineCount;
                    //    mDetailViewRectHeight = Mathf.Clamp(height, rect.height, float.MaxValue);

                    //    Rect detailRect = new Rect(
                    //        x: viewRect.x,
                    //        y: viewRect.y,
                    //        width: rect.width,
                    //        height: height);

                    //    GUIStyle detailStyle = EditorStyles.label;
                    //    detailStyle.alignment = TextAnchor.UpperLeft;
                    //    detailStyle.focused.textColor = detailStyle.normal.textColor;

                    //    // GUI.Label(viewRect, detail);
                    //    // EditorGUI.SelectableLabel(detailRect, detail);
                    //    EditorGUI.SelectableLabel(viewRect, detail, detailStyle);
                    //    // drawStackTrace(mTreeView.SelectedLogEntry);
                    //    // EditorGUI.LinkButton(viewRect, detail);
                    //    // EditorGUILayout.SelectableLabel(detail, GUILayout.ExpandHeight(true));
                    //    //EditorGUILayout.LabelField($"Frame: {mSelectedLog.Frame}    Order: {mSelectedLog.Order}");

                    //    //EditorGUILayout.Space(4);

                    //    //EditorGUILayout.SelectableLabel(mSelectedLog.Message, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                    //    //EditorGUILayout.Space(4);

                    //    //EditorGUILayout.SelectableLabel(mSelectedLog.StackTrace, GUILayout.ExpandHeight(true));
                    //}
                }
                GUI.EndScrollView();
                //EditorGUILayout.EndScrollView();

            }
            // EditorGUILayout.EndVertical();
            // GUILayout.EndArea();
        }

        private void drawStackTrace(Rect detailRect, LogEntry logEntry)
        {
            if (string.IsNullOrEmpty(logEntry.StackTrace))
                return;

            Rect lineRect = new Rect(
                x: detailRect.x,
                y: detailRect.y,
                width: detailRect.width,
                height: CustomConsoleStyles.HEIGHT_TEXT_DETAIL);

            string[] lines = logEntry.StackTrace.Split('\n');

            foreach (string line in lines)
            {
                drawStackTraceLine(lineRect, line);

                lineRect.y += lineRect.height;//  + Height_Spacing;
            }
        }

        private void drawStackTraceLine(Rect lineRect, string line)
        {
            // Rect rect = EditorGUILayout.GetControlRect(false, Height_Text);
            initializeStackTraceStyles();

            string richText = createStackTraceRichText(line);

            EditorGUI.SelectableLabel(lineRect, richText, mStackTraceStyle);

            //Match match = mStackTraceRegex.Match(line);

            //if (!match.Success)
            //{
            //    GUI.Label(lineRect, line);
            //    return;
            //}

            //Group filePathGroup = match.Groups[1];
            //Group lineNumberGroup = match.Groups[2];

            //string prefix = line.Substring(0, filePathGroup.Index);

            //string filePath = filePathGroup.Value;
            //int lineNumber = int.Parse(lineNumberGroup.Value);
            //string linkText = $"{filePath}:{lineNumberGroup.Value}";

            ////string suffix = line.Substring(filePathGroup.Index + filePathGroup.Length);
            //string suffix = line.Substring(filePathGroup.Index + filePathGroup.Length + lineNumberGroup.Length + 1);

            //drawStackTraceLink(lineRect, prefix, linkText, suffix, filePath, lineNumber);

            //GUIStyle detailStyle = EditorStyles.label;
            //detailStyle.alignment = TextAnchor.UpperLeft;
            //detailStyle.focused.textColor = detailStyle.normal.textColor;

            //GUI.Label(lineRect, line, detailStyle);
            //// EditorGUI.SelectableLabel(lineRect, line, detailStyle);

            //if(!tryParseStackTrace(line, out string filePath, out int lineNumber))
            //{
            //    return;
            //}

            //EditorGUIUtility.AddCursorRect(lineRect, MouseCursor.Link);

            //Event e = Event.current;

            //if(e.type == EventType.MouseDown &&
            //    e.button == 0 &&
            //    e.clickCount == 2) // && 
            //    // lineRect.Contains(e.mousePosition))
            //{
            //    if (lineRect.Contains(e.mousePosition))
            //    {
            //        openScript(filePath, lineNumber);

            //        e.Use();
            //    }
            //}
        }

        private void drawStackTraceLink(Rect lineRect, string prefix, string linkText, string suffix, string filePath, int lineNumber)
        {
            initializeStackTraceStyles();

            float x = lineRect.x;

            GUIContent prefixContent = new GUIContent(prefix);

            float prefixWidth = mStackTraceStyle.CalcSize(prefixContent).x;

            Rect prefixRect = new Rect(
                x: x,
                y: lineRect.y,
                width: prefixWidth,
                height: lineRect.height);

            GUI.Label(prefixRect, prefixContent, mStackTraceStyle);

            x += prefixWidth;

            GUIContent pathContent = new GUIContent(linkText);

            float pathWidth = mStackTraceLinkStyle.CalcSize(pathContent).x;

            Rect pathRect = new Rect(
                x: x,
                y: lineRect.y,
                width: pathWidth,
                height: lineRect.height);

            GUI.Label(pathRect, pathContent, mStackTraceLinkStyle);

            EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);

            Event e = Event.current;

            if(e.type == EventType.MouseDown && e.button == 0 && pathRect.Contains(e.mousePosition))
            {
                openScript(filePath, lineNumber);

                e.Use();
            }

            x += pathWidth;

            Rect suffixRect = new Rect(
                x: x,
                y: lineRect.y,
                width: lineRect.xMax - x,
                height: lineRect.height);

            GUI.Label(suffixRect, suffix, mStackTraceStyle);
        }

        private void initializeStackTraceStyles()
        {
            //if (mStackTraceStyle != null)
            //    return;

            mStackTraceStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            mStackTraceStyle.focused.textColor = mStackTraceStyle.normal.textColor;

            mStackTraceLinkStyle = new GUIStyle(EditorStyles.label);

            mStackTraceLinkStyle.normal.textColor = CustomConsoleStyles.color_text_normal;

            mStackTraceLinkStyle.hover.textColor = CustomConsoleStyles.color_text_hover;
        }

        private string createStackTraceRichText(string line)
        {
            Match match = mStackTraceRegex.Match(line);

            if (!match.Success)
                return line;

            string filePath = match.Groups[1].Value;
            string lineNumber = match.Groups[2].Value;

            string link = $"<a href=\"{filePath}\" line=\"{lineNumber}\">" +
                $"{filePath}:{lineNumber}</a>";

            return line.Substring(0, match.Index) + "(at " + link + ")";
        }

        private float DrawFrameGroup(Rect rect, int frame, int startIndex, int endIndex, bool alternate)
        {
            int logCount = endIndex - startIndex;

            int showedLogCount = countShowedLog(startIndex, endIndex);
            // int executionOrderGrounpCount = countExecutionOrderGroup(startIndex, endIndex);

            Rect frameGroupRect = new Rect(x: rect.x + 6f,
                                        y: rect.y + 4f,
                                        width: rect.width - 12f,
                                        height: Height_Text + Height_Spacing);

            EditorGUI.DrawRect(frameGroupRect, getLogBackgroundColor(alternate));

            GUI.Label(frameGroupRect, $"Frame {frame}", EditorStyles.boldLabel);

            indentLevel();

            Rect executionOrderGroupRect = new Rect(
                x: frameGroupRect.x,
                y: frameGroupRect.y + frameGroupRect.height,
                width: frameGroupRect.width,
                height: Height_Text);

            int executionOrderIndex = startIndex;

            while (executionOrderIndex < endIndex)
            {
                ExecutionOrder executionOrder = LogCollector.Logs[executionOrderIndex].executionOrder;
                int executionOrderStartIndex = executionOrderIndex;

                while (executionOrderIndex < LogCollector.Logs.Count && executionOrder == LogCollector.Logs[executionOrderIndex].executionOrder)
                {
                    executionOrderIndex++;
                }

                int executionOrderEndIndex = executionOrderIndex;

                if (showedLogCount < 1)
                    continue;

                float executionOrderGroupHeight = DrawExecutionOrderGroup(executionOrderGroupRect,
                    executionOrder,
                    executionOrderStartIndex,
                    executionOrderEndIndex);

                executionOrderGroupRect.y += executionOrderGroupHeight;

                frameGroupRect.height += executionOrderGroupHeight;
            }

            unindentLevel();

            return frameGroupRect.height;
        }

        private float DrawExecutionOrderGroup(Rect rect, ExecutionOrder executionOrder, int startIndex, int endIndex)
        {
            Rect executionOrderGroupRect = new Rect(
                x: rect.x,
                y: rect.y,
                width: rect.width,
                height: Height_Text + Height_Spacing);

            GUI.Label(executionOrderGroupRect, $"[{executionOrder.ToString()}]");

            indentLevel();

            Rect logRect = new Rect(
                x: executionOrderGroupRect.x,
                y: executionOrderGroupRect.y + executionOrderGroupRect.height,
                width: executionOrderGroupRect.width,
                height: Height_Text);

            bool bAlternate = false;

            for (int i = startIndex; i < endIndex; ++i)
            {
                LogEntry logEntry = LogCollector.Logs[i];

                EditorGUI.DrawRect(logRect, alternateColor(bAlternate, Color_LogBackground, Color_LogBackgroundAlt));
                float logHeight = DrawLog(logRect, logEntry);

                logRect.y += logHeight;
                bAlternate = !bAlternate;

                executionOrderGroupRect.height += logHeight;
            }

            unindentLevel();

            return executionOrderGroupRect.height;
        }

        private float DrawLog(Rect rect, LogEntry log)
        {
            if (!isEnabledLog(log))
            {
                return 0f;
            }

            bool selected = ReferenceEquals(mSelectedLog, log);

            if (selected)
            {
                EditorGUI.DrawRect(rect, Color_SelectedLog);
            }

            float labelHeight = DrawLabel(rect, log);

            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                mSelectedLog = log;

                Repaint();
                e.Use();
            }

            return labelHeight + Height_Spacing;
        }

        private float DrawLabel(Rect rect, LogEntry log)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);

            string stackTrace = log.StackTrace;
            string callFuntion = "";
            string[] stackTraceLines = stackTrace.Split('\n');
            if (stackTraceLines.Length > 2)
            {
                string callFuntionLine = stackTraceLines[2];
                string[] callFuntionLineSplit = callFuntionLine.Split(" ");
                callFuntion = $"{callFuntionLineSplit[0]}{callFuntionLineSplit[1]}";
            }

            string message = log.Message;

            string pattern = @"\[[^\]]*\]";
            string messageWithoutTags = message;

            if (Regex.IsMatch(message, pattern))
            {
                // MatchCollection matches = Regex.Matches(message, pattern);

                messageWithoutTags = Regex.Replace(message, pattern, "");
            }

            string[] messageLines = messageWithoutTags.Split('\n');
            // string text = $"[{log.Order}] {messageLines[0]}";
            string text = $"{messageLines[0]}";

            GUIContent iconContent = getLogIconContent(log.Type);
            iconContent.text = text;

            Rect messageRect = new Rect(
                x: rect.x,
                y: rect.y,
                width: mColumnHeader.GetColumnRect(0).width,
                height: rect.height);

            GUI.Label(messageRect, iconContent, style);

            Rect separatorRect = new Rect(
                x: rect.x + mColumnHeader.GetColumnRect(0).width,
                y: rect.y,
                width: 1f,
                height: rect.height);

            // EditorGUI.DrawRect(separatorRect, Color_Separator);

            Rect callFuntionRect = new Rect(
                x: rect.x + mColumnHeader.GetColumnRect(0).width,
                y: rect.y,
                width: mColumnHeader.GetColumnRect(1).width,
                height: rect.height);

            GUI.Label(callFuntionRect, callFuntion, style);

            return Height_Text;
        }

        private bool isEnabledLog(LogEntry logEntry)
        {
            bool bEnabled = true;

            switch (logEntry.Type)
            {
                case LogType.Log:
                    mInfoLogCount++;
                    bEnabled = mbShowInfoLog;
                    break;

                case LogType.Warning:
                    mWarningLogCount++;
                    bEnabled = mbShowWarningLog;
                    break;

                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    mErrorLogCount++;
                    bEnabled = mbShowErrorLog;
                    break;

                default:
                    break;
            }

            return bEnabled;
        }

        private GUIContent getLogIconContent(LogType logType)
        {
            GUIContent iconContent = EditorGUIUtility.IconContent("console.infoicon");

            switch (logType)
            {
                case LogType.Log:
                    break;

                case LogType.Warning:
                    iconContent = EditorGUIUtility.IconContent("console.warnicon");
                    break;

                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    iconContent = EditorGUIUtility.IconContent("console.erroricon");
                    break;

                default:
                    break;
            }

            return iconContent;
        }

        private int countShowedLog(int startIndex, int endIndex)
        {
            int count = 0;

            for (int i = startIndex; i < endIndex; ++i)
            {
                LogType logType = LogCollector.Logs[i].Type;

                switch (logType)
                {
                    case LogType.Log:
                        if (mbShowInfoLog)
                            count++;
                        break;

                    case LogType.Warning:
                        if (mbShowWarningLog)
                            count++;
                        break;

                    case LogType.Exception:
                    case LogType.Assert:
                    case LogType.Error:
                        if (mbShowErrorLog)
                            count++;
                        break;
                }
            }

            return count;
        }

        private int countExecutionOrderGroup(int startIndex, int endIndex)
        {
            int count = 0;
            ExecutionOrder executionOrder = ExecutionOrder.None;

            for (int i = startIndex; i < endIndex; ++i)
            {
                if (LogCollector.Logs[i].executionOrder != executionOrder)
                    count++;
            }

            return count;
        }

        private bool tryParseStackTrace(string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            Match match = mStackTraceRegex.Match(stackTrace);

            if (!match.Success)
                return false;

            filePath = match.Groups[1].Value;

            return int.TryParse(match.Groups[2].Value, out lineNumber);
        }

        private void openScript(string filePath, int lineNumber)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);

            if (script == null)
                return;

            AssetDatabase.OpenAsset(script, lineNumber);
        }

        private Color getLogBackgroundColor(bool alternate)
        {
            return alternateColor(alternate, Color_LogBackground, Color_LogBackgroundAlt);
        }

        private Color alternateColor(bool value, Color a, Color b)
        {
            return value ? a : b;
        }

        private void indentLevel()
        {
            EditorGUI.indentLevel += 2;
        }

        private void unindentLevel()
        {
            EditorGUI.indentLevel -= 2;
        }
    }

}