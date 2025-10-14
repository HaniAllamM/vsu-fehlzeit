using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using FehlzeitApp.Services;
using FehlzeitApp.Views;
using Velopack;

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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize Velopack
        VelopackApp.Build().Run();

        // Add global exception handler
        System.AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Unhandled exception: {ex.ExceptionObject}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Dispatcher exception: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Check for updates before starting the app
        _ = Task.Run(async () =>
        {
            try
            {
                // Set the update feed URL to GitHub releases
                var updateUrl = "https://github.com/HaniAllamM/vsu-fehlzeit/releases/latest/download/";
                var mgr = new UpdateManager(updateUrl);
                var updateInfo = await mgr.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    // Log the update availability
                    File.AppendAllText("update.log", $"[{DateTime.Now}] Update available: {updateInfo.TargetFullRelease.Version}\n");
                    
                    // Show a brief notification that update is starting (non-blocking)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Show a toast-like notification for 3 seconds
                        var notification = new System.Windows.Window
                        {
                            Title = "FehlzeitApp Update",
                            Content = new System.Windows.Controls.TextBlock
                            {
                                Text = $"Updating to version {updateInfo.TargetFullRelease.Version}...",
                                Padding = new System.Windows.Thickness(20),
                                FontSize = 14
                            },
                            Width = 300,
                            Height = 100,
                            WindowStyle = System.Windows.WindowStyle.ToolWindow,
                            ResizeMode = System.Windows.ResizeMode.NoResize,
                            Topmost = true,
                            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                        };
                        notification.Show();
                        
                        // Auto-close after 3 seconds
                        var timer = new System.Windows.Threading.DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(3);
                        timer.Tick += (s, e) => { notification.Close(); timer.Stop(); };
                        timer.Start();
                    });
                    
                    // Automatically apply update without asking user
                    File.AppendAllText("update.log", $"[{DateTime.Now}] Starting automatic update to version {updateInfo.TargetFullRelease.Version}\n");
                    mgr.ApplyUpdatesAndRestart(updateInfo);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't block app startup
                File.AppendAllText("update.log", $"[{DateTime.Now}] Update check failed: {ex.Message}\n");
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        });

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
            MessageBox.Show($"Warning: Failed to preload services: {ex.Message}\nThe app will continue with limited functionality.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        try
        {
            // Show login window immediately
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Failed to show login window: {ex.Message}\n\n{ex.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Clean up shared services
        try
        {
            SharedAuthService?.Dispose();
            SharedAuthService = null;
            SharedConfigService = null;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }

        base.OnExit(e);
    }
}

