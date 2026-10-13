using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace CustomConsole
{
    public class FrameTreeViewItem : TreeViewItem
    {
        public int Frame { get; }
        public int GroupIndex { get; }

        private List<ExecutionOrder> mExecutionOrders;
        private Dictionary<ExecutionOrder, ExecutionOrderTreeViewItem> mExecutionOrderDic;

        public FrameTreeViewItem(int id, int depth, int frame, int groupIndex)
            : base(id, depth, $"{frame}")// $"Frame {frame}")
        {
            Frame = frame;
            GroupIndex = groupIndex;

            mExecutionOrders = new List<ExecutionOrder>();
            mExecutionOrders.Add(ExecutionOrder.None);
        }

        public void AddExecutionOrder(ExecutionOrder executionOrder)
        {
            mExecutionOrders.Add(executionOrder);
        }

        public bool HasExecutionOrder(ExecutionOrder executionOrder)
        {
            return mExecutionOrders.Contains(executionOrder);
        }

        public ExecutionOrderTreeViewItem GetExecutionOrderTreeViewItemOrNull(ExecutionOrder executionOrder)
        {
            if (children == null)
                return null;

            foreach (TreeViewItem item in children)
            {
                if(item is ExecutionOrderTreeViewItem executionOrderTreeViewItem)
                {
                    if(executionOrderTreeViewItem.ExecutionOrder == executionOrder)
                        return executionOrderTreeViewItem;
                }
            }

            return null;
        }
    }
}