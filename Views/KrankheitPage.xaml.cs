using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class KrankheitPage : UserControl, INotifyPropertyChanged
    {
        private AuthService _authService;
        private KrankheitService? _krankheitService;
        private ObservableCollection<Krankheit> _krankheitList;
        private List<Krankheit> _allKrankheiten = new List<Krankheit>();
        private string _searchText = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 7;
        private int _totalCount = 0;
        private DispatcherTimer _searchTimer;
        private Krankheit? _currentEditingKrankheit;
        private bool _isEditMode = false;
        private bool _isAdmin = false;

        public ObservableCollection<Krankheit> KrankheitList
        {
            get => _krankheitList;
            set
            {
                _krankheitList = value;
                OnPropertyChanged(nameof(KrankheitList));
            }
        }

        public KrankheitPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _krankheitList = new ObservableCollection<Krankheit>();
            DataContext = this;

            // Check if user is admin
            _isAdmin = _authService.CurrentUser?.Role == "Admin";

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchTimer.Tick += SearchTimer_Tick;

            Loaded += KrankheitPage_Loaded;
        }

        private async void KrankheitPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set up UI based on user permissions
                SetupUserPermissions();
                
                SetLoadingState(true, "Loading from API...");
                
                // Initialize service and load from API
                var configService = await ConfigurationService.CreateAsync();
                _krankheitService = new KrankheitService(_authService, configService);
                
                // Load data from API
                var data = await GetKrankheitenFromApi();
                UpdateKrankheitList(data);
                SetLoadingState(false);
                StatusText.Text = $"Loaded {data.Count} krankheiten from API";
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                // Fallback to test data if API fails
                var testData = GetTestData();
                UpdateKrankheitList(testData);
                StatusText.Text = $"API failed, showing test data: {ex.Message}";
            }
        }

        private void SetupUserPermissions()
        {
            if (!_isAdmin)
            {
                // Hide CRUD operations for non-admin users
                BtnAddKrankheit.Visibility = Visibility.Collapsed;
                FormPanel.Visibility = Visibility.Collapsed;
                
                // Hide the actions column in DataGrid
                var actionsColumn = KrankheitDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Aktionen");
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

        private async Task<List<Krankheit>> GetKrankheitenFromApi()
        {
            if (_krankheitService == null) return GetTestData();

            try
            {
                var response = await _krankheitService.GetAllAsync();
                if (response.Success && response.Data != null && response.Data.Any())
                {
                    return response.Data;
                }
                else
                {
                    return GetTestData();
                }
            }
            catch (Exception)
            {
                return GetTestData();
            }
        }

        private List<Krankheit> GetTestData()
        {
            // NO MORE TEST DATA - return empty list
            return new List<Krankheit>();
        }

        private void UpdateKrankheitList(List<Krankheit> data)
        {
            // Store all data
            _allKrankheiten = data;
            
            // Apply search filter
            var filteredData = _allKrankheiten;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filteredData = _allKrankheiten.Where(k => 
                    k.Kurz.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    k.Beschreibung.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
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
            KrankheitList.Clear();
            foreach (var krankheit in pageData)
            {
                KrankheitList.Add(krankheit);
            }

            KrankheitDataGrid.ItemsSource = KrankheitList;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            RecordCountText.Text = $"{_totalCount} Krankheiten";
            var totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
            if (totalPages == 0) totalPages = 1;
            PageInfoText.Text = $"Seite {_currentPage} von {totalPages}";
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void SetLoadingState(bool isLoading, string message = "Loading...")
        {
            // You can add loading indicators here if needed
            if (isLoading)
            {
                StatusText.Text = message;
            }
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
                var data = await GetKrankheitenFromApi();
                UpdateKrankheitList(data);
                SetLoadingState(false);
                StatusText.Text = $"Refreshed - {data.Count} krankheiten loaded";
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                // Fallback to test data if API fails
                var testData = GetTestData();
                UpdateKrankheitList(testData);
                StatusText.Text = $"API refresh failed, showing test data: {ex.Message}";
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private void BtnAddKrankheit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                ShowError("Admin rights required for creating krankheiten.");
                return;
            }
            
            try
            {
                SetFormMode(false, null);
                ClearForm();
                TxtKurz.Focus();
            }
            catch (Exception ex)
            {
                ShowError($"Error preparing form: {ex.Message}");
            }
        }
        
        private int GetNextTestId()
        {
            var testData = GetTestData();
            return testData.Any() ? testData.Max(k => k.KrankheitId) + 1 : 1;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                ShowError("Admin rights required for editing krankheiten.");
                return;
            }
            
            try
            {
                if (sender is not Button { Tag: Krankheit krankheit }) return;
                
                SetFormMode(true, krankheit);
                LoadKrankheitToForm(krankheit);
                TxtKurz.Focus();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading krankheit for editing: {ex.Message}");
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                ShowError("Admin rights required for deleting krankheiten.");
                return;
            }
            
            try
            {
                if (sender is not Button { Tag: Krankheit krankheit }) return;

                var result = MessageBox.Show($"Sind Sie sicher, dass Sie '{krankheit.Kurz} - {krankheit.Beschreibung}' löschen möchten?", 
                                           "Löschen bestätigen", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    StatusText.Text = "Deleting...";
                    
                    // Delete via API
                    if (_krankheitService != null)
                    {
                        var response = await _krankheitService.DeleteAsync(krankheit.KrankheitId);
                        
                        if (response.Success)
                        {
                            StatusText.Text = $"'{krankheit.Kurz}' erfolgreich gelöscht";
                            
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
                        var krankheitToRemove = currentData.FirstOrDefault(k => k.KrankheitId == krankheit.KrankheitId);
                        if (krankheitToRemove != null)
                        {
                            currentData.Remove(krankheitToRemove);
                            UpdateKrankheitList(currentData);
                            StatusText.Text = $"'{krankheit.Kurz}' erfolgreich gelöscht (Test Mode)";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Löschen: {ex.Message}");
            }
        }

        // Form Management Methods
        private void SetFormMode(bool isEdit, Krankheit? krankheit)
        {
            _isEditMode = isEdit;
            _currentEditingKrankheit = krankheit;
            
            if (isEdit && krankheit != null)
            {
                FormTitle.Text = "Krankheit bearbeiten";
                FormSubtitle.Text = $"Bearbeiten Sie '{krankheit.Kurz}'";
                BtnSave.Content = "Änderungen speichern";
            }
            else
            {
                FormTitle.Text = "Neue Krankheit";
                FormSubtitle.Text = "Füllen Sie die Felder aus";
                BtnSave.Content = "Krankheit erstellen";
            }
        }

        private void LoadKrankheitToForm(Krankheit krankheit)
        {
            TxtKurz.Text = krankheit.Kurz ?? string.Empty;
            TxtBeschreibung.Text = krankheit.Beschreibung ?? string.Empty;
            ChkIsActive.IsChecked = krankheit.Aktiv;
        }

        private void ClearForm()
        {
            TxtKurz.Text = string.Empty;
            TxtBeschreibung.Text = string.Empty;
            ChkIsActive.IsChecked = true;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void ValidateForm(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            var errors = new List<string>();

            // Validate Kurz
            if (string.IsNullOrWhiteSpace(TxtKurz.Text))
            {
                errors.Add("Kurz ist erforderlich");
            }
            else if (TxtKurz.Text.Length > 50)
            {
                errors.Add("Kurz darf maximal 50 Zeichen lang sein");
            }

            // Validate Beschreibung
            if (string.IsNullOrWhiteSpace(TxtBeschreibung.Text))
            {
                errors.Add("Beschreibung ist erforderlich");
            }
            else if (TxtBeschreibung.Text.Length > 255)
            {
                errors.Add("Beschreibung darf maximal 255 Zeichen lang sein");
            }

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
                ShowError("Admin rights required for saving krankheiten.");
                return;
            }
            
            try
            {
                BtnSave.IsEnabled = false;
                StatusText.Text = "Saving...";
                
                Krankheit krankheit;
                
                if (_isEditMode && _currentEditingKrankheit != null)
                {
                    // Update existing krankheit
                    krankheit = _currentEditingKrankheit;
                    krankheit.Kurz = TxtKurz.Text.Trim();
                    krankheit.Beschreibung = TxtBeschreibung.Text.Trim();
                    krankheit.Aktiv = ChkIsActive.IsChecked ?? true;
                    krankheit.LastModifiedAt = DateTime.Now;
                }
                else
                {
                    // Create new krankheit
                    krankheit = new Krankheit
                    {
                        Kurz = TxtKurz.Text.Trim(),
                        Beschreibung = TxtBeschreibung.Text.Trim(),
                        Aktiv = ChkIsActive.IsChecked ?? true,
                        CreatedAt = DateTime.Now,
                        LastModifiedAt = DateTime.Now
                    };
                }

                // Validate using data annotations
                var validationContext = new ValidationContext(krankheit);
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                
                if (Validator.TryValidateObject(krankheit, validationContext, validationResults, true))
                {
                    // Save via API
                    if (_krankheitService != null)
                    {
                        FehlzeitApp.Models.ApiResponse<Krankheit> response;
                        
                        if (_isEditMode)
                        {
                            response = await _krankheitService.UpdateAsync(krankheit.KrankheitId, krankheit);
                        }
                        else
                        {
                            response = await _krankheitService.CreateAsync(krankheit);
                        }
                        
                        if (response.Success)
                        {
                            StatusText.Text = _isEditMode 
                                ? $"'{krankheit.Kurz}' erfolgreich aktualisiert"
                                : $"'{krankheit.Kurz}' erfolgreich erstellt";
                            
                            ClearForm();
                            SetFormMode(false, null);
                            
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
                            StatusText.Text = $"'{krankheit.Kurz}' erfolgreich aktualisiert (Test Mode)";
                        }
                        else
                        {
                            krankheit.KrankheitId = GetNextTestId();
                            currentData.Add(krankheit);
                            StatusText.Text = $"'{krankheit.Kurz}' erfolgreich erstellt (Test Mode)";
                        }
                        
                        UpdateKrankheitList(currentData);
                        ClearForm();
                        SetFormMode(false, null);
                    }
                }
                else
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    ErrorText.Text = string.Join("\\n", errors.Select(e => "• " + e));
                    ErrorPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Speichern: {ex.Message}");
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
        }

        private void KrankheitDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle selection changes
        }
        
        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateKrankheitList(_allKrankheiten);
            }
        }
        
        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            var totalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateKrankheitList(_allKrankheiten);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
