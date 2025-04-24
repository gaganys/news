using System;
using NewsClient.Services;
using System.Windows;
using NewsClient.Utility;

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
            var ip = txtServerIP.Text;
            var client = new Client(ip, AppConstants.WebSocketPort);

            try
            {
                await client.ConnectAsync();
                var firebaseService = new FirebaseService(AppConstants.FirebaseProjectId, client);

                var authWindow = new AuthWindow(firebaseService, client);
                authWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
