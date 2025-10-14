using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; // for DispatcherTimer
using System.Windows.Media; // for Colors, Brushes, SolidColorBrush
using FehlzeitApp.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace FehlzeitApp.Views
{
    public partial class NotificationPage : UserControl
    {
        private readonly AuthService _authService;
        private DispatcherTimer? _refreshTimer;
        private BenachrichtigungService? _benachrichtigungService;
        private bool _isSignalRConnected = false;
        private ObservableCollection<NotificationItemViewModel> _allNotifications = new();
        private ObservableCollection<NotificationItemViewModel> _filteredNotifications = new();
        private ObservableCollection<NotificationItemViewModel> _pagedNotifications = new();
        private bool _isLoading = false;
        
        // Pagination
        private int _currentPage = 1;
        private const int _itemsPerPage = 5;
        private int _totalPages = 1;
        
        // SignalR
        private HubConnection? _hubConnection;

        public NotificationPage(AuthService authService)
        {
            try
            {
                InitializeComponent();
                _authService = authService;
                
                // Safely set ItemsSource to paged collection
                if (NotificationsListBox != null)
                {
                    NotificationsListBox.ItemsSource = _pagedNotifications;
                }
                
                // Initialize timer
                _refreshTimer = new DispatcherTimer();
                _refreshTimer.Interval = TimeSpan.FromSeconds(30);
                _refreshTimer.Tick += async (s, e) => await LoadNotificationsAsync();
                
                Loaded += NotificationPage_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Benachrichtigungsseite: {ex.Message}\n\nStack: {ex.StackTrace}", 
                              "Initialisierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async void NotificationPage_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NotificationPage] ============ PAGE LOADED EVENT FIRED ============");
            
            try
            {
                await InitializeServicesAsync();
                await InitializeSignalRAsync();
                await LoadNotificationsAsync();
                
                // Start auto-refresh timer after initial load (as fallback)
                if (_refreshTimer != null)
                {
            _refreshTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in NotificationPage_Loaded:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                              "Page Load Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                _benachrichtigungService = new BenachrichtigungService(_authService, configService);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Services: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async Task LoadNotificationsAsync()
        {
            if (_isLoading)
            {
                System.Diagnostics.Debug.WriteLine("[NotificationPage] Already loading, skipping duplicate call");
                return;
            }

            if (_benachrichtigungService == null)
            {
                MessageBox.Show("BenachrichtigungService ist nicht initialisiert!", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _isLoading = true;
                ShowLoading(true);
                if (TxtStatusMessage != null)
                    TxtStatusMessage.Text = "Lade...";

                System.Diagnostics.Debug.WriteLine("[NotificationPage] Calling GetAllAsync...");
                var response = await _benachrichtigungService.GetAllAsync();
                
                System.Diagnostics.Debug.WriteLine($"[NotificationPage] Response - Success: {response.Success}, Message: {response.Message}, Data Count: {response.Data?.Count ?? 0}");
                
                if (response.Success && response.Data != null)
                {
                    _allNotifications.Clear();
                    foreach (var notif in response.Data.OrderByDescending(n => n.Erstellungsdatum))
                    {
                        _allNotifications.Add(new NotificationItemViewModel
                        {
                            Id = notif.Id,
                            UserId = notif.EmpfaengerId,
                            Title = notif.Titel,
                            Message = notif.Nachricht,
                            Type = notif.Typ,
                            IsRead = notif.Gelesen,
                            CreatedAt = notif.Erstellungsdatum,
                                RelatedEntityType = notif.RelatedEntityType,
                            RelatedEntityId = notif.RelatedEntityId,
                            MitarbeiterName = notif.MitarbeiterName,
                            MitarbeiterId = notif.MitarbeiterId,
                            ObjektName = notif.ObjektName,
                            ObjektId = notif.ObjektId
                        });
                    }

                    ApplyFilters();
                    UpdateUnreadBadge();
                    if (TxtStatusMessage != null)
                        TxtStatusMessage.Text = $"Aktualisiert: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    if (TxtStatusMessage != null)
                        TxtStatusMessage.Text = $"Fehler: {response.Message}";
                    ShowEmptyState(true);
                    
                    // Show debug info to user
                    MessageBox.Show($"API Fehler:\n\nSuccess: {response.Success}\nMessage: {response.Message}\nData ist null: {response.Data == null}", 
                                  "API Debug", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Benachrichtigungen:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                if (TxtStatusMessage != null)
                    TxtStatusMessage.Text = $"Fehler: {ex.Message}";
                ShowEmptyState(true);
            }
            finally
            {
                _isLoading = false;
                ShowLoading(false);
            }
        }

        private void ApplyFilters()
        {
            if (_allNotifications == null || _filteredNotifications == null)
                return;

            var filtered = _allNotifications.AsEnumerable();

            // Type filter
            if (CmbFilterType?.SelectedItem is ComboBoxItem typeItem && typeItem.Content?.ToString() != "Alle Typen")
            {
                var selectedType = typeItem.Content.ToString();
                if (!string.IsNullOrEmpty(selectedType))
                    filtered = filtered.Where(n => n.Type == selectedType);
            }

            // Status filter
            if (CmbFilterStatus?.SelectedItem is ComboBoxItem statusItem)
            {
                var selectedStatus = statusItem.Content?.ToString();
                if (selectedStatus == "Ungelesen")
                    filtered = filtered.Where(n => !n.IsRead);
                else if (selectedStatus == "Gelesen")
                    filtered = filtered.Where(n => n.IsRead);
            }

            _filteredNotifications.Clear();
            foreach (var notif in filtered)
            {
                _filteredNotifications.Add(notif);
            }

            // Reset to first page and update pagination
            _currentPage = 1;
            UpdatePagination();
            UpdateRecordCount();
            ShowEmptyState(_filteredNotifications.Count == 0);
        }
        
        private void UpdatePagination()
        {
            if (_filteredNotifications == null || _pagedNotifications == null)
                return;

            // Calculate total pages
            _totalPages = (int)Math.Ceiling(_filteredNotifications.Count / (double)_itemsPerPage);
            if (_totalPages == 0) _totalPages = 1;

            // Ensure current page is valid
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;

            // Get items for current page
            var pagedItems = _filteredNotifications
                .Skip((_currentPage - 1) * _itemsPerPage)
                .Take(_itemsPerPage)
                .ToList();

            // Update paged collection
            _pagedNotifications.Clear();
            foreach (var item in pagedItems)
            {
                _pagedNotifications.Add(item);
            }

            // Update pagination UI
            UpdatePaginationUI();
        }

        private void UpdatePaginationUI()
        {
            if (TxtPageInfo != null)
            {
                TxtPageInfo.Text = $"Seite {_currentPage} von {_totalPages}";
            }

            if (BtnPrevPage != null)
            {
                BtnPrevPage.IsEnabled = _currentPage > 1;
            }

            if (BtnNextPage != null)
            {
                BtnNextPage.IsEnabled = _currentPage < _totalPages;
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        private void UpdateRecordCount()
        {
            if (TxtRecordCount == null || _filteredNotifications == null) return;
            
            int count = _filteredNotifications.Count;
            TxtRecordCount.Text = count == 1 ? "1 Benachrichtigung" : $"{count} Benachrichtigungen";
        }

        private void UpdateUnreadBadge()
        {
            if (UnreadBadge == null || TxtUnreadCount == null || _allNotifications == null) return;
            
            int unreadCount = _allNotifications.Count(n => !n.IsRead);
            if (unreadCount > 0)
            {
                UnreadBadge.Visibility = Visibility.Visible;
                TxtUnreadCount.Text = unreadCount.ToString();
            }
            else
            {
                UnreadBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowLoading(bool show)
        {
            if (LoadingPanel != null)
                LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (NotificationsScrollViewer != null)
                NotificationsScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowEmptyState(bool show)
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (NotificationsScrollViewer != null)
                NotificationsScrollViewer.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadNotificationsAsync();
        }

        private async void BtnMarkAsRead_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotificationItemViewModel notif && _benachrichtigungService != null)
            {
                try
                {
                    var response = await _benachrichtigungService.MarkAsReadAsync(notif.Id);
                    
                    if (response.Success)
                    {
                        notif.IsRead = true;
                        UpdateUnreadBadge();
                        ApplyFilters();
                    }
                    else
                    {
                        MessageBox.Show($"Fehler: {response.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler: {ex.Message}", "Fehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NotificationItemViewModel notif && _benachrichtigungService != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie diese Benachrichtigung wirklich löschen?", 
                    "Löschen bestätigen", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _benachrichtigungService.DeleteAsync(notif.Id);
                        
                        if (response.Success)
                        {
                            _allNotifications.Remove(notif);
                            _filteredNotifications.Remove(notif);
                            UpdateRecordCount();
                            UpdateUnreadBadge();
                        }
                        else
                        {
                            MessageBox.Show($"Fehler: {response.Message}", "Fehler", 
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            if (_benachrichtigungService == null) return;

            try
            {
                var response = await _benachrichtigungService.MarkAllAsReadAsync();
                
                if (response.Success)
                {
                    foreach (var notif in _allNotifications)
                    {
                        notif.IsRead = true;
                    }
                    ApplyFilters();
                    UpdateUnreadBadge();
                    
                    MessageBox.Show("Alle Benachrichtigungen wurden als gelesen markiert.", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Fehler: {response.Message}", "Fehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (_benachrichtigungService == null) return;

            var result = MessageBox.Show(
                "Möchten Sie wirklich ALLE Benachrichtigungen löschen?", 
                "Alle löschen bestätigen", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _benachrichtigungService.DeleteAllAsync();
                    
                    if (response.Success)
                    {
                        _allNotifications.Clear();
                        _filteredNotifications.Clear();
                        UpdateRecordCount();
                        UpdateUnreadBadge();
                        ShowEmptyState(true);
                        
                        MessageBox.Show("Alle Benachrichtigungen wurden gelöscht.", 
                                      "Erfolg", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
            }
            else
            {
                        MessageBox.Show($"Fehler: {response.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allNotifications != null && _filteredNotifications != null)
                ApplyFilters();
        }

        private void CmbFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allNotifications != null && _filteredNotifications != null)
                ApplyFilters();
        }

        // ====================
        // SignalR Methods
        // ====================
        
        private async Task InitializeSignalRAsync()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                var baseUrl = configService.ApiSettings.BaseUrl;
                var hubUrl = $"{baseUrl}/notificationHub";
                
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ INITIALIZING ============");
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] Hub URL: {hubUrl}");
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] Auth Token: {(_authService.Token != null ? "Present" : "NULL")}");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(_authService.Token);
                        System.Diagnostics.Debug.WriteLine($"[SignalR Client] Token provider called");
                    })
                    .WithAutomaticReconnect()
                    .Build();

                // Handle incoming notifications
                _hubConnection.On<object>("ReceiveNotification", async (notification) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ NOTIFICATION RECEIVED ============");
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Notification data: {notification}");
                    
                    // Show popup notification on UI thread
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[SignalR Client] About to show popup...");
                            
                            // Show popup
                            ShowNotificationPopup("📬 Neue Benachrichtigung", "Eine neue Benachrichtigung ist eingetroffen!");
                            
                            System.Diagnostics.Debug.WriteLine($"[SignalR Client] Popup shown!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Fehler beim Anzeigen der Benachrichtigung:\n\n{ex.Message}", 
                                          "Fehler", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        }
                        
                        // Reload notifications list
                        System.Diagnostics.Debug.WriteLine($"[SignalR Client] Reloading notifications...");
                        await LoadNotificationsAsync();
                    });
                });

                // Connection lifecycle events
                _hubConnection.Closed += async (error) =>
                {
                    _isSignalRConnected = false;
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ CONNECTION CLOSED ============");
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Error: {error?.Message}");
                    await Task.Delay(5000); // Wait before reconnecting
                    await TryConnectSignalRAsync();
                };

                _hubConnection.Reconnecting += (error) =>
                {
                    _isSignalRConnected = false;
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ RECONNECTING ============");
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Error: {error?.Message}");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += (connectionId) =>
                {
                    _isSignalRConnected = true;
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ RECONNECTED ============");
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Connection ID: {connectionId}");
                    return Task.CompletedTask;
                };

                await TryConnectSignalRAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Initialization failed: {ex.Message}");
                MessageBox.Show($"SignalR Verbindung konnte nicht hergestellt werden: {ex.Message}\n\nFallback auf Polling.", 
                              "Info", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
        }

        private async Task TryConnectSignalRAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] ============ CONNECTING ============");
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] Current State: {_hubConnection?.State}");
                
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Starting connection...");
                    await _hubConnection.StartAsync();
                    _isSignalRConnected = true;
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ✅ Connected successfully!");
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] Connection ID: {_hubConnection.ConnectionId}");
                    
                    // Stop polling timer since we have real-time updates
                    _refreshTimer?.Stop();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR Client] ⚠️ Cannot connect - State is {_hubConnection?.State}");
                }
            }
            catch (Exception ex)
            {
                _isSignalRConnected = false;
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] ❌ Connection failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SignalR Client] Stack trace: {ex.StackTrace}");
                // Silently fall back to polling
            }
        }

        private async Task DisconnectSignalRAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _isSignalRConnected = false;
                    System.Diagnostics.Debug.WriteLine("[SignalR] Disconnected");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Disconnect error: {ex.Message}");
            }
        }
        
        // ====================
        // Popup Notification
        // ====================
        
        private void ShowNotificationPopup(string title, string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Popup] ============ CREATING POPUP ============");
            System.Diagnostics.Debug.WriteLine($"[Popup] Title: {title}");
            System.Diagnostics.Debug.WriteLine($"[Popup] Message: {message}");
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Popup] Creating window...");
                
                // Create a modern notification popup
                var popup = new Window
                {
                    Title = "Benachrichtigung",
                    Width = 400,
                    Height = 150,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    AllowsTransparency = true
                };
                
                // Position at bottom-right of screen
                var workingArea = SystemParameters.WorkArea;
                popup.Left = workingArea.Right - popup.Width - 20;
                popup.Top = workingArea.Bottom - popup.Height - 20;
                
                // Create content
                var border = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Gray,
                        Direction = 270,
                        ShadowDepth = 3,
                        BlurRadius = 10,
                        Opacity = 0.3
                    }
                };
                
                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(20)
                };
                
                var titleBlock = new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                
                var messageBlock = new TextBlock
                {
                    Text = message,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    TextWrapping = TextWrapping.Wrap
                };
                
                stackPanel.Children.Add(titleBlock);
                stackPanel.Children.Add(messageBlock);
                border.Child = stackPanel;
                popup.Content = border;
                
                // Auto-close after 4 seconds
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    popup.Close();
                };
                timer.Start();
                
                // Close on click
                popup.MouseDown += (s, e) => popup.Close();
                
                System.Diagnostics.Debug.WriteLine($"[Popup] About to show window...");
                popup.Show();
                System.Diagnostics.Debug.WriteLine($"[Popup] ✅ Window shown successfully!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Popup] ❌ ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Popup] Stack: {ex.StackTrace}");
                
                // Show simple MessageBox as fallback
                MessageBox.Show($"Neue Benachrichtigung empfangen!\n\n{message}", title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string Type { get; set; } = "Info";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        
        // Employee and Project Info
        public string? MitarbeiterName { get; set; }
        public int? MitarbeiterId { get; set; }
        public string? ObjektName { get; set; }
        public int? ObjektId { get; set; }
        
        // UI Properties
        public string CreatedAtString => CreatedAt.ToString("dd.MM.yyyy HH:mm");
        public string TypeIcon => Type switch
        {
            "Success" => "✓",
            "Warning" => "⚠",
            "Error" => "✕",
            _ => "ℹ"
        };
        public Visibility IsUnreadBadgeVisible => IsRead ? Visibility.Collapsed : Visibility.Visible;
        public Visibility MarkAsReadButtonVisibility => IsRead ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasEmployeeInfo => !string.IsNullOrEmpty(MitarbeiterName) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HasObjektInfo => !string.IsNullOrEmpty(ObjektName) ? Visibility.Visible : Visibility.Collapsed;
    }

    public class BenachrichtigungDto
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
