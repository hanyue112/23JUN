﻿using System.Collections.Generic;

namespace Tree
{
    public class TreeNode
    {
        private readonly TreeNode parent;
        private readonly List<TreeNode> children = new List<TreeNode>();
        public int data { get; set; }

        public TreeNode(TreeNode Parent, int Data)
        {
            parent = Parent;
            parent?.AddChild(this);
            data = Data;
        }

        private void AddChild(TreeNode n)
        {
            children.Add(n);
        }

        public int Sum()
        {
            int _subTotal = 0;
            if (children.Count == 0)
            {
                return data;
            }
            else
            {
                foreach (TreeNode n in children)
                {
                    _subTotal += n.Sum();
                }
                return _subTotal + data;
            }
        }
    }
}
