using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewsClient.Models;
using Newtonsoft.Json;

namespace NewsClient.Services.Network
{
    public class NewsTcpClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        public event Action<NewsItem> NewsPublished;
        public event Action<NewsItem> NewsUpdated;
        public event Action<string> NewsDeleted;
        public event Action<List<NewsItem>> NewsListReceived;
        public event Action<string> ConnectionError;
        
        public NewsTcpClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }
        
        public async Task ConnectAsync(string ip, int port)
        {
            Console.WriteLine($"Попытка подключения к {ip}:{port}");
            
            if (_tcpClient?.Connected == true)
                return;

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);
                _stream = _tcpClient.GetStream();
                _ = ReceiveMessagesAsync();
                Console.WriteLine($"Подключено к серверу {ip}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                ConnectionError?.Invoke($"Connection failed: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            var messageBuilder = new StringBuilder();

            try
            {
                while (_tcpClient.Connected && !_cts.IsCancellationRequested)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead == 0) break;

                    var messagePart = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(messagePart);

                    if (messagePart.Contains("\n"))
                    {
                        var fullMessage = messageBuilder.ToString().Trim();
                        messageBuilder.Clear();
                        ProcessMessage(fullMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke($"Receive error: {ex.Message}");
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(message);
                string type = data?.Type?.ToString();

                if (string.IsNullOrEmpty(type)) return;

                switch (type.ToLowerInvariant())
                {
                    case "publish":
                        var publishedItem = JsonConvert.DeserializeObject<NewsItem>(message);
                        NewsPublished?.Invoke(publishedItem);
                        break;

                    case "update":
                        var updatedItem = JsonConvert.DeserializeObject<NewsItem>(message);
                        NewsUpdated?.Invoke(updatedItem);
                        break;

                    case "delete":
                        string documentId = data.DocumentId?.ToString();
                        if (!string.IsNullOrEmpty(documentId))
                            NewsDeleted?.Invoke(documentId);
                        break;

                    case "newslist":
                        var newsList = JsonConvert.DeserializeObject<List<NewsItem>>(data.News.ToString());
                        NewsListReceived?.Invoke(newsList);
                        break;

                    case "error":
                        string errorMessage = data.Message?.ToString();
                        if (!string.IsNullOrEmpty(errorMessage))
                            ConnectionError?.Invoke(errorMessage);
                        break;
                }
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke($"Message processing error: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(object message)
        {
            if (_tcpClient?.Connected != true)
                throw new InvalidOperationException("Not connected to server");

            try
            {
                var json = JsonConvert.SerializeObject(message) + "\n";
                var buffer = Encoding.UTF8.GetBytes(json);
                await _stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token);
                await _stream.FlushAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                ConnectionError?.Invoke($"Send error: {ex.Message}");
                throw;
            }
        }
        
        public async Task GetAllNewsAsync()
        {
            await SendMessageAsync(new { Type = "getAllNews" });
        }

        public async Task UpdateNewsAsync(NewsItem newsItem)
        {
            await SendMessageAsync(new
            {
                Type = "update",
                newsItem.DocumentId,
                newsItem.Title,
                newsItem.Content,
                newsItem.Category,
                PublishDate = newsItem.PublishDate.ToString("o"),
                newsItem.UserId
            });
        }

        public async Task DeleteNewsAsync(string documentId)
        {
            await SendMessageAsync(new { Type = "delete", DocumentId = documentId });
        }

        public async Task PublishNewsAsync(NewsItem newsItem)
        {
            await SendMessageAsync(new
            {
                Type = "publish",
                newsItem.DocumentId,
                newsItem.Title,
                newsItem.Content,
                newsItem.Category,
                PublishDate = newsItem.PublishDate.ToString("o"),
                newsItem.UserId
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cts.Cancel();
            _stream?.Dispose();
            _tcpClient?.Dispose();
            _disposed = true;
        }
    }
}