using Google.Cloud.Firestore;
using NewsClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore.V1;
using NewsClient.Utility;

namespace NewsClient.Services
{
    public class FirebaseService: IDisposable
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly Client _client;
        private bool _disposed;

        public event Action<NewsItem> NewsPublished;
        public event Action<NewsItem> NewsUpdated;
        public event Action<string> NewsDeleted;

        public FirebaseService(string projectId, Client client)
        {
            var firestoreClient = new FirestoreClientBuilder
            {
                CredentialsPath = AppConstants.CredentialsPath
            }.Build();

            _firestoreDb = FirestoreDb.Create(projectId, firestoreClient);
            _client = client;

            // Подписка на события WebSocket
            _client.NewsPublished += OnNewsPublished;
            _client.NewsUpdated += OnNewsUpdated;
            _client.NewsDeleted += OnNewsDeleted;
        }
        
        private void OnNewsPublished(NewsItem news) => NewsPublished?.Invoke(news);
        private void OnNewsUpdated(NewsItem news) => NewsUpdated?.Invoke(news);
        private void OnNewsDeleted(string docId) => NewsDeleted?.Invoke(docId);

        public async Task<NewsItem> AddNewsItemAsync(NewsItem item)
        {
            item.PublishDate = DateTime.SpecifyKind(item.PublishDate, DateTimeKind.Utc);
            var docRef = await _firestoreDb.Collection("news").AddAsync(item);
            item.DocumentId = docRef.Id;
            NewsPublished?.Invoke(item);
            await _client.PublishNewsAsync(item);

            return item;
        }
        
        public async Task UpdateNewsAsync(NewsItem newsItem)
        {
            await _firestoreDb.Collection("news").Document(newsItem.DocumentId).SetAsync(newsItem);
            NewsUpdated?.Invoke(newsItem);
            await _client.UpdateNewsAsync(newsItem);
        }

        public async Task DeleteNewsAsync(string documentId)
        {
            if (string.IsNullOrEmpty(documentId))
                throw new ArgumentException("DocumentId is required for delete");

            await _firestoreDb.Collection("news").Document(documentId).DeleteAsync();
            NewsDeleted?.Invoke(documentId);
            await _client.DeleteNewsAsync(documentId);
        }

        public async Task<List<NewsItem>> GetAllNewsAsync()
        {
            try
            {
                Console.WriteLine("Запрос новостей из Firestore...");
                CollectionReference newsCollection = _firestoreDb.Collection("news");
                QuerySnapshot snapshot = await newsCollection.GetSnapshotAsync();

                if (snapshot == null)
                {
                    Console.WriteLine("Snapshot is null");
                    return new List<NewsItem>();
                }

                var result = snapshot.Documents.Select(d =>
                {
                    try
                    {
                        var item = d.ConvertTo<NewsItem>();
                        item.DocumentId = d.Id;
                        Console.WriteLine($"Загружена новость: {item.Title} (ID: {item.DocumentId})");
                        return item;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка конвертации документа {d.Id}: {ex.Message}");
                        return null;
                    }
                }).Where(x => x != null).ToList();

                Console.WriteLine($"Всего загружено: {result.Count} новостей");
                return result.OrderByDescending(n => n.PublishDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetAllNewsAsync: {ex.ToString()}");
                throw;
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