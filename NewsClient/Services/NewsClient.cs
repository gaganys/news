using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NewsClient.Models;

namespace NewsClient.Services
{
    public class Client : IDisposable
    {
        private readonly Uri _serverUri;
        private readonly ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;
        private const int MaxReconnectAttempts = 5;
        private const int ReconnectDelayMs = 5000;

        public event Action<NewsItem> NewsPublished;
        public event Action<NewsItem> NewsUpdated;
        public event Action<string> NewsDeleted;
        public event Action<List<NewsItem>> NewsListReceived;

        public Client(string serverIp, int serverPort)
        {
            if (string.IsNullOrWhiteSpace(serverIp))
                throw new ArgumentException("IP сервер не должен быть пустым значением", nameof(serverIp));
            if (serverPort <= 0)
                throw new ArgumentOutOfRangeException(nameof(serverPort), "Порт должен быть положительным значением");

            _serverUri = new Uri($"ws://{serverIp}:{serverPort}/");
        }

        public async Task ConnectAsync()
        {
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
                return;

            var attempt = 0;
            while (attempt < MaxReconnectAttempts && !_cts.IsCancellationRequested)
            {
                try
                {
                    await _webSocket.ConnectAsync(_serverUri, _cts.Token);
                    Console.WriteLine("Подключено к WebSocket-серверу");
                    _ = ReceiveMessagesAsync();
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"Попытка подключения {attempt} провалена: {ex.Message}");

                    if (attempt >= MaxReconnectAttempts)
                    {
                        throw new Exception("Ошибка подключения к WebSocket-серверу после нескольких попыток");
                    }

                    await Task.Delay(ReconnectDelayMs, _cts.Token);
                }
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", _cts.Token);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения: {ex.Message}");
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(message);
                string type = data?.Type?.ToString();

                if (string.IsNullOrEmpty(type))
                {
                    Console.WriteLine("Получено сообщение безз типа");
                    return;
                }

                switch (type.ToLowerInvariant())
                {
                    case "publish":
                    case "update":
                        var newsItem = JsonConvert.DeserializeObject<NewsItem>(message);
                        if (type == "publish")
                            NewsPublished?.Invoke(newsItem);
                        else
                            NewsUpdated?.Invoke(newsItem);
                        break;

                    case "delete":
                        string documentId = data.DocumentId;
                        NewsDeleted?.Invoke(documentId);
                        break;

                    case "newslist":
                        var newsList = JsonConvert.DeserializeObject<List<NewsItem>>(data.News.ToString());
                        NewsListReceived?.Invoke(newsList);
                        break;

                    case "subscribed":
                        Console.WriteLine("Успешная подписка на обновление новостей");
                        break;

                    case "error":
                        Console.WriteLine($"Ошибка сервера: {data.Message}");
                        break;

                    default:
                        Console.WriteLine($"Неизвестная ошибка сообщения: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        public async Task SubscribeToNewsUpdatesAsync()
        {
            await SendMessageAsync(new { Type = "subscribe" });
        }

        public async Task GetAllNewsAsync()
        {
            await SendMessageAsync(new { Type = "getAllNews" });
        }

        public async Task PublishNewsAsync(NewsItem newsItem)
        {
            if (newsItem == null) throw new ArgumentNullException(nameof(newsItem));

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

        public async Task UpdateNewsAsync(NewsItem newsItem)
        {
            if (newsItem == null) throw new ArgumentNullException(nameof(newsItem));

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
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("DocumentId cannot be null or empty", nameof(documentId));

            await SendMessageAsync(new { Type = "delete", DocumentId = documentId });
        }

        public async Task SendPingAsync()
        {
            if (_webSocket.State != WebSocketState.Open) return;

            try
            {
                await SendMessageAsync(new { Type = "ping" });
            }
            catch
            {
                // Ignore ping errors
            }
        }

        private async Task SendMessageAsync(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (_webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket не подключен");

            try
            {
                var json = JsonConvert.SerializeObject(message);
                var buffer = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cts.Cancel();

            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Завершение работы клиента", CancellationToken.None)
                        .Wait(1000);
                }
                _webSocket.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}