using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NewsClient.Models;
using NewsClient.Services;

namespace NewsClient.Views
{
    public partial class NewsWindow : Window
    {
        private readonly FirebaseService _firebaseService;
        private readonly Client _client;
        private readonly string _userId;
        private bool _disposed;

        public ObservableCollection<NewsItem> AllNewsItems { get; } = new ObservableCollection<NewsItem>();
        public ObservableCollection<NewsItem> UserNewsItems { get; } = new ObservableCollection<NewsItem>();

        public NewsWindow(string userId, FirebaseService firebaseService, Client client)
        {
            InitializeComponent();
            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _firebaseService = firebaseService ?? throw new ArgumentNullException(nameof(firebaseService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
            DataContext = this;

            // Подписка на события FirebaseService
            _firebaseService.NewsPublished += OnNewsPublished;
            _firebaseService.NewsUpdated += OnNewsUpdated;
            _firebaseService.NewsDeleted += OnNewsDeleted;
    
            // Подписка на события WebSocket клиента
            _client.NewsPublished += OnNewsPublished;
            _client.NewsUpdated += OnNewsUpdated;
            _client.NewsDeleted += OnNewsDeleted;
            _client.NewsListReceived += OnNewsListReceived;

            Loaded += async (s, e) => await InitializeAsync();
            Closed += (s, e) => Dispose();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _client.ConnectAsync();
                await _client.SubscribeToNewsUpdatesAsync();
                await LoadNewsAsync();
                
                Console.WriteLine("WebSocket connected and subscribed to updates");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private async Task LoadNewsAsync()
        {
            try
            {
                Console.WriteLine("Попытка загрузить новости...");
                var newsList = await _firebaseService.GetAllNewsAsync();
        
                if (newsList == null)
                {
                    Console.WriteLine("Получен пустой список новостей");
                    return;
                }
        
                Console.WriteLine($"Успешно загружено {newsList.Count} новостей");
                UpdateNewsCollections(newsList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новостей: {ex.Message}");
            }
        }

        private void UpdateNewsCollections(IEnumerable<NewsItem> newsList)
        {
            AllNewsItems.Clear();
            UserNewsItems.Clear();

            foreach (var news in newsList.OrderByDescending(n => n.PublishDate))
            {
                AllNewsItems.Add(news);
                if (news.UserId == _userId)
                {
                    UserNewsItems.Add(news);
                }
            }
        }

        private void OnNewsPublished(NewsItem newsItem)
        {
            Dispatcher.Invoke(() =>
            {
                // Удаляем если уже существует (на случай дублирования событий)
                var existing = AllNewsItems.FirstOrDefault(n => n.DocumentId == newsItem.DocumentId);
                if (existing != null) AllNewsItems.Remove(existing);
                
                // Доабвление новой версии в начало списка
                AllNewsItems.Insert(0, newsItem);

                if (newsItem.UserId == _userId)
                {
                    existing = UserNewsItems.FirstOrDefault(n => n.DocumentId == newsItem.DocumentId);
                    if (existing != null) UserNewsItems.Remove(existing);
                    UserNewsItems.Insert(0, newsItem);
                }
            });
        }

        private void OnNewsUpdated(NewsItem newsItem)
        {
            Dispatcher.Invoke(() =>
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
            var existing = collection.FirstOrDefault(n => n.DocumentId == updatedItem.DocumentId);
            if (existing != null)
            {
                var index = collection.IndexOf(existing);
                collection.RemoveAt(index);
                collection.Insert(index, updatedItem); // Сохраняем позицию, но обновляем элемент
            }
            else
            {
                collection.Insert(0, updatedItem);
            }
        }

        private void OnNewsDeleted(string documentId)
        {
            Dispatcher.Invoke(() =>
            {
                var itemToRemove = AllNewsItems.FirstOrDefault(n => n.DocumentId == documentId);
                if (itemToRemove != null) AllNewsItems.Remove(itemToRemove);

                itemToRemove = UserNewsItems.FirstOrDefault(n => n.DocumentId == documentId);
                if (itemToRemove != null) UserNewsItems.Remove(itemToRemove);
            });
        }

        private void OnNewsListReceived(List<NewsItem> newsList)
        {
            Dispatcher.Invoke(() => UpdateNewsCollections(newsList));
        }

        private async void AddNews_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Заголовок и содержание обязательны");
                return;
            }

            try
            {
                var newsItem = new NewsItem
                {
                    Title = TitleTextBox.Text,
                    Content = ContentTextBox.Text,
                    Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Общее",
                    PublishDate = DateTime.UtcNow,
                    UserId = _userId
                };

                var addedItem = await _firebaseService.AddNewsItemAsync(newsItem);
                await _client.PublishNewsAsync(addedItem);

                TitleTextBox.Clear();
                ContentTextBox.Clear();
                CategoryComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления новости: {ex.Message}");
            }
        }

        private async void EditNews_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var newsItem = button.DataContext as NewsItem;

            if (newsItem == null) return;

            var editWindow = new EditNewsWindow(newsItem, _firebaseService, _client);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    var updatedItem = new NewsItem
                    {
                        DocumentId = newsItem.DocumentId,
                        Title = editWindow.EditedNewsItem.Title,
                        Content = editWindow.EditedNewsItem.Content,
                        Category = editWindow.EditedNewsItem.Category,
                        PublishDate = DateTime.UtcNow,
                        UserId = _userId
                    };

                    await _firebaseService.UpdateNewsAsync(updatedItem);
                    await _client.UpdateNewsAsync(updatedItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления новости: {ex.Message}");
                }
            }
        }

        private async void DeleteNews_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var newsItem = button.DataContext as NewsItem;

            if (newsItem == null) return;

            var confirm = MessageBox.Show("Вы уверены, что хотите удалить новость?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _firebaseService.DeleteNewsAsync(newsItem.DocumentId);
                    await _client.DeleteNewsAsync(newsItem.DocumentId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления новости: {ex.Message}");
                }
            }
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Вы уверены, что хотите удалить аккаунт?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var authService = new FirebaseAuthService();
                    var result = await authService.DeleteAccountAsync(_userId);

                    if (result.IsSuccess)
                    {
                        MessageBox.Show("Аккаунт успешно удален");
                        new AuthWindow(_firebaseService, _client).Show();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка удаления аккаунта: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _client.NewsPublished -= OnNewsPublished;
            _client.NewsUpdated -= OnNewsUpdated;
            _client.NewsDeleted -= OnNewsDeleted;
            _client.NewsListReceived -= OnNewsListReceived;

            _client.Dispose();
            _firebaseService.Dispose();

            _disposed = true;
        }
    }
}