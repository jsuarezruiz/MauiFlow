using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiFlow.Models
{
    /// <summary>
    /// Represents a saved app in the history with all its associated data.
    /// </summary>
    public class AppHistoryItem : INotifyPropertyChanged
    {
        string _appName = string.Empty;
        DateTime _dateCreated;
        DateTime _lastModified;
        ObservableCollection<ChatMessage> _chatHistory = new();
        string _xamlCode = string.Empty;
        string _csharpCode = string.Empty;
        string _description = string.Empty;
        bool _isSelected;

        /// <summary>
        /// Gets or sets the unique identifier for this app.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the name of the app.
        /// </summary>
        public string AppName
        {
            get => _appName;
            set
            {
                _appName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the date when the app was created.
        /// </summary>
        public DateTime DateCreated
        {
            get => _dateCreated;
            set
            {
                _dateCreated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DateCreatedFormatted));
            }
        }

        /// <summary>
        /// Gets or sets the date when the app was last modified.
        /// </summary>
        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LastModifiedFormatted));
            }
        }

        /// <summary>
        /// Gets or sets the chat history associated with this app.
        /// </summary>
        public ObservableCollection<ChatMessage> ChatHistory
        {
            get => _chatHistory;
            set
            {
                _chatHistory = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the generated XAML code for this app.
        /// </summary>
        public string XamlCode
        {
            get => _xamlCode;
            set
            {
                _xamlCode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the generated C# code for this app.
        /// </summary>
        public string CsharpCode
        {
            get => _csharpCode;
            set
            {
                _csharpCode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the description or initial prompt for this app.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether this app is currently selected in the history list.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a formatted string representation of the creation date.
        /// </summary>
        public string DateCreatedFormatted => DateCreated.ToString("MMM dd, yyyy");

        /// <summary>
        /// Gets a formatted string representation of the last modified date.
        /// </summary>
        public string LastModifiedFormatted => LastModified.ToString("MMM dd, yyyy HH:mm");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}