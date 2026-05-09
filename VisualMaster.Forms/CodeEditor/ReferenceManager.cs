using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VisualMaster.Forms.CodeEditor
{
    public class ReferenceManager : Form
    {
        private ListBox _lstDll;
        private ListBox _lstNuGet;
        private TextBox _txtNuGetAdd;
        private Button _btnAddDll;
        private Button _btnRemoveDll;
        private Button _btnAddNuGet;
        private Button _btnRemoveNuGet;
        private Button _btnOk;
        private Button _btnCancel;

        public List<string> DllReferences { get; private set; }
        public List<string> NuGetPackages { get; private set; }

        public ReferenceManager(List<string> dllRefs, List<string> nugetPkgs)
        {
            Text = "管理外部引用";
            Size = new Size(600, 450);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = Color.FromArgb(45, 45, 45);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
            };

            // DLL tab
            var pageDll = new TabPage("DLL 引用");
            pageDll.BackColor = Color.FromArgb(45, 45, 45);

            var dllTop = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(4) };
            _btnAddDll = new Button { Text = "添加...", Size = new Size(60, 24), Location = new Point(4, 3), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8F) };
            _btnAddDll.FlatAppearance.BorderSize = 0;
            _btnAddDll.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "DLL 文件 (*.dll)|*.dll", Multiselect = true })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        foreach (var f in dlg.FileNames)
                            if (!_lstDll.Items.Contains(f))
                                _lstDll.Items.Add(f);
                    }
                }
            };
            _btnRemoveDll = new Button { Text = "删除", Size = new Size(60, 24), Location = new Point(68, 3), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8F) };
            _btnRemoveDll.FlatAppearance.BorderSize = 0;
            _btnRemoveDll.Click += (s, e) =>
            {
                if (_lstDll.SelectedItem != null) _lstDll.Items.Remove(_lstDll.SelectedItem);
            };
            dllTop.Controls.Add(_btnAddDll);
            dllTop.Controls.Add(_btnRemoveDll);

            _lstDll = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.FromArgb(200, 200, 200), BorderStyle = BorderStyle.None };
            foreach (var d in dllRefs) _lstDll.Items.Add(d);

            pageDll.Controls.Add(_lstDll);
            pageDll.Controls.Add(dllTop);

            // NuGet tab
            var pageNuGet = new TabPage("NuGet 包");
            pageNuGet.BackColor = Color.FromArgb(45, 45, 45);

            var nugetTop = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(4) };
            _txtNuGetAdd = new TextBox { Location = new Point(4, 4), Size = new Size(280, 24), BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.FromArgb(200, 200, 200), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9F) };
            _btnAddNuGet = new Button { Text = "添加", Size = new Size(60, 24), Location = new Point(288, 4), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8F) };
            _btnAddNuGet.FlatAppearance.BorderSize = 0;
            _btnAddNuGet.Click += (s, e) =>
            {
                var id = _txtNuGetAdd.Text.Trim();
                if (!string.IsNullOrEmpty(id) && !_lstNuGet.Items.Contains(id))
                    _lstNuGet.Items.Add(id);
                _txtNuGetAdd.Clear();
            };
            _btnRemoveNuGet = new Button { Text = "删除", Size = new Size(60, 24), Location = new Point(4, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(60, 60, 60), Font = new Font("Microsoft YaHei", 8F) };
            _btnRemoveNuGet.FlatAppearance.BorderSize = 0;
            _btnRemoveNuGet.Click += (s, e) =>
            {
                if (_lstNuGet.SelectedItem != null) _lstNuGet.Items.Remove(_lstNuGet.SelectedItem);
            };
            nugetTop.Controls.Add(_txtNuGetAdd);
            nugetTop.Controls.Add(_btnAddNuGet);
            nugetTop.Controls.Add(_btnRemoveNuGet);

            _lstNuGet = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.FromArgb(200, 200, 200), BorderStyle = BorderStyle.None };
            foreach (var n in nugetPkgs) _lstNuGet.Items.Add(n);

            pageNuGet.Controls.Add(_lstNuGet);
            pageNuGet.Controls.Add(nugetTop);

            tabs.TabPages.Add(pageDll);
            tabs.TabPages.Add(pageNuGet);

            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(50, 50, 50) };
            _btnOk = new Button { Text = "确定", Location = new Point(400, 8), Size = new Size(80, 26), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(70, 70, 70) };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) => { DllReferences = _lstDll.Items.Cast<string>().ToList(); NuGetPackages = _lstNuGet.Items.Cast<string>().ToList(); DialogResult = DialogResult.OK; Close(); };
            _btnCancel = new Button { Text = "取消", Location = new Point(490, 8), Size = new Size(80, 26), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(70, 70, 70), DialogResult = DialogResult.Cancel };
            _btnCancel.FlatAppearance.BorderSize = 0;
            bottomBar.Controls.Add(_btnOk);
            bottomBar.Controls.Add(_btnCancel);

            Controls.Add(tabs);
            Controls.Add(bottomBar);
        }
    }
}
