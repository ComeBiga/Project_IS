using UnityEditor.IMGUI.Controls;

namespace CustomConsole
{
    public class LogTreeViewItem : TreeViewItem
    {
        public LogEntry Log { get; }
        public int GroupIndex { get; }
        public int VisibleIndex { get; set; }
        public bool ShowFrame { get; set; }
        public bool ShowExecutionOrder { get; set; }

        public LogTreeViewItem(int id, int depth, LogEntry log, int groupIndex)
            : base(id, depth, log.Message)
        {
            Log = log;
            GroupIndex = groupIndex;
        }
    }
}
