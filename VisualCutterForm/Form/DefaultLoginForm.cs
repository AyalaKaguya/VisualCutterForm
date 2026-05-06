using System;
using System.Drawing;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class DefaultLoginForm : Form
    {
        private TextBox _txtAdminPwd;
        private ComboBox _cmbDefaultRole;
        private Button _btnOk;
        private Button _btnCancel;
        private Label _lblError;

        public UserRole SelectedRole { get; private set; }

        public DefaultLoginForm(AppConfig config)
        {
            Text = "设置默认登录用户";
            Size = new Size(360, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            var lblAdmin = new Label
            {
                Text = "管理员密码:",
                Location = new Point(24, 24),
                AutoSize = true,
            };
            _txtAdminPwd = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(200, 24),
                UseSystemPasswordChar = true,
            };

            var lblRole = new Label
            {
                Text = "默认用户:",
                Location = new Point(24, 60),
                AutoSize = true,
            };
            _cmbDefaultRole = new ComboBox
            {
                Location = new Point(120, 56),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _cmbDefaultRole.Items.AddRange(new object[] { "不自动登录", "用户", "工程师", "管理员" });
            _cmbDefaultRole.SelectedIndex = RoleToIndex(config.DefaultRole);

            _lblError = new Label
            {
                Location = new Point(120, 88),
                AutoSize = true,
                ForeColor = Color.Red,
                Font = new Font("Microsoft YaHei", 8F),
            };

            _btnOk = new Button
            {
                Text = "确定",
                Location = new Point(120, 132),
                Size = new Size(80, 28),
            };
            _btnOk.Click += (s, e) => DoSave(config);

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(210, 132),
                Size = new Size(80, 28),
                DialogResult = DialogResult.Cancel,
            };

            Controls.Add(lblAdmin);
            Controls.Add(_txtAdminPwd);
            Controls.Add(lblRole);
            Controls.Add(_cmbDefaultRole);
            Controls.Add(_lblError);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void DoSave(AppConfig config)
        {
            if (_txtAdminPwd.Text != config.AdminPassword)
            {
                _lblError.Text = "管理员密码错误";
                return;
            }

            SelectedRole = IndexToRole(_cmbDefaultRole.SelectedIndex);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static int RoleToIndex(UserRole role)
        {
            switch (role)
            {
                case UserRole.Operator: return 1;
                case UserRole.Engineer: return 2;
                case UserRole.Admin: return 3;
                default: return 0;
            }
        }

        private static UserRole IndexToRole(int index)
        {
            switch (index)
            {
                case 1: return UserRole.Operator;
                case 2: return UserRole.Engineer;
                case 3: return UserRole.Admin;
                default: return UserRole.None;
            }
        }
    }
}
