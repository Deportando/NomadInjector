using System.ComponentModel;
using System.IO;

namespace NomadInjector
{
    public class DLLItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string FullPath { get; set; }
        public string DisplayName => Path.GetFileName(FullPath);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public DLLItem(string path, bool isSelected = true)
        {
            FullPath = path;
            _isSelected = isSelected;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}