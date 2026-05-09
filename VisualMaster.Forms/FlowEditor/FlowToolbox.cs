using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VisualMaster.WorkFlow;

namespace VisualMaster.Forms.FlowEditor
{
    public class FlowToolbox : UserControl
    {
        private TreeView _treeView;

        public event Action<Type> NodeTypeSelected;

        public FlowToolbox()
        {
            BackColor = Color.FromArgb(50, 50, 50);
            ForeColor = Color.White;

            var header = new Label
            {
                Text = "节点工具箱",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Microsoft YaHei", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
            };

            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9F),
                HideSelection = false,
            };
            _treeView.NodeMouseDoubleClick += OnNodeDoubleClicked;

            Controls.Add(_treeView);
            Controls.Add(header);

            RefreshToolbox();
        }

        public void RefreshToolbox()
        {
            _treeView.Nodes.Clear();

            var types = NodeFactory.GetAvailableNodeTypes();
            var grouped = types.GroupBy(t => t.Category);

            foreach (var group in grouped)
            {
                var catNode = new TreeNode(group.Key, 0, 0);
                foreach (var t in group)
                {
                    catNode.Nodes.Add(new TreeNode(t.DisplayName, 1, 1)
                    {
                        Tag = t.NodeType,
                    });
                }
                _treeView.Nodes.Add(catNode);
            }

            foreach (TreeNode catNode in _treeView.Nodes)
                catNode.Expand();
        }

        private void OnNodeDoubleClicked(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is Type nodeType)
            {
                NodeTypeSelected?.Invoke(nodeType);
            }
        }
    }
}
