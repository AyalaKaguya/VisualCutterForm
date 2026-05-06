using System;
using System.Drawing;
using System.Windows.Forms;
using VisualCutterForm.Lib;

namespace VisualCutterForm
{
    public class LoginForm : Form
    {
        private ComboBox _cmbRole;
        private TextBox _txtPassword;
        private Button _btnLogin;
        private Button _btnCancel;
        private Label _lblError;

        public UserRole SelectedRole { get; private set; } = UserRole.None;

        public LoginForm(AppConfig config)
        {
            Text = "登录";
            Size = new Size(320, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei", 9F);

            var lblRole = new Label
            {
                Text = "角色:",
                Location = new Point(24, 24),
                AutoSize = true,
            };
            _cmbRole = new ComboBox
            {
                Location = new Point(80, 20),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _cmbRole.Items.AddRange(new object[] { "管理员", "工程师", "用户" });
            _cmbRole.SelectedIndex = 2;

            var lblPwd = new Label
            {
                Text = "密码:",
                Location = new Point(24, 60),
                AutoSize = true,
            };
            _txtPassword = new TextBox
            {
                Location = new Point(80, 56),
                Size = new Size(200, 24),
                UseSystemPasswordChar = true,
            };
            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) DoLogin(config);
            };

            _lblError = new Label
            {
                Location = new Point(80, 88),
                AutoSize = true,
                ForeColor = Color.Red,
                Font = new Font("Microsoft YaHei", 8F),
            };

            _btnLogin = new Button
            {
                Text = "登录",
                Location = new Point(120, 120),
                Size = new Size(80, 28),
            };
            _btnLogin.Click += (s, e) => DoLogin(config);

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(210, 120),
                Size = new Size(80, 28),
                DialogResult = DialogResult.Cancel,
            };

            Controls.Add(lblRole);
            Controls.Add(_cmbRole);
            Controls.Add(lblPwd);
            Controls.Add(_txtPassword);
            Controls.Add(_lblError);
            Controls.Add(_btnLogin);
            Controls.Add(_btnCancel);

            AcceptButton = _btnLogin;
            CancelButton = _btnCancel;
        }

        private void DoLogin(AppConfig config)
        {
            var password = _txtPassword.Text;
            var role = config.VerifyPassword(password);
            UserRole expectedRole;
            switch (_cmbRole.SelectedIndex)
            {
                case 0: expectedRole = UserRole.Admin; break;
                case 1: expectedRole = UserRole.Engineer; break;
                case 2: expectedRole = UserRole.Operator; break;
                default: expectedRole = UserRole.None; break;
            }

            if (role == UserRole.None || role < expectedRole)
            {
                _lblError.Text = "密码错误或权限不足";
                return;
            }

            SelectedRole = expectedRole;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
