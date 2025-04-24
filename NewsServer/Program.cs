using System;
using System.Threading.Tasks;

namespace NewsServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Укажите IP сервера (по умолчанию: localhost):");
            var ip = Console.ReadLine();
            if (string.IsNullOrEmpty(ip)) ip = "localhost";

            var server = new Server(ip, 8080);
            await server.StartAsync();
        }
    }
}