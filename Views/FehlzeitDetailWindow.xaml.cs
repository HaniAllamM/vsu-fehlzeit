using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using FehlzeitApp.Services;
using FehlzeitApp.Models;

namespace FehlzeitApp.Views
{
    // Converter to convert Color to SolidColorBrush
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Gray;
        }
    }

    public class SicknessData
    {
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = string.Empty; // Will contain the selected text from combobox
        public int KrankheitId { get; set; }
        public int MeldungId { get; set; } // This will store the actual database ID
        public int SelectedMitarbeiterId { get; set; } = 0;
        public string SelectedMitarbeiterName { get; set; } = string.Empty;
        public Meldung? SelectedMeldung { get; set; } // Store the selected Meldung with color
    }

    public partial class FehlzeitDetailWindow : Window
    {
        public SicknessData SicknessInfo { get; private set; }
        private readonly AuthService _authService;
        private KrankheitService? _krankheitService;
        private MeldungService? _meldungService;
        private FehlzeitService? _fehlzeitService;

        // Employee information from main page
        public int SelectedMitarbeiterId { get; set; } = 0;
        public string SelectedMitarbeiterName { get; set; } = string.Empty;

        public FehlzeitDetailWindow(AuthService authService)
        {
            InitializeComponent();
            
            // Initialize services
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            SicknessInfo = new SicknessData();
            SetGermanCulture();
            
            // Initialize services and combo boxes asynchronously
            _ = InitializeServicesAsync();
            
            // Add window drag functionality
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
            
            // Add entrance animation - using Window_Loaded method
            this.Loaded += Window_Loaded;
        }

        private void AnimateEntrance()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            var slideUp = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(400));
            
            slideUp.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            fadeIn.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            var transform = new TranslateTransform();
            this.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // This matches the login page animation
            this.Opacity = 0;
            AnimateEntrance();
            
            // Update the selected employee display
            UpdateSelectedEmployeeDisplay();
        }

        private void UpdateSelectedEmployeeDisplay()
        {
            if (!string.IsNullOrEmpty(SelectedMitarbeiterName))
            {
                txtSelectedEmployee.Text = $"{SelectedMitarbeiterName} (ID: {SelectedMitarbeiterId})";
            }
            else
            {
                txtSelectedEmployee.Text = "Kein Mitarbeiter ausgewählt";
            }
        }

        private void SetGermanCulture()
        {
            // Set DatePickers to German format
            dpVon.Language = System.Windows.Markup.XmlLanguage.GetLanguage("de-DE");
            dpBis.Language = System.Windows.Markup.XmlLanguage.GetLanguage("de-DE");
            
            // Set default dates
            dpVon.SelectedDate = DateTime.Today;
            dpBis.SelectedDate = DateTime.Today;
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                // Create service instances
                var configService = await ConfigurationService.CreateAsync();
                _krankheitService = new KrankheitService(_authService, configService);
                _meldungService = new MeldungService(_authService, configService);
                _fehlzeitService = new FehlzeitService(_authService, configService);

                // Initialize combo boxes after services are ready
                await InitializeComboBoxes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren der Services: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeComboBoxes()
        {
            // Load Krankheit data ONLY from database - no fallback data
            List<Krankheit> krankheiten = new List<Krankheit>();
            if (_krankheitService != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("=== CALLING KrankheitService.GetAllAsync() ===");
                    var krankheitenResponse = await _krankheitService.GetAllAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response Success: {krankheitenResponse.Success}");
                    System.Diagnostics.Debug.WriteLine($"API Response Message: {krankheitenResponse.Message}");
                    
                    if (krankheitenResponse.Success && krankheitenResponse.Data != null)
                    {
                        krankheiten = krankheitenResponse.Data;
                        System.Diagnostics.Debug.WriteLine($"Loaded {krankheiten.Count} Krankheit records from database");
                        
                        // Debug: Show each record
                        foreach (var k in krankheiten)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Krankheit: ID={k.KrankheitId}, Kurz='{k.Kurz}', Beschreibung='{k.Beschreibung}'");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No Krankheit data from database: {krankheitenResponse.Message}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Krankheit data: {ex.Message}");
                }
            }
            
            // Only populate if we have actual database data
            if (krankheiten.Count > 0)
            {
                // Add empty option at the top
                var krankheitList = new List<Krankheit> { new Krankheit { KrankheitId = 0, Kurz = "", Beschreibung = "Keine Auswahl" } };
                krankheitList.AddRange(krankheiten);
                cmbKrankheit.ItemsSource = krankheitList;
                cmbKrankheit.SelectedValuePath = "KrankheitId";
                cmbKrankheit.SelectedIndex = 0;
            }
            else
            {
                // No database data - show empty combobox
                cmbKrankheit.ItemsSource = new List<Krankheit> { new Krankheit { KrankheitId = 0, Kurz = "Keine Daten", Beschreibung = "" } };
                cmbKrankheit.SelectedValuePath = "KrankheitId";
                cmbKrankheit.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine("No Krankheit data available - showing empty combobox");
            }

            // Load Meldung data ONLY from database - no fallback data
            List<Meldung> meldungen = new List<Meldung>();
            if (_meldungService != null)
            {
                try
                {
                    var meldungenResponse = await _meldungService.GetAllAsync();
                    if (meldungenResponse.Success && meldungenResponse.Data != null)
                    {
                        meldungen = meldungenResponse.Data;
                        System.Diagnostics.Debug.WriteLine($"Loaded {meldungen.Count} Meldung records from database");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No Meldung data from database: {meldungenResponse.Message}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading Meldung data: {ex.Message}");
                }
            }
            
            // Only populate if we have actual database data
            if (meldungen.Count > 0)
            {
                // Don't add empty option - set "eingetragen" as default
                cmbMeldung.ItemsSource = meldungen;
                cmbMeldung.SelectedValuePath = "MeldungId";
                
                // Find and set "eingetragen" as default selection
                var eingetragenMeldung = meldungen.FirstOrDefault(m => 
                    m.Beschreibung != null && m.Beschreibung.ToLower().Contains("eingetragen"));
                
                if (eingetragenMeldung != null)
                {
                    cmbMeldung.SelectedItem = eingetragenMeldung;
                    System.Diagnostics.Debug.WriteLine($"Set default meldung to: {eingetragenMeldung.Beschreibung}");
                }
                else
                {
                    // Fallback: select first item if "eingetragen" not found
                    cmbMeldung.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine("'eingetragen' not found, selected first item as default");
                }
                
                // Add event handler for selection changes to show color preview
                cmbMeldung.SelectionChanged += CmbMeldung_SelectionChanged;
            }
            else
            {
                // No database data - show empty combobox
                cmbMeldung.ItemsSource = new List<Meldung> { new Meldung { MeldungId = 0, Beschreibung = "Keine Daten" } };
                cmbMeldung.SelectedValuePath = "MeldungId";
                cmbMeldung.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine("No Meldung data available - showing empty combobox");
            }
        }
        
        private void CmbMeldung_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When user selects a Meldung, we'll use this color when saving
            if (cmbMeldung.SelectedItem is Meldung selectedMeldung)
            {
                System.Diagnostics.Debug.WriteLine($"Selected Meldung: {selectedMeldung.Beschreibung} with color {selectedMeldung.Farbe}");
                
                // Store the selected Meldung for when we save the data
                // The color will be applied to the DataGrid when the user saves
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Animate close
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => 
            {
                this.DialogResult = false;
                this.Close();
            };
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateForm())
                {
                    return;
                }

                var von = dpVon.SelectedDate!.Value;
                var bis = dpBis.SelectedDate!.Value;
                var krankheit = cmbKrankheit.SelectedItem as Krankheit;
                var krankheitName = krankheit?.Kurz ?? cmbKrankheit.Text ?? "";
                var meldung = cmbMeldung.SelectedItem as Meldung;
                var meldungName = meldung?.Beschreibung ?? cmbMeldung.Text ?? "";

                // Store the sickness data
                SicknessInfo.StartDate = von;
                SicknessInfo.EndDate = bis;
                SicknessInfo.Status = krankheitName; // This will be used for display
                SicknessInfo.KrankheitId = krankheit?.KrankheitId ?? 0;
                SicknessInfo.MeldungId = meldung?.MeldungId ?? 0; // Store the actual database ID
                SicknessInfo.SelectedMitarbeiterId = SelectedMitarbeiterId;
                SicknessInfo.SelectedMitarbeiterName = SelectedMitarbeiterName;
                
                // Store the selected Meldung for color application
                SicknessInfo.SelectedMeldung = meldung;
                
                System.Diagnostics.Debug.WriteLine($"Saving Fehlzeit with Meldung: {meldungName} (Color: {meldung?.Farbe})");

                // Actually save to database via API
                await SaveToDatabase(von, bis, krankheit, meldung);

                // Close with success
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ein Fehler ist aufgetreten:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Error animation
                AnimateButton(btnSave, Colors.Red);
            }
        }

        private bool ValidateForm()
        {
            // Check if dates are selected
            if (dpVon.SelectedDate == null)
            {
                ShowValidationError("Bitte wählen Sie ein 'Von' Datum aus.", dpVon);
                return false;
            }

            if (dpBis.SelectedDate == null)
            {
                ShowValidationError("Bitte wählen Sie ein 'Bis' Datum aus.", dpBis);
                return false;
            }

            // Check date logic
            if (dpVon.SelectedDate > dpBis.SelectedDate)
            {
                ShowValidationError("Das 'Von' Datum darf nicht nach dem 'Bis' Datum liegen.", dpVon);
                return false;
            }

            // Check if dates are not in the far future
            if (dpVon.SelectedDate > DateTime.Today.AddDays(30))
            {
                ShowValidationError("Das 'Von' Datum liegt zu weit in der Zukunft.", dpVon);
                return false;
            }

            // Check if dates are not too far in the past
            if (dpBis.SelectedDate < DateTime.Today.AddDays(-365))
            {
                ShowValidationError("Das 'Bis' Datum liegt zu weit in der Vergangenheit.", dpBis);
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message, FrameworkElement focusElement)
        {
            MessageBox.Show(message, "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
            
            // Focus the problematic element
            focusElement.Focus();
            
            // Add shake animation
            ShakeElement(focusElement);
        }

        private void ShakeElement(FrameworkElement element)
        {
            var transform = new TranslateTransform();
            element.RenderTransform = transform;
            
            var shakeAnimation = new DoubleAnimationUsingKeyFrames();
            shakeAnimation.Duration = TimeSpan.FromMilliseconds(400);
            
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-5, KeyTime.FromPercent(0.1)));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromPercent(0.2)));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-3, KeyTime.FromPercent(0.3)));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(3, KeyTime.FromPercent(0.4)));
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));
            
            transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
        }

        private void AnimateButton(FrameworkElement button, Color color)
        {
            // Create a colored border flash effect
            var originalBackground = ((Button)button).Background;
            var colorBrush = new SolidColorBrush(color);
            
            var colorAnimation = new ColorAnimation();
            colorAnimation.To = color;
            colorAnimation.Duration = TimeSpan.FromMilliseconds(150);
            colorAnimation.AutoReverse = true;
            colorAnimation.Completed += (s, e) =>
            {
                ((Button)button).Background = originalBackground;
            };
            
            var animatedBrush = new SolidColorBrush();
            ((Button)button).Background = animatedBrush;
            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation for unsaved changes if form has data
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "Sie haben ungespeicherte Änderungen. Möchten Sie wirklich abbrechen?",
                    "Ungespeicherte Änderungen",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Animate close
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => 
            {
                this.DialogResult = false;
                this.Close();
            };
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private bool HasUnsavedChanges()
        {
            // Check if user has made any changes from defaults
            bool datesChanged = dpVon.SelectedDate != DateTime.Today || dpBis.SelectedDate != DateTime.Today;
            bool krankheitChanged = cmbKrankheit.SelectedIndex != 0;  // Empty option is selected by default (index 0)
            
            // For meldung, check if it's different from the default "eingetragen" selection
            bool meldungChanged = false;
            if (cmbMeldung.ItemsSource is List<Meldung> meldungen && meldungen.Count > 0)
            {
                var defaultMeldung = meldungen.FirstOrDefault(m => 
                    m.Beschreibung != null && m.Beschreibung.ToLower().Contains("eingetragen"));
                meldungChanged = cmbMeldung.SelectedItem != defaultMeldung;
            }
            
            return datesChanged || krankheitChanged || meldungChanged;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            // Handle keyboard shortcuts
            if (e.Key == Key.Escape)
            {
                BtnCancel_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnSave_Click(this, new RoutedEventArgs());
            }
        }

        // Additional helper methods for better UX
        private void OnDateChanged()
        {
            // Auto-calculate duration when dates change
            if (dpVon.SelectedDate.HasValue && dpBis.SelectedDate.HasValue)
            {
                var duration = (dpBis.SelectedDate.Value - dpVon.SelectedDate.Value).Days + 1;
                // Could show duration in UI if needed
            }
        }
        
        private async Task SaveToDatabase(DateTime von, DateTime bis, Krankheit? krankheit, Meldung? meldung)
        {
            try
            {
                // Wait for services to be initialized if they're not ready yet
                if (_fehlzeitService == null)
                {
                    System.Diagnostics.Debug.WriteLine("FehlzeitService is null - waiting for initialization...");
                    
                    // Wait up to 10 seconds for service initialization
                    int attempts = 0;
                    while (_fehlzeitService == null && attempts < 100)
                    {
                        await Task.Delay(100); // Wait 100ms
                        attempts++;
                    }
                    
                    if (_fehlzeitService == null)
                    {
                        MessageBox.Show("Fehler: Services konnten nicht initialisiert werden. Bitte versuchen Sie es erneut.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("FehlzeitService initialized successfully");
                }
                
                // Save each day in the date range
                for (DateTime date = von; date <= bis; date = date.AddDays(1))
                {
                    var createRequest = new CreateFehlzeitDayRequest
                    {
                        MitarbeiterId = SelectedMitarbeiterId,
                        Datum = date,
                        KrankheitId = krankheit?.KrankheitId ?? 0,
                        MeldungId = meldung?.MeldungId ?? 0,
                        Bemerkung = null
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Saving Fehlzeit for {date:yyyy-MM-dd}: Employee {SelectedMitarbeiterId}, Krankheit {krankheit?.Kurz}, Meldung {meldung?.Beschreibung}");
                    
                    System.Diagnostics.Debug.WriteLine($"Making API call to upsert Fehlzeit for {date:yyyy-MM-dd}...");
                    
                    var result = await _fehlzeitService.UpsertFehlzeitAsync(createRequest);
                    
                    System.Diagnostics.Debug.WriteLine($"API call completed. Success: {result.Success}, Message: {result.Message}");
                    
                    if (result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully saved Fehlzeit for {date:yyyy-MM-dd}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save Fehlzeit for {date:yyyy-MM-dd}: {result.Message}");
                        
                        // Show detailed error information
                        string errorDetails = $"Datum: {date:dd.MM.yyyy}\nFehler: {result.Message}";
                        if (result.Errors != null && result.Errors.Count > 0)
                        {
                            errorDetails += $"\nDetails: {string.Join(", ", result.Errors)}";
                        }
                        
                        MessageBox.Show(errorDetails, "API Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                        break; // Stop trying to save more days if one fails
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveToDatabase: {ex.Message}");
                MessageBox.Show($"Fehler beim Speichern in die Datenbank: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}