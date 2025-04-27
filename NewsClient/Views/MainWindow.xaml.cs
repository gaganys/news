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
                var tcpClient = ServiceLocator.GetService<NewsTcpClient>();
                await tcpClient.ConnectAsync(txtServerIP.Text, AppConstants.ServerPort);
        
                var authWindow = ServiceLocator.GetService<AuthWindow>();
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
