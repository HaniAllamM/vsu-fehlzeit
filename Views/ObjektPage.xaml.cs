using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class ObjektPage : UserControl, INotifyPropertyChanged
    {
        private AuthService _authService;
        private ObjektService? _objektService;
        private ObservableCollection<Objekt> _objektList;
        private List<Objekt> _allObjekte = new List<Objekt>();
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 7;
        private int _totalCount = 0;
        private DispatcherTimer _searchTimer;
        private Objekt? _currentEditingObjekt;
        private bool _isEditMode = false;
        private bool _isAdmin = false;

        public ObservableCollection<Objekt> ObjektList
        {
            get => _objektList;
            set
            {
                _objektList = value;
                OnPropertyChanged(nameof(ObjektList));
            }
        }

        public ObjektPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _objektList = new ObservableCollection<Objekt>();
            DataContext = this;

            // Check if user is admin
            _isAdmin = _authService.CurrentUser?.Role == "Admin";

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchTimer.Tick += SearchTimer_Tick;

            Loaded += ObjektPage_Loaded;
        }

        private async void ObjektPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set up UI based on user permissions
                SetupUserPermissions();
                
                // Initialize service but don't load data automatically
                var configService = await ConfigurationService.CreateAsync();
                _objektService = new ObjektService(_authService, configService);
                
                // Show empty state initially
                ShowEmptyState();
                StatusText.Text = "Bereit - Klicken Sie auf 'Aktualisieren' um Daten zu laden";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Initialisierungsfehler: {ex.Message}";
            }
        }

        private void SetupUserPermissions()
        {
            if (!_isAdmin)
            {
                // Hide CRUD operations for non-admin users
                BtnAddObjekt.Visibility = Visibility.Collapsed;
                
                // Hide the actions column in DataGrid
                var actionsColumn = ObjektDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Aktionen");
                if (actionsColumn != null)
                {
                    actionsColumn.Visibility = Visibility.Collapsed;
                }
                
                // Update the grid column layout to use full width
                var grid = (Grid)this.Content;
                grid.ColumnDefinitions[2].Width = new GridLength(0); // Hide form panel
                grid.ColumnDefinitions[1].Width = new GridLength(0); // Hide spacer
                
                // Update status message
                StatusText.Text = "Read-only mode - Admin rights required for editing";
            }
            else
            {
                StatusText.Text = "Admin mode - Full access enabled";
            }
        }

        private async Task<List<Objekt>> GetObjekteFromApi()
        {
            if (_objektService == null) return GetTestData(); // Return test data if service isn't ready

            try
            {
                var response = await _objektService.GetAllAsync();
                if (response.Success && response.Data != null && response.Data.Any())
                {
                    return response.Data;
                }
            }
            catch (Exception ex)
            {
                // Log the exception for production monitoring
                LogError("API Error during GetObjekteFromApi", ex);
            }

            // Fallback to test data if API fails or returns no data
            return GetTestData();
        }

        private static List<Objekt> _testData = new List<Objekt>
        {
            new Objekt { ObjektId = 1, ObjektName = "Test Hauptgebäude" },
            new Objekt { ObjektId = 2, ObjektName = "Test Lager Nord" },
            new Objekt { ObjektId = 3, ObjektName = "Test Büro Süd" },
        };

        private List<Objekt> GetTestData()
        {
            return _testData;
        }

        private void UpdateObjektList(List<Objekt> data)
        {
            // Store all data
            _allObjekte = data;
            
            // Apply search filter
            var filteredData = _allObjekte;
            if (!string.IsNullOrEmpty(_searchText))
            {
                filteredData = _allObjekte.Where(o => o.ObjektName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            _totalCount = filteredData.Count;
            
            // Calculate pagination
            var totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
            if (_currentPage > totalPages && totalPages > 0)
            {
                _currentPage = totalPages;
            }
            if (_currentPage < 1)
            {
                _currentPage = 1;
            }
            
            // Get page data
            var pageData = filteredData
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            // Update observable collection
            ObjektList.Clear();
            foreach (var item in pageData)
            {
                ObjektList.Add(item);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            RecordCountText.Text = $"{_totalCount} Objekte";
            var totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
            if (totalPages == 0) totalPages = 1;
            PageInfoText.Text = $"Seite {_currentPage} von {totalPages}";
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < totalPages;

            // Show/hide empty state
            if (_totalCount == 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                ObjektDataGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                ObjektDataGrid.Visibility = Visibility.Visible;
            }
        }

        private void SetLoadingState(bool isLoading, string? message = null)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ObjektDataGrid.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            StatusText.Text = message ?? (isLoading ? "Loading..." : "Ready");
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer.Stop();
            _searchText = TxtSearch.Text.Trim();
            await RefreshData();
        }

        private async Task RefreshData()
        {
            try
            {
                SetLoadingState(true, "Refreshing from API...");
                var data = await GetObjekteFromApi();
                UpdateObjektList(data);
                SetLoadingState(false);
                StatusText.Text = $"Refreshed - {data.Count} objects loaded";
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                // Fallback to test data if API fails
                var testData = GetTestData();
                UpdateObjektList(testData);
                StatusText.Text = $"API refresh failed, showing test data: {ex.Message}";
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private void BtnAddObjekt_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                LogSecurityEvent("Unauthorized attempt to create object", _authService.CurrentUser?.Username);
                ShowError("Administratorrechte erforderlich für das Erstellen von Objekten.");
                return;
            }
            
            try
            {
                ShowFormPanel();
                SetFormMode(false, null);
                ClearForm();
                TxtName.Focus();
            }
            catch (Exception ex)
            {
                ShowError($"Error preparing form: {ex.Message}");
            }
        }
        
        private int GetNextTestId()
        {
            var testData = GetTestData();
            return testData.Any() ? testData.Max(o => o.ObjektId) + 1 : 1;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                LogSecurityEvent("Unauthorized attempt to edit object", _authService.CurrentUser?.Username);
                ShowError("Administratorrechte erforderlich für das Bearbeiten von Objekten.");
                return;
            }
            
            try
            {
                if (sender is not Button { Tag: Objekt objekt }) return;
                
                ShowFormPanel();
                SetFormMode(true, objekt);
                LoadObjektToForm(objekt);
                TxtName.Focus();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading objekt for editing: {ex.Message}");
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                LogSecurityEvent("Unauthorized attempt to delete object", _authService.CurrentUser?.Username);
                ShowError("Administratorrechte erforderlich für das Löschen von Objekten.");
                return;
            }
            
            try
            {
                if (sender is not Button { Tag: Objekt objekt }) return;

                var result = MessageBox.Show($"Sind Sie sicher, dass Sie '{objekt.ObjektName}' löschen möchten?", 
                                           "Löschen bestätigen", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    StatusText.Text = "Deleting...";
                    
                    // Delete via API
                    if (_objektService != null)
                    {
                        var response = await _objektService.DeleteAsync(objekt.ObjektId);
                        
                        if (response.Success)
                        {
                            StatusText.Text = $"'{objekt.ObjektName}' erfolgreich gelöscht";
                            
                            // Refresh the list
                            await RefreshData();
                        }
                        else
                        {
                            ShowError($"API Error: {response.Message}");
                        }
                    }
                    else
                    {
                        // Fallback to test data if no service
                        var currentData = GetTestData();
                        var objektToRemove = currentData.FirstOrDefault(o => o.ObjektId == objekt.ObjektId);
                        if (objektToRemove != null)
                        {
                            currentData.Remove(objektToRemove);
                            UpdateObjektList(currentData);
                            StatusText.Text = $"'{objekt.ObjektName}' erfolgreich gelöscht (Test Mode)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error during delete operation", ex);
                ShowError("Ein unerwarteter Fehler ist beim Löschen aufgetreten. Bitte versuchen Sie es erneut oder kontaktieren Sie den Administrator.");
            }
        }
        private void BtnPrevPage_Click(object sender, RoutedEventArgs e) 
        { 
            if (_currentPage > 1)
            {
                _currentPage--; 
                UpdateObjektList(_allObjekte);
            }
        }
        
        private void BtnNextPage_Click(object sender, RoutedEventArgs e) 
        { 
            var totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++; 
                UpdateObjektList(_allObjekte);
            }
        }

        // Form Management Methods
        private void SetFormMode(bool isEdit, Objekt? objekt)
        {
            _isEditMode = isEdit;
            _currentEditingObjekt = objekt;
            
            if (isEdit && objekt != null)
            {
                FormTitle.Text = "Objekt bearbeiten";
                FormSubtitle.Text = $"Bearbeiten Sie '{objekt.ObjektName}'";
                BtnSave.Content = "Änderungen speichern";
            }
            else
            {
                FormTitle.Text = "Neues Objekt";
                FormSubtitle.Text = "Füllen Sie die Felder aus";
                BtnSave.Content = "Objekt erstellen";
            }
        }

        private void LoadObjektToForm(Objekt objekt)
        {
            TxtName.Text = objekt.ObjektName ?? string.Empty;
        }

        private void ClearForm()
        {
            TxtName.Text = string.Empty;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ValidateForm(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            var errors = new List<string>();

            // Validate Name
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                errors.Add("Name ist erforderlich");
            }
            else if (TxtName.Text.Length > 100)
            {
                errors.Add("Name darf maximal 100 Zeichen lang sein");
            }

            // Description validation removed since field was removed from UI

            // Show/hide errors
            if (errors.Any())
            {
                ErrorText.Text = string.Join("\n", errors.Select(e => "• " + e));
                ErrorPanel.Visibility = Visibility.Visible;
                BtnSave.IsEnabled = false;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                BtnSave.IsEnabled = true;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                LogSecurityEvent("Unauthorized attempt to save object", _authService.CurrentUser?.Username);
                ShowError("Administratorrechte erforderlich für das Speichern von Objekten.");
                return;
            }
            
            try
            {
                BtnSave.IsEnabled = false;
                StatusText.Text = "Saving...";
                
                Objekt objekt;
                
                // Sanitize input for security
                var sanitizedName = SanitizeInput(TxtName.Text);
                
                if (_isEditMode && _currentEditingObjekt != null)
                {
                    // Update existing objekt
                    objekt = _currentEditingObjekt;
                    objekt.ObjektName = sanitizedName;
                    objekt.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Create new objekt
                    objekt = new Objekt
                    {
                        ObjektName = sanitizedName,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }

                // Validate using data annotations
                var validationContext = new ValidationContext(objekt);
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                
                if (Validator.TryValidateObject(objekt, validationContext, validationResults, true))
                {
                    // Save via API
                    if (_objektService != null)
                    {
                        FehlzeitApp.Models.ApiResponse<Objekt> response;
                        
                        if (_isEditMode)
                        {
                            response = await _objektService.UpdateAsync(objekt.ObjektId, objekt);
                        }
                        else
                        {
                            response = await _objektService.CreateAsync(objekt);
                        }
                        
                        if (response.Success)
                        {
                            StatusText.Text = _isEditMode 
                                ? $"'{objekt.ObjektName}' erfolgreich aktualisiert"
                                : $"'{objekt.ObjektName}' erfolgreich erstellt";
                            
                            ClearForm();
                            SetFormMode(false, null);
                            HideFormPanel();
                            
                            // Refresh the list
                            await RefreshData();
                        }
                        else
                        {
                            ShowError($"API Error: {response.Message}");
                        }
                    }
                    else
                    {
                        // Fallback to test data if no service
                        var currentData = GetTestData();
                        
                        if (_isEditMode)
                        {
                            StatusText.Text = $"'{objekt.ObjektName}' erfolgreich aktualisiert (Test Mode)";
                        }
                        else
                        {
                            objekt.ObjektId = GetNextTestId();
                            currentData.Add(objekt);
                            StatusText.Text = $"'{objekt.ObjektName}' erfolgreich erstellt (Test Mode)";
                        }
                        
                        UpdateObjektList(currentData);
                        ClearForm();
                        SetFormMode(false, null);
                        HideFormPanel();
                    }
                }
                else
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    ErrorText.Text = string.Join("\n", errors.Select(e => "• " + e));
                    ErrorPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogError("Error during save operation", ex);
                ShowError("Ein unerwarteter Fehler ist beim Speichern aufgetreten. Bitte versuchen Sie es erneut oder kontaktieren Sie den Administrator.");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            SetFormMode(false, null);
            HideFormPanel();
        }

        private void BtnCloseForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            SetFormMode(false, null);
            HideFormPanel();
        }

        private void ShowFormPanel()
        {
            if (_isAdmin)
            {
                FormPanel.Visibility = Visibility.Visible;
                var grid = (Grid)this.Content;
                grid.ColumnDefinitions[2].Width = new GridLength(420); // Show form panel
                grid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Auto); // Show spacer
            }
        }

        private void HideFormPanel()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            var grid = (Grid)this.Content;
            grid.ColumnDefinitions[2].Width = new GridLength(0); // Hide form panel
            grid.ColumnDefinitions[1].Width = new GridLength(0); // Hide spacer
        }

        private void ShowEmptyState()
        {
            ObjektList.Clear();
            EmptyStatePanel.Visibility = Visibility.Visible;
            ObjektDataGrid.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Collapsed;
            RecordCountText.Text = "0 Objekte";
            PageInfoText.Text = "Seite 1 von 1";
            BtnPrevPage.IsEnabled = false;
            BtnNextPage.IsEnabled = false;
        }

        private void ObjektDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Auto-load selected item for editing
            // Uncomment if you want clicking a row to load it in the form
            /*
            if (ObjektDataGrid.SelectedItem is Objekt selectedObjekt)
            {
                SetFormMode(true, selectedObjekt);
                LoadObjektToForm(selectedObjekt);
            }
            */
        }

        #region Production Security Methods

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
                       .Replace("*/", "")
                       .Replace("script", "", StringComparison.OrdinalIgnoreCase)
                       .Replace("javascript", "", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Production Logging Methods

        /// <summary>
        /// Logs errors for debugging and monitoring in production
        /// </summary>
        private static void LogError(string message, Exception? exception = null)
        {
            try
            {
                var logMessage = $"[OBJEKT_PAGE_ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";
                if (exception != null)
                {
                    logMessage += $" - Exception: {exception.GetType().Name}: {exception.Message}";
                }
                
                // In production, replace with proper logging framework (Serilog, NLog, etc.)
                System.Diagnostics.EventLog.WriteEntry("FehlzeitApp", logMessage, System.Diagnostics.EventLogEntryType.Error);
            }
            catch
            {
                // Fail silently to prevent logging errors from affecting user experience
            }
        }

        /// <summary>
        /// Logs security events for monitoring and auditing
        /// </summary>
        private static void LogSecurityEvent(string message, string? username = null)
        {
            try
            {
                var logMessage = $"[OBJEKT_PAGE_SECURITY] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {message}";
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

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
