using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Models;
using FehlzeitApp.Services;
using Microsoft.Win32;

namespace FehlzeitApp.Views
{
    public partial class UnterlagePage : UserControl, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private UnterlageService? _unterlageService;
        private MitarbeiterService? _mitarbeiterService;
        private ObjektService? _objektService;
        private ObservableCollection<Unterlage> _unterlagenList = new();
        private ObservableCollection<Mitarbeiter> _mitarbeiterList = new();
        private List<Unterlage> _allUnterlagen = new();
        private List<string> _kategorien = new();
        private List<Mitarbeiter> _availableMitarbeiter = new();
        private List<string> _mitarbeiterNames = new();
        private Unterlage? _selectedUnterlage;
        private bool _isEditMode = false;
        private bool _isNewMode = false;
        private string _selectedFilePath = string.Empty;

        public ObservableCollection<Unterlage> UnterlagenList
        {
            get => _unterlagenList;
            set
            {
                _unterlagenList = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Mitarbeiter> MitarbeiterList
        {
            get => _mitarbeiterList;
            set
            {
                _mitarbeiterList = value;
                OnPropertyChanged();
            }
        }

        public UnterlagePage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            DataContext = this;
            Loaded += UnterlagePage_Loaded;
            
            // Initialize kategorie options
            _kategorien = new List<string>
            {
                "Alle Kategorien",
                "Arbeitsvertrag",
                "Zeugnisse",
                "Zertifikate",
                "Fortbildungen",
                "Gesundheitszeugnis",
                "Sonstiges"
            };
            
            InitializeKategorieComboBoxes();
        }

        private void InitializeKategorieComboBoxes()
        {
            // Filter ComboBox
            CmbKategorie.Items.Clear();
            foreach (var kategorie in _kategorien)
            {
                CmbKategorie.Items.Add(kategorie);
            }
            CmbKategorie.SelectedIndex = 0;

            // Details ComboBox (without "Alle Kategorien")
            CmbDetailsKategorie.Items.Clear();
            foreach (var kategorie in _kategorien.Skip(1))
            {
                CmbDetailsKategorie.Items.Add(kategorie);
            }
        }

        private async void UnterlagePage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeServicesAsync();
            await LoadDataAsync();
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                _unterlageService = new UnterlageService(_authService, configService);
                _mitarbeiterService = new MitarbeiterService(_authService, configService);
                _objektService = new ObjektService(_authService, configService);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Services: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            if (_unterlageService == null || _mitarbeiterService == null || _objektService == null) return;

            try
            {
                ShowLoading(true);

                // Load Objekte for ComboBox
                await LoadObjekteAsync();

                // Load Mitarbeiter for ComboBoxes
                var mitarbeiterResponse = await _mitarbeiterService.GetAllAsync();
                if (mitarbeiterResponse.Success && mitarbeiterResponse.Data != null)
                {
                    _availableMitarbeiter = mitarbeiterResponse.Data;
                    _mitarbeiterNames = mitarbeiterResponse.Data.Select(m => m.Name).ToList();

                    MitarbeiterList.Clear();
                    var allMitarbeiterItem = new Mitarbeiter { MitarbeiterId = 0, Name = "Alle Mitarbeiter" };
                    MitarbeiterList.Add(allMitarbeiterItem);
                    
                    foreach (var mitarbeiter in mitarbeiterResponse.Data)
                    {
                        MitarbeiterList.Add(mitarbeiter);
                    }

                    // Details ComboBox (without "Alle Mitarbeiter")
                    CmbDetailsMitarbeiter.ItemsSource = MitarbeiterList.Skip(1);
                    CmbDetailsMitarbeiter.DisplayMemberPath = "Name";
                    CmbDetailsMitarbeiter.SelectedValuePath = "MitarbeiterId";
                }

                // Load Objekte for details ComboBox
                var objektResponse = await _objektService.GetAllAsync();
                if (objektResponse.Success && objektResponse.Data != null)
                {
                    CmbDetailsObjekt.ItemsSource = objektResponse.Data;
                    CmbDetailsObjekt.DisplayMemberPath = "ObjektName";
                    CmbDetailsObjekt.SelectedValuePath = "ObjektId";
                }

                // Initialize Mitarbeiter TextBox with placeholder
                InitializeMitarbeiterTextBox();

                // Load Unterlagen
                await LoadUnterlagenAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Daten: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadUnterlagenAsync()
        {
            if (_unterlageService == null) return;

            try
            {
                var response = await _unterlageService.GetAllAsync();
                if (response.Success && response.Data != null)
                {
                    _allUnterlagen = response.Data;
                    ApplyFilters();
                    UpdateRecordCount();
                }
                else
                {
                    MessageBox.Show(response.Message ?? "Fehler beim Laden der Unterlagen", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Unterlagen: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filteredList = _allUnterlagen.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                var searchTerm = TxtSearch.Text.ToLower();
                filteredList = filteredList.Where(u => 
                    u.Bezeichnung.ToLower().Contains(searchTerm) ||
                    u.Dateiname.ToLower().Contains(searchTerm) ||
                    (u.MitarbeiterName?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.Kategorie?.ToLower().Contains(searchTerm) ?? false));
            }

            // Category filter
            if (CmbKategorie.SelectedItem != null && CmbKategorie.SelectedItem.ToString() != "Alle Kategorien")
            {
                var selectedKategorie = CmbKategorie.SelectedItem.ToString();
                filteredList = filteredList.Where(u => u.Kategorie == selectedKategorie);
            }

            // Employee filter from TextBox
            if (!string.IsNullOrWhiteSpace(TxtMitarbeiter.Text) && TxtMitarbeiter.Text != "Mitarbeiter eingeben...")
            {
                var employeeName = TxtMitarbeiter.Text.ToLower();
                filteredList = filteredList.Where(u => 
                    u.MitarbeiterName?.ToLower().Contains(employeeName) ?? false);
            }

            // Add StatusText property for display
            var unterlagenWithStatus = filteredList.Select(u => 
            {
                u.StatusText = u.IstAktiv ? "Aktiv" : "Inaktiv";
                return u;
            }).ToList();

            UnterlagenList.Clear();
            foreach (var unterlage in unterlagenWithStatus)
            {
                UnterlagenList.Add(unterlage);
            }
        }

        private void UpdateRecordCount()
        {
            TxtRecordCount.Text = $"{UnterlagenList.Count} Unterlagen gefunden";
        }

        private void ShowLoading(bool show)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            UnterlagenDataGrid.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClearForm()
        {
            TxtBezeichnung.Text = string.Empty;
            CmbDetailsKategorie.SelectedIndex = -1;
            CmbDetailsMitarbeiter.SelectedIndex = -1;
            DpGueltigAb.SelectedDate = null;
            DpGueltigBis.SelectedDate = null;
            ChkIstAktiv.IsChecked = true;
            TxtFilePath.Text = string.Empty;
            TxtFileInfo.Text = string.Empty;
            _selectedFilePath = string.Empty;
            _selectedUnterlage = null;
            _isEditMode = false;
            _isNewMode = false;
        }

        private void ShowDetails(Unterlage unterlage)
        {
            _selectedUnterlage = unterlage;
            _isEditMode = true;
            _isNewMode = false;

            TxtBezeichnung.Text = unterlage.Bezeichnung;
            
            if (!string.IsNullOrEmpty(unterlage.Kategorie))
            {
                CmbDetailsKategorie.SelectedItem = unterlage.Kategorie;
            }

            if (unterlage.MitarbeiterId > 0)
            {
                CmbDetailsMitarbeiter.SelectedValue = unterlage.MitarbeiterId;
            }

            DpGueltigAb.SelectedDate = unterlage.GueltigAb;
            DpGueltigBis.SelectedDate = unterlage.GueltigBis;
            ChkIstAktiv.IsChecked = unterlage.IstAktiv;
            TxtFilePath.Text = unterlage.Dateiname;
            
            if (unterlage.Dateigroesse.HasValue)
            {
                var sizeInKB = unterlage.Dateigroesse.Value / 1024.0;
                TxtFileInfo.Text = $"Dateigröße: {sizeInKB:F1} KB | Typ: {unterlage.Dateityp}";
            }

            TxtDetailsTitle.Text = "Unterlage bearbeiten";
            DetailsPanel.Visibility = Visibility.Visible;
            ActionButtonsPanel.Visibility = Visibility.Visible;
        }

        // Event Handlers
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadUnterlagenAsync();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
            UpdateRecordCount();
        }

        private void CmbKategorie_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateRecordCount();
        }

        private async Task LoadObjekteAsync()
        {
            if (_objektService == null) return;

            try
            {
                var response = await _objektService.GetAllAsync();
                if (response.Success && response.Data != null)
                {
                    var objektNames = response.Data.Select(o => o.ObjektName).ToList();
                    CmbObjekt.ItemsSource = objektNames;
                    CmbObjekt.SelectedIndex = -1; // No selection initially
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Objekte: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void InitializeMitarbeiterTextBox()
        {
            TxtMitarbeiter.Text = "Mitarbeiter eingeben...";
            TxtMitarbeiter.Foreground = System.Windows.Media.Brushes.Gray;
            
            TxtMitarbeiter.GotFocus += (s, e) =>
            {
                if (TxtMitarbeiter.Text == "Mitarbeiter eingeben...")
                {
                    TxtMitarbeiter.Text = "";
                    TxtMitarbeiter.Foreground = System.Windows.Media.Brushes.Black;
                }
            };
            
            TxtMitarbeiter.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(TxtMitarbeiter.Text))
                {
                    TxtMitarbeiter.Text = "Mitarbeiter eingeben...";
                    TxtMitarbeiter.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };
        }

        private void CmbObjekt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When object changes, we could filter employees by object if needed
            // For now, just trigger a filter update
            ApplyFilters();
            UpdateRecordCount();
        }

        private void TxtMitarbeiter_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Only apply filters if not in placeholder state
            if (TxtMitarbeiter.Text != "Mitarbeiter eingeben...")
            {
                ApplyFilters();
                UpdateRecordCount();
            }
        }

        private async void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            await LoadUnterlagenAsync();
        }

        private void UnterlagenDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnterlagenDataGrid.SelectedItem is Unterlage selectedUnterlage)
            {
                ShowDetails(selectedUnterlage);
            }
        }

        private void BtnAddUnterlage_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _isNewMode = true;
            _isEditMode = false;
            TxtDetailsTitle.Text = "Neue Unterlage erstellen";
            DetailsPanel.Visibility = Visibility.Visible;
            ActionButtonsPanel.Visibility = Visibility.Visible;
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Unterlage unterlage && _unterlageService != null)
            {
                try
                {
                    // Open PDF in browser
                    await OpenPdfViewer(unterlage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen des Dokuments: {ex.Message}", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
        }

        private async Task OpenPdfViewer(Unterlage unterlage)
        {
            if (_unterlageService == null)
            {
                MessageBox.Show("Service nicht verfügbar", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get the authenticated pre-signed URL from the API
                var downloadResponse = await _unterlageService.GetDownloadUrlAsync(unterlage.UnterlageId);
                
                if (downloadResponse.Success && !string.IsNullOrEmpty(downloadResponse.DownloadUrl))
                {
                    string downloadUrl = downloadResponse.DownloadUrl;
                    
                    // Debug: Show the URL and test it manually first
                    var result = MessageBox.Show($"URL Generated:\n{downloadUrl}\n\nClick YES to try opening in browser, or NO to copy URL to clipboard for manual testing.", 
                                                "Debug - Test URL", 
                                                MessageBoxButton.YesNo, 
                                                MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.No)
                    {
                        // Copy to clipboard for manual testing
                        System.Windows.Clipboard.SetText(downloadUrl);
                        MessageBox.Show("URL copied to clipboard. Please paste it in your browser manually to test.", "URL Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    try
                    {
                        // Method 1: Direct Process.Start with UseShellExecute
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = downloadUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // Method 2: Using cmd start
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "cmd",
                                Arguments = $"/c start \"\" \"{downloadUrl}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                        }
                        catch (Exception ex2)
                        {
                            try
                            {
                                // Method 3: Direct Process.Start with UseShellExecute
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = downloadUrl,
                                    UseShellExecute = true
                                });
                            }
                            catch (Exception ex3)
                            {
                                MessageBox.Show($"Alle Methoden fehlgeschlagen:\n\n1. Shell Execute: {ex.Message}\n2. CMD Start: {ex2.Message}\n3. Direct Start: {ex3.Message}\n\nURL: {downloadUrl}\n\nBitte kopieren Sie die URL und öffnen Sie sie manuell im Browser.", 
                                              "Browser-Fehler", 
                                              MessageBoxButton.OK, 
                                              MessageBoxImage.Warning);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(downloadResponse.Message ?? "Fehler beim Abrufen der Download-URL", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des PDFs: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
            }
        }


        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Unterlage unterlage)
            {
                ShowDetails(unterlage);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Unterlage unterlage && _unterlageService != null)
            {
                var result = MessageBox.Show($"Sind Sie sicher, dass Sie die Unterlage '{unterlage.Bezeichnung}' löschen möchten?", 
                                           "Löschen bestätigen", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var deleteResponse = await _unterlageService.DeleteAsync(unterlage.UnterlageId);
                        if (deleteResponse.Success)
                        {
                            MessageBox.Show("Unterlage erfolgreich gelöscht.", 
                                          "Erfolg", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Information);
                            await LoadUnterlagenAsync();
                            ClearForm();
                            DetailsPanel.Visibility = Visibility.Collapsed;
                            ActionButtonsPanel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            MessageBox.Show(deleteResponse.Message ?? "Fehler beim Löschen der Unterlage", 
                                          "Fehler", 
                                          MessageBoxButton.OK, 
                                          MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen: {ex.Message}", 
                                      "Fehler", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Datei auswählen",
                Filter = "Alle Dateien (*.*)|*.*|PDF Dateien (*.pdf)|*.pdf|Word Dokumente (*.docx)|*.docx|Excel Dateien (*.xlsx)|*.xlsx|Bilder (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtFilePath.Text = Path.GetFileName(_selectedFilePath);
                
                var fileInfo = new FileInfo(_selectedFilePath);
                var sizeInKB = fileInfo.Length / 1024.0;
                TxtFileInfo.Text = $"Dateigröße: {sizeInKB:F1} KB | Typ: {fileInfo.Extension}";
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_unterlageService == null) return;

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(TxtBezeichnung.Text))
                {
                    MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbDetailsMitarbeiter.SelectedValue == null)
                {
                    MessageBox.Show("Bitte wählen Sie einen Mitarbeiter aus.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isNewMode && string.IsNullOrEmpty(_selectedFilePath))
                {
                    MessageBox.Show("Bitte wählen Sie eine Datei aus.", "Validierungsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isNewMode)
                {
                    // Create new unterlage with file
                    var createRequest = new CreateUnterlageWithFileRequest
                    {
                        MitarbeiterId = (int)CmbDetailsMitarbeiter.SelectedValue,
                        Bezeichnung = TxtBezeichnung.Text,
                        Kategorie = CmbDetailsKategorie.SelectedItem?.ToString(),
                        GueltigAb = DpGueltigAb.SelectedDate,
                        GueltigBis = DpGueltigBis.SelectedDate,
                        IstAktiv = ChkIstAktiv.IsChecked ?? true,
                        FilePath = _selectedFilePath
                    };

                    var createResponse = await _unterlageService.CreateWithFileAsync(createRequest);
                    if (createResponse.Success)
                    {
                        MessageBox.Show("Unterlage erfolgreich erstellt.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadUnterlagenAsync();
                        ClearForm();
                        DetailsPanel.Visibility = Visibility.Collapsed;
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show(createResponse.Message ?? "Fehler beim Erstellen der Unterlage", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (_isEditMode && _selectedUnterlage != null)
                {
                    // Update existing unterlage
                    var updatedUnterlage = new Unterlage
                    {
                        UnterlageId = _selectedUnterlage.UnterlageId,
                        MitarbeiterId = (int)CmbDetailsMitarbeiter.SelectedValue,
                        Bezeichnung = TxtBezeichnung.Text,
                        Kategorie = CmbDetailsKategorie.SelectedItem?.ToString(),
                        GueltigAb = DpGueltigAb.SelectedDate,
                        GueltigBis = DpGueltigBis.SelectedDate,
                        IstAktiv = ChkIstAktiv.IsChecked ?? true,
                        Dateiname = _selectedUnterlage.Dateiname,
                        Dateityp = _selectedUnterlage.Dateityp,
                        Dateigroesse = _selectedUnterlage.Dateigroesse,
                        ObjectKey = _selectedUnterlage.ObjectKey,
                        Speicherpfad = _selectedUnterlage.Speicherpfad
                    };

                    var updateResponse = await _unterlageService.UpdateAsync(_selectedUnterlage.UnterlageId, updatedUnterlage);
                    if (updateResponse.Success)
                    {
                        MessageBox.Show("Unterlage erfolgreich aktualisiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadUnterlagenAsync();
                        ClearForm();
                        DetailsPanel.Visibility = Visibility.Collapsed;
                        ActionButtonsPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show(updateResponse.Message ?? "Fehler beim Aktualisieren der Unterlage", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            DetailsPanel.Visibility = Visibility.Collapsed;
            ActionButtonsPanel.Visibility = Visibility.Collapsed;
            TxtDetailsTitle.Text = "Unterlage Details";
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            // Implement pagination if needed
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            // Implement pagination if needed
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
