using VisualMaster.CameraLink.UI.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VisualMaster.CameraLink.UI
{
    public partial class CameraManagerPanel : UserControl
    {
        public CameraManagerPanel()
        {
            InitializeComponent();
        }

        public CameraManagerPanel(CameraManagerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void OnCameraListRightClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;

            var item = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
            if (item != null)
            {
                item.IsSelected = true;
            }
            else if (listBox.SelectedItem == null)
            {
                return;
            }

            var menu = new ContextMenu();
            var rename = new MenuItem { Header = "重命名" };
            rename.Click += OnRenameCamera;
            menu.Items.Add(rename);

            var delete = new MenuItem { Header = "删除" };
            delete.Click += OnDeleteCamera;
            menu.Items.Add(delete);

            listBox.ContextMenu = menu;
        }

        private void OnRenameCamera(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as CameraManagerViewModel;
            if (vm?.SelectedCamera == null) return;

            var dialog = new RenameDialog("重命名相机", vm.SelectedCamera.DisplayName)
            {
                Owner = Window.GetWindow(this),
            };
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.NewName)) return;

            vm.RenameSelectedCamera(dialog.NewName.Trim());
        }

        private void OnDeleteCamera(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as CameraManagerViewModel;
            vm?.RemoveCameraCommand.Execute(null);
        }

        private static T FindParent<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T typed) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void OnRoiNumUp(object sender, RoutedEventArgs e)
        {
            AdjustRoiNum(sender, 1);
        }

        private void OnRoiNumDown(object sender, RoutedEventArgs e)
        {
            AdjustRoiNum(sender, -1);
        }

        private static void AdjustRoiNum(object sender, int delta)
        {
            var btn = sender as FrameworkElement;
            if (btn == null) return;

            var parentGrid = FindParent<Grid>(btn);
            if (parentGrid == null) return;

            var textBox = parentGrid.Children.OfType<TextBox>().FirstOrDefault();
            if (textBox == null) return;

            if (int.TryParse(textBox.Text, out int v))
            {
                v = Math.Max(0, v + delta);
                textBox.Text = v.ToString();
            }
        }
    }
}
