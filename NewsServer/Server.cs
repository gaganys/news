using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewsServer.Repositories;
using Newtonsoft.Json;

namespace NewsServer
{
    public class Server : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new ConcurrentDictionary<string, ClientInfo>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SemaphoreSlim _broadcastSemaphore = new SemaphoreSlim(10);
        private readonly IFirebaseRepository _firebaseRepository;
        private bool _disposed;
        
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DateFormatString = "o",
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        public Server(ServerConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _firebaseRepository = new FirebaseRepository(
                config.FirebaseCredentialsPath,
                config.FirebaseProjectId);

            _listener = new TcpListener(IPAddress.Parse(config.ServerIp), config.ServerPort);
        }
        
         public async Task StartAsync()
        {
            try
            {
                _listener.Start(100);
                Console.WriteLine($"Server started at {_listener.LocalEndpoint}");

                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        _ = HandleTcpClientConnectionAsync(tcpClient);
                    }
                    catch (Exception ex) when (ex is SocketException || ex is OperationCanceledException)
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
            }
        }

        private async Task HandleTcpClientConnectionAsync(TcpClient tcpClient)
        {
            var connectionId = Guid.NewGuid().ToString();
            NetworkStream stream = null;

            try
            {
                stream = tcpClient.GetStream();
                var clientInfo = new ClientInfo
                {
                    TcpClient = tcpClient,
                    Stream = stream,
                    ConnectionId = connectionId
                };

                if (!_clients.TryAdd(connectionId, clientInfo))
                    throw new InvalidOperationException("Failed to add client to dictionary");

                Console.WriteLine($"Client connected: {connectionId}");
                await ReceiveMessagesAsync(clientInfo, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP connection error for {connectionId}: {ex.Message}");
            }
            finally
            {
                if (stream != null)
                {
                    _clients.TryRemove(connectionId, out _);
                    await CloseClientAsync(new ClientInfo { TcpClient = tcpClient, Stream = stream, ConnectionId = connectionId });
                    Console.WriteLine($"Client disconnected: {connectionId}");
                }
            }
        }

        private async Task CloseClientAsync(ClientInfo clientInfo)
        {
            try
            {
                if (clientInfo.TcpClient?.Connected == true)
                    clientInfo.TcpClient.Close();

                clientInfo.Stream?.Dispose();
                clientInfo.TcpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing client {clientInfo.ConnectionId}: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync(ClientInfo clientInfo, CancellationToken ct)
        {
            var buffer = new byte[1024 * 4];
            var messageBuilder = new StringBuilder();

            try
            {
                while (clientInfo.TcpClient.Connected && !ct.IsCancellationRequested)
                {
                    var bytesRead = await clientInfo.Stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead == 0) break;

                    var messagePart = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(messagePart);

                    if (messagePart.Contains("\n"))
                    {
                        var fullMessage = messageBuilder.ToString().Trim();
                        messageBuilder.Clear();
                        _ = ProcessClientMessageAsync(fullMessage, clientInfo);
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

                var createdItem = await _firebaseRepository.AddNewsAsync(newsItem);

                await BroadcastAsync(new
                {
                    Type = "news",
                    Action = "publish",
                    createdItem.DocumentId,
                    createdItem.Title,
                    createdItem.Content,
                    createdItem.Category,
                    PublishDate = createdItem.PublishDate.ToString("o"),
                    createdItem.UserId
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

                var existingItem = await _firebaseRepository.GetNewsByIdAsync(documentId);
                if (existingItem == null || existingItem.UserId != clientInfo.UserId)
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "News not found or unauthorized" });
                    return;
                }

                var updatedItem = new NewsItem
                {
                    DocumentId = documentId,
                    Title = data.Title?.ToString() ?? existingItem.Title,
                    Content = data.Content?.ToString() ?? existingItem.Content,
                    Category = data.Category?.ToString() ?? existingItem.Category,
                    PublishDate = DateTime.TryParse(data.PublishDate?.ToString(), out DateTime date) ? date : existingItem.PublishDate,
                    UserId = clientInfo.UserId
                };

                await _firebaseRepository.UpdateNewsAsync(updatedItem);

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
                
                var existingItem = await _firebaseRepository.GetNewsByIdAsync(documentId);
                if (existingItem == null || existingItem.UserId != clientInfo.UserId)
                {
                    await SendToClientAsync(clientInfo, new { Type = "error", Message = "Not authorized" });
                    return;
                }

                await _firebaseRepository.DeleteNewsAsync(documentId);
                await BroadcastAsync(new { Type = "delete", DocumentId = documentId });
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
                var newsList = (await _firebaseRepository.GetAllNewsAsync())
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
                var json = JsonConvert.SerializeObject(message, _jsonSettings) + "\n";
                var buffer = Encoding.UTF8.GetBytes(json);

                var tasks = _clients.Values
                    .Where(c => c.TcpClient?.Connected == true && c.ConnectionId != excludeConnectionId)
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
            if (client?.TcpClient == null || !client.TcpClient.Connected)
                return;

            try
            {
                var json = JsonConvert.SerializeObject(message, _jsonSettings) + "\n";
                await SendToClientAsync(client, Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error to {client.ConnectionId}: {ex.Message}");
            }
        }

        private async Task SendToClientAsync(ClientInfo client, byte[] buffer)
        {
            if (client?.TcpClient == null || !client.TcpClient.Connected)
                return;

            try
            {
                await client.Stream.WriteAsync(buffer, 0, buffer.Length);
                await client.Stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error to {client.ConnectionId}: {ex.Message}");
                _clients.TryRemove(client.ConnectionId, out _);
            }
        }

        public async Task StopAsync()
        {
            _cts.Cancel();
            await Task.WhenAll(_clients.Values.Select(c => CloseClientAsync(c)));
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _cts.Cancel();

                if (_listener.Server != null && _listener.Server.IsBound)
                    _listener.Stop();

                var closeTasks = _clients.Values.Select(client => 
                {
                    try
                    {
                        if (client.TcpClient?.Connected == true)
                            client.TcpClient.Close();
                        return Task.CompletedTask;
                    }
                    catch
                    {
                        return Task.CompletedTask;
                    }
                }).ToArray();

                Task.WaitAll(closeTasks, TimeSpan.FromSeconds(5));

                foreach (var client in _clients.Values)
                {
                    client.Stream?.Dispose();
                    client.TcpClient?.Dispose();
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