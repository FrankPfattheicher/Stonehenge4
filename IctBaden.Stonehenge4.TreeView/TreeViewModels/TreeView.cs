using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBeProtected.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace IctBaden.Stonehenge.Extension.TreeViewModels
{
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once UnusedType.Global
    public class TreeView
    {
        public List<TreeNode> RootNodes { get; set; }
        public TreeNode SelectedNode { get; private set; }

        // ReSharper disable once UnusedMember.Global
        public TreeView()
        {
            RootNodes = new();
        }

        public void SetRootNodes(IEnumerable<TreeNode> rootNodes, bool expandRootNodes = false)
        {
            RootNodes = rootNodes.ToList();
            
            if (!expandRootNodes) return;
            
            foreach (var node in RootNodes)
            {
                node.IsExpanded = true;
            }
        }
        
        public IEnumerable<TreeNode> AllNodes() => 
            new TreeNode(null, null ) { Children = RootNodes }.AllNodes();
        
        public TreeNode FindNodeById(string id)
        {
            return AllNodes().FirstOrDefault(node => node.Id == id);
        }

        public void TreeToggle(string nodeId)
        {
            var node = FindNodeById(nodeId);
            if (node == null) return;

            node.IsExpanded = !node.IsExpanded;
        }

        public void TreeSelect(string nodeId)
        {
            var node = FindNodeById(nodeId);
            if (node == null) return;

            foreach (var treeNode in AllNodes())
            {
                treeNode.IsSelected = false;
            }
            node.IsSelected = true;
            SelectedNode = node;
            SelectionChanged?.Invoke(SelectedNode);
        }

        public event Action<TreeNode> SelectionChanged;
    }
}
