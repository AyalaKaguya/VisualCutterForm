using System.Windows.Forms;

namespace VisualCutterForm.FlowEditor
{
    public class InputDialog : Form
    {
        private TextBox _txt;

        public string InputText { get; private set; }

        public InputDialog(string title, string prompt, string initialValue = "")
        {
            Width = 300;
            Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Text = title;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label
            {
                Text = prompt,
                Left = 10,
                Top = 20,
                Width = 260,
            };

            _txt = new TextBox
            {
                Left = 10,
                Top = 45,
                Width = 260,
                Text = initialValue,
            };

            var btnOk = new Button
            {
                Text = "确定",
                Left = 100,
                Width = 80,
                Top = 75,
                DialogResult = DialogResult.OK,
            };
            var btnCancel = new Button
            {
                Text = "取消",
                Left = 190,
                Width = 80,
                Top = 75,
                DialogResult = DialogResult.Cancel,
            };

            Controls.AddRange(new Control[] { lbl, _txt, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        public new DialogResult ShowDialog()
        {
            var result = base.ShowDialog();
            if (result == DialogResult.OK)
                InputText = _txt.Text.Trim();
            return result;
        }
    }
}
