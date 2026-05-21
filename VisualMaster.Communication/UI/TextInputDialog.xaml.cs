using System.Windows;

namespace VisualMaster.Communication.UI
{
    public partial class TextInputDialog : Window
    {
        public TextInputDialog(string title, string prompt, string value)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            ValueBox.Text = value ?? "";
            ValueBox.SelectAll();
            ValueBox.Focus();
        }

        public string Value => ValueBox.Text;

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
