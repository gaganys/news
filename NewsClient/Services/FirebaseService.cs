using Google.Cloud.Firestore;
using NewsClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore.V1;

namespace NewsClient.Services
{
    public class FirebaseService: IDisposable
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly Client _client;
        private bool _disposed;

        public event Action<NewsItem> NewsPublished;
        public event Action<NewsItem> NewsUpdated;

        public FirebaseService(string projectId)
        {
            var firestoreClient = new FirestoreClientBuilder
            {
                CredentialsPath = "D:\\КурсоваяКСиС\\NewsDistributionSystem\\NewsClient\\credentials\\newsdistributionsystem-firebase-adminsdk-fbsvc-f09dc1102d.json"
            }.Build();

            _firestoreDb = FirestoreDb.Create(projectId, firestoreClient);
            _client = new Client("localhost", 8080);
            _ = _client.ConnectAsync();

        }

        public async Task<NewsItem> AddNewsItemAsync(NewsItem item)
        {
            item.PublishDate = DateTime.SpecifyKind(item.PublishDate, DateTimeKind.Utc);
            var docRef = await _firestoreDb.Collection("news").AddAsync(item);
            item.DocumentId = docRef.Id;
            await _client.PublishNewsAsync(item);

            NewsPublished?.Invoke(item);
            return item;
        }

        public async Task<List<NewsItem>> GetAllNewsAsync()
        {
            CollectionReference newsCollection = _firestoreDb.Collection("news");
            QuerySnapshot snapshot = await newsCollection.GetSnapshotAsync();
            return snapshot.Documents.Select(d =>
            {
                var item = d.ConvertTo<NewsItem>();
                item.DocumentId = d.Id;
                return item;
            }).OrderByDescending(n => n.PublishDate).ToList();
        }
        
        public async Task UpdateNewsAsync(NewsItem newsItem)
        {
            await _firestoreDb.Collection("news").Document(newsItem.DocumentId).SetAsync(newsItem);
            await _client.UpdateNewsAsync(newsItem);

            NewsUpdated?.Invoke(newsItem);
        }

        public async Task DeleteNewsAsync(string documentId)
        {
            if (string.IsNullOrEmpty(documentId))
                throw new ArgumentException("DocumentId is required for delete");

            await _firestoreDb.Collection("news").Document(documentId).DeleteAsync();
            await PublishDelete(documentId); // Notify clients of deletion
        }
        
        private async Task PublishDelete(string documentId)
        {
            try
            {
                await _client.DeleteNewsAsync(documentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при публикации удаления: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Освобождаем управляемые ресурсы
                _client?.Dispose();
            }

            _disposed = true;
        }

        ~FirebaseService()
        {
            Dispose(false);
        }
    }
}