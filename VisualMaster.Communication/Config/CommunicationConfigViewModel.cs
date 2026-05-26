using System;
using System.Windows.Input;
using VisualMaster.Config.Abstractions;
using VisualMaster.Communication.UI.ViewModels;

namespace VisualMaster.Communication.Config
{
    public sealed class CommunicationConfigViewModel : NotifyBase
    {
        private readonly ConfigSession<CommunicationConfigSection> _session;
        private readonly Action<CommunicationConfigSection> _onConfigChanged;
        private string _statusMessage;

        public CommunicationConfigSection Section => _session.Current;

        public bool IsDirty => _session.HasChanges;

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand RevertCommand { get; }

        public CommunicationConfigViewModel(
            ConfigSession<CommunicationConfigSection> session,
            Action<CommunicationConfigSection> onConfigChanged)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _onConfigChanged = onConfigChanged;

            SaveCommand = new RelayCommand(ExecuteSave);
            RevertCommand = new RelayCommand(ExecuteRevert);
        }

        private void ExecuteSave()
        {
            try
            {
                _session.Commit();
                _onConfigChanged?.Invoke((CommunicationConfigSection)_session.Current.Clone());
                StatusMessage = "配置已保存。";
                OnPropertyChanged(nameof(IsDirty));
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败：{ex.Message}";
            }
        }

        private void ExecuteRevert()
        {
            if (!_session.HasChanges)
            {
                StatusMessage = "没有可还原的更改。";
                return;
            }
            _session.Revert();
            _onConfigChanged?.Invoke((CommunicationConfigSection)_session.Current.Clone());
            StatusMessage = "已还原到上次保存的配置。";
            OnPropertyChanged(nameof(Section));
            OnPropertyChanged(nameof(IsDirty));
        }

        public void NotifyConfigChanged()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(Section));
        }
    }
}
