// BaseWindow.cs
using System.Windows;
using NewsClient.Services;
using NewsClient.Services.Network;

namespace NewsClient.Views
{
    public class BaseWindow : Window
    {
        protected readonly NewsTcpClient TcpClient;

        public BaseWindow(NewsTcpClient tcpClient)
        {
            TcpClient = tcpClient;
            
            // Общие настройки для всех окон
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
        }

        protected void ShowError(string message) 
            => MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}