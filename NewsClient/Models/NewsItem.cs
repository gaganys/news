using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NewsClient.Models
{
    public class NewsItem : INotifyPropertyChanged
    {
        private string _title;
        private string _content;
        private string _category;
        private DateTime _publishDate;
        private string _userId;
        private string _documentId;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public DateTime PublishDate
        {
            get => _publishDate;
            set { _publishDate = value; OnPropertyChanged(); }
        }

        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public string DocumentId
        {
            get => _documentId;
            set { _documentId = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}