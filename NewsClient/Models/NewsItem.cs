using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore;

namespace NewsClient.Models
{
    [FirestoreData]
    public class NewsItem: INotifyPropertyChanged
    {
        private string _title;
        private string _content;
        private string _category;
        private DateTime _publishDate;
        private string _userId;
        private string _documentId;

        [FirestoreProperty]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        [FirestoreProperty]
        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        [FirestoreProperty]
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        [FirestoreProperty]
        public DateTime PublishDate
        {
            get => _publishDate;
            set { _publishDate = value; OnPropertyChanged(); }
        }

        [FirestoreProperty]
        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        [FirestoreProperty]
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
