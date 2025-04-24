using NewsClient.Services;
using System.Windows;
using NewsClient.Models;
using System.Windows.Controls;
using System;

namespace NewsClient.Views
{
    public partial class EditNewsWindow : Window
    {
        private readonly NewsItem _originalNewsItem;
        private readonly FirebaseService _firestoreService;

        public NewsItem EditedNewsItem { get; private set; }

        public EditNewsWindow(NewsItem newsItem)
        {
            InitializeComponent();
            _originalNewsItem = newsItem;
            _firestoreService = new FirebaseService("newsdistributionsystem");

            // Инициализация полей
            TitleTextBox.Text = _originalNewsItem.Title;
            ContentTextBox.Text = _originalNewsItem.Content;

            // Установка категории
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Content.ToString() == _originalNewsItem.Category)
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем обновленный объект
                EditedNewsItem = new NewsItem
                {
                    Title = TitleTextBox.Text,
                    Content = ContentTextBox.Text,
                    Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                    PublishDate = _originalNewsItem.PublishDate,
                    UserId = _originalNewsItem.UserId,
                    DocumentId = _originalNewsItem.DocumentId
                };

                await _firestoreService.UpdateNewsAsync(EditedNewsItem);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
