using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class LoginWindow : Window
    {
        private AuthService? _authService;
        private static readonly string LogSource = "LoginWindow";
        private static DateTime _lastLoginAttempt = DateTime.MinValue;
        private static int _failedAttempts = 0;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public LoginWindow()
        {
            InitializeComponent();
            
            // Set focus to username field and create auth service
            Loaded += Window_Loaded;
            
            // Handle Enter key press
            KeyDown += LoginWindow_KeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Use preloaded AuthService if available, otherwise create new one synchronously
            if (App.SharedAuthService != null)
            {
                _authService = App.SharedAuthService;
            }
            else
            {
                // Fallback: create new AuthService synchronously if preloading failed
                _authService = AuthService.CreateSync();
            }
            
            TxtUsername.Focus();
        }

        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Check for rate limiting (production security)
            if (IsTemporarilyLocked())
            {
                var remainingMinutes = LockoutMinutes - (int)(DateTime.Now - _lastLoginAttempt).TotalMinutes;
                ShowError($"Zu viele fehlgeschlagene Anmeldeversuche. Bitte versuchen Sie es in {remainingMinutes} Minuten erneut.");
                LogSecurityEvent($"Login attempt during lockout period", TxtUsername.Text?.Trim());
                return;
            }

            // Validate input
            if (_authService == null)
            {
                ShowError("Authentifizierungsdienst wird noch initialisiert. Bitte warten Sie einen Moment.");
                return;
            }

            // Sanitize and validate username
            var username = SanitizeInput(TxtUsername.Text);
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Bitte geben Sie einen gültigen Benutzernamen ein.");
                TxtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(TxtPassword.Password))
            {
                ShowError("Bitte geben Sie ein Passwort ein.");
                TxtPassword.Focus();
                return;
            }

            // Show loading state
            SetLoadingState(true);
            HideError();

            try
            {
                // For testing - bypass API call to isolate the delay
                // TODO: Remove this bypass after identifying the bottleneck
                LoginResponse result;
                
                if (username == "test" && TxtPassword.Password == "test")
                {
                    // Mock successful login for testing
                    result = new LoginResponse 
                    { 
                        Success = true, 
                        Token = "mock-token",
                        User = new User 
                        { 
                            UserId = 1, 
                            Username = username, 
                            Role = "Admin",
                            FirstName = "Test",
                            LastName = "User"
                        }
                    };
                    
                    // Set the current user in AuthService
                    _authService.GetType().GetProperty("CurrentUser")?.SetValue(_authService, result.User);
                    _authService.GetType().GetProperty("Token")?.SetValue(_authService, result.Token);
                }
                else
                {
                    // Try Web API login
                    var loginRequest = new LoginRequest
                    {
                        Username = username,
                        Password = TxtPassword.Password,
                        RememberMe = false // Removed remember me functionality
                    };

                    result = await _authService.LoginAsync(loginRequest);
                }

                if (result.Success)
                {
                    // Reset failed attempts on successful login
                    _failedAttempts = 0;
                    
                    // Login successful - open main window immediately
                    BtnLogin.Content = "✓ Erfolgreich - Öffne Anwendung...";
                    BtnLogin.IsEnabled = false;
                    
                    // Show MainWindow immediately with high priority
                    _ = Dispatcher.BeginInvoke(() =>
                    {
                        var mainWindow = new MainWindow(_authService);
                        mainWindow.Show();
                        this.Close();
                    }, System.Windows.Threading.DispatcherPriority.Send);
                    return;
                }
                else
                {
                    // Web API login failed, try offline fallback
                    try
                    {
                        bool offlineSuccess = await _authService.LoginOfflineAsync(username, TxtPassword.Password);
                        
                        if (offlineSuccess)
                        {
                            // Reset failed attempts on successful login
                            _failedAttempts = 0;
                            
                            // Offline login successful - open main window immediately
                            BtnLogin.Content = "✓ Erfolgreich - Öffne Anwendung...";
                            BtnLogin.IsEnabled = false;
                            
                            // Show MainWindow immediately with high priority
                            _ = Dispatcher.BeginInvoke(() =>
                            {
                                var mainWindow = new MainWindow(_authService);
                                mainWindow.Show();
                                this.Close();
                            }, System.Windows.Threading.DispatcherPriority.Send);
                            return;
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ShowError(ex.Message);
                        return;
                    }
                    
                    // Both Web API and offline failed - track failure
                    TrackFailedAttempt(username);
                    ShowError(result.Message.Contains("Web API not available") || result.Message.Contains("Connection") 
                        ? "Ungültige Anmeldedaten oder Server nicht erreichbar." 
                        : "Ungültige Anmeldedaten. Bitte überprüfen Sie Benutzername und Passwort.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Track failed attempt and log security event
                TrackFailedAttempt(username);
                LogError("Unauthorized access during login", ex);
                ShowError("Ungültige Anmeldedaten. Bitte überprüfen Sie Benutzername und Passwort.");
            }
            catch (HttpRequestException ex)
            {
                // Log connection issues for monitoring
                LogError("Connection error during login", ex);
                ShowError("Verbindungsfehler: Kann keine Verbindung zum Server herstellen. Bitte prüfen Sie Ihre Internetverbindung oder versuchen Sie es später erneut.");
            }
            catch (Exception ex)
            {
                // Log unexpected errors for debugging
                LogError("Unexpected error during login", ex);
                ShowError("Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es erneut oder kontaktieren Sie den Administrator.");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
        }

        private void SetLoadingState(bool isLoading)
        {
            BtnLogin.IsEnabled = !isLoading;
            TxtUsername.IsEnabled = !isLoading;
            TxtPassword.IsEnabled = !isLoading;
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

            if (isLoading)
            {
                BtnLogin.Content = "Wird angemeldet...";
            }
            else
            {
                BtnLogin.Content = "Anmelden";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _authService?.Dispose();
            base.OnClosed(e);
        }

        #region Production Security Methods

        /// <summary>
        /// Checks if login attempts are temporarily locked due to too many failures
        /// </summary>
        private static bool IsTemporarilyLocked()
        {
            if (_failedAttempts >= MaxFailedAttempts)
            {
                var timeSinceLastAttempt = DateTime.Now - _lastLoginAttempt;
                return timeSinceLastAttempt.TotalMinutes < LockoutMinutes;
            }
            return false;
        }

        /// <summary>
        /// Tracks failed login attempts for rate limiting
        /// </summary>
        private static void TrackFailedAttempt(string username)
        {
            _failedAttempts++;
            _lastLoginAttempt = DateTime.Now;
            
            LogSecurityEvent($"Failed login attempt ({_failedAttempts}/{MaxFailedAttempts})", username);
            
            if (_failedAttempts >= MaxFailedAttempts)
            {
                LogSecurityEvent($"Account temporarily locked due to {MaxFailedAttempts} failed attempts", username);
            }
        }

        /// <summary>
        /// Sanitizes user input to prevent injection attacks
        /// </summary>
        private static string SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
                
            // Remove dangerous characters and trim whitespace
            return input.Trim()
                       .Replace("<", "")
                       .Replace(">", "")
                       .Replace("'", "")
                       .Replace("\"", "")
                       .Replace(";", "")
                       .Replace("--", "")
                       .Replace("/*", "")
                       .Replace("*/", "");
        }

        #endregion

        #region Production Logging Methods

        /// <summary>
        /// Logs security events for monitoring and auditing
        /// </summary>
        private static void LogSecurityEvent(string message, string? username = null)
        {
            try
            {
                var logMessage = $"[LOGIN_SECURITY] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";
                if (!string.IsNullOrEmpty(username))
                {
                    logMessage += $" - User: {username}";
                }
                
                // In production, replace with proper logging framework
                System.Diagnostics.EventLog.WriteEntry("FehlzeitApp", logMessage, System.Diagnostics.EventLogEntryType.Warning);
            }
            catch
            {
                // Fail silently to prevent logging errors from affecting user experience
            }
        }

        /// <summary>
        /// Logs errors for debugging and monitoring
        /// </summary>
        private static void LogError(string message, Exception? exception = null)
        {
            try
            {
                var logMessage = $"[ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";
                if (exception != null)
                {
                    logMessage += $" - Exception: {exception.GetType().Name}: {exception.Message}";
                }
                
                // TODO: Replace with proper logging framework
                System.Diagnostics.EventLog.WriteEntry(LogSource, logMessage, System.Diagnostics.EventLogEntryType.Error);
            }
            catch
            {
                // Fail silently to prevent logging errors from affecting user experience
            }
        }

        #endregion
    }
}
