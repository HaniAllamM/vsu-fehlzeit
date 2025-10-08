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
        
        // Benutzer import fields
        private UserService? _userService;
        private List<ImportBenutzerItem> _importData_Benutzer = new();
        private string _selectedFilePath_Benutzer = string.Empty;

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
                // Disable all import sections since they're now in tabs
                if (ImportSection != null)
                {
                    ImportSection.IsEnabled = false;
                    ImportSection.Opacity = 0.5;
                }
                
                // Show warning message in the main content area
                ShowAdminWarning();
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

        // ==================== BENUTZER IMPORT METHODS ====================

        private void BtnSelectFile_Benutzer_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Excel-Datei auswählen",
                Filter = "Excel Dateien (*.xlsx)|*.xlsx|Alle Dateien (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath_Benutzer = openFileDialog.FileName;
                TxtSelectedFile_Benutzer.Text = Path.GetFileName(_selectedFilePath_Benutzer);
                
                LoadBenutzerExcelFile(_selectedFilePath_Benutzer);
            }
        }

        private void LoadBenutzerExcelFile(string filePath)
        {
            try
            {
                _importData_Benutzer.Clear();
                
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
                    
                    foreach (var row in rows)
                    {
                        var username = row.Cell(1).GetString().Trim();
                        var email = row.Cell(2).GetString().Trim();
                        var role = row.Cell(3).GetString().Trim();
                        var isActive = row.Cell(4).GetString().Trim();
                        var password = row.Cell(5).GetString().Trim(); // Optional default password
                        
                        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email))
                        {
                            _importData_Benutzer.Add(new ImportBenutzerItem
                            {
                                Username = username,
                                Email = email,
                                Role = string.IsNullOrWhiteSpace(role) ? "User" : role,
                                IsActive = isActive == "1" || isActive.ToLower() == "true" || isActive.ToLower() == "aktiv",
                                Password = string.IsNullOrWhiteSpace(password) ? "DefaultPassword123!" : password
                            });
                        }
                    }
                }
                
                if (_importData_Benutzer.Count > 0)
                {
                    PreviewDataGrid_Benutzer.ItemsSource = _importData_Benutzer;
                    PreviewDataGrid_Benutzer.Visibility = Visibility.Visible;
                    EmptyState_Benutzer.Visibility = Visibility.Collapsed;
                    
                    TxtRecordCount_Benutzer.Text = $"{_importData_Benutzer.Count} Benutzer geladen";
                    BtnValidate_Benutzer.IsEnabled = true;
                    
                    ValidateBenutzerData();
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
                MessageBox.Show($"Fehler beim Lesen der Excel-Datei: {ex.Message}\n\nStellen Sie sicher, dass die Datei das richtige Format hat:\nSpalte A: Benutzername\nSpalte B: Email\nSpalte C: Rolle (Admin/User)\nSpalte D: Aktiv (1/0 oder true/false)\nSpalte E: Passwort (optional)", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                ResetBenutzerUI();
            }
        }

        private void BtnValidate_Benutzer_Click(object sender, RoutedEventArgs e)
        {
            ValidateBenutzerData();
        }

        private void ValidateBenutzerData()
        {
            try
            {
                var errors = new List<string>();
                
                // Check for empty usernames
                var emptyUsernames = _importData_Benutzer.Where(b => string.IsNullOrWhiteSpace(b.Username)).Count();
                if (emptyUsernames > 0)
                {
                    errors.Add($"❌ {emptyUsernames} Benutzer mit leerem Benutzernamen gefunden");
                }
                
                // Check for empty emails
                var emptyEmails = _importData_Benutzer.Where(b => string.IsNullOrWhiteSpace(b.Email)).Count();
                if (emptyEmails > 0)
                {
                    errors.Add($"❌ {emptyEmails} Benutzer mit leerer Email gefunden");
                }
                
                // Check for duplicate usernames
                var duplicateUsernames = _importData_Benutzer
                    .GroupBy(b => b.Username.ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                
                if (duplicateUsernames.Any())
                {
                    errors.Add($"❌ {duplicateUsernames.Count} doppelte Benutzernamen gefunden");
                }
                
                // Check for duplicate emails
                var duplicateEmails = _importData_Benutzer
                    .GroupBy(b => b.Email.ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                
                if (duplicateEmails.Any())
                {
                    errors.Add($"❌ {duplicateEmails.Count} doppelte Email-Adressen gefunden");
                }
                
                // Check for invalid email formats
                var invalidEmails = _importData_Benutzer
                    .Where(b => !string.IsNullOrWhiteSpace(b.Email) && !b.Email.Contains("@"))
                    .Count();
                
                if (invalidEmails > 0)
                {
                    errors.Add($"❌ {invalidEmails} Benutzer mit ungültigen Email-Adressen");
                }
                
                // Check for invalid roles
                var validRoles = new[] { "admin", "user" };
                var invalidRoles = _importData_Benutzer
                    .Where(b => !validRoles.Contains(b.Role.ToLower()))
                    .Count();
                
                if (invalidRoles > 0)
                {
                    errors.Add($"❌ {invalidRoles} Benutzer mit ungültigen Rollen (erlaubt: Admin, User)");
                }
                
                // Show results
                ValidationPanel_Benutzer.Visibility = Visibility.Visible;
                
                if (errors.Count == 0)
                {
                    ValidationPanel_Benutzer.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 234, 254));
                    ValidationPanel_Benutzer.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                    TxtValidationTitle_Benutzer.Text = "✓ Validierung erfolgreich";
                    TxtValidationTitle_Benutzer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175));
                    TxtValidationMessage_Benutzer.Text = $"Alle {_importData_Benutzer.Count} Benutzer sind gültig und bereit zum Importieren.";
                    TxtValidationMessage_Benutzer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
                    
                    BtnImport_Benutzer.IsEnabled = true;
                }
                else
                {
                    ValidationPanel_Benutzer.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242));
                    ValidationPanel_Benutzer.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    TxtValidationTitle_Benutzer.Text = "❌ Validierungsfehler";
                    TxtValidationTitle_Benutzer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                    TxtValidationMessage_Benutzer.Text = string.Join("\n", errors);
                    TxtValidationMessage_Benutzer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(153, 27, 27));
                    
                    BtnImport_Benutzer.IsEnabled = false;
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

        private async void BtnImport_Benutzer_Click(object sender, RoutedEventArgs e)
        {
            if (_userService == null || _importData_Benutzer.Count == 0)
            {
                MessageBox.Show("Keine Daten zum Importieren vorhanden.", 
                              "Warnung", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var confirmMessage = ChkClearExisting_Benutzer.IsChecked == true
                ? $"ACHTUNG: Alle bestehenden Benutzer werden gelöscht!\n\nMöchten Sie wirklich {_importData_Benutzer.Count} Benutzer importieren?"
                : $"Möchten Sie {_importData_Benutzer.Count} Benutzer importieren?";
            
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
                BtnImport_Benutzer.IsEnabled = false;
                BtnValidate_Benutzer.IsEnabled = false;
                BtnSelectFile_Benutzer.IsEnabled = false;
                
                LogSection.Visibility = Visibility.Visible;
                TxtImportLog.Text = "🔄 Benutzer-Import wird gestartet...\n";
                TxtImportLog.Text += $"📊 Anzahl Benutzer: {_importData_Benutzer.Count}\n";
                TxtImportLog.Text += $"🗑️ Bestehende löschen: {ChkClearExisting_Benutzer.IsChecked}\n\n";
                
                TxtImportLog.Text += $"📤 Sende Daten an Server...\n";
                
                var importResult = await _userService.BulkImportAsync(_importData_Benutzer, ChkClearExisting_Benutzer.IsChecked == true);
                
                TxtImportLog.Text += $"📥 Antwort erhalten: Success={importResult.Success}\n";
                
                if (importResult.Success)
                {
                    TxtImportLog.Text += $"✓ Erfolgreich: {importResult.InsertedCount} Benutzer importiert\n";
                    TxtImportLog.Text += $"✓ {importResult.Message}\n";
                    
                    MessageBox.Show($"Import erfolgreich!\n\n{importResult.InsertedCount} Benutzer wurden importiert.", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    ResetBenutzerUI();
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
                BtnImport_Benutzer.IsEnabled = true;
                BtnValidate_Benutzer.IsEnabled = true;
                BtnSelectFile_Benutzer.IsEnabled = true;
            }
        }

        private void ResetBenutzerUI()
        {
            _importData_Benutzer.Clear();
            _selectedFilePath_Benutzer = string.Empty;
            TxtSelectedFile_Benutzer.Text = "Keine Datei ausgewählt";
            PreviewDataGrid_Benutzer.ItemsSource = null;
            PreviewDataGrid_Benutzer.Visibility = Visibility.Collapsed;
            EmptyState_Benutzer.Visibility = Visibility.Visible;
            ValidationPanel_Benutzer.Visibility = Visibility.Collapsed;
            TxtRecordCount_Benutzer.Text = "0 Benutzer geladen";
            BtnValidate_Benutzer.IsEnabled = false;
            BtnImport_Benutzer.IsEnabled = false;
            ChkClearExisting_Benutzer.IsChecked = false;
        }
    }
}
