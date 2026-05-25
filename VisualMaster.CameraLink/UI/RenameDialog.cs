using System.Windows;
using System.Windows.Controls;

namespace VisualMaster.CameraLink.UI
{
    public partial class RenameDialog : Window
    {
        public string NewName { get; private set; }

        public RenameDialog(string title, string currentName)
        {
            Title = title;
            Width = 340;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.White;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock { Text = "名称:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            var textBox = new TextBox { Text = currentName ?? "", Margin = new Thickness(0, 0, 0, 8) };
            textBox.SelectAll();
            textBox.Focus();
            Grid.SetRow(textBox, 0);
            Grid.SetColumn(textBox, 1);
            grid.Children.Add(textBox);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(panel, 1);
            Grid.SetColumnSpan(panel, 2);
            grid.Children.Add(panel);

            var ok = new Button { Content = "确定", Width = 72, Height = 28, Margin = new Thickness(0, 0, 6, 0) };
            ok.Click += (s, e) =>
            {
                NewName = textBox.Text;
                DialogResult = true;
                Close();
            };
            panel.Children.Add(ok);

            var cancel = new Button { Content = "取消", Width = 72, Height = 28 };
            cancel.Click += (s, e) => { DialogResult = false; Close(); };
            panel.Children.Add(cancel);

            Content = grid;
        }
    }
}
