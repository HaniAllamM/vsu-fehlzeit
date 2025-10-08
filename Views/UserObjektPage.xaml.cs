using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class UserObjektPage : UserControl
    {
        private readonly AuthService _authService;
        private UserObjektService? _userObjektService;
        private UserService? _userService;
        private ObjektService? _objektService;
        
        private List<UserObjektAssignment> _allAssignments = new();
        private List<UserDto> _allUsers = new();
        private List<Objekt> _allObjekts = new();
        private bool _isInitialized = false;
        
        // State preservation - remember last selections
        private int? _lastSelectedUserId = null;
        private int? _lastSelectedObjektId = null;
        private bool _isShowByObjektMode = false;
        
        // Animation
        private Storyboard? _loadingStoryboard;

        public UserObjektPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            Loaded += UserObjektPage_Loaded;
        }

        private async void UserObjektPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Only initialize once - this preserves state when navigating back
            if (_isInitialized)
                return;

            _isInitialized = true;
            await InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            // Check admin permission first
            if (!CheckAdminPermission())
            {
                ShowAccessDenied();
                return;
            }
            
            ShowLoading("Initialisiere...");
            
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                _userObjektService = new UserObjektService(_authService, configService);
                _userService = new UserService(_authService, configService);
                _objektService = new ObjektService(_authService, configService);

                await LoadBasicDataAsync();
            }
            catch (Exception ex)
            {
                HideLoading();
                MessageBox.Show($"Fehler beim Initialisieren: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                HideLoading();
            }
        }
        
        private bool CheckAdminPermission()
        {
            var currentUser = _authService.CurrentUser;
            return currentUser?.Role == "Admin";
        }
        
        private void ShowAccessDenied()
        {
            MainContent.Visibility = Visibility.Collapsed;
            AccessDeniedPanel.Visibility = Visibility.Visible;
        }

        private void ShowLoading(string message = "Lädt...")
        {
            LoadingText.Text = message;
            LoadingOverlay.Visibility = Visibility.Visible;
            MainContent.IsEnabled = false;
            
            // Start rotation animation
            if (_loadingStoryboard == null)
            {
                _loadingStoryboard = new Storyboard();
                var rotationAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(2),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                
                Storyboard.SetTarget(rotationAnimation, LoadingRotation);
                Storyboard.SetTargetProperty(rotationAnimation, new PropertyPath("Angle"));
                _loadingStoryboard.Children.Add(rotationAnimation);
            }
            
            _loadingStoryboard.Begin();
        }

        private void HideLoading()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            MainContent.IsEnabled = true;
            
            // Stop rotation animation
            _loadingStoryboard?.Stop();
        }

        private async Task LoadBasicDataAsync()
        {
            if (_userService == null || _objektService == null)
            {
                MessageBox.Show("Services nicht initialisiert.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                ShowLoading("Lade Benutzer und Projekte...");

                // Load users
                var usersResponse = await _userService.GetAllUsersAsync();
                if (usersResponse.Success && usersResponse.Data != null)
                {
                    _allUsers = usersResponse.Data;
                    CmbUsers.ItemsSource = _allUsers;
                    CmbNewUser.ItemsSource = _allUsers;
                    
                    // Restore last selected user if exists
                    if (_lastSelectedUserId.HasValue)
                    {
                        var lastUser = _allUsers.FirstOrDefault(u => u.Id == _lastSelectedUserId.Value);
                        if (lastUser != null)
                        {
                            CmbUsers.SelectedItem = lastUser;
                        }
                    }
                }
                else
                {
                    ShowErrorMessage("Fehler beim Laden der Benutzer", usersResponse.Message);
                }

                // Load objekts
                var objektsResponse = await _objektService.GetAllAsync();
                if (objektsResponse.Success && objektsResponse.Data != null)
                {
                    _allObjekts = objektsResponse.Data;
                    CmbObjekts.ItemsSource = _allObjekts;
                    CmbNewObjekt.ItemsSource = _allObjekts;
                    
                    // Restore last selected objekt if exists
                    if (_lastSelectedObjektId.HasValue)
                    {
                        var lastObjekt = _allObjekts.FirstOrDefault(o => o.ObjektId == _lastSelectedObjektId.Value);
                        if (lastObjekt != null)
                        {
                            CmbObjekts.SelectedItem = lastObjekt;
                        }
                    }
                }
                else
                {
                    ShowErrorMessage("Fehler beim Laden der Projekte", objektsResponse.Message);
                }

                // Restore filter mode
                if (_isShowByObjektMode)
                {
                    RbShowByObjekt.IsChecked = true;
                }
                else
                {
                    RbShowByUser.IsChecked = true;
                }
                
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Fehler beim Laden der Daten", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }
        
        private void UpdateStatistics()
        {
            TxtResultsCount.Text = $"{_allAssignments.Count} Einträge";
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task LoadAssignmentsForUserAsync(UserDto user)
        {
            if (_userObjektService == null)
                return;

            try
            {
                ShowLoading($"Lade Projekte für {user.DisplayName}...");
                
                // Save state
                _lastSelectedUserId = user.Id;
                
                var response = await _userObjektService.GetAssignmentsForUserAsync(user.Id, user.DisplayName);
                
                if (response.Success && response.Data != null)
                {
                    _allAssignments = response.Data;
                    AssignmentsDataGrid.ItemsSource = _allAssignments;
                    TxtResultsHeader.Text = $"Projekte von {user.DisplayName}";
                    
                    // Show/hide columns based on mode
                    ColUserId.Visibility = Visibility.Collapsed;
                    ColUsername.Visibility = Visibility.Collapsed;
                    ColObjektId.Visibility = Visibility.Visible;
                    ColObjektName.Visibility = Visibility.Visible;
                }
                else
                {
                    _allAssignments.Clear();
                    AssignmentsDataGrid.ItemsSource = null;
                    ShowErrorMessage("Fehler beim Laden", response.Message);
                }
                
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _allAssignments.Clear();
                AssignmentsDataGrid.ItemsSource = null;
                ShowErrorMessage("Fehler beim Laden der Zuordnungen", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }
        
        private async Task LoadAssignmentsForObjektAsync(Objekt objekt)
        {
            if (_userObjektService == null)
                return;

            try
            {
                ShowLoading($"Lade Benutzer für {objekt.ObjektName}...");
                
                // Save state
                _lastSelectedObjektId = objekt.ObjektId;
                
                var response = await _userObjektService.GetAssignmentsForObjektAsync(objekt.ObjektId, objekt.ObjektName);
                
                if (response.Success && response.Data != null)
                {
                    _allAssignments = response.Data;
                    AssignmentsDataGrid.ItemsSource = _allAssignments;
                    TxtResultsHeader.Text = $"Benutzer im Projekt {objekt.ObjektName}";
                    
                    // Show/hide columns based on mode
                    ColUserId.Visibility = Visibility.Visible;
                    ColUsername.Visibility = Visibility.Visible;
                    ColObjektId.Visibility = Visibility.Collapsed;
                    ColObjektName.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _allAssignments.Clear();
                    AssignmentsDataGrid.ItemsSource = null;
                    ShowErrorMessage("Fehler beim Laden", response.Message);
                }
                
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                _allAssignments.Clear();
                AssignmentsDataGrid.ItemsSource = null;
                ShowErrorMessage("Fehler beim Laden der Zuordnungen", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isShowByObjektMode)
            {
                if (CmbObjekts.SelectedItem is Objekt selectedObjekt)
                {
                    await LoadAssignmentsForObjektAsync(selectedObjekt);
                }
                else
                {
                    MessageBox.Show("Bitte wählen Sie zuerst ein Projekt aus.", 
                                  "Hinweis", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
            else
            {
                if (CmbUsers.SelectedItem is UserDto selectedUser)
                {
                    await LoadAssignmentsForUserAsync(selectedUser);
                }
                else
                {
                    MessageBox.Show("Bitte wählen Sie zuerst einen Benutzer aus.", 
                                  "Hinweis", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
        }

        private void FilterMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
                return;
                
            _isShowByObjektMode = RbShowByObjekt.IsChecked == true;
            
            // Toggle visibility of filter panels
            if (_isShowByObjektMode)
            {
                ObjektFilterPanel.Visibility = Visibility.Visible;
                UserFilterPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ObjektFilterPanel.Visibility = Visibility.Collapsed;
                UserFilterPanel.Visibility = Visibility.Visible;
            }
            
            // Clear current results
            _allAssignments.Clear();
            AssignmentsDataGrid.ItemsSource = null;
            TxtResultsHeader.Text = "Zuordnungen";
            UpdateStatistics();
        }

        private async void CmbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUsers.SelectedItem is UserDto selectedUser)
            {
                await LoadAssignmentsForUserAsync(selectedUser);
            }
            else
            {
                _allAssignments.Clear();
                AssignmentsDataGrid.ItemsSource = null;
                TxtResultsHeader.Text = "Zuordnungen";
                UpdateStatistics();
            }
        }

        private async void CmbObjekts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbObjekts.SelectedItem is Objekt selectedObjekt)
            {
                await LoadAssignmentsForObjektAsync(selectedObjekt);
            }
            else
            {
                _allAssignments.Clear();
                AssignmentsDataGrid.ItemsSource = null;
                TxtResultsHeader.Text = "Zuordnungen";
                UpdateStatistics();
            }
        }

        private void BtnAddAssignment_Click(object sender, RoutedEventArgs e)
        {
            // Show dialog
            AddAssignmentDialog.Visibility = Visibility.Visible;
        }

        private void BtnCancelDialog_Click(object sender, RoutedEventArgs e)
        {
            // Hide dialog and clear selections
            AddAssignmentDialog.Visibility = Visibility.Collapsed;
            CmbNewUser.SelectedIndex = -1;
            CmbNewObjekt.SelectedIndex = -1;
        }

        private async void BtnSaveAssignment_Click(object sender, RoutedEventArgs e)
        {
            var userToAssign = CmbNewUser.SelectedItem as UserDto;
            if (userToAssign == null)
            {
                MessageBox.Show("Bitte wählen Sie einen Benutzer aus.", 
                              "Validierung", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var objektToAssign = CmbNewObjekt.SelectedItem as Objekt;
            if (objektToAssign == null)
            {
                MessageBox.Show("Bitte wählen Sie ein Projekt aus.", 
                              "Validierung", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            BtnSaveAssignment.IsEnabled = false;
            ShowLoading("Erstelle Zuordnung...");

            try
            {
                var response = await _userObjektService!.AssignUserToObjektAsync(userToAssign.Id, objektToAssign.ObjektId);
                
                if (response.Success)
                {
                    MessageBox.Show("Zuordnung erfolgreich erstellt!", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    // Hide dialog and clear selections
                    AddAssignmentDialog.Visibility = Visibility.Collapsed;
                    CmbNewUser.SelectedIndex = -1;
                    CmbNewObjekt.SelectedIndex = -1;
                    
                    // Reload based on current mode
                    if (_isShowByObjektMode)
                    {
                        if (CmbObjekts.SelectedItem is Objekt selectedObjekt && selectedObjekt.ObjektId == objektToAssign.ObjektId)
                        {
                            await LoadAssignmentsForObjektAsync(selectedObjekt);
                        }
                    }
                    else
                    {
                        if (CmbUsers.SelectedItem is UserDto selectedUser && selectedUser.Id == userToAssign.Id)
                        {
                            await LoadAssignmentsForUserAsync(selectedUser);
                        }
                    }
                }
                else
                {
                    ShowErrorMessage("Fehler beim Erstellen", response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Fehler", ex.Message);
            }
            finally
            {
                BtnSaveAssignment.IsEnabled = true;
                HideLoading();
            }
        }

        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not UserObjektAssignment assignment)
                return;

            var result = MessageBox.Show(
                $"Zuordnung wirklich entfernen?\n\n" +
                $"Benutzer: {assignment.Username}\n" +
                $"Projekt: {assignment.ObjektName}",
                "Zuordnung entfernen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            ShowLoading("Entferne Zuordnung...");

            try
            {
                var response = await _userObjektService!.RemoveUserFromObjektAsync(assignment.UserId, assignment.ObjektId);
                
                if (response.Success)
                {
                    MessageBox.Show("Zuordnung erfolgreich entfernt!", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    // Reload based on current mode
                    if (_isShowByObjektMode)
                    {
                        if (CmbObjekts.SelectedItem is Objekt selectedObjekt)
                        {
                            await LoadAssignmentsForObjektAsync(selectedObjekt);
                        }
                    }
                    else
                    {
                        if (CmbUsers.SelectedItem is UserDto selectedUser)
                        {
                            await LoadAssignmentsForUserAsync(selectedUser);
                        }
                    }
                }
                else
                {
                    ShowErrorMessage("Fehler beim Entfernen", response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Fehler", ex.Message);
            }
            finally
            {
                HideLoading();
            }
        }
    }
}
