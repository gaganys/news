using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsServer.Repositories
{
    public interface IFirebaseRepository
    {
        Task<NewsItem> AddNewsAsync(NewsItem item);
        Task UpdateNewsAsync(NewsItem item);
        Task DeleteNewsAsync(string documentId);
        Task<List<NewsItem>> GetAllNewsAsync();
        Task<NewsItem> GetNewsByIdAsync(string documentId);
    }
}