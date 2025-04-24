using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewsServer
{
    public class Server : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new ConcurrentDictionary<string, ClientInfo>();
        private readonly ConcurrentDictionary<string, NewsItem> _newsArchive = new ConcurrentDictionary<string, NewsItem>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SemaphoreSlim _broadcastSemaphore = new SemaphoreSlim(10);
        private bool _disposed;

        public Server(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip)) throw new ArgumentException("IP не может быть пустым значением", nameof(ip));
            if (port <= 0) throw new ArgumentOutOfRangeException(nameof(port), "Порт должен быть положительным значением");

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{ip}:{port}/");
        }

        public async Task StartAsync()
        {
            try
            {
                _listener.Start();
                Console.WriteLine($"Server started at {_listener.Prefixes.First()}");

                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync().ConfigureAwait(false);
                        if (context.Request.IsWebSocketRequest)
                        {
                            _ = HandleWebSocketConnectionAsync(context);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch (Exception ex) when (ex is HttpListenerException || ex is OperationCanceledException)
                    {
                        if (!_cts.IsCancellationRequested)
                            Console.WriteLine($"Server error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected server error: {ex}");
                    }
                }
            }
            finally
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await Task.WhenAll(_clients.Values.Select(c => CloseClientAsync(c)));
        }

        private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
        {
            var connectionId = Guid.NewGuid().ToString();
            WebSocket webSocket = null;

            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                webSocket = webSocketContext.WebSocket;


                var clientInfo = new ClientInfo
                {
                    WebSocket = webSocket,
                    ConnectionId = connectionId
                };

                if (!_clients.TryAdd(connectionId, clientInfo))
                {
                    throw new InvalidOperationException("Failed to add client to dictionary");
                }

                Console.WriteLine($"Client connected: {connectionId}");
                await ReceiveMessagesAsync(clientInfo, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error for {connectionId}: {ex.Message}");
            }
            finally
            {
                if (webSocket != null)
                {
                    _clients.TryRemove(connectionId, out _);
                    await CloseClientAsync(new ClientInfo { WebSocket = webSocket, ConnectionId = connectionId });
                    Console.WriteLine($"Client disconnected: {connectionId}");
                }
            }
        }

        private async Task CloseClientAsync(ClientInfo clientInfo)
        {
            try
            {
                if (clientInfo.WebSocket?.State == WebSocketState.Open)
                {
                    await clientInfo.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server shutdown",
                        CancellationToken.None);
                }
                clientInfo.WebSocket?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing client {clientInfo.ConnectionId}: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync(ClientInfo clientInfo, CancellationToken ct)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (clientInfo.WebSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await clientInfo.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _ = ProcessClientMessageAsync(message, clientInfo);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await clientInfo.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
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
                Console.WriteLine($"Receive error for {clientInfo.ConnectionId}: {ex.Message}");
            }
        }

        private async Task ProcessClientMessageAsync(string message, ClientInfo clientInfo)
        {
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine($"Empty message from {clientInfo.ConnectionId}");
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<dynamic>(message);
                string type = data?.Type?.ToString();

                if (string.IsNullOrEmpty(type))
                {
                    Console.WriteLine($"Invalid message format from {clientInfo.ConnectionId}");
                    return;
                }

                Console.WriteLine($"Processing {type} from {clientInfo.ConnectionId}");

                switch (type.ToLowerInvariant())
                {
                    case "auth":
                        clientInfo.UserId = data.UserId?.ToString();
                        Console.WriteLine($"Client {clientInfo.ConnectionId} authenticated as {clientInfo.UserId}");
                        break;

                    case "publish":
                        await HandlePublishAsync(data, clientInfo);
                        break;

                    case "update":
                        await HandleUpdateAsync(data, clientInfo);
                        break;

                    case "delete":
                        await HandleDeleteAsync(data, clientInfo);
                        break;

                    case "subscribe":
                        await SendToClientAsync(clientInfo, new { Type = "subscribed" });
                        await SendNewsListAsync(clientInfo);
                        break;

                    case "getallnews":
                        await SendNewsListAsync(clientInfo);
                        break;

                    case "ping":
                        await SendToClientAsync(clientInfo, new { Type = "pong" });
                        break;

                    default:
                        Console.WriteLine($"Unknown message type '{type}' from {clientInfo.ConnectionId}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message from {clientInfo.ConnectionId}: {ex.Message}");
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Internal server error" });
            }
        }

        private async Task HandlePublishAsync(dynamic data, ClientInfo clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo.UserId))
            {
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Authentication required" });
                return;
            }

            try
            {
                var newsItem = new NewsItem
                {
                    DocumentId = data.DocumentId?.ToString() ?? Guid.NewGuid().ToString(),
                    Title = data.Title?.ToString() ?? string.Empty,
                    Content = data.Content?.ToString() ?? string.Empty,
                    Category = data.Category?.ToString() ?? string.Empty,
                    PublishDate = DateTime.TryParse(data.PublishDate?.ToString(), out DateTime date) ? date : DateTime.UtcNow,
                    UserId = clientInfo.UserId
                };

                if (!_newsArchive.TryAdd(newsItem.DocumentId, newsItem))
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "News item already exists" });
                    return;
                }

                await BroadcastAsync(new
                {
                    Type = "news",
                    Action = "publish",
                    newsItem.DocumentId,
                    newsItem.Title,
                    newsItem.Content,
                    newsItem.Category,
                    PublishDate = newsItem.PublishDate.ToString("o"),
                    newsItem.UserId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Publish error: {ex}");
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Publish failed" });
            }
        }

        private async Task HandleUpdateAsync(dynamic data, ClientInfo clientInfo)
        {
            if (string.IsNullOrEmpty(clientInfo.UserId))
            {
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Authentication required" });
                return;
            }

            try
            {
                var documentId = data.DocumentId?.ToString();
                if (string.IsNullOrEmpty(documentId))
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "Invalid DocumentId" });
                    return;
                }

                if (!_newsArchive.TryGetValue(documentId, out NewsItem existing) || existing.UserId != clientInfo.UserId)
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "News not found or unauthorized" });
                    return;
                }

                var updatedItem = new NewsItem
                {
                    DocumentId = documentId,
                    Title = data.Title?.ToString() ?? existing.Title,
                    Content = data.Content?.ToString() ?? existing.Content,
                    Category = data.Category?.ToString() ?? existing.Category,
                    PublishDate = DateTime.TryParse(data.PublishDate?.ToString(), out DateTime date) ? date : existing.PublishDate,
                    UserId = clientInfo.UserId
                };

                _newsArchive[documentId] = updatedItem;

                await BroadcastAsync(new
                {
                    Type = "update",
                    updatedItem.DocumentId,
                    updatedItem.Title,
                    updatedItem.Content,
                    updatedItem.Category,
                    PublishDate = updatedItem.PublishDate.ToString("o"),
                    updatedItem.UserId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex}");
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Update failed" });
            }
        }

        private async Task HandleDeleteAsync(dynamic data, ClientInfo clientInfo)
        {
            try
            {
                var documentId = data.DocumentId?.ToString();
                if (string.IsNullOrEmpty(documentId))
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "Invalid DocumentId" });
                    return;
                }

                if (_newsArchive.TryRemove(documentId, out NewsItem _))
                {
                    await BroadcastAsync(new { Type = "delete", DocumentId = documentId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex}");
                await SendToClientAsync(clientInfo, new { Type = "error", Message = "Delete failed" });
            }
        }

        private async Task SendNewsListAsync(ClientInfo clientInfo)
        {
            try
            {
                var newsList = _newsArchive.Values
                    .OrderByDescending(n => n.PublishDate)
                    .Select(n => new
                    {
                        n.DocumentId,
                        n.Title,
                        n.Content,
                        n.Category,
                        PublishDate = n.PublishDate.ToString("o"),
                        n.UserId
                    })
                    .ToList();

                await SendToClientAsync(clientInfo, new { Type = "newsList", News = newsList });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendNewsList error: {ex}");
            }
        }

        private async Task BroadcastAsync(object message, string excludeConnectionId = null)
        {
            if (message == null) return;

            await _broadcastSemaphore.WaitAsync();
            try
            {
                var settings = new JsonSerializerSettings { DateFormatString = "o" };
                var json = JsonConvert.SerializeObject(message, settings);
                var buffer = Encoding.UTF8.GetBytes(json);

                var tasks = _clients.Values
                    .Where(c => c.WebSocket?.State == WebSocketState.Open && c.ConnectionId != excludeConnectionId)
                    .Select(c => SendToClientAsync(c, buffer));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Broadcast failed: {ex}");
            }
            finally
            {
                _broadcastSemaphore.Release();
            }
        }

        private async Task SendToClientAsync(ClientInfo client, object message)
        {
            if (client?.WebSocket == null || client.WebSocket.State != WebSocketState.Open)
                return;

            try
            {
                var json = JsonConvert.SerializeObject(message);
                await SendToClientAsync(client, Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error to {client.ConnectionId}: {ex.Message}");
            }
        }

        private async Task SendToClientAsync(ClientInfo client, byte[] buffer)
        {
            if (client?.WebSocket == null || client.WebSocket.State != WebSocketState.Open)
                return;

            try
            {
                await client.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error to {client.ConnectionId}: {ex.Message}");
                _clients.TryRemove(client.ConnectionId, out _);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Отменяем все операции
                _cts.Cancel();

                // Останавливаем listener
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener.Close();

                // Закрываем все WebSocket соединения
                var closeTasks = _clients.Values.Select(client => 
                {
                    try
                    {
                        if (client.WebSocket?.State == WebSocketState.Open)
                        {
                            return client.WebSocket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Server shutdown",
                                CancellationToken.None);
                        }
                        return Task.CompletedTask;
                    }
                    catch
                    {
                        return Task.CompletedTask;
                    }
                }).ToArray();

                // Ожидаем завершения закрытия соединений с таймаутом
                Task.WaitAll(closeTasks, TimeSpan.FromSeconds(5));

                // Освобождаем все ресурсы
                foreach (var client in _clients.Values)
                {
                    client.WebSocket?.Dispose();
                }
                _clients.Clear();

                _broadcastSemaphore?.Dispose();
                _cts?.Dispose();
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~Server()
        {
            Dispose();
        }
    }
}