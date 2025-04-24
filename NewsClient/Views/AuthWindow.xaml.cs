using System.Windows;
using NewsClient.Services;

namespace NewsClient.Views
{
    public partial class AuthWindow : BaseWindow
    {
        private readonly FirebaseAuthService _authService;

        public AuthWindow(FirebaseService firebaseService, Client client) : base(firebaseService, client)
        {
            InitializeComponent();
            _authService = new FirebaseAuthService();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var email = txtEmail.Text;
            var password = txtPassword.Password;

            var result = await _authService.Login(email, password);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var userId = result.Auth.User.LocalId;

            // Переход в окно с новостями
            var newsWindow = new NewsWindow(userId, FirebaseService, WebSocketClient);
            newsWindow.Show();
            Close();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            if (RepeatPasswordContainer.Visibility == Visibility.Collapsed)
            {
                RepeatPasswordContainer.Visibility = Visibility.Visible;
                return;
            }

            var email = txtEmail.Text;
            var password = txtPassword.Password;
            var repeatPassword = txtRepeatPassword.Password;

            if (password != repeatPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _authService.Register(email, password);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Регистрация успешна! Вы можете войти.", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            RepeatPasswordContainer.Visibility = Visibility.Collapsed;
        }
    }
}
