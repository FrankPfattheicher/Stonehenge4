using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Extension.TreeViewModels
{
    public class TreeNode
    {
        public string Id { get; }
        /// for example fa fa-folder, fa fa-folder-open
        public string Icon { get; private set; } 
        public string Name { get; init; }
        public string Tooltip { get; set; } = string.Empty;

        public List<TreeNode> Children { get; set; }

        // ReSharper disable once UnusedMember.Global
        public bool IsVisible => Parent?.IsExpanded ?? true;
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        // ReSharper disable once UnusedMember.Global
        public bool HasChildren => Children.Count > 0;
        public bool IsDraggable { get; internal set; }

        
        public string Class => IsSelected ? "tree-selected" : "";

        public string ExpandIcon => HasChildren
            ? (IsExpanded ? "fa fa-caret-down" : "fa fa-caret-right")
            : "fa";

        
        private readonly IExpandedProvider? _expanded;
        public readonly TreeNode? Parent;
        public readonly object? Item;
        
        public TreeNode(TreeNode? parentNode, object? item, IExpandedProvider? expanded = null)
        {
            Item = item;
            Id = (GetItemProperty("Id") as string) ?? Guid.NewGuid().ToString("N");
            _expanded = expanded;
            Parent = parentNode;
            Children = new List<TreeNode>();
            
            IsExpanded = _expanded?.GetExpanded(Id) ?? false;
            IsDraggable = parentNode != null;

            Name = GetItemProperty("Name") as string ?? string.Empty;
            Icon = GetItemProperty("Icon") as string ?? string.Empty;
            
            CreateChildCfgNodes();
        }

        private object? GetItemProperty(string propertyName)
        {
            if (Item == null) return null;
            var prop = Item.GetType().GetProperty(propertyName);
            return prop == null ? null : prop.GetValue(Item);
        }
        
        private void CreateChildCfgNodes()
        {
            if (!(GetItemProperty("Children") is IEnumerable<object> children)) return;
            
            if (Children.Any()) return;
            
            foreach (var child in children)
            {
                var childNode = new TreeNode(this, child, _expanded);
                childNode.CreateChildCfgNodes();
                Children.Add(childNode);
            }
        }
        
        public IEnumerable<TreeNode> AllNodes()
        {
            yield return this;
            foreach (var node in Children.SelectMany(child => child.AllNodes()))
            {
                yield return node;
            }
        }

    }
}