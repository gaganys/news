using System.Net.Sockets;
using System.Net.WebSockets;

namespace NewsServer
{
    public class ClientInfo
    {
        public TcpClient TcpClient { get; set; }
        public NetworkStream Stream { get; set; }
        public string ConnectionId { get; set; }
        public string UserId { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }
}