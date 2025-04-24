// BaseWindow.cs
using System.Windows;
using NewsClient.Services;

namespace NewsClient.Views
{
    public class BaseWindow : Window
    {
        protected readonly FirebaseService FirebaseService;
        protected readonly Client WebSocketClient;

        public BaseWindow(FirebaseService firebaseService, Client client)
        {
            FirebaseService = firebaseService;
            WebSocketClient = client;
            
            // Общие настройки для всех окон
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
        }

        protected void ShowError(string message) 
            => MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}