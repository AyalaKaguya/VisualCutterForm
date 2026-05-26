using System.Drawing;
using System.Windows.Forms;

namespace VisualMaster.Forms
{
    public static class DarkTheme
    {
        public static readonly Color Bg = Color.FromArgb(40, 40, 40);
        public static readonly Color Panel = Color.FromArgb(50, 50, 50);
        public static readonly Color PanelLight = Color.FromArgb(60, 60, 60);
        public static readonly Color ControlBg = Color.FromArgb(70, 70, 70);
        public static readonly Color HeaderBg = Color.FromArgb(45, 45, 45);
        public static readonly Color Text = Color.FromArgb(220, 220, 220);
        public static readonly Color TextDim = Color.FromArgb(180, 180, 180);
        public static readonly Color Accent = Color.FromArgb(46, 204, 113);
        public static readonly Color Error = Color.FromArgb(231, 76, 60);
        public static readonly Color Warning = Color.FromArgb(241, 196, 15);

        public static readonly Font DefaultFont = new Font("Microsoft YaHei", 9F);

        public static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = HeaderBg,
                ForeColor = Text,
                Font = DefaultFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
            };
        }

        public static Button CreateButton(string text)
        {
            return new Button
            {
                Text = text,
                BackColor = ControlBg,
                ForeColor = Text,
                Font = DefaultFont,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 28),
            };
        }

        public static ComboBox CreateComboBox()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ControlBg,
                ForeColor = Text,
                Font = DefaultFont,
            };
        }

        public static TextBox CreateTextBox()
        {
            return new TextBox
            {
                BackColor = ControlBg,
                ForeColor = Text,
                Font = DefaultFont,
                BorderStyle = BorderStyle.FixedSingle,
            };
        }

        public static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = TextDim,
                Font = DefaultFont,
                AutoSize = true,
            };
        }
    }
}
