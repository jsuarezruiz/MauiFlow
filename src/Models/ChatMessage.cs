using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiFlow.Models
{
    /// <summary>
    /// Represents a chat message in the application. 
    /// </summary>
    public class ChatMessage : INotifyPropertyChanged
    {
        bool _sender;
        string _content = string.Empty;

        public bool Sender
        {
            get => _sender;
            set
            {
                _sender = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}