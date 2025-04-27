using System;
using Microsoft.Extensions.DependencyInjection;
using NewsClient.Services;
using NewsClient.Services.Auth;
using NewsClient.Services.Network;
using NewsClient.ViewModels;
using NewsClient.Views;

namespace NewsClient.Utils
{
    public static class ServiceLocator
    {
        public static IServiceProvider Provider { get; }

        static ServiceLocator()
        {
            var services = new ServiceCollection();

            // Регистрация сервисов
            services.AddSingleton<NewsTcpClient>(provider => 
                new NewsTcpClient(AppConstants.DefaultServerIp, AppConstants.ServerPort));
            
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<INewsService, NewsService>();

            // Регистрация ViewModels с зависимостями
            services.AddTransient<AuthViewModel>(provider => 
                new AuthViewModel(
                    provider.GetRequiredService<IAuthService>(),
                    provider.GetRequiredService<NewsTcpClient>()
                ));
            services.AddTransient<NewsListViewModel>();

            // Регистрация окон
            services.AddTransient<MainWindow>();
            services.AddTransient<AuthWindow>(provider =>
                new AuthWindow(
                    provider.GetRequiredService<NewsTcpClient>()
                ));
            services.AddTransient<NewsWindow>(provider => 
            {
                var tcpClient = provider.GetRequiredService<NewsTcpClient>();
                return new NewsWindow(null, tcpClient); // UserId будет установлен позже
            });

            Provider = services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : class
        {
            return Provider.GetRequiredService<T>();
        }
    }
}