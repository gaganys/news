using NewsClient.Services;
using System.Windows;

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

            var client = new Client(ip, 8080);
            await client.ConnectAsync();

            var authWindow = new AuthWindow();
            authWindow.Show();
            Close();
        }
    }
}
