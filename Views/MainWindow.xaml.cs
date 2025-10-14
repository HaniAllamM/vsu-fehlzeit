using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FehlzeitApp.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace FehlzeitApp.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AuthService _authService;
    private HubConnection? _hubConnection;
    private BenachrichtigungService? _benachrichtigungService;
    // private UpdateService? _updateService; // Temporarily disabled
    private bool _isLoggingOut = false;
    
    // Note: Individual page references are no longer needed - NavigationManager handles caching
    // private ObjektPage? _objektPage;
    // private KrankheitPage? _krankheitPage;
    // private MeldungPage? _meldungPage;
    // private UnterlagePage? _unterlagePage;
    // private MitarbeiterPage? _mitarbeiterPage;
    // private Fehlzeit? _fehlzeitPage;

    public MainWindow(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        // Show loading state immediately
        ShowLoadingState();
        
        // Update UI with user info immediately (lightweight)
        UpdateUserInfo();
        
        // Add cleanup on window closing
        Closing += MainWindow_Closing;
        
        // Defer ALL heavy initialization to after window is shown
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize NavigationManager immediately
        InitializeNavigationManager();
        
        // Initialize services and SignalR
        await InitializeServicesAsync();
        await InitializeSignalRAsync();
        await LoadUnreadCountAsync();

        // Initialize update service and check for updates
        // await InitializeUpdateServiceAsync(); // Temporarily disabled
        
        // Load dashboard after everything is initialized
        LoadDashboard();
    }
    
    private void InitializeNavigationManager()
    {
        try
        {
            var navigationFrame = new Frame();
            MainContentArea.Children.Clear();
            MainContentArea.Children.Add(navigationFrame);
            NavigationManager.Instance.Initialize(navigationFrame);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize NavigationManager: {ex.Message}");
            // Fallback to simple content
            var errorText = new TextBlock
            {
                Text = "Navigation konnte nicht initialisiert werden",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            MainContentArea.Children.Clear();
            MainContentArea.Children.Add(errorText);
        }
    }
    

    private void UpdateUserInfo()
    {
        if (_authService.CurrentUser != null)
        {
            // Update the user info in the sidebar - we'll need to find the TextBlock and update it
            // For now, we'll update the window title
            Title = $"Hama Fehlzeit - {_authService.CurrentUser.FullName} ({_authService.CurrentUser.Role})";
        }
    }

    #region Navigation Event Handlers

    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
    {
        LoadDashboard();
        UpdatePageTitle("Dashboard");
    }

    private void BtnMitarbeiter_Click(object sender, RoutedEventArgs e)
    {
        LoadMitarbeiterView();
        UpdatePageTitle("Mitarbeiter");
    }

    private void BtnFehlzeiten_Click(object sender, RoutedEventArgs e)
    {
        LoadFehlzeitenView();
        UpdatePageTitle("Fehlzeiten");
    }

    private void BtnKrankheiten_Click(object sender, RoutedEventArgs e)
    {
        LoadKrankheitenView();
        UpdatePageTitle("Krankheiten");
    }

    private void BtnMeldungen_Click(object sender, RoutedEventArgs e)
    {
        LoadMeldungenView();
        UpdatePageTitle("Meldungen");
    }

    private void BtnObjekte_Click(object sender, RoutedEventArgs e)
    {
        LoadObjekteView();
        UpdatePageTitle("Objekte");

        // Note: NavigationManager handles page caching and refresh
        // The ObjektPage will handle its own refresh in its Loaded event
    }

    private void BtnUnterlagen_Click(object sender, RoutedEventArgs e)
    {
        LoadUnterlagenView();
        UpdatePageTitle("Unterlagen");
    }

    private void BtnUserObjekt_Click(object sender, RoutedEventArgs e)
    {
        LoadUserObjektView();
        UpdatePageTitle("Benutzer-Projekte");
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        LoadSettingsView();
        UpdatePageTitle("Einstellungen");
    }

    private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_authService?.CurrentUser == null)
            {
                MessageBox.Show("Sie sind nicht angemeldet.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create UserService to handle password change
            var configService = await ConfigurationService.CreateAsync();
            var userService = new UserService(_authService, configService);

            var dialog = new ChangePasswordDialog(userService, _authService.CurrentUser);
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Öffnen des Passwort-Dialogs: {ex.Message}", 
                          "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?", 
                                   "Abmelden", 
                                   MessageBoxButton.YesNo, 
                                   MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            // Set logout flag to prevent app shutdown
            _isLoggingOut = true;
            
            // Clear all cached pages before logout
            NavigationManager.Instance.ClearCache();
            
            // Logout from auth service and dispose it
            _authService.Logout();
            _authService.Dispose();
            
            // Clear the shared auth service reference to force fresh login
            App.SharedAuthService = null;
            
            // Show login window (it will create a fresh AuthService)
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            
            // Close this window (but don't shutdown the app)
            this.Close();
        }
    }


    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Clean up SignalR connection
        try
        {
            _hubConnection?.StopAsync().Wait();
            _hubConnection?.DisposeAsync().AsTask().Wait();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disconnect SignalR: {ex.Message}");
        }
        
        // Clean up resources - NavigationManager handles page cleanup
        NavigationManager.Instance.ClearCache();

        // Clean up BenachrichtigungService
        try
        {
            _benachrichtigungService = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to cleanup BenachrichtigungService: {ex.Message}");
        }

        // Only shutdown the application if NOT logging out
        // When logging out, we want to return to login screen, not exit the app
        if (!_isLoggingOut)
        {
            // Clean up AuthService only on full exit
            try
            {
                _authService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to dispose AuthService: {ex.Message}");
            }
            
            // Force application shutdown only when closing (not logout)
            Application.Current.Shutdown();
        }
    }

    #endregion

    #region View Loading Methods

    private void LoadDashboard()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<DashboardPage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden des Dashboards: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadMitarbeiterView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<MitarbeiterPage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Mitarbeiter-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadFehlzeitenView()
    {
        try
        {
            // NavigationManager will cache the Fehlzeit page and preserve its state
            NavigationManager.Instance.NavigateTo<Fehlzeit>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Fehlzeiten-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadKrankheitenView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<KrankheitPage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Krankheiten-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadMeldungenView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<MeldungPage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Meldungen-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadObjekteView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<ObjektPage>(_authService);
            
            // Note: The ObjektPage will handle its own refresh in its Loaded event
            // The NavigationManager preserves the page state between navigations
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Objekte-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadUnterlagenView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<UnterlagePage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Unterlagen-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadUserObjektView()
    {
        try
        {
            NavigationManager.Instance.NavigateTo<UserObjektPage>(_authService);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Benutzer-Projekte-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void LoadSettingsView()
    {
        try
        {
            // Create the actual EinstellungenPage
            var einstellungenPage = new EinstellungenPage(_authService);
            
            // Load it into the main content area
            var frame = MainContentArea.Children.OfType<Frame>().FirstOrDefault();
            if (frame != null)
            {
                frame.Content = einstellungenPage;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Einstellungen: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void BtnNotificationsPage_Click(object sender, RoutedEventArgs e)
    {
        LoadNotificationsView();
        UpdatePageTitle("Notifications - Hard Data");
    }

    private void LoadNotificationsView()
    {
        try
        {
            var notificationPage = new NotificationPage(_authService);
            
            var frame = MainContentArea.Children.OfType<Frame>().FirstOrDefault();
            if (frame != null)
            {
                frame.Content = notificationPage;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Laden der Notifications-Seite: {ex.Message}", 
                          "Fehler", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Shows a loading state immediately when MainWindow is created
    /// </summary>
    private void ShowLoadingState()
    {
        try
        {
            var loadingText = new TextBlock
            {
                Text = "Lade Anwendung...",
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };
            
            MainContentArea.Children.Clear();
            MainContentArea.Children.Add(loadingText);
        }
        catch (Exception ex)
        {
            // Fail silently to prevent loading state from blocking window display
            System.Diagnostics.Debug.WriteLine($"Failed to show loading state: {ex.Message}");
        }
    }

    // Note: ClearMainContent is no longer needed - NavigationManager handles content switching
    // private void ClearMainContent() - REMOVED
    // The NavigationManager automatically handles switching between cached pages

    private void UpdatePageTitle(string title)
    {
        TxtPageTitle.Text = title;
    }

    #endregion
    
    #region Real-Time Notifications with SignalR
    
    private async Task InitializeServicesAsync()
    {
        try
        {
            var configService = await ConfigurationService.CreateAsync();
            _benachrichtigungService = new BenachrichtigungService(_authService, configService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize notification service: {ex.Message}");
        }
    }
    
    private async Task InitializeSignalRAsync()
    {
        try
        {
            var configService = await ConfigurationService.CreateAsync();
            var baseUrl = configService.ApiSettings.BaseUrl;
            var hubUrl = $"{baseUrl}/notificationHub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_authService.Token);
                })
                .WithAutomaticReconnect()
                .Build();

            // Handle incoming notifications
            _hubConnection.On<object>("ReceiveNotification", async (notification) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR MainWindow] ============ NOTIFICATION RECEIVED ============");
                System.Diagnostics.Debug.WriteLine($"[SignalR MainWindow] Notification data: {notification}");
                
                // Update badge and show popup on UI thread
                await Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        // Show popup notification
                        ShowNotificationPopup("📬 Neue Benachrichtigung", "Eine neue Benachrichtigung ist eingetroffen!");
                        
                        // Update unread count badge
                        await LoadUnreadCountAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SignalR MainWindow] Error: {ex.Message}");
                        MessageBox.Show($"Neue Benachrichtigung empfangen!", "Benachrichtigung", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            });

            await _hubConnection.StartAsync();
            System.Diagnostics.Debug.WriteLine("[SignalR MainWindow] Connected successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR MainWindow] Connection failed: {ex.Message}");
        }
    }
    
    private async Task LoadUnreadCountAsync()
    {
        try
        {
            if (_benachrichtigungService == null) return;
            
            var response = await _benachrichtigungService.GetUnreadCountAsync();
            
            if (response.Success && response.Data > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                TxtNotificationCount.Text = response.Data.ToString();
            }
            else
            {
                NotificationBadge.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load unread count: {ex.Message}");
        }
    }
    
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
    
    #endregion

    #region Update Management

    // Update functionality temporarily disabled
    /*
    private async Task InitializeUpdateServiceAsync()
    {
        try
        {
            _updateService = new UpdateService();
            await CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize update service: {ex.Message}");
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            if (_updateService == null) return;

            var updateAvailable = await _updateService.CheckForUpdatesAsync();

            if (updateAvailable)
            {
                UpdateBadge.Visibility = Visibility.Visible;
                TxtUpdateBadge.Text = "!";
            }
            else
            {
                UpdateBadge.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to check for updates: {ex.Message}");
        }
    }
    */

    // Placeholder method for update button (temporarily disabled)
    private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Update functionality is temporarily disabled.", 
                        "Information", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
    }

    #endregion
}