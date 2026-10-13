using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CustomConsole
{
    public enum Column
    {
        Foldout,
        Frame,
        ExecutionOrder,
        Message,
        CallMethod
    }

    public class ConsoleTreeView : TreeView
    {
        public class LogTypeSet
        {
            public bool enable = true;
            public int count = 0;
        }

        // public int InfoLogCount { get; private set; }
        // public float FoldoutWidth => foldoutWidth;

        public event Action<LogEntry> LogSelected;

        private readonly List<TreeViewItem> mRows = new List<TreeViewItem>();
        private readonly List<LogTreeViewItem> mLogItems = new List<LogTreeViewItem>();
        private bool mUseTreeView = false;

        private FrameTreeViewItem mCurrentFrameItem;
        private LogTreeViewItem mSelectedLogItem;
        private int mNextGroupItemID = -1;
        private int mGroupIndex = -1;
        private int mVisibleLogIndex = 0;
        private Dictionary<LogType, LogTypeSet> mLogTypeCount = new Dictionary<LogType, LogTypeSet>();

        public ConsoleTreeView(TreeViewState state)
            : base(state)
        {
            rowHeight = 20f;
            depthIndentWidth = 0f;

            showBorder = true;
            showAlternatingRowBackgrounds = false;

            Reload();

            initLogTypeCount();

            foreach (LogEntry logEntry in LogCollector.Logs)
            {
                AddLogEntry(logEntry);
            }
        }

        public void SetColumnHeader(MultiColumnHeader multiColumnHeader)
        {
            base.multiColumnHeader = multiColumnHeader;

            columnIndexForTreeFoldouts = 0;

            if (!mUseTreeView)
            {
                List<int> visibleColumns = new List<int>(multiColumnHeader.state.visibleColumns);
                visibleColumns.Remove((int)Column.Foldout);
                multiColumnHeader.state.visibleColumns = visibleColumns.ToArray();
                multiColumnHeader.state.columns[(int)Column.Foldout].allowToggleVisibility = false;
            }
        }

        public new void Reload()
        {
            base.Reload();
        }

        public void AddLogEntry(LogEntry logEntry)
        {
            bool newFrame = mCurrentFrameItem == null || mCurrentFrameItem.Frame != logEntry.Frame;

            if (newFrame)
                AddFrame(logEntry.Frame);

            LogTreeViewItem logItem = new LogTreeViewItem(
                logEntry.Id,
                2,
                logEntry,
                mGroupIndex);

            addLogTypeCount(logEntry.Type);

            if (logEntry.executionOrder == ExecutionOrder.None)
            {
                mCurrentFrameItem.AddChild(logItem);
            }
            else
            {
                ExecutionOrderTreeViewItem executionOrderItem =
                    mCurrentFrameItem.GetExecutionOrderTreeViewItemOrNull(logEntry.executionOrder);

                if (executionOrderItem == null)
                {
                    mCurrentFrameItem.AddExecutionOrder(logEntry.executionOrder);
                    executionOrderItem = new ExecutionOrderTreeViewItem(
                        getNextGroupItemID(),
                        1,
                        logEntry.executionOrder,
                        mGroupIndex);

                    mCurrentFrameItem.AddChild(executionOrderItem);
                    setExpandedWithoutRebuild(executionOrderItem.id);
                }

                executionOrderItem.AddChild(logItem);
            }

            mLogItems.Add(logItem);

            if (mUseTreeView)
                rebuildTreeRows(rootItem);
            else
                addFlatRow(logItem);
        }

        public void AddFrame(int frame)
        {
            mGroupIndex++;

            FrameTreeViewItem frameItem = new FrameTreeViewItem(
                    getNextGroupItemID(),
                    0,
                    frame,
                    mGroupIndex);

            rootItem.AddChild(frameItem);

            mCurrentFrameItem = frameItem;
            mVisibleLogIndex = 0;

            // Frame 자체는 항상 표시
            // mRows.Add(frameItem);
            if (mUseTreeView)
                addRow(frameItem);

            setExpandedWithoutRebuild(frameItem.id);
        }

        public void Clear()
        {
            rootItem.children.Clear();

            mRows.Clear();
            mLogItems.Clear();
            mVisibleLogIndex = 0;

            mCurrentFrameItem = null;
            mSelectedLogItem = null;
            mGroupIndex = -1;
            mNextGroupItemID = -1;

            state.expandedIDs.Clear();

            SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
            state.scrollPos = Vector2.zero;

            clearLogTypeCount();
            Repaint();
        }

        public void RefreshRows()
        {
            rebuildRows(rootItem);

            Repaint();
        }

        public void ScrollToLog(LogEntry logEntry)
        {
            if (logEntry == null)
                return;

            if (!mUseTreeView)
            {
                foreach (TreeViewItem row in mRows)
                {
                    if (row.id != logEntry.Id)
                        continue;

                    FrameItem(row.id);
                    return;
                }

                return;
            }

            TreeViewItem logItem = FindItem(logEntry.Id, rootItem);
            frameItemRecursive(logItem);
        }

        public bool IsNearBottom()
        {
            float viewHeight = treeViewRect.height;
            float contentHeight = totalHeight;

            float bottom =
                state.scrollPos.y + viewHeight;

            const float threshold = 30f;

            return bottom >= contentHeight - threshold;
        }

        public int CountLogType(LogType logType)
        {
            return getLogTypeSet(logType).count;
        }

        public LogTypeSet GetLogTypeSet(LogType logType)
        {
            return getLogTypeSet(logType);
        }

        public bool SetLogTypeEnable(LogType logType, bool enable)
        {
            LogTypeSet logTypeSet = GetLogTypeSet(logType);

            if (logTypeSet.enable != enable)
            {
                logTypeSet.enable = enable;

                RefreshRows();

                return true;
            }

            return false;
        }

        public bool TryGetSelectedLogEntry(out LogEntry selectedLogEntry)
        {
            if (mSelectedLogItem == null)
            {
                selectedLogEntry = null;
                return false;
            }

            selectedLogEntry = mSelectedLogItem.Log;
            return true;
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem
            {
                id = 0,
                depth = -1,
                displayName = "Root",
                children = new List<TreeViewItem>()
            };

            //int lastFrame = -1;
            //int groupIndex = -1;
            //InfoLogCount = 0;

            //FrameTreeViewItem currentFrameItem = null;

            //foreach (LogEntry log in LogCollector.Logs)
            //{
            //    if (log.Frame != lastFrame)
            //    {
            //        lastFrame = log.Frame;
            //        int frameItemId = GetFrameItemId(log.Frame);
            //        groupIndex++;

            //        currentFrameItem = new FrameTreeViewItem(
            //            frameItemId,
            //            0,
            //            log.Frame,
            //            groupIndex);

            //        root.AddChild(currentFrameItem);
            //    }

            //    var logItem =
            //        new LogTreeViewItem(log.Id,
            //                            1,
            //                            log,
            //                            groupIndex);

            //    switch (log.Type)
            //    {
            //        case LogType.Log:
            //            InfoLogCount++;
            //            break;

            //        case LogType.Warning:
            //            break;

            //        case LogType.Exception:
            //        case LogType.Assert:
            //        case LogType.Error:
            //            break;
            //    }

            //    currentFrameItem.AddChild(logItem);
            //}

            //SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            rebuildRows(root);
            //mRows.Clear();

            //// AddExpandedRows(root, mRows);
            //if (root.children == null)
            //    return mRows;

            //foreach (TreeViewItem frameItem in root.children)
            //{
            //    mRows.Add(frameItem);

            //    if (!IsExpanded(frameItem.id))
            //        continue;

            //    if (frameItem.children == null)
            //        continue;

            //    foreach(TreeViewItem executionOrderItem in frameItem.children)
            //    {
            //        mRows.Add(executionOrderItem);

            //        if (!IsExpanded(executionOrderItem.id))
            //            continue;

            //        if (executionOrderItem.children == null)
            //            continue;

            //        foreach(TreeViewItem logItem in executionOrderItem.children)
            //        {
            //            mRows.Add(logItem);
            //        }
            //    }
            //}

            return mRows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            drawRowBackground(args);

            if (!mUseTreeView)
            {
                for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
                {
                    drawCell(args.GetCellRect(visibleColumnIndex), args.item,
                        (Column)args.GetColumn(visibleColumnIndex), args);
                }

                return;
            }
            //int groupIndex = getGroupIndex(args.item);

            //if (Event.current.type == EventType.Repaint)
            //{
            //    Color backgroundColor;

            //    if (groupIndex % 2 == 0)
            //    {
            //        backgroundColor =
            //            new Color(0.18f, 0.18f, 0.18f);
            //    }
            //    else
            //    {
            //        backgroundColor =
            //            new Color(0.22f, 0.22f, 0.22f);
            //    }

            //    EditorGUI.DrawRect(args.rowRect, backgroundColor);
            //}

            for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect cellRect = args.GetCellRect(visibleColumnIndex);

                Column column = (Column)args.GetColumn(visibleColumnIndex);

                if (args.item is FrameTreeViewItem && column == Column.Frame)
                {
                    RowGUIArgs firstColumnArgs = args;
                    firstColumnArgs.rowRect = cellRect;

                    // columnIndexForTreeFoldouts = 0;

                    base.RowGUI(firstColumnArgs);

                    continue;
                }

                if (args.item is ExecutionOrderTreeViewItem && column == Column.ExecutionOrder)
                {
                    RowGUIArgs firstColumnArgs = args;
                    firstColumnArgs.rowRect = cellRect;

                    // columnIndexForTreeFoldouts = 1;

                    base.RowGUI(firstColumnArgs);

                    continue;
                }

                if (args.item is LogTreeViewItem logTreeViewItem && column == Column.Message)
                {
                    RowGUIArgs firstColumnArgs = args;
                    firstColumnArgs.rowRect = cellRect;

                    // base.RowGUI(firstColumnArgs);

                    GUIContent logContent = getIconContent(logTreeViewItem.Log.Type);
                    logContent.text = logTreeViewItem.Log.Message;

                    GUI.Label(cellRect, logContent);

                    continue;
                }

                if (column == Column.CallMethod)
                    drawCell(cellRect, args.item, column, args);
            }

            // base.RowGUI(args);
        }

        protected override void SingleClickedItem(int id)
        {

        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            mSelectedLogItem = null;

            if (selectedIds.Count == 0)
            {
                LogSelected?.Invoke(null);
                return;
            }

            TreeViewItem item = FindItem(selectedIds[0], rootItem);

            if (item is LogTreeViewItem logItem)
            {
                mSelectedLogItem = logItem;

                LogSelected?.Invoke(logItem.Log);
            }
            else
            {
                LogSelected?.Invoke(null);
            }
        }

        private int getNextGroupItemID()
        {
            return mNextGroupItemID--;
        }

        [Obsolete]
        private static int getFrameItemId(int frame)
        {
            // LogEntry는 양수 ID,
            // Frame은 음수 ID 사용
            return -(frame + 1);
        }

        private void setExpandedWithoutRebuild(int id)
        {
            if (state.expandedIDs.Contains(id))
                return;

            state.expandedIDs.Add(id);
            state.expandedIDs.Sort();
        }

        private int getGroupIndex(TreeViewItem item)
        {
            if (item is FrameTreeViewItem frameItem)
                return frameItem.GroupIndex;

            if (item is ExecutionOrderTreeViewItem executionOrderItem)
                return executionOrderItem.GroupIndex;

            if (item is LogTreeViewItem logItem)
                return logItem.GroupIndex;

            return 0;
        }

        private void frameItemRecursive(TreeViewItem item)
        {
            if (item == null)
                return;

            if (mRows.Contains(item))
            {
                FrameItem(item.id);
            }
            else
            {
                if (item.parent == null)
                    return;

                frameItemRecursive(item.parent);
            }
        }

        private void addRow(TreeViewItem treeViewItem)
        {
            if (treeViewItem is FrameTreeViewItem frameTreeViewItem)
            {
                mRows.Add(frameTreeViewItem);
            }
            else if (treeViewItem is ExecutionOrderTreeViewItem executionOrderTreeViewItem)
            {
                mRows.Add(executionOrderTreeViewItem);
            }
            else if (treeViewItem is LogTreeViewItem logTreeViewItem)
            {
                if (isLogTypeEnabled(logTreeViewItem.Log.Type))
                {
                    mRows.Add(logTreeViewItem);
                }
            }
        }

        private void rebuildRows(TreeViewItem root)
        {
            if (mUseTreeView)
                rebuildTreeRows(root);
            else
                rebuildFlatRows();
        }

        private void addFlatRow(LogTreeViewItem logItem)
        {
            if (!isLogTypeEnabled(logItem.Log.Type))
                return;

            LogTreeViewItem previousLogItem = mRows.Count > 0
                ? (LogTreeViewItem)mRows[mRows.Count - 1]
                : null;

            logItem.ShowFrame = previousLogItem == null
                || previousLogItem.Log.Frame != logItem.Log.Frame;
            logItem.ShowExecutionOrder = logItem.ShowFrame
                || previousLogItem.Log.executionOrder != logItem.Log.executionOrder;

            logItem.VisibleIndex = mRows.Count;
            mRows.Add(logItem);
        }

        private void rebuildFlatRows()
        {
            mRows.Clear();

            foreach (LogTreeViewItem logItem in mLogItems)
                addFlatRow(logItem);
        }

        private void rebuildTreeRows(TreeViewItem root)
        {
            mRows.Clear();

            if (root.children == null)
                return;

            foreach (TreeViewItem frameItem in root.children)
            {
                if (!hasVisibleLogInFrame(frameItem))
                    continue;

                mRows.Add(frameItem);

                if (!IsExpanded(frameItem.id))
                    continue;

                if (frameItem.children == null)
                    continue;

                int visibleLogIndex = 0;

                foreach (TreeViewItem frameChildrenItem in frameItem.children)
                {
                    // Log
                    if (frameChildrenItem is LogTreeViewItem logTreeViewItem)
                    {
                        if (!isLogTypeEnabled(logTreeViewItem.Log.Type))
                            continue;

                        logTreeViewItem.VisibleIndex = visibleLogIndex++;

                        mRows.Add(frameChildrenItem);

                        continue;
                    }
                    // Execution Order
                    else
                    {
                        if (!hasVisibleLogInExecutionOrder(frameChildrenItem))
                            continue;

                        mRows.Add(frameChildrenItem);

                        if (!IsExpanded(frameChildrenItem.id))
                            continue;

                        if (frameChildrenItem.children == null)
                            continue;

                        visibleLogIndex = 0;

                        foreach (TreeViewItem logItem in frameChildrenItem.children)
                        {
                            logTreeViewItem = (LogTreeViewItem)logItem;

                            if (!isLogTypeEnabled(logTreeViewItem.Log.Type))
                                continue;

                            logTreeViewItem.VisibleIndex = visibleLogIndex++;

                            mRows.Add(logItem);
                        }
                    }
                }
            }
        }

        private bool hasVisibleLogInExecutionOrder(TreeViewItem executionOrderItem)
        {
            if (executionOrderItem.children == null)
                return false;

            foreach (TreeViewItem logItem in executionOrderItem.children)
            {
                LogTreeViewItem logTreeViewItem = (LogTreeViewItem)logItem;

                if (isLogTypeEnabled(logTreeViewItem.Log.Type))
                    return true;
            }

            return false;
        }

        private bool hasVisibleLogInFrame(TreeViewItem frameItem)
        {
            if (frameItem.children == null)
                return false;

            foreach (TreeViewItem frameChildrenItem in frameItem.children)
            {
                if (frameChildrenItem is LogTreeViewItem logTreeViewItem)
                {
                    if (isLogTypeEnabled(logTreeViewItem.Log.Type))
                        return true;
                }
                else
                {
                    if (hasVisibleLogInExecutionOrder(frameChildrenItem))
                        return true;
                }
            }

            return false;
        }

        private void drawRowBackground(RowGUIArgs args)
        {
            Rect rowRect = args.rowRect;

            if (args.item is FrameTreeViewItem frameTreeViewItem)
            {
                EditorGUI.DrawRect(rowRect, CustomConsoleStyles.color_frame);

                drawFrameSeparator(rowRect);

                return;
            }

            if (args.item is ExecutionOrderTreeViewItem executionOrderTreeViewItem)
            {
                EditorGUI.DrawRect(rowRect, CustomConsoleStyles.color_executionOrder);

                drawExecutionOrderSeparator(rowRect);
            }

            if (args.item is LogTreeViewItem logTreeViewItem)
            {
                if (logTreeViewItem == mSelectedLogItem)
                {
                    EditorGUI.DrawRect(rowRect, CustomConsoleStyles.color_selectedLog);
                }
                else
                {

                    Color logAlternateColor = logTreeViewItem.VisibleIndex % 2 == 0 ?
                        CustomConsoleStyles.color_log_A : CustomConsoleStyles.color_log_B;

                    EditorGUI.DrawRect(rowRect, logAlternateColor);
                }

                if (!mUseTreeView)
                {
                    if (logTreeViewItem.ShowFrame)
                        drawFrameSeparator(rowRect);
                    else if (logTreeViewItem.ShowExecutionOrder)
                        drawExecutionOrderSeparator(rowRect);
                }
            }
        }

        private void drawFrameSeparator(Rect rowRect)
        {
            Rect lineRect = new Rect(
                x: rowRect.x,
                y: rowRect.y,
                width: rowRect.width,
                height: CustomConsoleStyles.FRAME_SEPARATER_SIZE);

            EditorGUI.DrawRect(lineRect, CustomConsoleStyles.color_frameSeparator);
        }

        private void drawExecutionOrderSeparator(Rect rowRect)
        {
            Rect lineRect = new Rect(
                x: rowRect.x,
                y: rowRect.y,
                width: rowRect.width,
                height: CustomConsoleStyles.EXECUTION_ORDER_SEPARATER_SIZE);

            EditorGUI.DrawRect(lineRect, CustomConsoleStyles.color_executionOrderSeparator);
        }

        private void drawCell(Rect cellRect, TreeViewItem item, Column column, RowGUIArgs args)
        {
            if (item is FrameTreeViewItem frameItem)
            {
                drawFrameCell(cellRect, frameItem, column, args);

                return;
            }

            if (item is ExecutionOrderTreeViewItem executionItem)
            {
                drawExecutionOrderCell(cellRect, executionItem, column, args);

                return;
            }

            if (item is LogTreeViewItem logItem)
            {
                drawLogCell(cellRect, logItem, column, args);
            }
        }

        private void drawFrameCell(Rect cellRect, FrameTreeViewItem item, Column column, RowGUIArgs args)
        {
            if (column != Column.Frame)
                return;

            GUI.Label(cellRect, $"Frame {item.Frame}");
        }

        private void drawExecutionOrderCell(Rect cellRect, ExecutionOrderTreeViewItem item, Column column, RowGUIArgs args)
        {
            if (column != Column.ExecutionOrder)
                return;

            GUI.Label(cellRect, item.displayName);
        }

        private void drawLogCell(Rect cellRect, LogTreeViewItem item, Column column, RowGUIArgs args)
        {
            switch (column)
            {
                case Column.Frame:
                    {
                        if (!mUseTreeView && item.ShowFrame)
                            GUI.Label(cellRect, item.Log.Frame.ToString());

                        break;
                    }

                case Column.ExecutionOrder:
                    {
                        if (!mUseTreeView && item.ShowExecutionOrder)
                            GUI.Label(cellRect, item.Log.executionOrder.ToString());

                        break;
                    }

                case Column.Message:
                    {
                        GUIContent logContent = getIconContent(item.Log.Type);
                        logContent.text = item.Log.Message;
                        GUI.Label(cellRect, logContent);

                        break;
                    }

                // Call Method
                case Column.CallMethod:
                    {
                        GUI.Label(cellRect, item.Log.callMethod);

                        if (!string.IsNullOrEmpty(item.Log.CallFilePath) && item.Log.CallLineNumber > 0)
                        {
                            EditorGUIUtility.AddCursorRect(cellRect, MouseCursor.Link);

                            Event currentEvent = Event.current;
                            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0
                                && cellRect.Contains(currentEvent.mousePosition))
                            {
                                SetSelection(new List<int> { item.id }, TreeViewSelectionOptions.FireSelectionChanged);
                                openCallFunction(item.Log);
                                currentEvent.Use();
                            }
                        }

                        break;
                    }

                default:
                    break;
            }
        }

        private void openCallFunction(LogEntry log)
        {
            string filePath = log.CallFilePath;
            string projectPath = Application.dataPath.Substring(0,
                Application.dataPath.Length - "Assets".Length);

            if (filePath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(projectPath.Length);

            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
            if (script != null)
                AssetDatabase.OpenAsset(script, log.CallLineNumber);
        }

        private GUIContent getIconContent(LogType logType)
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

        private void initLogTypeCount()
        {
            mLogTypeCount.Clear();
            mLogTypeCount.Add(LogType.Log, new LogTypeSet());
            mLogTypeCount.Add(LogType.Warning, new LogTypeSet());
            mLogTypeCount.Add(LogType.Error, new LogTypeSet());
        }

        private LogTypeSet getLogTypeSet(LogType logType)
        {
            LogTypeSet logTypeSet = null;

            switch (logType)
            {
                case LogType.Log:
                    logTypeSet = mLogTypeCount[LogType.Log];
                    break;

                case LogType.Warning:
                    logTypeSet = mLogTypeCount[LogType.Warning];
                    break;

                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    logTypeSet = mLogTypeCount[LogType.Error];
                    break;

                default:
                    break;
            }

            return logTypeSet;
        }

        private void clearLogTypeCount()
        {
            getLogTypeSet(LogType.Log).count = 0;
            getLogTypeSet(LogType.Warning).count = 0;
            getLogTypeSet(LogType.Error).count = 0;
        }

        private void addLogTypeCount(LogType logType)
        {
            getLogTypeSet(logType).count += 1;
        }

        private bool isLogTypeEnabled(LogType logType)
        {
            LogTypeSet logTypeSet = getLogTypeSet(logType);

            return logTypeSet.enable;
        }
    }
}