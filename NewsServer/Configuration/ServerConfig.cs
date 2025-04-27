using System;

namespace NewsServer
{
    public class ServerConfig
    {
        public string FirebaseCredentialsPath { get; set; }
        public string FirebaseProjectId { get; set; }
        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 8080;
        
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(FirebaseCredentialsPath))
                throw new ArgumentNullException(nameof(FirebaseCredentialsPath));
            if (string.IsNullOrWhiteSpace(FirebaseProjectId))
                throw new ArgumentNullException(nameof(FirebaseProjectId));
            if (string.IsNullOrWhiteSpace(ServerIp))
                throw new ArgumentNullException(nameof(ServerIp));
            if (ServerPort <= 0 || ServerPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(ServerPort));
        }
    }
}