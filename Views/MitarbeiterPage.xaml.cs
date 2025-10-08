using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class MitarbeiterPage : UserControl, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private MitarbeiterService? _mitarbeiterService;
        private ObjektService? _objektService;
        private ObservableCollection<Mitarbeiter> _mitarbeiterList = new();
        private ObservableCollection<Objekt> _objektList = new();
        private List<Mitarbeiter> _allMitarbeiter = new();
        private List<Mitarbeiter> _filteredMitarbeiter = new();
        private Mitarbeiter? _selectedMitarbeiter;
        private bool _isEditMode = false;
        
        // Pagination
        private int _currentPage = 1;
        private const int _itemsPerPage = 7;
        private int _totalPages = 1;

        public ObservableCollection<Mitarbeiter> MitarbeiterList
        {
            get => _mitarbeiterList;
            set
            {
                _mitarbeiterList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Objekt> ObjektList
        {
            get => _objektList;
            set
            {
                _objektList = value;
                OnPropertyChanged();
            }
        }

        public MitarbeiterPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            DataContext = this;
            Loaded += MitarbeiterPage_Loaded;
        }

        private async void MitarbeiterPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeServices();
            await SetupUserPermissions();
            await LoadInitialData();
        }

        private async Task InitializeServices()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();

                _mitarbeiterService = new MitarbeiterService(_authService, configService);
                _objektService = new ObjektService(_authService, configService);
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Initialisieren der Services: {ex.Message}");
            }
        }

        private Task SetupUserPermissions()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    ShowError("Benutzer nicht angemeldet");
                    return Task.CompletedTask;
                }

                bool isAdmin = currentUser.Role == "Admin";

                // Hide form panel and action buttons for non-admin users
                if (!isAdmin)
                {
                    FormPanel.Visibility = Visibility.Collapsed;
                    ActionsColumn.Visibility = Visibility.Collapsed;
                    BtnAddMitarbeiter.Visibility = Visibility.Collapsed;
                    
                    StatusText.Text = "Nur-Lesen Modus - Sie können nur Mitarbeiter anzeigen";
                }
                else
                {
                    FormPanel.Visibility = Visibility.Visible;
                    ActionsColumn.Visibility = Visibility.Visible;
                    BtnAddMitarbeiter.Visibility = Visibility.Visible;
                    
                    StatusText.Text = "Administrator - Vollzugriff";
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Einrichten der Benutzerberechtigungen: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        private async Task LoadInitialData()
        {
            try
            {
                // Only load objects for the filter dropdown
                await LoadObjekte();

                // Show empty state initially - don't load all employees
                _allMitarbeiter = new List<Mitarbeiter>();
                MitarbeiterList.Clear();
                
                // Show message to use search
                StatusText.Text = "Verwenden Sie die Suchfilter oben, um Mitarbeiter zu finden";
                RecordCountText.Text = "0 Mitarbeiter";
                
                // Show empty state with custom message
                ShowEmptyStateWithMessage("Verwenden Sie die Suchfilter oben", "Filtern Sie nach Objekt, suchen Sie nach Namen oder verwenden Sie den Aktualisieren-Button");
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Laden der Daten: {ex.Message}");
            }
        }

        private async Task LoadData()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                MitarbeiterDataGrid.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Collapsed;

                // Load objects first
                await LoadObjekte();

                // Load employees
                await LoadMitarbeiter();

                LoadingPanel.Visibility = Visibility.Collapsed;
                MitarbeiterDataGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                MitarbeiterDataGrid.Visibility = Visibility.Visible;
                ShowError($"Fehler beim Laden der Daten: {ex.Message}");
            }
        }

        private async Task LoadObjekte()
        {
            if (_objektService == null) return;

            try
            {
                var response = await _objektService.GetAllAsync();
                if (response.Success && response.Data != null)
                {
                    ObjektList.Clear();
                    foreach (var objekt in response.Data)
                    {
                        ObjektList.Add(objekt);
                    }

                    // Populate filter combobox
                    CmbObjektFilter.ItemsSource = null;
                    var filterObjekte = new List<Objekt> { new Objekt { ObjektId = -1, ObjektName = "Alle Objekte" } };
                    filterObjekte.AddRange(response.Data);
                    CmbObjektFilter.ItemsSource = filterObjekte;
                    CmbObjektFilter.DisplayMemberPath = "ObjektName";
                    CmbObjektFilter.SelectedValuePath = "ObjektId";
                    CmbObjektFilter.SelectedIndex = 0; // Select "Alle Objekte"

                    // Set default status filter
                    CmbStatusFilter.SelectedIndex = 0; // Select "Alle"
                }
                else
                {
                    ShowError($"Fehler beim Laden der Objekte: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Laden der Objekte: {ex.Message}");
            }
        }

        private async Task LoadMitarbeiter()
        {
            if (_mitarbeiterService == null) 
            {
                StatusText.Text = "MitarbeiterService nicht initialisiert - keine Daten verfügbar";
                _allMitarbeiter = new List<Mitarbeiter>();
                ApplyFilters();
                ShowError("MitarbeiterService konnte nicht initialisiert werden.");
                return;
            }

            // Check authentication before making API call
            if (!_authService.IsAuthenticated)
            {
                StatusText.Text = "Benutzer nicht angemeldet - keine Daten verfügbar";
                _allMitarbeiter = new List<Mitarbeiter>();
                ApplyFilters();
                ShowError("Sie müssen sich anmelden, um Mitarbeiter zu laden.");
                return;
            }

            try
            {
                StatusText.Text = "Lade Mitarbeiter von Web API...";
                
                // Debug: Show authentication status
                System.Diagnostics.Debug.WriteLine($"Auth Status - IsAuthenticated: {_authService.IsAuthenticated}");
                System.Diagnostics.Debug.WriteLine($"Auth Status - CurrentUser: {_authService.CurrentUser?.Username}");
                System.Diagnostics.Debug.WriteLine($"Auth Status - Token: {(!string.IsNullOrEmpty(_authService.Token) ? "Present" : "Missing")}");
                
                var response = await _mitarbeiterService.GetAllAsync();
                
                // Enhanced error logging
                System.Diagnostics.Debug.WriteLine($"API Response - Success: {response.Success}, Message: {response.Message}");
                if (response.Errors?.Any() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"API Errors: {string.Join(", ", response.Errors)}");
                }
                
                if (response.Success && response.Data != null)
                {
                    _allMitarbeiter = response.Data;
                    _currentPage = 1; // Reset to first page
                    ApplyFilters();
                    StatusText.Text = $"{_allMitarbeiter.Count} Mitarbeiter von Web API geladen";
                }
                else
                {
                    var errorDetails = response.Errors?.Any() == true ? string.Join("; ", response.Errors) : "Keine Details verfügbar";
                    StatusText.Text = $"Web API Fehler: {response.Message}";
                    _allMitarbeiter = new List<Mitarbeiter>();
                    ApplyFilters();
                    ShowError($"Web API Fehler beim Laden der Mitarbeiter: {response.Message}\nDetails: {errorDetails}");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Verbindungsfehler: {ex.Message}";
                _allMitarbeiter = new List<Mitarbeiter>();
                ApplyFilters();
                ShowError($"Verbindungsfehler beim Laden der Mitarbeiter: {ex.Message}");
            }
        }

        private void LoadTestMitarbeiter()
        {
            StatusText.Text = "Keine Mitarbeiter verfügbar - Web API Verbindung fehlgeschlagen";
            _allMitarbeiter = new List<Mitarbeiter>();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allMitarbeiter.AsEnumerable();

            // Objekt filter
            if (CmbObjektFilter.SelectedValue != null)
            {
                var selectedObjektId = (int)CmbObjektFilter.SelectedValue;
                if (selectedObjektId != -1) // -1 means "Alle Objekte"
                {
                    filtered = filtered.Where(m => m.ObjektId == selectedObjektId);
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                var searchTerm = TxtSearch.Text.ToLower();
                filtered = filtered.Where(m => 
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.Personalnummer?.ToLower().Contains(searchTerm) ?? false) ||
                    (m.Objektname?.ToLower().Contains(searchTerm) ?? false));
            }

            // Status filter
            if (CmbStatusFilter.SelectedItem is ComboBoxItem statusItem)
            {
                var statusTag = statusItem.Tag?.ToString();
                if (statusTag == "Active")
                {
                    filtered = filtered.Where(m => m.Aktive);
                }
                else if (statusTag == "Inactive")
                {
                    filtered = filtered.Where(m => !m.Aktive);
                }
                // "All" shows everything, no additional filter needed
            }

            _filteredMitarbeiter = filtered.OrderBy(m => m.Name).ToList();
            
            // Reset to first page when filters change
            _currentPage = 1;
            
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            // Calculate pagination
            _totalPages = (int)Math.Ceiling((double)_filteredMitarbeiter.Count / _itemsPerPage);
            if (_totalPages == 0) _totalPages = 1;
            
            // Ensure current page is valid
            if (_currentPage > _totalPages) _currentPage = _totalPages;
            if (_currentPage < 1) _currentPage = 1;
            
            // Get items for current page
            var startIndex = (_currentPage - 1) * _itemsPerPage;
            var pageItems = _filteredMitarbeiter.Skip(startIndex).Take(_itemsPerPage).ToList();
            
            // Update DataGrid
            MitarbeiterList.Clear();
            foreach (var mitarbeiter in pageItems)
            {
                MitarbeiterList.Add(mitarbeiter);
            }

            // Update UI
            UpdatePaginationUI();
            
            // Show empty state if no results
            if (_filteredMitarbeiter.Count == 0 && _allMitarbeiter.Count > 0)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                MitarbeiterDataGrid.Visibility = Visibility.Collapsed;
                PaginationPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                MitarbeiterDataGrid.Visibility = Visibility.Visible;
                PaginationPanel.Visibility = _totalPages > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdatePaginationUI()
        {
            // Update record count
            var startRecord = (_currentPage - 1) * _itemsPerPage + 1;
            var endRecord = Math.Min(_currentPage * _itemsPerPage, _filteredMitarbeiter.Count);
            
            if (_filteredMitarbeiter.Count == 0)
            {
                RecordCountText.Text = "0 Mitarbeiter";
            }
            else
            {
                RecordCountText.Text = $"{startRecord}-{endRecord} von {_filteredMitarbeiter.Count} Mitarbeitern";
            }
            
            // Update page info
            PageInfoText.Text = $"Seite {_currentPage} von {_totalPages}";
            
            // Update button states
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Only load data if user has typed something or if we already have data
            if (!string.IsNullOrWhiteSpace(TxtSearch.Text) || _allMitarbeiter.Any())
            {
                if (!_allMitarbeiter.Any())
                {
                    await LoadMitarbeiter();
                }
                ApplyFilters();
            }
        }

        private async void CmbObjektFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Load data if a specific object is selected or if we already have data
            if ((CmbObjektFilter.SelectedValue != null && (int)CmbObjektFilter.SelectedValue != -1) || _allMitarbeiter.Any())
            {
                if (!_allMitarbeiter.Any())
                {
                    await LoadMitarbeiter();
                }
                ApplyFilters();
            }
        }

        private async void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only apply filter if we have data
            if (_allMitarbeiter.Any())
            {
                ApplyFilters();
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyPagination();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyPagination();
            }
        }

        private void BtnCloseForm_Click(object sender, RoutedEventArgs e)
        {
            FormPanel.Visibility = Visibility.Collapsed;
            ClearForm();
            _isEditMode = false;
            _selectedMitarbeiter = null;
        }

        private void ValidateForm(object sender, TextChangedEventArgs e)
        {
            // Enable/disable save button based on form validation
            BtnSave.IsEnabled = !string.IsNullOrWhiteSpace(TxtName.Text);
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private void BtnAddMitarbeiter_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAdminPermission()) return;

            ClearForm();
            _isEditMode = false;
            FormTitle.Text = "Neuer Mitarbeiter";
            FormSubtitle.Text = "Füllen Sie die Felder aus";
            BtnSave.Content = "💾 Speichern";
            FormPanel.Visibility = Visibility.Visible;
        }

        private void MitarbeiterDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MitarbeiterDataGrid.SelectedItem is Mitarbeiter selected)
            {
                _selectedMitarbeiter = selected;
                
                if (CheckAdminPermission())
                {
                    LoadMitarbeiterToForm(selected);
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAdminPermission()) return;

            if (sender is Button button && button.Tag is Mitarbeiter mitarbeiter)
            {
                _selectedMitarbeiter = mitarbeiter;
                LoadMitarbeiterToForm(mitarbeiter);
                _isEditMode = true;
                FormTitle.Text = "Mitarbeiter bearbeiten";
                FormSubtitle.Text = "Ändern Sie die gewünschten Felder";
                BtnSave.Content = "💾 Speichern";
                FormPanel.Visibility = Visibility.Visible;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAdminPermission()) return;

            if (sender is Button button && button.Tag is Mitarbeiter mitarbeiter)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie den Mitarbeiter '{mitarbeiter.Name}' wirklich löschen?",
                    "Mitarbeiter löschen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await DeleteMitarbeiter(mitarbeiter.MitarbeiterId);
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAdminPermission()) return;

            if (!ValidateForm())
            {
                return;
            }

            try
            {
                if (_isEditMode && _selectedMitarbeiter != null)
                {
                    await UpdateMitarbeiter();
                }
                else
                {
                    await CreateMitarbeiter();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Speichern: {ex.Message}");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _isEditMode = false;
            _selectedMitarbeiter = null;
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TxtName.Text))
                errors.Add("Name ist erforderlich");

            if (errors.Any())
            {
                ErrorText.Text = string.Join("\n", errors);
                ErrorPanel.Visibility = Visibility.Visible;
                return false;
            }

            ErrorPanel.Visibility = Visibility.Collapsed;
            return true;
        }

        private async Task CreateMitarbeiter()
        {
            if (_mitarbeiterService == null) return;

            var mitarbeiter = new Mitarbeiter
            {
                Name = TxtName.Text.Trim(),
                Personalnummer = TxtPersonalnummer.Text.Trim(),
                ObjektId = CmbObjekt.SelectedValue as int?,
                Eintritt = DpEintritt.SelectedDate,
                Austritt = DpAustritt.SelectedDate,
                Notes = TxtNotes.Text.Trim(),
                Aktive = GetActiveStatusFromComboBox(),
                OwnerUserId = _authService.CurrentUser?.UserId ?? 0
            };

            var response = await _mitarbeiterService.CreateAsync(mitarbeiter);
            if (response.Success)
            {
                StatusText.Text = "Mitarbeiter erfolgreich erstellt";
                ClearForm();
                await LoadMitarbeiter();
            }
            else
            {
                var errorDetails = response.Errors?.Any() == true ? string.Join("; ", response.Errors) : "Keine Details verfügbar";
                ShowError($"Fehler beim Erstellen des Mitarbeiters: {response.Message}\nDetails: {errorDetails}");
                System.Diagnostics.Debug.WriteLine($"Create Error - Message: {response.Message}");
                System.Diagnostics.Debug.WriteLine($"Create Error - Details: {errorDetails}");
            }
        }

        private async Task UpdateMitarbeiter()
        {
            if (_mitarbeiterService == null || _selectedMitarbeiter == null) return;

            var updatedMitarbeiter = new Mitarbeiter
            {
                MitarbeiterId = _selectedMitarbeiter.MitarbeiterId,
                Name = TxtName.Text.Trim(),
                Personalnummer = TxtPersonalnummer.Text.Trim(),
                ObjektId = CmbObjekt.SelectedValue as int?,
                Eintritt = DpEintritt.SelectedDate,
                Austritt = DpAustritt.SelectedDate,
                Notes = TxtNotes.Text.Trim(),
                Aktive = GetActiveStatusFromComboBox(),
                OwnerUserId = _selectedMitarbeiter.OwnerUserId,
                LastModifiedBy = _authService.CurrentUser?.UserId
            };

            var response = await _mitarbeiterService.UpdateAsync(_selectedMitarbeiter.MitarbeiterId, updatedMitarbeiter);
            if (response.Success)
            {
                StatusText.Text = "Mitarbeiter erfolgreich aktualisiert";
                ClearForm();
                _isEditMode = false;
                _selectedMitarbeiter = null;
                await LoadMitarbeiter();
            }
            else
            {
                var errorDetails = response.Errors?.Any() == true ? string.Join("; ", response.Errors) : "Keine Details verfügbar";
                ShowError($"Fehler beim Aktualisieren des Mitarbeiters: {response.Message}\nDetails: {errorDetails}");
                System.Diagnostics.Debug.WriteLine($"Update Error - Message: {response.Message}");
                System.Diagnostics.Debug.WriteLine($"Update Error - Details: {errorDetails}");
            }
        }

        private async Task DeleteMitarbeiter(int mitarbeiterId)
        {
            if (_mitarbeiterService == null) return;

            var response = await _mitarbeiterService.DeleteAsync(mitarbeiterId);
            if (response.Success)
            {
                StatusText.Text = "Mitarbeiter erfolgreich gelöscht";
                await LoadMitarbeiter();
            }
            else
            {
                var errorDetails = response.Errors?.Any() == true ? string.Join("; ", response.Errors) : "Keine Details verfügbar";
                ShowError($"Fehler beim Löschen des Mitarbeiters: {response.Message}\nDetails: {errorDetails}");
                System.Diagnostics.Debug.WriteLine($"Delete Error - Message: {response.Message}");
                System.Diagnostics.Debug.WriteLine($"Delete Error - Details: {errorDetails}");
            }
        }

        private void LoadMitarbeiterToForm(Mitarbeiter mitarbeiter)
        {
            TxtName.Text = mitarbeiter.Name;
            TxtPersonalnummer.Text = mitarbeiter.Personalnummer;
            CmbObjekt.SelectedValue = mitarbeiter.ObjektId;
            DpEintritt.SelectedDate = mitarbeiter.Eintritt;
            DpAustritt.SelectedDate = mitarbeiter.Austritt;
            TxtNotes.Text = mitarbeiter.Notes;
            SetActiveStatusComboBox(mitarbeiter.Aktive);
        }

        private void ClearForm()
        {
            TxtName.Clear();
            TxtPersonalnummer.Clear();
            CmbObjekt.SelectedIndex = -1;
            DpEintritt.SelectedDate = null;
            DpAustritt.SelectedDate = null;
            TxtNotes.Clear();
            CmbAktiveStatus.SelectedIndex = 0; // Default to "Aktiv"
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            FormTitle.Text = "Neuer Mitarbeiter";
            FormSubtitle.Text = "Füllen Sie die Felder aus";
            BtnSave.Content = "💾 Speichern";
        }

        private bool CheckAdminPermission()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser?.Role != "Admin")
            {
                MessageBox.Show("Sie haben keine Berechtigung für diese Aktion.", 
                               "Zugriff verweigert", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void ShowError(string message)
        {
            StatusText.Text = $"Fehler: {message}";
            MessageBox.Show(message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowEmptyStateWithMessage(string title, string subtitle)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            MitarbeiterDataGrid.Visibility = Visibility.Collapsed;
            LoadingPanel.Visibility = Visibility.Collapsed;
            
            // Update the empty state text (you'll need to access the TextBlocks in the EmptyStatePanel)
            // For now, we'll use the status text
            StatusText.Text = title;
        }

        private bool GetActiveStatusFromComboBox()
        {
            if (CmbAktiveStatus.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString() == "True";
            }
            return true; // Default to active
        }

        private void SetActiveStatusComboBox(bool isActive)
        {
            CmbAktiveStatus.SelectedIndex = isActive ? 0 : 1; // 0 = Aktiv, 1 = Inaktiv
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
