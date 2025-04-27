using System;
using Google.Cloud.Firestore;

namespace NewsServer
{
    [FirestoreData]
    public class NewsItem
    {
        [FirestoreProperty]
        public string DocumentId { get; set; }
        
        [FirestoreProperty]
        public string Title { get; set; }
        
        [FirestoreProperty]
        public string Content { get; set; }
        
        [FirestoreProperty]
        public DateTime PublishDate { get; set; }
        
        [FirestoreProperty]
        public string Category { get; set; }
        
        [FirestoreProperty]
        public string UserId { get; set; }
    }
}