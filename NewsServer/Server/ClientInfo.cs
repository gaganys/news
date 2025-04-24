using System.Net.WebSockets;

namespace NewsServer
{
    public class ClientInfo
    {
        public WebSocket WebSocket { get; set; }
        public string ConnectionId { get; set; }
        public string UserId { get; set; }
    }
}