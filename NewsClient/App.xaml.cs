using System.Windows;
using NewsClient.Utils;
using NewsClient.Views;

namespace NewsClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            
           
            var mainWindow = ServiceLocator.GetService<MainWindow>();
            mainWindow.Show();
        }
    }
}