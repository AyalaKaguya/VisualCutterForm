using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VisualMaster.CameraLink.UI.ViewModels
{
    /// <summary>MVVM ViewModel 基类，实现 INotifyPropertyChanged。</summary>
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
