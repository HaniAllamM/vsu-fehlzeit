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
using Microsoft.Web.WebView2.Core;
using CreateUnterlageWithFileRequest = FehlzeitApp.Services.CreateUnterlageWithFileRequest;

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
        private List<Objekt> _availableObjekte = new();
        private Unterlage? _selectedUnterlage;
        private bool _isEditMode = false;
        private bool _isNewMode = false;
        private string _selectedFilePath = string.Empty;
        private bool _isWebViewInitialized = false;
        private bool _wasPdfVisibleBeforePopup = false;
        private bool _isLoadingDetails = false;

        public ObservableCollection<Unterlage> UnterlagenList
        {
            get => _unterlagenList;
            set
            {
                _unterlagenList = value;
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
            
            // Kategorie is now a TextBox - no initialization needed
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync();
                await PdfWebView.EnsureCoreWebView2Async(environment);
                _isWebViewInitialized = true;
                
                // Add navigation event handlers for debugging
                PdfWebView.CoreWebView2.NavigationCompleted += (sender, args) =>
                {
                    if (args.IsSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine("PDF loaded successfully!");
                        PdfLoadingPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF load failed: {args.WebErrorStatus}");
                        MessageBox.Show($"Fehler beim Laden des PDFs: {args.WebErrorStatus}", 
                                      "Ladefehler", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Warning);
                        PdfLoadingPanel.Visibility = Visibility.Collapsed;
                        EmptyStatePanel.Visibility = Visibility.Visible;
                    }
                };
                
                PdfWebView.CoreWebView2.NavigationStarting += (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation starting to: {args.Uri}");
                };

                // Configure for inline PDF viewing (use defaults compatible with SDK version)
                PdfWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                PdfWebView.CoreWebView2.NewWindowRequested += (sender, args) =>
                {
                    // Prevent opening in external windows
                    args.Handled = true;
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization error: {ex.Message}");
                MessageBox.Show($"Fehler bei der WebView2 Initialisierung: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        // Kategorie is now a TextBox - no initialization needed

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

                // Load Mitarbeiter for ComboBoxes
                var mitarbeiterResponse = await _mitarbeiterService.GetAllAsync();
                if (mitarbeiterResponse.Success && mitarbeiterResponse.Data != null)
                {
                    _availableMitarbeiter = mitarbeiterResponse.Data;
                    
                    // Populate popup mitarbeiter combobox
                    CmbDetailsMitarbeiter.Items.Clear();
                    foreach (var mitarbeiter in mitarbeiterResponse.Data)
                    {
                        CmbDetailsMitarbeiter.Items.Add(mitarbeiter);
                    }
                    CmbDetailsMitarbeiter.DisplayMemberPath = "Name";
                    CmbDetailsMitarbeiter.SelectedValuePath = "MitarbeiterId";
                }

                // Load Objekte for ComboBoxes
                var objektResponse = await _objektService.GetAllAsync();
                if (objektResponse.Success && objektResponse.Data != null)
                {
                    _availableObjekte = objektResponse.Data;

                    // Populate filter objekt combobox
                    CmbObjekt.Items.Clear();

                    // Add "Alle Objekte" option
                    CmbObjekt.Items.Add(new Objekt
                    {
                        ObjektId = -1,
                        ObjektName = "Alle Objekte"
                    });

                    foreach (var objekt in objektResponse.Data)
                    {
                        CmbObjekt.Items.Add(objekt);
                    }

                    // Populate popup objekt combobox
                    CmbDetailsObjekt.Items.Clear();

                    // Add "Kein spezifisches Objekt" option (for optional Objekt assignment)
                    CmbDetailsObjekt.Items.Add(new Objekt
                    {
                        ObjektId = -1,
                        ObjektName = "Kein spezifisches Objekt"
                    });

                    foreach (var objekt in objektResponse.Data)
                    {
                        CmbDetailsObjekt.Items.Add(objekt);
                    }
                    
                    // Default to first option
                    CmbDetailsObjekt.SelectedIndex = 0;
                }

                // Populate filter Mitarbeiter combobox (initially with all)
                CmbFilterMitarbeiter.Items.Clear();
                CmbFilterMitarbeiter.Items.Add(new Mitarbeiter
                {
                    MitarbeiterId = -1,
                    Name = "Alle Mitarbeiter"
                });
                if (_availableMitarbeiter != null && _availableMitarbeiter.Count > 0)
                {
                    foreach (var mitarbeiter in _availableMitarbeiter)
                    {
                        CmbFilterMitarbeiter.Items.Add(mitarbeiter);
                    }
                }
                CmbFilterMitarbeiter.SelectedIndex = 0;

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
                // Get date filters
                DateTime? vonDatum = DpFilterVonDatum.SelectedDate;
                DateTime? bisDatum = DpFilterBisDatum.SelectedDate;

                var response = await _unterlageService.GetAllAsync(null, null, vonDatum, bisDatum);
                if (response.Success)
                {
                    _allUnterlagen = response.Data ?? new List<Unterlage>();
                    
                    // Populate ObjektName if not provided by API
                    foreach (var unterlage in _allUnterlagen)
                    {
                        if (unterlage.ObjektId.HasValue && string.IsNullOrEmpty(unterlage.ObjektName))
                        {
                            var objekt = _availableObjekte.FirstOrDefault(o => o.ObjektId == unterlage.ObjektId.Value);
                            if (objekt != null)
                            {
                                unterlage.ObjektName = objekt.ObjektName;
                            }
                        }
                    }
                    
                    ApplyFilters();
                    UpdateRecordCount();
                }
                else
                {
                    var details = string.Empty;
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        details = "\n\nDetails: " + string.Join("; ", response.Errors);
                    }
                    MessageBox.Show((response.Message ?? "Fehler beim Laden der Unterlagen") + details, 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    _allUnterlagen = new List<Unterlage>();
                    ApplyFilters();
                    UpdateRecordCount();
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

            // Objekt filter
            if (CmbObjekt.SelectedItem is Objekt selectedObjekt && selectedObjekt.ObjektId != -1)
            {
                filteredList = filteredList.Where(u => u.ObjektId == selectedObjekt.ObjektId);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(TxtKategorie.Text))
            {
                var searchKategorie = TxtKategorie.Text.ToLower();
                filteredList = filteredList.Where(u => u.Kategorie != null && u.Kategorie.ToLower().Contains(searchKategorie));
            }

            // Employee filter from ComboBox
            if (CmbFilterMitarbeiter.SelectedItem is Mitarbeiter selectedMitarbeiter && selectedMitarbeiter.MitarbeiterId != -1)
            {
                filteredList = filteredList.Where(u => u.MitarbeiterId == selectedMitarbeiter.MitarbeiterId);
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
            int count = UnterlagenList.Count;
            TxtRecordCount.Text = count == 1 ? "1 Dokument" : $"{count} Dokumente";
        }

        private void ShowLoading(bool show)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            UnterlagenDataGrid.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowPdfLoading(bool show)
        {
            PdfLoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            PdfWebView.Visibility = Visibility.Collapsed;
        }

        private async void UnterlagenDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnterlagenDataGrid.SelectedItem is Unterlage selectedUnterlage)
            {
                _selectedUnterlage = selectedUnterlage;
                await LoadPdfPreview(selectedUnterlage);
            }
        }

        private async Task LoadPdfPreview(Unterlage unterlage)
        {
            System.Diagnostics.Debug.WriteLine("=== LoadPdfPreview STARTED ===");
            System.Diagnostics.Debug.WriteLine($"Service null? {_unterlageService == null}");
            System.Diagnostics.Debug.WriteLine($"WebView initialized? {_isWebViewInitialized}");
            
            if (_unterlageService == null)
            {
                MessageBox.Show("UnterlageService ist nicht initialisiert!", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (!_isWebViewInitialized)
            {
                MessageBox.Show("WebView2 ist nicht initialisiert! Bitte warten Sie einen Moment und versuchen Sie es erneut.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowPdfLoading(true);
                
                // Update header
                TxtPdfTitle.Text = $"📑 {unterlage.Bezeichnung}";
                TxtPdfSubtitle.Text = $"{unterlage.Dateiname} • {unterlage.MitarbeiterName}";
                BtnDownloadPdf.Visibility = Visibility.Visible;
                BtnDownloadPdf.Tag = unterlage;

                System.Diagnostics.Debug.WriteLine($"Loading PDF for ID: {unterlage.UnterlageId}");
                
                // Prefer a URL suitable for inline display
                var pdfUrl = await _unterlageService.GetPdfDisplayUrlAsync(unterlage.UnterlageId);
                if (string.IsNullOrEmpty(pdfUrl))
                    throw new Exception("PDF URL konnte nicht abgerufen werden");

                System.Diagnostics.Debug.WriteLine($"=== NAVIGATING TO PDF URL ===");
                System.Diagnostics.Debug.WriteLine($"URL: {pdfUrl}");

                // Navigate via Source for reliable PDF rendering
                PdfWebView.Source = new Uri(pdfUrl);
                
                System.Diagnostics.Debug.WriteLine("Navigation called, showing WebView...");
                
                // Show PDF viewer immediately
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                PdfLoadingPanel.Visibility = Visibility.Collapsed;
                PdfWebView.Visibility = Visibility.Visible;
                
                System.Diagnostics.Debug.WriteLine($"WebView Visibility: {PdfWebView.Visibility}");
                System.Diagnostics.Debug.WriteLine($"EmptyState Visibility: {EmptyStatePanel.Visibility}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== EXCEPTION in LoadPdfPreview ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                MessageBox.Show($"Fehler beim Laden der PDF-Vorschau: {ex.Message}\n\nDetails: {ex.StackTrace}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                
                // Show empty state on error
                EmptyStatePanel.Visibility = Visibility.Visible;
                PdfLoadingPanel.Visibility = Visibility.Collapsed;
                PdfWebView.Visibility = Visibility.Collapsed;
            }
        }

        // POPUP DIALOG METHODS

        private void ShowPopup(bool isEditMode = false)
        {
            _isEditMode = isEditMode;
            _isNewMode = !isEditMode;
            
            if (isEditMode)
            {
                TxtPopupTitle.Text = "Unterlage bearbeiten";
            }
            else
            {
                TxtPopupTitle.Text = "Neue Unterlage hinzufügen";
                ClearForm();
            }
            
            // Hide PDF viewer while popup is open to avoid airspace issues
            _wasPdfVisibleBeforePopup = PdfWebView.Visibility == Visibility.Visible;
            PdfWebView.Visibility = Visibility.Collapsed;
            PdfLoadingPanel.Visibility = Visibility.Collapsed;

            PopupOverlay.Visibility = Visibility.Visible;
        }

        private void ClosePopup()
        {
            PopupOverlay.Visibility = Visibility.Collapsed;
            ClearForm();
            _isEditMode = false;
            _isNewMode = false;

            // Restore PDF viewer visibility if it was visible before opening popup
            if (_wasPdfVisibleBeforePopup && _selectedUnterlage != null)
            {
                PdfWebView.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearForm()
        {
            TxtBezeichnung.Text = string.Empty;
            
            // Reset to "Kein spezifisches Objekt" (first item)
            if (CmbDetailsObjekt.Items.Count > 0)
            {
                CmbDetailsObjekt.SelectedIndex = 0;
            }
            
            // Mitarbeiter will be populated by the Objekt SelectionChanged event
            CmbDetailsMitarbeiter.SelectedIndex = -1;
            
            TxtDetailsKategorie.Text = string.Empty;
            DpGueltigAb.SelectedDate = null;
            DpGueltigBis.SelectedDate = null;
            ChkIstAktiv.IsChecked = true;
            TxtFilePath.Text = string.Empty;
            TxtFileInfo.Text = string.Empty;
            _selectedFilePath = string.Empty;
            _selectedUnterlage = null;
        }

        private void ShowDetails(Unterlage unterlage)
        {
            _selectedUnterlage = unterlage;
            _isLoadingDetails = true;

            TxtBezeichnung.Text = unterlage.Bezeichnung;
            TxtDetailsKategorie.Text = unterlage.Kategorie ?? string.Empty;
            DpGueltigAb.SelectedDate = unterlage.GueltigAb;
            DpGueltigBis.SelectedDate = unterlage.GueltigBis;
            ChkIstAktiv.IsChecked = unterlage.IstAktiv;
            
            // First, select the objekt (this will trigger filtering of Mitarbeiter)
            if (unterlage.ObjektId.HasValue)
            {
                bool objektFound = false;
                foreach (var item in CmbDetailsObjekt.Items)
                {
                    if (item is Objekt o && o.ObjektId == unterlage.ObjektId.Value)
                    {
                        CmbDetailsObjekt.SelectedItem = item;
                        objektFound = true;
                        break;
                    }
                }
                
                if (!objektFound)
                {
                    // If Objekt not found, select "Alle Objekte"
                    CmbDetailsObjekt.SelectedIndex = 0;
                }
            }
            else
            {
                // No Objekt assigned, select "Alle Objekte"
                CmbDetailsObjekt.SelectedIndex = 0;
            }
            
            // Then, find and select the mitarbeiter (from the filtered list)
            foreach (var item in CmbDetailsMitarbeiter.Items)
            {
                if (item is Mitarbeiter m && m.MitarbeiterId == unterlage.MitarbeiterId)
                {
                    CmbDetailsMitarbeiter.SelectedItem = item;
                    break;
                }
            }
            
            _isLoadingDetails = false;
            
            TxtFilePath.Text = unterlage.Dateiname;
            TxtFileInfo.Text = $"Vorhandene Datei: {unterlage.Dateiname}";
        }

        // EVENT HANDLERS

        private void BtnAddUnterlage_Click(object sender, RoutedEventArgs e)
        {
            ShowPopup(false);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Unterlage unterlage)
            {
                ShowDetails(unterlage);
                ShowPopup(true);
            }
        }

        private void BtnClosePopup_Click(object sender, RoutedEventArgs e)
        {
            ClosePopup();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClosePopup();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_unterlageService == null) return;

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(TxtBezeichnung.Text))
                {
                    MessageBox.Show("Bitte geben Sie eine Bezeichnung ein.", "Validierungsfehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbDetailsMitarbeiter.SelectedItem == null)
                {
                    MessageBox.Show("Bitte wählen Sie einen Mitarbeiter aus.", "Validierungsfehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isNewMode && string.IsNullOrWhiteSpace(_selectedFilePath))
                {
                    MessageBox.Show("Bitte wählen Sie eine Datei aus.", "Validierungsfehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

                BtnSave.IsEnabled = false;
                BtnSave.Content = "⏳ Speichern...";

                if (_isNewMode)
                {
                    // Create new unterlage with file upload
                    var mitarbeiter = (Mitarbeiter)CmbDetailsMitarbeiter.SelectedItem;
                    var objekt = CmbDetailsObjekt.SelectedItem as Objekt;
                    
                    // Don't save "Alle Objekte" (-1) as the ObjektId
                    int? objektId = (objekt != null && objekt.ObjektId != -1) ? objekt.ObjektId : null;
                    
                    var request = new CreateUnterlageWithFileRequest
                    {
                        MitarbeiterId = mitarbeiter.MitarbeiterId,
                        ObjektId = objektId,
                        Bezeichnung = TxtBezeichnung.Text,
                        Kategorie = string.IsNullOrWhiteSpace(TxtDetailsKategorie.Text) ? null : TxtDetailsKategorie.Text,
                        GueltigAb = DpGueltigAb.SelectedDate,
                        GueltigBis = DpGueltigBis.SelectedDate,
                        IstAktiv = ChkIstAktiv.IsChecked ?? true,
                        FilePath = _selectedFilePath
                    };

                    var response = await _unterlageService.CreateWithFileAsync(request);
                    
                    if (response.Success)
                    {
                        MessageBox.Show("Unterlage erfolgreich hochgeladen!", "Erfolg", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadUnterlagenAsync();
                        ClosePopup();
                    }
                    else
                    {
                        MessageBox.Show(response.Message ?? "Fehler beim Hochladen der Unterlage", 
                                      "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (_isEditMode && _selectedUnterlage != null)
                {
                    // Update existing unterlage
                    var mitarbeiter = (Mitarbeiter)CmbDetailsMitarbeiter.SelectedItem;
                    var objekt = CmbDetailsObjekt.SelectedItem as Objekt;
                    
                    // Don't save "Alle Objekte" (-1) as the ObjektId
                    int? objektId = (objekt != null && objekt.ObjektId != -1) ? objekt.ObjektId : null;
                    
                    var response = await _unterlageService.UpdateAsync(_selectedUnterlage.UnterlageId, new Unterlage
                    {
                        UnterlageId = _selectedUnterlage.UnterlageId,
                        MitarbeiterId = mitarbeiter.MitarbeiterId,
                        Bezeichnung = TxtBezeichnung.Text,
                        ObjektId = objektId,
                        Kategorie = string.IsNullOrWhiteSpace(TxtDetailsKategorie.Text) ? null : TxtDetailsKategorie.Text,
                        GueltigAb = DpGueltigAb.SelectedDate,
                        GueltigBis = DpGueltigBis.SelectedDate,
                        IstAktiv = ChkIstAktiv.IsChecked ?? true
                    });

                    if (response.Success)
                    {
                        MessageBox.Show("Unterlage erfolgreich aktualisiert!", "Erfolg", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadUnterlagenAsync();
                        ClosePopup();
                    }
                    else
                    {
                        MessageBox.Show(response.Message ?? "Fehler beim Aktualisieren der Unterlage", 
                                      "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnSave.Content = "💾 Speichern";
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*",
                Title = "Datei auswählen"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtFilePath.Text = Path.GetFileName(_selectedFilePath);
                
                var fileInfo = new FileInfo(_selectedFilePath);
                var fileSizeKB = fileInfo.Length / 1024.0;
                var fileSizeMB = fileSizeKB / 1024.0;
                
                if (fileSizeMB >= 1)
                {
                    TxtFileInfo.Text = $"Größe: {fileSizeMB:F2} MB";
                }
                else
                {
                    TxtFileInfo.Text = $"Größe: {fileSizeKB:F2} KB";
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Unterlage unterlage && _unterlageService != null)
            {
                var result = MessageBox.Show(
                    $"Möchten Sie die Unterlage '{unterlage.Bezeichnung}' wirklich löschen?", 
                                           "Löschen bestätigen", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _unterlageService.DeleteAsync(unterlage.UnterlageId);
                        if (response.Success)
                        {
                            MessageBox.Show("Unterlage erfolgreich gelöscht!", "Erfolg", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadUnterlagenAsync();
                            
                            // Reset PDF viewer
                            EmptyStatePanel.Visibility = Visibility.Visible;
                            PdfWebView.Visibility = Visibility.Collapsed;
                            BtnDownloadPdf.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            MessageBox.Show(response.Message ?? "Fehler beim Löschen der Unterlage", 
                                          "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fehler beim Löschen: {ex.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnDownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUnterlage == null || _unterlageService == null) return;

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = _selectedUnterlage.Dateiname,
                    Filter = "PDF Dateien (*.pdf)|*.pdf|Alle Dateien (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Get download URL and download the file
                    var downloadResponse = await _unterlageService.GetDownloadUrlAsync(_selectedUnterlage.UnterlageId);
                    if (downloadResponse.Success && !string.IsNullOrEmpty(downloadResponse.DownloadUrl))
                    {
                        // Download logic would go here
                        MessageBox.Show($"Download-URL erhalten: {downloadResponse.DownloadUrl}", 
                                      "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(downloadResponse.Message ?? "Fehler beim Abrufen der Download-URL", 
                                      "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Download: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnFixObjektIds_Click(object sender, RoutedEventArgs e)
        {
            if (_unterlageService == null) return;

            try
            {
                var result = MessageBox.Show(
                    "Dies wird alle Unterlagen ohne ObjektId reparieren, indem die ObjektId vom zugehörigen Mitarbeiter gesetzt wird.\n\nDies ist nur für Administratoren verfügbar und sollte nur einmal ausgeführt werden.\n\nMöchten Sie fortfahren?",
                    "Objekt-IDs reparieren",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    BtnFixObjektIds.IsEnabled = false;
                    BtnFixObjektIds.Content = "⏳ Repariere...";

                    var response = await _unterlageService.FixMissingObjektIdsAsync();

                    if (response.Success)
                    {
                        MessageBox.Show($"✅ Erfolgreich repariert! {response.Data?.FixedCount ?? 0} Unterlagen wurden aktualisiert.", 
                                      "Reparatur erfolgreich", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        // Refresh the data to show the fixed ObjektNames
                        await LoadUnterlagenAsync();
                    }
                    else
                    {
                        MessageBox.Show($"❌ Fehler beim Reparieren: {response.Message}", 
                                      "Reparatur fehlgeschlagen", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Fehler beim Reparieren der Objekt-IDs: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnFixObjektIds.IsEnabled = true;
                BtnFixObjektIds.Content = "🔧 Objekt-IDs reparieren";
            }
        }

        private async void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            await LoadUnterlagenAsync();
        }

        private void CmbObjekt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update filter Mitarbeiter ComboBox based on selected Objekt
            if (CmbObjekt.SelectedItem is Objekt selectedObjekt && _availableMitarbeiter.Count > 0)
            {
                var currentSelection = CmbFilterMitarbeiter.SelectedItem as Mitarbeiter;
                
                CmbFilterMitarbeiter.Items.Clear();
                CmbFilterMitarbeiter.Items.Add(new Mitarbeiter
                {
                    MitarbeiterId = -1,
                    Name = "Alle Mitarbeiter"
                });

                if (selectedObjekt.ObjektId == -1)
                {
                    // Show all Mitarbeiter
                    foreach (var mitarbeiter in _availableMitarbeiter)
                    {
                        CmbFilterMitarbeiter.Items.Add(mitarbeiter);
                    }
                }
                else
                {
                    // Filter Mitarbeiter by Objekt
                    var filteredMitarbeiter = _availableMitarbeiter
                        .Where(m => m.ObjektId == selectedObjekt.ObjektId)
                        .ToList();

                    foreach (var mitarbeiter in filteredMitarbeiter)
                    {
                        CmbFilterMitarbeiter.Items.Add(mitarbeiter);
                    }
                }

                // Try to restore previous selection or default to "Alle"
                if (currentSelection != null)
                {
                    var itemToSelect = CmbFilterMitarbeiter.Items.Cast<Mitarbeiter>()
                        .FirstOrDefault(m => m.MitarbeiterId == currentSelection.MitarbeiterId);
                    
                    CmbFilterMitarbeiter.SelectedItem = itemToSelect ?? CmbFilterMitarbeiter.Items[0];
                }
                else
                {
                    CmbFilterMitarbeiter.SelectedIndex = 0;
                }
            }
            
            ApplyFilters();
            UpdateRecordCount();
        }

        private void TxtKategorie_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
            UpdateRecordCount();
        }

        private void CmbFilterMitarbeiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
            UpdateRecordCount();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
            UpdateRecordCount();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Clear all filters
            TxtSearch.Text = string.Empty;
            TxtKategorie.Text = string.Empty;
            DpFilterVonDatum.SelectedDate = null;
            DpFilterBisDatum.SelectedDate = null;

            // Reset Objekt combobox to "Alle Objekte" (first item)
            if (CmbObjekt.Items.Count > 0)
            {
                CmbObjekt.SelectedIndex = 0;
            }

            // Reset Mitarbeiter combobox to "Alle Mitarbeiter" (first item)
            if (CmbFilterMitarbeiter.Items.Count > 0)
            {
                CmbFilterMitarbeiter.SelectedIndex = 0;
            }

            ApplyFilters();
            UpdateRecordCount();
        }

        private async void DpFilterVonDatum_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_unterlageService != null && _allUnterlagen.Count > 0)
            {
                await LoadUnterlagenAsync();
            }
        }

        private async void DpFilterBisDatum_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_unterlageService != null && _allUnterlagen.Count > 0)
            {
                await LoadUnterlagenAsync();
            }
        }

        private void CmbDetailsMitarbeiter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mitarbeiterService == null) return;

            try
            {
                var selectedMitarbeiter = CmbDetailsMitarbeiter.SelectedItem as Mitarbeiter;

                // Only auto-set Objekt if we're not loading details (to avoid overriding user selection)
                if (!_isLoadingDetails && selectedMitarbeiter != null)
                {
                    // Find the corresponding Objekt for this Mitarbeiter
                    if (selectedMitarbeiter.ObjektId.HasValue)
                    {
                        // Find the Objekt in the dropdown and select it
                        foreach (var item in CmbDetailsObjekt.Items)
                        {
                            if (item is Objekt obj && obj.ObjektId == selectedMitarbeiter.ObjektId.Value)
                            {
                                CmbDetailsObjekt.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Mitarbeiter has no Objekt, select "Kein spezifisches Objekt"
                        if (CmbDetailsObjekt.Items.Count > 0)
                        {
                            CmbDetailsObjekt.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-setting Objekt from Mitarbeiter: {ex.Message}");
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void CmbDetailsObjekt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Repopulate the Mitarbeiter dropdown in the details popup based on selected Objekt
            if (_availableMitarbeiter.Count == 0)
            {
                return;
            }

            var selectedObjekt = CmbDetailsObjekt.SelectedItem as Objekt;
            var filteredMitarbeiter = _availableMitarbeiter.AsEnumerable();

            if (selectedObjekt != null && selectedObjekt.ObjektId != -1)
            {
                filteredMitarbeiter = filteredMitarbeiter.Where(m => m.ObjektId == selectedObjekt.ObjektId);
            }

            var previouslySelected = CmbDetailsMitarbeiter.SelectedItem as Mitarbeiter;
            CmbDetailsMitarbeiter.Items.Clear();
            foreach (var m in filteredMitarbeiter)
            {
                CmbDetailsMitarbeiter.Items.Add(m);
            }

            if (_isLoadingDetails && previouslySelected != null)
            {
                // Try to keep previous selection when loading existing details
                foreach (var item in CmbDetailsMitarbeiter.Items)
                {
                    if (item is Mitarbeiter m && m.MitarbeiterId == previouslySelected.MitarbeiterId)
                    {
                        CmbDetailsMitarbeiter.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                CmbDetailsMitarbeiter.SelectedIndex = -1;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
