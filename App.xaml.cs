using System.Configuration;
using System.Data;
using System.Windows;
using FehlzeitApp.Services;
using FehlzeitApp.Views;

namespace FehlzeitApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Shared AuthService instance to avoid recreating on every login
    /// </summary>
    public static AuthService? SharedAuthService { get; set; }

    /// <summary>
    /// Shared ConfigurationService instance to avoid recreating for every service
    /// </summary>
    public static ConfigurationService? SharedConfigService { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Preload ConfigurationService first
            SharedConfigService = ConfigurationService.CreateSync();
            
            // Preload AuthService using shared config
            SharedAuthService = new AuthService(SharedConfigService);
        }
        catch (System.Exception ex)
        {
            // If AuthService fails to initialize, continue anyway
            // LoginWindow will handle the fallback
            System.Diagnostics.Debug.WriteLine($"Failed to preload AuthService: {ex.Message}");
        }

        // Show login window immediately
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }
}

