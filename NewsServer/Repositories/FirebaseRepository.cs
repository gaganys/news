using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace NewsServer.Repositories
{
    public class FirebaseRepository:  IFirebaseRepository
    {
        private readonly FirestoreDb _firestoreDb;
        
        public FirebaseRepository(string credentialsPath, string projectId)
        {
            var firestoreClient = new FirestoreClientBuilder
            {
                CredentialsPath = credentialsPath
            }.Build();
            
            _firestoreDb = FirestoreDb.Create(projectId, firestoreClient);
        }
        
        public async Task<NewsItem> AddNewsAsync(NewsItem item)
        {
            item.PublishDate = DateTime.SpecifyKind(item.PublishDate, DateTimeKind.Utc);
            var docRef = await _firestoreDb.Collection("news").AddAsync(item);
            item.DocumentId = docRef.Id;
            return item;
        }

        public async Task UpdateNewsAsync(NewsItem item)
        {
            try 
            {
                if (string.IsNullOrEmpty(item.DocumentId))
                    throw new ArgumentException("DocumentId is required");

                await _firestoreDb.Collection("news").Document(item.DocumentId)
                    .SetAsync(item, SetOptions.MergeAll);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firestore update error: {ex}");
                throw new RepositoryException("Update failed", ex);
            }
        }

        public async Task DeleteNewsAsync(string documentId)
        {
            await _firestoreDb.Collection("news").Document(documentId).DeleteAsync();
        }

        public async Task<List<NewsItem>> GetAllNewsAsync()
        {
            var snapshot = await _firestoreDb.Collection("news").GetSnapshotAsync();
            return snapshot.Documents.Select(d => 
            {
                var item = d.ConvertTo<NewsItem>();
                item.DocumentId = d.Id;
                return item;
            }).OrderByDescending(n => n.PublishDate).ToList();
        }

        public async Task<NewsItem> GetNewsByIdAsync(string documentId)
        {
            var snapshot = await _firestoreDb.Collection("news").Document(documentId).GetSnapshotAsync();
            if (snapshot.Exists)
            {
                var item = snapshot.ConvertTo<NewsItem>();
                item.DocumentId = snapshot.Id;
                return item;
            }
            return null;
        }
        
        public class RepositoryException : Exception
        {
            public RepositoryException(string message, Exception inner) 
                : base(message, inner) { }
        }
    }
}