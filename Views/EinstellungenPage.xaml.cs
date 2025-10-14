using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using FehlzeitApp.Models;
using FehlzeitApp.Services;
using Microsoft.Win32;

namespace FehlzeitApp.Views
{
    public partial class EinstellungenPage : UserControl
    {
        private readonly AuthService _authService;
        private ObjektService? _objektService;
        private List<ImportObjektItem> _importData = new();
        private string _selectedFilePath = string.Empty;
        
        // Mitarbeiter import fields
        private MitarbeiterService? _mitarbeiterService;
        private List<ImportMitarbeiterItem> _importData_Mitarbeiter = new();
        private string _selectedFilePath_Mitarbeiter = string.Empty;
        
        // User service for User Management (CRUD)
        private UserService? _userService;

        public EinstellungenPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            Loaded += EinstellungenPage_Loaded;
        }

        private async void EinstellungenPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeServicesAsync();
            CheckAdminAccess();
            
            // Load users for the management tab (only for admins)
            bool isAdmin = _authService.CurrentUser?.IsAdmin ?? false;
            if (isAdmin)
            {
                await LoadUsersAsync();
            }
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                var configService = await ConfigurationService.CreateAsync();
                _objektService = new ObjektService(_authService, configService);
                _mitarbeiterService = new MitarbeiterService(_authService, configService);
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

        private void CheckAdminAccess()
        {
            // Check if user is admin
            bool isAdmin = _authService.CurrentUser?.IsAdmin ?? false;
            
            if (!isAdmin)
            {
                // Hide ALL tabs for non-admin users
                if (ObjektImportTab != null)
                {
                    ObjektImportTab.Visibility = Visibility.Collapsed;
                }
                
                if (MitarbeiterImportTab != null)
                {
                    MitarbeiterImportTab.Visibility = Visibility.Collapsed;
                }
                
                if (UserManagementTab != null)
                {
                    UserManagementTab.Visibility = Visibility.Collapsed;
                }
                
                // Disable the entire TabControl
                if (MainTabControl != null)
                {
                    MainTabControl.IsEnabled = false;
                }
                
                // Show warning message
                ShowAdminWarning();
            }
            else
            {
                // Show ALL tabs for admin users
                if (ObjektImportTab != null)
                {
                    ObjektImportTab.Visibility = Visibility.Visible;
                }
                
                if (MitarbeiterImportTab != null)
                {
                    MitarbeiterImportTab.Visibility = Visibility.Visible;
                }
                
                if (UserManagementTab != null)
                {
                    UserManagementTab.Visibility = Visibility.Visible;
                }
                
                // Enable the TabControl
                if (MainTabControl != null)
                {
                    MainTabControl.IsEnabled = true;
                }
            }
        }

        private void ShowAdminWarning()
        {
            // Create warning message that will be visible regardless of tab selection
            var warningBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 243, 199)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var warningStack = new StackPanel();
            var warningTitle = new TextBlock
            {
                Text = "⚠️ Zugriff verweigert",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 83, 9)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var warningText = new TextBlock
            {
                Text = "Nur Administratoren können Daten importieren. Bitte wenden Sie sich an Ihren Administrator.",
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(146, 64, 14)),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            
            warningStack.Children.Add(warningTitle);
            warningStack.Children.Add(warningText);
            warningBorder.Child = warningStack;
            
            // Find the main StackPanel and insert warning after header
            var mainStackPanel = this.Content as ScrollViewer;
            if (mainStackPanel?.Content is StackPanel stackPanel && stackPanel.Children.Count > 1)
            {
                stackPanel.Children.Insert(1, warningBorder);
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Excel-Datei auswählen",
                Filter = "Excel Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtSelectedFile.Text = Path.GetFileName(_selectedFilePath);
                
                // Load and preview Excel data
                LoadExcelFile(_selectedFilePath);
            }
        }

        private void LoadExcelFile(string filePath)
        {
            try
            {
                _importData.Clear();
                
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // First sheet
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row
                    
                    foreach (var row in rows)
                    {
                        // Column A: objektid (for display only)
                        // Column B: objektname (this will be imported)
                        var objektId = row.Cell(1).GetString().Trim();
                        var objektName = row.Cell(2).GetString().Trim();
                        
                        if (!string.IsNullOrWhiteSpace(objektName))
                        {
                            _importData.Add(new ImportObjektItem
                            {
                                ExcelObjektId = objektId,
                                ObjektName = objektName
                            });
                        }
                    }
                }
                
                // Show preview
                if (_importData.Count > 0)
                {
                    PreviewDataGrid.ItemsSource = _importData;
                    PreviewDataGrid.Visibility = Visibility.Visible;
                    EmptyState.Visibility = Visibility.Collapsed;
                    
                    TxtRecordCount.Text = $"{_importData.Count} Objekte geladen";
                    BtnValidate.IsEnabled = true;
                    
                    // Auto-validate
                    ValidateData();
                }
                else
                {
                    MessageBox.Show("Keine Daten in der Excel-Datei gefunden.", 
                                  "Warnung", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Lesen der Excel-Datei: {ex.Message}\n\nStellen Sie sicher, dass die Datei das richtige Format hat:\nSpalte A: objektid\nSpalte B: objektname", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                ResetUI();
            }
        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            ValidateData();
        }

        private void ValidateData()
        {
            try
            {
                var errors = new List<string>();
                
                // Check for empty names
                var emptyNames = _importData.Where(o => string.IsNullOrWhiteSpace(o.ObjektName)).Count();
                if (emptyNames > 0)
                {
                    errors.Add($"❌ {emptyNames} Objekte mit leerem Namen gefunden");
                }
                
                // Check for duplicates
                var duplicates = _importData
                    .GroupBy(o => o.ObjektName.ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                
                if (duplicates.Any())
                {
                    errors.Add($"❌ {duplicates.Count} doppelte Objektnamen gefunden: {string.Join(", ", duplicates.Take(5))}");
                }
                
                // Check for too long names
                var tooLong = _importData.Where(o => o.ObjektName.Length > 255).Count();
                if (tooLong > 0)
                {
                    errors.Add($"❌ {tooLong} Objektnamen sind zu lang (max. 255 Zeichen)");
                }
                
                // Show results
                ValidationPanel.Visibility = Visibility.Visible;
                
                if (errors.Count == 0)
                {
                    // Success
                    ValidationPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 234, 254));
                    ValidationPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                    TxtValidationTitle.Text = "✓ Validierung erfolgreich";
                    TxtValidationTitle.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175));
                    TxtValidationMessage.Text = $"Alle {_importData.Count} Objekte sind gültig und bereit zum Importieren.";
                    TxtValidationMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
                    
                    BtnImport.IsEnabled = true;
                }
                else
                {
                    // Errors found
                    ValidationPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242));
                    ValidationPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    TxtValidationTitle.Text = "❌ Validierungsfehler";
                    TxtValidationTitle.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                    TxtValidationMessage.Text = string.Join("\n", errors);
                    TxtValidationMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27));
                    
                    BtnImport.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Validierung: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (_objektService == null || _importData.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Importieren vorhanden.", 
                              "Warnung", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            // Confirm action
            var confirmMessage = ChkClearExisting.IsChecked == true
                ? $"ACHTUNG: Alle bestehenden Objekte werden gelöscht!\n\nMöchten Sie wirklich {_importData.Count} Objekte importieren?"
                : $"Möchten Sie {_importData.Count} Objekte importieren?";
            
            var result = MessageBox.Show(confirmMessage, 
                                        "Import bestätigen", 
                                        MessageBoxButton.YesNo, 
                                        MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                BtnImport.IsEnabled = false;
                BtnValidate.IsEnabled = false;
                BtnSelectFile.IsEnabled = false;
                
                LogSection.Visibility = Visibility.Visible;
                TxtImportLog.Text = "🔄 Import wird gestartet...\n";
                TxtImportLog.Text += $"📊 Anzahl Objekte: {_importData.Count}\n";
                TxtImportLog.Text += $"🗑️ Bestehende löschen: {ChkClearExisting.IsChecked}\n\n";
                
                // Call bulk import API
                var importResult = await _objektService.BulkImportAsync(_importData, ChkClearExisting.IsChecked == true);
                
                if (importResult.Success)
                {
                    TxtImportLog.Text += $"✓ Erfolgreich: {importResult.InsertedCount} Objekte importiert\n";
                    TxtImportLog.Text += $"✓ {importResult.Message}\n";
                    
                    MessageBox.Show($"Import erfolgreich!\n\n{importResult.InsertedCount} Objekte wurden importiert.", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    // Reset UI
                    ResetUI();
                }
                else
                {
                    TxtImportLog.Text += $"❌ Fehler: {importResult.Message}\n";
                    TxtImportLog.Text += $"❌ Eingefügte: {importResult.InsertedCount}\n";
                    TxtImportLog.Text += $"❌ Fehleranzahl: {importResult.ErrorCount}\n";
                    
                    if (importResult.Errors != null && importResult.Errors.Count > 0)
                    {
                        TxtImportLog.Text += "\n❌ Fehlerdetails:\n";
                        TxtImportLog.Text += string.Join("\n", importResult.Errors.Select(e => $"  - {e}"));
                    }
                    
                    MessageBox.Show($"Import fehlgeschlagen!\n\n{importResult.Message}\n\nFehler: {importResult.ErrorCount}", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                TxtImportLog.Text += $"❌ Exception: {ex.Message}\n";
                TxtImportLog.Text += $"❌ Stack Trace: {ex.StackTrace}\n";
                
                MessageBox.Show($"Fehler beim Import: {ex.Message}\n\nDetails im Log anzeigen.", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                BtnImport.IsEnabled = true;
                BtnValidate.IsEnabled = true;
                BtnSelectFile.IsEnabled = true;
            }
        }

        private void ResetUI()
        {
            _importData.Clear();
            _selectedFilePath = string.Empty;
            TxtSelectedFile.Text = "Keine Datei ausgewählt";
            PreviewDataGrid.ItemsSource = null;
            PreviewDataGrid.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            ValidationPanel.Visibility = Visibility.Collapsed;
            TxtRecordCount.Text = "0 Objekte geladen";
            BtnValidate.IsEnabled = false;
            BtnImport.IsEnabled = false;
            ChkClearExisting.IsChecked = false;
        }

        // ==================== MITARBEITER IMPORT METHODS ====================

        private void BtnSelectFile_Mitarbeiter_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Excel-Datei auswählen",
                Filter = "Excel Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath_Mitarbeiter = openFileDialog.FileName;
                TxtSelectedFile_Mitarbeiter.Text = Path.GetFileName(_selectedFilePath_Mitarbeiter);
                
                LoadMitarbeiterExcelFile(_selectedFilePath_Mitarbeiter);
            }
        }

        private void LoadMitarbeiterExcelFile(string filePath)
        {
            try
            {
                _importData_Mitarbeiter.Clear();
                
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
                    
                    foreach (var row in rows)
                    {
                        var mitarbeiterId = row.Cell(1).GetString().Trim();
                        var personalnummer = row.Cell(2).GetString().Trim();
                        var name = row.Cell(3).GetString().Trim();
                        var objektId = row.Cell(4).GetString().Trim();
                        var eintritt = row.Cell(5).GetString().Trim();
                        var austritt = row.Cell(6).GetString().Trim();
                        var notes = row.Cell(7).GetString().Trim();
                        var aktive = row.Cell(8).GetString().Trim();
                        
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            _importData_Mitarbeiter.Add(new ImportMitarbeiterItem
                            {
                                MitarbeiterId = int.TryParse(mitarbeiterId, out var mid) ? mid : 0,
                                Personalnummer = string.IsNullOrWhiteSpace(personalnummer) ? null : personalnummer,
                                Name = name,
                                ObjektId = int.TryParse(objektId, out var oid) ? oid : (int?)null,
                                Eintritt = DateTime.TryParse(eintritt, out var ein) ? ein : (DateTime?)null,
                                Austritt = DateTime.TryParse(austritt, out var aus) ? aus : (DateTime?)null,
                                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                                Aktive = aktive == "1" || aktive.ToLower() == "true"
                            });
                        }
                    }
                }
                
                if (_importData_Mitarbeiter.Count > 0)
                {
                    PreviewDataGrid_Mitarbeiter.ItemsSource = _importData_Mitarbeiter;
                    PreviewDataGrid_Mitarbeiter.Visibility = Visibility.Visible;
                    EmptyState_Mitarbeiter.Visibility = Visibility.Collapsed;
                    
                    TxtRecordCount_Mitarbeiter.Text = $"{_importData_Mitarbeiter.Count} Mitarbeiter geladen";
                    BtnValidate_Mitarbeiter.IsEnabled = true;
                    
                    ValidateMitarbeiterData();
                }
                else
                {
                    MessageBox.Show("Keine Daten in der Excel-Datei gefunden.", 
                                  "Warnung", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Lesen der Excel-Datei: {ex.Message}\n\nStellen Sie sicher, dass die Datei das richtige Format hat:\nSpalte A: MitarbeiterId\nSpalte B: Personalnummer\nSpalte C: Name\nSpalte D: ObjektId\nSpalte E: Eintritt\nSpalte F: Austritt\nSpalte G: Notes\nSpalte H: Aktive", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                ResetMitarbeiterUI();
            }
        }

        private void BtnValidate_Mitarbeiter_Click(object sender, RoutedEventArgs e)
        {
            ValidateMitarbeiterData();
        }

        private void ValidateMitarbeiterData()
        {
            try
            {
                var errors = new List<string>();
                
                // Check for empty names
                var emptyNames = _importData_Mitarbeiter.Where(m => string.IsNullOrWhiteSpace(m.Name)).Count();
                if (emptyNames > 0)
                {
                    errors.Add($"❌ {emptyNames} Mitarbeiter mit leerem Namen gefunden");
                }
                
                // Check for duplicates
                var duplicates = _importData_Mitarbeiter
                    .GroupBy(m => m.MitarbeiterId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                
                if (duplicates.Any())
                {
                    errors.Add($"❌ {duplicates.Count} doppelte MitarbeiterIds gefunden");
                }
                
                // Check for invalid dates
                var invalidDates = _importData_Mitarbeiter
                    .Where(m => m.Eintritt.HasValue && m.Austritt.HasValue && m.Eintritt > m.Austritt)
                    .Count();
                
                if (invalidDates > 0)
                {
                    errors.Add($"❌ {invalidDates} Mitarbeiter mit ungültigen Daten (Eintritt nach Austritt)");
                }
                
                // Show results
                ValidationPanel_Mitarbeiter.Visibility = Visibility.Visible;
                
                if (errors.Count == 0)
                {
                    ValidationPanel_Mitarbeiter.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 234, 254));
                    ValidationPanel_Mitarbeiter.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                    TxtValidationTitle_Mitarbeiter.Text = "✓ Validierung erfolgreich";
                    TxtValidationTitle_Mitarbeiter.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175));
                    TxtValidationMessage_Mitarbeiter.Text = $"Alle {_importData_Mitarbeiter.Count} Mitarbeiter sind gültig und bereit zum Importieren.";
                    TxtValidationMessage_Mitarbeiter.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
                    
                    BtnImport_Mitarbeiter.IsEnabled = true;
                }
                else
                {
                    ValidationPanel_Mitarbeiter.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242));
                    ValidationPanel_Mitarbeiter.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    TxtValidationTitle_Mitarbeiter.Text = "❌ Validierungsfehler";
                    TxtValidationTitle_Mitarbeiter.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                    TxtValidationMessage_Mitarbeiter.Text = string.Join("\n", errors);
                    TxtValidationMessage_Mitarbeiter.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27));
                    
                    BtnImport_Mitarbeiter.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Validierung: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private async void BtnImport_Mitarbeiter_Click(object sender, RoutedEventArgs e)
        {
            if (_mitarbeiterService == null || _importData_Mitarbeiter.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Importieren vorhanden.", 
                              "Warnung", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var confirmMessage = ChkClearExisting_Mitarbeiter.IsChecked == true
                ? $"ACHTUNG: Alle bestehenden Mitarbeiter werden gelöscht!\n\nMöchten Sie wirklich {_importData_Mitarbeiter.Count} Mitarbeiter importieren?"
                : $"Möchten Sie {_importData_Mitarbeiter.Count} Mitarbeiter importieren?";
            
            var result = MessageBox.Show(confirmMessage, 
                                        "Import bestätigen", 
                                        MessageBoxButton.YesNo, 
                                        MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                BtnImport_Mitarbeiter.IsEnabled = false;
                BtnValidate_Mitarbeiter.IsEnabled = false;
                BtnSelectFile_Mitarbeiter.IsEnabled = false;
                
                LogSection.Visibility = Visibility.Visible;
                TxtImportLog.Text = "🔄 Mitarbeiter-Import wird gestartet...\n";
                TxtImportLog.Text += $"📊 Anzahl Mitarbeiter: {_importData_Mitarbeiter.Count}\n";
                TxtImportLog.Text += $"🗑️ Bestehende löschen: {ChkClearExisting_Mitarbeiter.IsChecked}\n\n";
                
                TxtImportLog.Text += $"📤 Sende Daten an Server...\n";
                
                var importResult = await _mitarbeiterService.BulkImportAsync(_importData_Mitarbeiter, ChkClearExisting_Mitarbeiter.IsChecked == true);
                
                TxtImportLog.Text += $"📥 Antwort erhalten: Success={importResult.Success}\n";
                
                if (importResult.Success)
                {
                    TxtImportLog.Text += $"✓ Erfolgreich: {importResult.InsertedCount} Mitarbeiter importiert\n";
                    TxtImportLog.Text += $"✓ {importResult.Message}\n";
                    
                    MessageBox.Show($"Import erfolgreich!\n\n{importResult.InsertedCount} Mitarbeiter wurden importiert.", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    ResetMitarbeiterUI();
                }
                else
                {
                    TxtImportLog.Text += $"❌ Fehler: {importResult.Message}\n";
                    TxtImportLog.Text += $"❌ Eingefügte: {importResult.InsertedCount}\n";
                    TxtImportLog.Text += $"❌ Fehleranzahl: {importResult.ErrorCount}\n";
                    
                    if (importResult.Errors != null && importResult.Errors.Count > 0)
                    {
                        TxtImportLog.Text += "\n❌ Fehlerdetails:\n";
                        TxtImportLog.Text += string.Join("\n", importResult.Errors.Select(e => $"  - {e}"));
                    }
                    
                    MessageBox.Show($"Import fehlgeschlagen!\n\n{importResult.Message}\n\nFehler: {importResult.ErrorCount}", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                TxtImportLog.Text += $"❌ Exception: {ex.Message}\n";
                
                MessageBox.Show($"Fehler beim Import: {ex.Message}\n\nDetails im Log anzeigen.", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                BtnImport_Mitarbeiter.IsEnabled = true;
                BtnValidate_Mitarbeiter.IsEnabled = true;
                BtnSelectFile_Mitarbeiter.IsEnabled = true;
            }
        }

        private void ResetMitarbeiterUI()
        {
            _importData_Mitarbeiter.Clear();
            _selectedFilePath_Mitarbeiter = string.Empty;
            TxtSelectedFile_Mitarbeiter.Text = "Keine Datei ausgewählt";
            PreviewDataGrid_Mitarbeiter.ItemsSource = null;
            PreviewDataGrid_Mitarbeiter.Visibility = Visibility.Collapsed;
            EmptyState_Mitarbeiter.Visibility = Visibility.Visible;
            ValidationPanel_Mitarbeiter.Visibility = Visibility.Collapsed;
            TxtRecordCount_Mitarbeiter.Text = "0 Mitarbeiter geladen";
            BtnValidate_Mitarbeiter.IsEnabled = false;
            BtnImport_Mitarbeiter.IsEnabled = false;
            ChkClearExisting_Mitarbeiter.IsChecked = false;
        }

        #region User Management (CRUD)

        private List<UserDto> _allUsers = new();
        private List<UserDto> _filteredUsers = new();

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog(null, _userService);
            if (dialog.ShowDialog() == true)
            {
                await LoadUsersAsync();
            }
        }

        private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UserDto user)
            {
                var dialog = new UserEditDialog(user, _userService);
                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                }
            }
        }

        // Password reset button removed - users must change their own password
        // Admin cannot reset passwords to maintain security - users use the 🔑 button in main window
        // Method kept but disabled to avoid build errors if referenced elsewhere
        private async void BtnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            await Task.CompletedTask; // Suppress async warning
            MessageBox.Show(
                "Passwort-Reset durch Admin wurde aus Sicherheitsgründen entfernt.\n\n" +
                "Benutzer können ihr Passwort selbst über die Schaltfläche 🔑 im Hauptmenü ändern.",
                "Funktion deaktiviert",
                                  MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private async void TxtSearchUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchUser == null || BtnClearSearch == null) return;
            
            BtnClearSearch.Visibility = string.IsNullOrWhiteSpace(TxtSearchUser.Text) ? Visibility.Collapsed : Visibility.Visible;
            FilterUsers();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (TxtSearchUser != null)
            {
                TxtSearchUser.Text = string.Empty;
            }
        }

        private void FilterUsers()
        {
            if (UsersDataGrid == null || TxtSearchUser == null) return;

            var searchTerm = TxtSearchUser.Text?.ToLower() ?? "";
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _filteredUsers = new List<UserDto>(_allUsers);
                }
                else
                {
                _filteredUsers = _allUsers.Where(u =>
                    (u.Username?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.Email?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.FirstName?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.LastName?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();
            }

            UsersDataGrid.ItemsSource = null;
            UsersDataGrid.ItemsSource = _filteredUsers;
        }

        public async Task LoadUsersAsync()
        {
            try
            {
                if (_userService == null)
                {
                    MessageBox.Show("UserService ist nicht initialisiert.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

                var response = await _userService.GetAllUsersAsync();
                
                if (response.Success && response.Data != null)
                {
                    _allUsers = response.Data;
                    FilterUsers();
                }
                else
                {
                    MessageBox.Show($"Fehler beim Laden der Benutzer: {response.Message}", 
                                  "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
