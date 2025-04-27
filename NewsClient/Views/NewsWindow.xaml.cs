using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NewsClient.Models;
using NewsClient.Services;
using NewsClient.Services.Network;
using NewsClient.ViewModels;

namespace NewsClient.Views
{
    public partial class NewsWindow : BaseWindow
    {
        private readonly NewsListViewModel _viewModel;

        public NewsWindow(string userId, NewsTcpClient tcpClient) : base(tcpClient)
        {
            InitializeComponent();
            _viewModel = new NewsListViewModel(userId, tcpClient);
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
            Closed += (s, e) => _viewModel.Dispose();
        }
        
        // Обработчики событий кнопок (если нужно оставить code-behind логику)
        private void EditNews_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NewsItem newsItem)
            {
                _viewModel.EditNewsCommand.Execute(newsItem);
            }
        }

        private void DeleteNews_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is NewsItem newsItem)
            {
                _viewModel.DeleteNewsCommand.Execute(newsItem);
            }
        }

        private void AddNews_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddNewsCommand.Execute(new
            {
                Title = TitleTextBox.Text,
                Content = ContentTextBox.Text,
                Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
            });
        }

        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteAccountCommand.Execute(null);
        }
    }
}