using System;
using System.Threading.Tasks;
using System.Windows;
using NewsClient.Services;
using NewsClient.Views;

namespace NewsClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
        }
    }
}