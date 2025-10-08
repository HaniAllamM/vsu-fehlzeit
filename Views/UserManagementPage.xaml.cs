using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class UserManagementPage : UserControl
    {
        private readonly AuthService _authService;
        private UserService? _userService;
        private List<UserDto> _allUsers = new();
        private List<UserDto> _filteredUsers = new();
        private bool _isInitialized = false;
        
        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 4;
        private int _totalPages = 1;

        public UserManagementPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            Loaded += UserManagementPage_Loaded;
        }

        private async void UserManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Only initialize once - preserves state when navigating back
            if (_isInitialized)
                return;
                
            _isInitialized = true;
            await InitializeServicesAsync();
            await LoadUsersAsync();
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                _userService = new UserService(_authService, configService);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Services: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            if (_userService == null)
            {
                MessageBox.Show("Service nicht initialisiert.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[UserManagementPage] Calling GetAllUsersAsync...");
                var response = await _userService.GetAllUsersAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserManagementPage] Response: Success={response.Success}, Message={response.Message}");
                
                if (response.Success && response.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPage] Loaded {response.Data.Count} users");
                    
                    // Debug each user's data
                    foreach (var user in response.Data)
                    {
                        System.Diagnostics.Debug.WriteLine($"  User {user.Id}: {user.Username}, IsActive={user.IsActive}, IsAdmin={user.IsAdmin}");
                    }
                    
                    _allUsers = response.Data;
                    ApplyFilters();
                    UpdateStatistics();
                }
                else
                {
                    var errors = response.Errors != null && response.Errors.Count > 0 
                        ? string.Join("\n", response.Errors) 
                        : "Keine Details verfügbar";
                    var errorMsg = $"Fehler beim Laden der Benutzer:\n\n{response.Message}\n\nDetails:\n{errors}";
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPage] ERROR: {errorMsg}");
                    MessageBox.Show(errorMsg, 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Fehler beim Laden der Benutzer:\n\n{ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack: {ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine($"[UserManagementPage] EXCEPTION: {errorMsg}");
                MessageBox.Show(errorMsg, 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            // Null checks for UI controls
            if (TxtSearch == null || ChkActiveOnly == null || UsersDataGrid == null)
            {
                System.Diagnostics.Debug.WriteLine("[ApplyFilters] UI controls not ready yet");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Starting with {_allUsers.Count} users");
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] ChkActiveOnly.IsChecked = {ChkActiveOnly.IsChecked}");
            
            var filtered = _allUsers.AsEnumerable();

            // Search filter
            var searchTerm = TxtSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(u => 
                    u.Username.ToLower().Contains(searchTerm) ||
                    (u.Email?.ToLower().Contains(searchTerm) ?? false) ||
                    u.DisplayName.ToLower().Contains(searchTerm));
                System.Diagnostics.Debug.WriteLine($"[ApplyFilters] After search filter: {filtered.Count()} users");
            }

            // Active only filter
            if (ChkActiveOnly.IsChecked == true)
            {
                var beforeCount = filtered.Count();
                filtered = filtered.Where(u => u.IsActive);
                var afterCount = filtered.Count();
                System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Active filter: {beforeCount} -> {afterCount} users");
                
                // Debug each user's IsActive status
                foreach (var user in _allUsers)
                {
                    System.Diagnostics.Debug.WriteLine($"  User {user.Id} ({user.Username}): IsActive = {user.IsActive}");
                }
            }

            _filteredUsers = filtered.ToList();
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Final filtered count: {_filteredUsers.Count}");
            
            // Reset to first page when filters change
            _currentPage = 1;
            UpdatePagedData();
        }
        
        private void UpdatePagedData()
        {
            // Calculate pagination
            _totalPages = (int)Math.Ceiling((double)_filteredUsers.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;
            
            if (_currentPage > _totalPages)
                _currentPage = _totalPages;
            if (_currentPage < 1)
                _currentPage = 1;
            
            // Get page data
            var pageData = _filteredUsers
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            UsersDataGrid.ItemsSource = pageData;
            UsersDataGrid.Items.Refresh();
            UpdatePaginationUI();
        }
        
        private void UpdatePaginationUI()
        {
            if (PageInfoText != null && BtnPrevPage != null && BtnNextPage != null)
            {
                PageInfoText.Text = $"Seite {_currentPage} von {_totalPages}";
                BtnPrevPage.IsEnabled = _currentPage > 1;
                BtnNextPage.IsEnabled = _currentPage < _totalPages;
            }
        }
        
        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagedData();
            }
        }
        
        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdatePagedData();
            }
        }

        private void UpdateStatistics()
        {
            // Null checks for UI controls
            if (TxtTotalUsers == null || TxtActiveUsers == null || TxtAdminUsers == null || TxtLockedUsers == null)
                return;

            TxtTotalUsers.Text = _allUsers.Count.ToString();
            TxtActiveUsers.Text = _allUsers.Count(u => u.IsActive).ToString();
            TxtAdminUsers.Text = _allUsers.Count(u => u.IsAdmin).ToString();
            TxtLockedUsers.Text = _allUsers.Count(u => u.FailedLoginAttempts >= 5).ToString();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ChkActiveOnly_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateEditUserDialog(null);
            if (dialog.ShowDialog() == true)
            {
                var request = dialog.UserRequest;
                
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPage] Creating user: {request.Username}");
                    var response = await _userService!.CreateUserAsync(request);
                    
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPage] Create response: Success={response.Success}, Message={response.Message}");
                    
                    if (response.Success && response.Data != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UserManagementPage] User created with ID: {response.Data.UserId}");
                        
                        // Show temporary password
                        var tempPassword = response.Data.TemporaryPassword;
                        var message = $"Benutzer erfolgreich erstellt!\n\n" +
                                    $"Temporäres Passwort:\n{tempPassword}\n\n" +
                                    $"⚠️ Bitte notieren Sie dieses Passwort und geben Sie es dem Benutzer.\n" +
                                    $"Der Benutzer muss das Passwort bei der ersten Anmeldung ändern.";
                        
                        MessageBox.Show(message, "Benutzer erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        await LoadUsersAsync();
                    }
                    else
                    {
                        var errors = response.Errors != null && response.Errors.Count > 0 
                            ? string.Join("\n", response.Errors) 
                            : "Keine Details verfügbar";
                        var errorMsg = $"Fehler beim Erstellen:\n\n{response.Message}\n\nDetails:\n{errors}";
                        System.Diagnostics.Debug.WriteLine($"[UserManagementPage] ERROR: {errorMsg}");
                        MessageBox.Show(errorMsg, 
                                      "Fehler", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Fehler: {ex.Message}\n\nStack: {ex.StackTrace}";
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPage] EXCEPTION: {errorMsg}");
                    MessageBox.Show(errorMsg, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDto user)
            {
                var dialog = new CreateEditUserDialog(user);
                if (dialog.ShowDialog() == true)
                {
                    var request = new UpdateUserRequest
                    {
                        Email = dialog.UserRequest.Email,
                        FirstName = dialog.UserRequest.FirstName,
                        LastName = dialog.UserRequest.LastName,
                        IsActive = null // Will be set separately if changed
                    };

                    try
                    {
                        var response = await _userService!.UpdateUserAsync(user.Id, request);
                        
                        if (response.Success)
                        {
                            MessageBox.Show("Benutzer erfolgreich aktualisiert!", 
                                          "Erfolg", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Information);
                            await LoadUsersAsync();
                        }
                        else
                        {
                            MessageBox.Show($"Fehler beim Aktualisieren: {response.Message}", 
                                          "Fehler", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDto user)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie das Passwort für Benutzer \"{user.Username}\" zurücksetzen?\n\n" +
                    $"Ein neues temporäres Passwort wird generiert. Der Benutzer muss es bei der nächsten Anmeldung ändern.",
                    "Passwort zurücksetzen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _userService!.ResetPasswordAsync(user.Id);
                        
                        if (response.Success && response.Data != null)
                        {
                            var tempPassword = response.Data.NewPassword;
                            var message = $"Passwort erfolgreich zurückgesetzt!\n\n" +
                                        $"Neues temporäres Passwort:\n{tempPassword}\n\n" +
                                        $"⚠️ Bitte notieren Sie dieses Passwort und geben Sie es dem Benutzer.";
                            
                            MessageBox.Show(message, "Passwort zurückgesetzt", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Fehler beim Zurücksetzen: {response.Message}", 
                                          "Fehler", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is UserDto user)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Benutzer \"{user.Username}\" wirklich löschen?\n\n" +
                    $"ℹ️ Wenn der Benutzer zugeordnete Daten hat, wird er deaktiviert statt gelöscht.",
                    "Benutzer löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _userService!.DeleteUserAsync(user.Id);
                        
                        if (response.Success)
                        {
                            MessageBox.Show("Benutzer erfolgreich gelöscht!", 
                                          "Erfolg", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Information);
                            await LoadUsersAsync();
                        }
                        else
                        {
                            MessageBox.Show($"Fehler beim Löschen: {response.Message}", 
                                          "Fehler", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
