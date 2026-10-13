using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CustomConsole
{

    public class ExecutionOrderTreeViewItem : TreeViewItem
    {
        public ExecutionOrder ExecutionOrder;
        public int GroupIndex { get; set; }

        public ExecutionOrderTreeViewItem(int id, int depth, ExecutionOrder executionOrder, int groupIndex) 
            : base(id, depth, $"[{executionOrder.ToString()}]")
        {
            ExecutionOrder = executionOrder;
            GroupIndex = groupIndex;
        }
    }
}