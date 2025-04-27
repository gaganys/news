using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NewsClient.Models;
using NewsClient.Services.Network;
using NewsClient.Utils;

namespace NewsClient.ViewModels
{
    public class NewsListViewModel : BaseViewModel
    {
        private readonly NewsTcpClient _tcpClient;
        private string _userId;

        public ObservableCollection<NewsItem> AllNewsItems { get; } = new ObservableCollection<NewsItem>();
        public ObservableCollection<NewsItem> UserNewsItems { get; } = new ObservableCollection<NewsItem>();

        public ICommand EditNewsCommand { get; }
        public ICommand DeleteNewsCommand { get; }
        public ICommand AddNewsCommand { get; }
        public ICommand DeleteAccountCommand { get; }

        public NewsListViewModel(string userId, NewsTcpClient tcpClient)
        {
            _userId = userId;
            _tcpClient = tcpClient;

            // Инициализация команд
            EditNewsCommand = new AsyncRelayCommand<NewsItem>(EditNewsAsync);
            DeleteNewsCommand = new AsyncRelayCommand<NewsItem>(DeleteNewsAsync);
            AddNewsCommand = new AsyncRelayCommand<dynamic>(AddNewsAsync);
            DeleteAccountCommand = new AsyncRelayCommand(DeleteAccountAsync);

            // Подписка на события TCP клиента
            _tcpClient.NewsPublished += OnNewsPublished;
            _tcpClient.NewsUpdated += OnNewsUpdated;
            _tcpClient.NewsDeleted += OnNewsDeleted;
            _tcpClient.NewsListReceived += OnNewsListReceived;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _tcpClient.ConnectAsync();
                await _tcpClient.GetAllNewsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private async Task EditNewsAsync(NewsItem newsItem)
        {
            try
            {
                await _tcpClient.UpdateNewsAsync(newsItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования: {ex.Message}");
            }
        }

        private async Task DeleteNewsAsync(NewsItem newsItem)
        {
            try
            {
                var result = MessageBox.Show("Удалить новость?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _tcpClient.DeleteNewsAsync(newsItem.DocumentId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        private async Task AddNewsAsync(dynamic newsData)
        {
            try
            {
                var newsItem = new NewsItem
                {
                    Title = newsData.Title,
                    Content = newsData.Content,
                    Category = newsData.Category,
                    PublishDate = DateTime.UtcNow,
                    UserId = _userId
                };

                await _tcpClient.PublishNewsAsync(newsItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}");
            }
        }

        private async Task DeleteAccountAsync()
        {
            try
            {
                var result = MessageBox.Show("Удалить аккаунт?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Логика удаления аккаунта
                    MessageBox.Show("Аккаунт удален");
                    RequestCloseWindow();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления аккаунта: {ex.Message}");
            }
        }

        private void OnNewsPublished(NewsItem newsItem)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AllNewsItems.Insert(0, newsItem);
                if (newsItem.UserId == _userId)
                {
                    UserNewsItems.Insert(0, newsItem);
                }
            });
        }

        private void OnNewsUpdated(NewsItem newsItem)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateNewsItem(AllNewsItems, newsItem);
                if (newsItem.UserId == _userId)
                {
                    UpdateNewsItem(UserNewsItems, newsItem);
                }
            });
        }

        private void UpdateNewsItem(ObservableCollection<NewsItem> collection, NewsItem updatedItem)
        {
            var index = -1;
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i].DocumentId == updatedItem.DocumentId)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                collection[index] = updatedItem;
            }
            else
            {
                collection.Insert(0, updatedItem);
            }
        }

        private void OnNewsDeleted(string documentId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RemoveNewsItem(AllNewsItems, documentId);
                RemoveNewsItem(UserNewsItems, documentId);
            });
        }

        private void RemoveNewsItem(ObservableCollection<NewsItem> collection, string documentId)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i].DocumentId == documentId)
                {
                    collection.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnNewsListReceived(List<NewsItem> newsList)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AllNewsItems.Clear();
                UserNewsItems.Clear();

                foreach (var news in newsList)
                {
                    AllNewsItems.Add(news);
                    if (news.UserId == _userId)
                    {
                        UserNewsItems.Add(news);
                    }
                }
            });
        }

        public void Dispose()
        {
            _tcpClient.NewsPublished -= OnNewsPublished;
            _tcpClient.NewsUpdated -= OnNewsUpdated;
            _tcpClient.NewsDeleted -= OnNewsDeleted;
            _tcpClient.NewsListReceived -= OnNewsListReceived;
        }
    }
}