using System.Collections.Generic;
using System.Threading.Tasks;
using NewsClient.Models;
using NewsClient.Services.Network;

namespace NewsClient.Services
{
    public class NewsService: INewsService
    {
        private readonly NewsTcpClient _tcpClient;

        public NewsService(NewsTcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public async Task PublishNewsAsync(NewsItem item)
        {
            await _tcpClient.SendMessageAsync(new
            {
                Type = "publish",
                item.DocumentId,
                item.Title,
                item.Content,
                item.Category,
                PublishDate = item.PublishDate.ToString("o"),
                item.UserId
            });
        }

        public async Task UpdateNewsAsync(NewsItem item)
        {
            await _tcpClient.SendMessageAsync(new
            {
                Type = "update",
                item.DocumentId,
                item.Title,
                item.Content,
                item.Category,
                PublishDate = item.PublishDate.ToString("o"),
                item.UserId
            });
        }

        public async Task DeleteNewsAsync(string documentId)
        {
            await _tcpClient.SendMessageAsync(new
            {
                Type = "delete",
                DocumentId = documentId
            });
        }

        public async Task<List<NewsItem>> GetAllNewsAsync()
        {
            await _tcpClient.SendMessageAsync(new { Type = "getAllNews" });
            return null; // Результат придет через событие NewsListReceived
        }
    }
}