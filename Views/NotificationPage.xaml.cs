using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FehlzeitApp.Models;

namespace FehlzeitApp.Views
{
    public partial class NotificationPage : Page
    {
        private readonly HttpClient _httpClient;
        private readonly DispatcherTimer _refreshTimer;
        private ObservableCollection<NotificationItem> _notifications;

        public NotificationPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _notifications = new ObservableCollection<NotificationItem>();
            NotificationGrid.ItemsSource = _notifications;

            // Auto-refresh every 5 seconds
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += async (s, e) => await LoadNotifications();
            _refreshTimer.Start();

            // Load initial data
            Loaded += async (s, e) => await LoadNotifications();
        }

        private async Task LoadNotifications()
        {
            try
            {
                StatusText.Text = $"Loading... ({DateTime.Now:HH:mm:ss})";

                // Get notifications from WebAPI
                var response = await _httpClient.GetAsync("http://localhost:5000/api/benachrichtigung");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<List<NotificationDto>>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _notifications.Clear();
                        foreach (var notif in apiResponse.Data.OrderByDescending(n => n.CreatedAt))
                        {
                            _notifications.Add(new NotificationItem
                            {
                                Id = notif.BenachrichtigungId,
                                UserId = notif.UserId,
                                Title = notif.Title,
                                Message = notif.Message,
                                Type = notif.Type,
                                IsRead = notif.IsRead,
                                CreatedAt = notif.CreatedAt,
                                RelatedEntityType = notif.RelatedEntityType,
                                RelatedEntityId = notif.RelatedEntityId
                            });
                        }

                        StatusText.Text = $"Loaded {_notifications.Count} notifications ({DateTime.Now:HH:mm:ss})";
                        CountText.Text = $"Total: {_notifications.Count}";
                    }
                    else
                    {
                        StatusText.Text = "API returned no data";
                        CountText.Text = "Total: 0";
                    }
                }
                else
                {
                    StatusText.Text = $"API Error: {response.StatusCode}";
                    CountText.Text = "Total: 0";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                CountText.Text = "Total: 0";
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadNotifications();
        }

        private void ToggleAutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_refreshTimer.IsEnabled)
            {
                _refreshTimer.Stop();
                ToggleAutoRefreshButton.Content = "Start Auto-Refresh";
            }
            else
            {
                _refreshTimer.Start();
                ToggleAutoRefreshButton.Content = "Stop Auto-Refresh";
            }
        }

        private async void CreateTestNotification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var testNotification = new
                {
                    objektId = 2,
                    title = "Test Notification",
                    message = $"Test created at {DateTime.Now:HH:mm:ss}",
                    type = "Info",
                    relatedEntityType = "Test",
                    relatedEntityId = 999
                };

                var json = JsonSerializer.Serialize(testNotification);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:5000/api/benachrichtigung/for-objekt", content);
                
                if (response.IsSuccessStatusCode)
                {
                    StatusText.Text = "Test notification created successfully";
                    await LoadNotifications();
                }
                else
                {
                    StatusText.Text = $"Failed to create test notification: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error creating test notification: {ex.Message}";
            }
        }
    }

    public class NotificationItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string Status => IsRead ? "READ" : "UNREAD";
        public string CreatedAtString => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class NotificationDto
    {
        public int BenachrichtigungId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }

}
