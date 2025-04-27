using System;
using NewsClient.Services.Auth;
using NewsClient.Services.Network;
using NewsClient.Utils;
using NewsClient.ViewModels;

namespace NewsClient.Views
{
    public partial class AuthWindow : BaseWindow
    {
        public AuthWindow(NewsTcpClient tcpClient) : base(tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            
            InitializeComponent();
            
            // Явная инициализация ViewModel
            var authService = ServiceLocator.GetService<IAuthService>();
            DataContext = new AuthViewModel(authService, tcpClient);
                
            // Привязка PasswordBox к ViewModel
            txtPassword.PasswordChanged += (s, e) => 
            {
                if (DataContext is AuthViewModel vm)
                    vm.Password = txtPassword.Password;
            };
            
            txtRepeatPassword.PasswordChanged += (s, e) => 
            {
                if (DataContext is AuthViewModel vm)
                    vm.RepeatPassword = txtRepeatPassword.Password;
            };
        }
    }
}
