using System;
using System.Threading.Tasks;

namespace NewsServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ServerConfig
            {
                FirebaseCredentialsPath = "D:\\КурсоваяКСиС\\NewsDistributionSystem\\NewsServer\\credentials\\newsdistributionsystem-firebase-adminsdk-fbsvc-15c304d0b6.json",
                FirebaseProjectId = "newsdistributionsystem",
                ServerIp = "127.0.0.1",
                ServerPort = 8080
            };

            try
            {
                config.Validate();
                var server = new Server(config);  
            
                Console.CancelKeyPress += (sender, e) => 
                {
                    e.Cancel = true;
                    server.StopAsync().Wait();
                };
                
                await server.StartAsync();

            } 
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}