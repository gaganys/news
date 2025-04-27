using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NewsClient.Models;
using NewsClient.Services.Auth;
using NewsClient.Services.Network;
using NewsClient.Utils;
using NewsClient.Views;

namespace NewsClient.ViewModels
{
    public class AuthViewModel: BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly NewsTcpClient _tcpClient;
        
        private string _email;
        private string _password;
        private string _repeatPassword;
        private bool _isRegisterMode;
        
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }
        
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        
        public string RepeatPassword
        {
            get => _repeatPassword;
            set => SetProperty(ref _repeatPassword, value);
        }
        
        public bool IsRegisterMode
        {
            get => _isRegisterMode;
            set => SetProperty(ref _isRegisterMode, value);
        }
        
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public AuthViewModel(IAuthService authService, NewsTcpClient tcpClient)
        {
            _authService = authService;
            _tcpClient = tcpClient;

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
        }
        
        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Email) && 
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsRegisterMode;
        }
        
        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Email) && 
                   !string.IsNullOrWhiteSpace(Password) &&
                   Password == RepeatPassword;
        }

        private async Task LoginAsync()
        {
            try
            {
                var result = await _authService.LoginAsync(Email, Password);
                
                if (result.IsSuccess)
                {
                    // Успешный вход
                    var newsWindow = new NewsWindow(result.Auth.User.LocalId, _tcpClient);
                    newsWindow.Show();
                    RequestCloseWindow();
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Ошибка входа", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task RegisterAsync()
        {
            if (!IsRegisterMode)
            {
                IsRegisterMode = true;
                return;
            }
            
            try
            {
                var result = await _authService.RegisterAsync(Email, Password);
                
                if (result.IsSuccess)
                {
                    MessageBox.Show("Регистрация успешна! Вы можете войти.", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsRegisterMode = false;
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Ошибка регистрации", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}