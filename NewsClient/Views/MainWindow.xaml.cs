using System;
using System.Windows;
using NewsClient.Services.Auth;
using NewsClient.Services.Network;
using NewsClient.Utils;
using NewsClient.ViewModels;

namespace NewsClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serverIp = txtServerIP.Text.Trim();
                if (string.IsNullOrEmpty(serverIp))
                {
                    MessageBox.Show("Введите IP сервера!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var tcpClient = ServiceLocator.GetService<NewsTcpClient>();
                await tcpClient.ConnectAsync(serverIp, AppConstants.ServerPort);
        
                var authWindow = new AuthWindow(tcpClient);
                authWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
