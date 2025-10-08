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
using FehlzeitApp.Services;

namespace FehlzeitApp.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AuthService _authService;
    
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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize immediately without NavigationManager for fastest startup
        InitializeMainContentDirectly();
        
        // Initialize NavigationManager later for navigation functionality
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var navigationFrame = new Frame();
            MainContentArea.Children.Clear();
            MainContentArea.Children.Add(navigationFrame);
            NavigationManager.Instance.Initialize(navigationFrame);
            LoadDashboard();
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
    
    private void InitializeMainContentDirectly()
    {
        try
        {
            // Clear loading state and show dashboard immediately
            MainContentArea.Children.Clear();
            
            // Create dashboard content directly without NavigationManager
            var dashboardText = new TextBlock
            {
                Text = "Dashboard\n\nWillkommen im Fehlzeit Manager!",
                FontSize = 18,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };
            
            MainContentArea.Children.Add(dashboardText);
            UpdatePageTitle("Dashboard");
        }
        catch (Exception ex)
        {
            // Fallback to simple text if anything fails
            System.Diagnostics.Debug.WriteLine($"Error in InitializeMainContentDirectly: {ex.Message}");
            var errorText = new TextBlock
            {
                Text = "Lade...",
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

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        LoadSettingsView();
        UpdatePageTitle("Einstellungen");
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?", 
                                   "Abmelden", 
                                   MessageBoxButton.YesNo, 
                                   MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            // Clear all cached pages before logout
            NavigationManager.Instance.ClearCache();
            
            // Logout from auth service
            _authService.Logout();
            
            // Show login window
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            
            // Close this window
            this.Close();
        }
    }


    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Clean up resources - NavigationManager handles page cleanup
        NavigationManager.Instance.ClearCache();

        // Stop any running timers or background tasks if needed
        // This is where you would clean up any resources used by the pages
    }

    #endregion

    #region View Loading Methods

    private void LoadDashboard()
    {
        try
        {
            // Create a minimal dashboard quickly
            var frame = MainContentArea.Children.OfType<Frame>().FirstOrDefault();
            if (frame != null)
            {
                // Create simple content without complex UserControl
                var text = new TextBlock
                {
                    Text = "Dashboard\n\nWillkommen im Fehlzeit Manager!",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                
                frame.Content = text;
                UpdatePageTitle("Dashboard");
            }
        }
        catch (Exception ex)
        {
            // Simplified error handling
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            UpdatePageTitle("Fehler beim Laden");
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
            var notificationPage = new NotificationPage();
            
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
}