using System.Collections.Generic;
using System.Threading.Tasks;
using NewsClient.Models;

namespace NewsClient.Services
{
    public interface INewsService
    {
        Task PublishNewsAsync(NewsItem item);
        Task UpdateNewsAsync(NewsItem item);
        Task DeleteNewsAsync(string documentId);
        Task<List<NewsItem>> GetAllNewsAsync();
    }
}