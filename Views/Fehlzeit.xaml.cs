using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using FehlzeitApp.Services;
using FehlzeitApp.Models;

namespace FehlzeitApp.Views
{
    public partial class Fehlzeit : UserControl
    {
        // --- Performance Optimization Properties ---
        private const int PAGE_SIZE = 12; // Show 12 records per page
        private const int MAX_EMPLOYEES_WITHOUT_PAGINATION = 50; // Auto-paginate above this
        private int _currentPage = 0;
        private int _totalPages = 0;
        private List<AttendanceRecord> _allRecords = new List<AttendanceRecord>();
        private bool _isLoading = false;
        private readonly System.Diagnostics.Stopwatch _performanceTimer = new();
        private readonly List<long> _loadTimes = new();
        private readonly Dictionary<string, List<AttendanceRecord>> _dataCache = new();
        private readonly Dictionary<string, List<Mitarbeiter>> _employeeCache = new();
        private CancellationTokenSource? _loadingCancellationToken;

        // --- Color Caching for Performance ---
        private static readonly Dictionary<int, (Brush Background, Brush Text)> _meldungColorCache = new();
        private static readonly object _colorCacheLock = new object();

        // --- Services ---
        private readonly AuthService _authService;
        private MitarbeiterService? _mitarbeiterService;
        private ObjektService? _objektService;
        private FehlzeitService? _fehlzeitService;
        private MeldungService? _meldungService;

        // --- Data classes ---
        public class AttendanceDay : INotifyPropertyChanged
        {
            private string _status = string.Empty;
            private Brush _backgroundColor = Brushes.Transparent;
            private Brush _textColor = Brushes.Black;
            private int _meldungId = 0;
            private string _meldungBeschreibung = string.Empty;

            public string Status
            {
                get => _status;
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged(nameof(Status));
                    }
                }
            }

            public Brush BackgroundColor
            {
                get => _backgroundColor;
                set
                {
                    if (_backgroundColor != value)
                    {
                        _backgroundColor = value;
                        OnPropertyChanged(nameof(BackgroundColor));
                    }
                }
            }

            public Brush TextColor
            {
                get => _textColor;
                set
                {
                    if (_textColor != value)
                    {
                        _textColor = value;
                        OnPropertyChanged(nameof(TextColor));
                    }
                }
            }

            public int MeldungId
            {
                get => _meldungId;
                set
                {
                    if (_meldungId != value)
                    {
                        _meldungId = value;
                        OnPropertyChanged(nameof(MeldungId));
                        // Update colors when Meldung changes
                        UpdateColorsBasedOnMeldung();
                    }
                }
            }

            public string MeldungBeschreibung
            {
                get => _meldungBeschreibung;
                set
                {
                    if (_meldungBeschreibung != value)
                    {
                        _meldungBeschreibung = value;
                        OnPropertyChanged(nameof(MeldungBeschreibung));
                    }
                }
            }

            private void UpdateColorsBasedOnMeldung()
            {
                // Use cached color lookup for better performance
                var colors = GetMeldungColors(_meldungId, _meldungBeschreibung);
                BackgroundColor = colors.Background;
                TextColor = colors.Text;
            }

            // Static method to get colors directly from database
            public static async Task<(Brush Background, Brush Text)> GetMeldungColorsFromDatabaseAsync(int meldungId, MeldungService? meldungService)
            {
                if (meldungService != null && meldungId > 0)
                {
                    try
                    {
                        var meldungResponse = await meldungService.GetByIdAsync(meldungId);
                        if (meldungResponse.Success && meldungResponse.Data != null && !string.IsNullOrEmpty(meldungResponse.Data.Farbe))
                        {
                            var meldung = meldungResponse.Data;
                            var color = meldung.FarbeColor;
                            var lightColor = Color.FromArgb(80, color.R, color.G, color.B); // Make it lighter for background
                            var darkColor = Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7)); // Darker for text
                            return (new SolidColorBrush(lightColor), new SolidColorBrush(darkColor));
                        }
                    }
                    catch
                    {
                    }
                }

                // Fallback to default white background
                return (Brushes.White, Brushes.Black);
            }

            private static (Brush Background, Brush Text) GetMeldungColors(int meldungId, string beschreibung)
            {
                // Fallback method for backward compatibility - returns default colors
                return (Brushes.White, Brushes.Black);
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Alternative AttendanceRecord class with explicit day properties
        // This approach ensures proper data binding notifications
        public class AttendanceRecord : INotifyPropertyChanged
        {
            private string _name = string.Empty;
            private int _mitarbeiterId = 0;

            public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
            public int MitarbeiterId { get => _mitarbeiterId; set { _mitarbeiterId = value; OnPropertyChanged(); } }

            // Explicit properties for each day (example for first few days)
            private AttendanceDay _day01 = new AttendanceDay();
            private AttendanceDay _day02 = new AttendanceDay();
            private AttendanceDay _day03 = new AttendanceDay();
            private AttendanceDay _day04 = new AttendanceDay();
            private AttendanceDay _day05 = new AttendanceDay();
            private AttendanceDay _day06 = new AttendanceDay();
            private AttendanceDay _day07 = new AttendanceDay();
            private AttendanceDay _day08 = new AttendanceDay();
            private AttendanceDay _day09 = new AttendanceDay();
            private AttendanceDay _day10 = new AttendanceDay();
            private AttendanceDay _day11 = new AttendanceDay();
            private AttendanceDay _day12 = new AttendanceDay();
            private AttendanceDay _day13 = new AttendanceDay();
            private AttendanceDay _day14 = new AttendanceDay();
            private AttendanceDay _day15 = new AttendanceDay();
            private AttendanceDay _day16 = new AttendanceDay();
            private AttendanceDay _day17 = new AttendanceDay();
            private AttendanceDay _day18 = new AttendanceDay();
            private AttendanceDay _day19 = new AttendanceDay();
            private AttendanceDay _day20 = new AttendanceDay();
            private AttendanceDay _day21 = new AttendanceDay();
            private AttendanceDay _day22 = new AttendanceDay();
            private AttendanceDay _day23 = new AttendanceDay();
            private AttendanceDay _day24 = new AttendanceDay();
            private AttendanceDay _day25 = new AttendanceDay();
            private AttendanceDay _day26 = new AttendanceDay();
            private AttendanceDay _day27 = new AttendanceDay();
            private AttendanceDay _day28 = new AttendanceDay();
            private AttendanceDay _day29 = new AttendanceDay();
            private AttendanceDay _day30 = new AttendanceDay();
            private AttendanceDay _day31 = new AttendanceDay();

            public AttendanceDay Day01 { get => _day01; set { _day01 = value; OnPropertyChanged(); } }
            public AttendanceDay Day02 { get => _day02; set { _day02 = value; OnPropertyChanged(); } }
            public AttendanceDay Day03 { get => _day03; set { _day03 = value; OnPropertyChanged(); } }
            public AttendanceDay Day04 { get => _day04; set { _day04 = value; OnPropertyChanged(); } }
            public AttendanceDay Day05 { get => _day05; set { _day05 = value; OnPropertyChanged(); } }
            public AttendanceDay Day06 { get => _day06; set { _day06 = value; OnPropertyChanged(); } }
            public AttendanceDay Day07 { get => _day07; set { _day07 = value; OnPropertyChanged(); } }
            public AttendanceDay Day08 { get => _day08; set { _day08 = value; OnPropertyChanged(); } }
            public AttendanceDay Day09 { get => _day09; set { _day09 = value; OnPropertyChanged(); } }
            public AttendanceDay Day10 { get => _day10; set { _day10 = value; OnPropertyChanged(); } }
            public AttendanceDay Day11 { get => _day11; set { _day11 = value; OnPropertyChanged(); } }
            public AttendanceDay Day12 { get => _day12; set { _day12 = value; OnPropertyChanged(); } }
            public AttendanceDay Day13 { get => _day13; set { _day13 = value; OnPropertyChanged(); } }
            public AttendanceDay Day14 { get => _day14; set { _day14 = value; OnPropertyChanged(); } }
            public AttendanceDay Day15 { get => _day15; set { _day15 = value; OnPropertyChanged(); } }
            public AttendanceDay Day16 { get => _day16; set { _day16 = value; OnPropertyChanged(); } }
            public AttendanceDay Day17 { get => _day17; set { _day17 = value; OnPropertyChanged(); } }
            public AttendanceDay Day18 { get => _day18; set { _day18 = value; OnPropertyChanged(); } }
            public AttendanceDay Day19 { get => _day19; set { _day19 = value; OnPropertyChanged(); } }
            public AttendanceDay Day20 { get => _day20; set { _day20 = value; OnPropertyChanged(); } }
            public AttendanceDay Day21 { get => _day21; set { _day21 = value; OnPropertyChanged(); } }
            public AttendanceDay Day22 { get => _day22; set { _day22 = value; OnPropertyChanged(); } }
            public AttendanceDay Day23 { get => _day23; set { _day23 = value; OnPropertyChanged(); } }
            public AttendanceDay Day24 { get => _day24; set { _day24 = value; OnPropertyChanged(); } }
            public AttendanceDay Day25 { get => _day25; set { _day25 = value; OnPropertyChanged(); } }
            public AttendanceDay Day26 { get => _day26; set { _day26 = value; OnPropertyChanged(); } }
            public AttendanceDay Day27 { get => _day27; set { _day27 = value; OnPropertyChanged(); } }
            public AttendanceDay Day28 { get => _day28; set { _day28 = value; OnPropertyChanged(); } }
            public AttendanceDay Day29 { get => _day29; set { _day29 = value; OnPropertyChanged(); } }
            public AttendanceDay Day30 { get => _day30; set { _day30 = value; OnPropertyChanged(); } }
            public AttendanceDay Day31 { get => _day31; set { _day31 = value; OnPropertyChanged(); } }

            // Method to get/set day by string key (for backward compatibility)
            public AttendanceDay GetDay(string dayKey)
            {
                return dayKey switch
                {
                    "Day01" => Day01,
                    "Day02" => Day02,
                    "Day03" => Day03,
                    "Day04" => Day04,
                    "Day05" => Day05,
                    "Day06" => Day06,
                    "Day07" => Day07,
                    "Day08" => Day08,
                    "Day09" => Day09,
                    "Day10" => Day10,
                    "Day11" => Day11,
                    "Day12" => Day12,
                    "Day13" => Day13,
                    "Day14" => Day14,
                    "Day15" => Day15,
                    "Day16" => Day16,
                    "Day17" => Day17,
                    "Day18" => Day18,
                    "Day19" => Day19,
                    "Day20" => Day20,
                    "Day21" => Day21,
                    "Day22" => Day22,
                    "Day23" => Day23,
                    "Day24" => Day24,
                    "Day25" => Day25,
                    "Day26" => Day26,
                    "Day27" => Day27,
                    "Day28" => Day28,
                    "Day29" => Day29,
                    "Day30" => Day30,
                    "Day31" => Day31,
                    _ => new AttendanceDay()
                };
            }

            public void SetDay(string dayKey, AttendanceDay day)
            {
                switch (dayKey)
                {
                    case "Day01": Day01 = day; break;
                    case "Day02": Day02 = day; break;
                    case "Day03": Day03 = day; break;
                    case "Day04": Day04 = day; break;
                    case "Day05": Day05 = day; break;
                    case "Day06": Day06 = day; break;
                    case "Day07": Day07 = day; break;
                    case "Day08": Day08 = day; break;
                    case "Day09": Day09 = day; break;
                    case "Day10": Day10 = day; break;
                    case "Day11": Day11 = day; break;
                    case "Day12": Day12 = day; break;
                    case "Day13": Day13 = day; break;
                    case "Day14": Day14 = day; break;
                    case "Day15": Day15 = day; break;
                    case "Day16": Day16 = day; break;
                    case "Day17": Day17 = day; break;
                    case "Day18": Day18 = day; break;
                    case "Day19": Day19 = day; break;
                    case "Day20": Day20 = day; break;
                    case "Day21": Day21 = day; break;
                    case "Day22": Day22 = day; break;
                    case "Day23": Day23 = day; break;
                    case "Day24": Day24 = day; break;
                    case "Day25": Day25 = day; break;
                    case "Day26": Day26 = day; break;
                    case "Day27": Day27 = day; break;
                    case "Day28": Day28 = day; break;
                    case "Day29": Day29 = day; break;
                    case "Day30": Day30 = day; break;
                    case "Day31": Day31 = day; break;
                }
            }

            // Method to update day status and colors
            public void UpdateDay(string dayKey, string status, Brush backgroundColor, Brush textColor)
            {
                var day = GetDay(dayKey);
                day.Status = status;
                day.BackgroundColor = backgroundColor;
                day.TextColor = textColor;

                // Trigger property change for the specific day
                OnPropertyChanged(dayKey);

                // Also trigger property change for the day object itself
                day.OnPropertyChanged(nameof(day.BackgroundColor));
                day.OnPropertyChanged(nameof(day.Status));
                day.OnPropertyChanged(nameof(day.TextColor));
            }

            // Method to update day with Meldung information (colors will be set automatically)
            public void UpdateDayWithMeldung(string dayKey, string status, int meldungId, string meldungBeschreibung)
            {
                var day = GetDay(dayKey);
                day.Status = status;
                day.MeldungId = meldungId; // This will automatically update colors via UpdateColorsBasedOnMeldung()
                day.MeldungBeschreibung = meldungBeschreibung;

                // Trigger property change for the specific day
                OnPropertyChanged(dayKey);

                // Also trigger property change for the day object itself
                day.OnPropertyChanged(nameof(day.BackgroundColor));
                day.OnPropertyChanged(nameof(day.Status));
                day.OnPropertyChanged(nameof(day.TextColor));
                day.OnPropertyChanged(nameof(day.MeldungId));
                day.OnPropertyChanged(nameof(day.MeldungBeschreibung));
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            // Public method to trigger property changes from outside the class
            public void TriggerPropertyChange(string propertyName)
            {
                OnPropertyChanged(propertyName);
            }
        }

        // --- Cached Color Lookup Method (Performance Optimization) ---
        private async Task<(Brush Background, Brush Text)> GetColorsFromCache(int meldungId)
        {
            // Handle MeldungId = 0 (default/no meldung) - from memory of previous fixes
            if (meldungId == 0)
            {
                return (new SolidColorBrush(Color.FromRgb(240, 240, 240)), Brushes.Black); // Light gray background
            }

            // Check cache first (thread-safe)
            lock (_colorCacheLock)
            {
                if (_meldungColorCache.ContainsKey(meldungId))
                {
                    return _meldungColorCache[meldungId];
                }
            }

            // Not in cache, load from database
            var colors = await AttendanceDay.GetMeldungColorsFromDatabaseAsync(meldungId, _meldungService);
            
            // Cache the result (thread-safe)
            lock (_colorCacheLock)
            {
                if (!_meldungColorCache.ContainsKey(meldungId))
                {
                    _meldungColorCache[meldungId] = colors;
                }
            }

            return colors;
        }

        // --- Holiday Data Classes ---
        public class GermanHoliday
        {
            public string Name { get; set; } = string.Empty;
            public DateTime Date2025 { get; set; }
            public DateTime Date2026 { get; set; }
            public DateTime Date2027 { get; set; }
            public List<string> Bundeslaender { get; set; } = new List<string>();
        }

        public class ObjektBundeslandMapping
        {
            public string ObjektName { get; set; } = string.Empty;
            public string Bundesland { get; set; } = string.Empty;
        }

        // --- Data Collections ---
        private List<GermanHoliday> germanHolidays = new();
        private List<ObjektBundeslandMapping> objektBundeslandMappings = new();
        private List<Mitarbeiter> _availableMitarbeiter = new();
        private List<string> _mitarbeiterNames = new();

        // --- Constructor ---
        public Fehlzeit(AuthService authService)
        {
            // IMMEDIATE CONSOLE OUTPUT TO VERIFY PAGE LOADING
            
            InitializeComponent();
            

            // Initialize services
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            
            
            // Initialize services asynchronously
            _ = InitializeServicesAsync();

            LoadHolidaysFromJson();
            InitializeObjektBundeslandMappings();
            // InitializeFilters will be called after services are ready
            
            
            // Services will be initialized and filters will be set up automatically
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                // Use shared ConfigurationService if available, otherwise create new one
                ConfigurationService configService;
                if (App.SharedConfigService != null)
                {
                    configService = App.SharedConfigService;
                }
                else
                {
                    configService = await ConfigurationService.CreateAsync();
                }

                // Create all service instances with shared config (performance optimization)
                _mitarbeiterService = new MitarbeiterService(_authService, configService);
                _objektService = new ObjektService(_authService, configService);
                _fehlzeitService = new FehlzeitService(_authService, configService);
                _meldungService = new MeldungService(_authService, configService);


                // Initialize filters after services are ready
                Dispatcher.Invoke(() => InitializeFilters());
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => 
                {
                    MessageBox.Show($"Fehler beim Initialisieren der Services: {ex.Message}", "Fehler", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        // --- Load Employees for Location Async ---
        private async Task LoadEmployeesForLocationAsync(string selectedLocation)
        {
            try
            {
                UpdateStatus("Lade Mitarbeiter...");
                
                var mitarbeiterResponse = await _mitarbeiterService!.GetAllAsync();
                var allMitarbeiter = mitarbeiterResponse.Success ? mitarbeiterResponse.Data ?? new List<Mitarbeiter>() : new List<Mitarbeiter>();
                
                _availableMitarbeiter = allMitarbeiter
                    .Where(m => m.Objektname != null && m.Objektname.Equals(selectedLocation, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Extract employee names for auto-complete
                _mitarbeiterNames = _availableMitarbeiter
                    .Select(m => m.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                // Update UI on main thread
                Dispatcher.Invoke(() =>
                {
                    // Clear DataGrid when Objekt is selected - user must click "Load" to see data
                    dgAttendance.ItemsSource = null;
                    dgAttendance.Columns.Clear();
                    
                    // Update status message
                    if (_availableMitarbeiter.Count == 0)
                    {
                        UpdateStatus($"Keine Mitarbeiter für Standort '{selectedLocation}' gefunden. Klicken Sie 'Daten laden' um fortzufahren.");
                    }
                    else
                    {
                        UpdateStatus($"{_availableMitarbeiter.Count} Mitarbeiter für Standort '{selectedLocation}' verfügbar. Klicken Sie 'Daten laden' um die Fehlzeiten anzuzeigen.");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateStatus($"Fehler beim Laden der Mitarbeiter: {ex.Message}");
                });
            }
        }

        // --- Show All Fehlzeit Data After Login ---
        private async Task ShowAllFehlzeitDataAsync()
        {
            try
            {
                if (_fehlzeitService == null)
                {
                    MessageBox.Show("Services not ready yet. Please try again.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Load ALL Fehlzeit data for October 2025 without filtering by employee
                var request = new FehlzeitListRequest
                {
                    MitarbeiterId = null, // Get ALL employees
                    VonDatum = new DateTime(2025, 10, 1),
                    BisDatum = new DateTime(2025, 10, 31)
                };

                var response = await _fehlzeitService.GetFehlzeitenAsync(request);
                
                if (response.Success && response.Data != null)
                {
                    try
                    {
                        JsonElement dataToProcess;
                        
                        if (response.Data is JsonElement jsonElement)
                        {
                            // Check if it's directly an array FIRST: [...]
                            if (jsonElement.ValueKind == JsonValueKind.Array)
                            {
                                dataToProcess = jsonElement;
                            }
                            // Then check if it's wrapped: {"success":true, "data":[...]} 
                            else if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("data", out var wrappedData))
                            {
                                dataToProcess = wrappedData;
                            }
                            else
                            {
                                MessageBox.Show($"Unexpected JSON structure. Root element kind: {jsonElement.ValueKind}\n\nExpected: Array or Object with 'data' property", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Response.Data is not JsonElement: {response.Data.GetType()}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        var fehlzeitList = new List<string>();
                        
                        foreach (var item in dataToProcess.EnumerateArray())
                        {
                            try
                            {
                                var empId = item.GetProperty("mitarbeiterId").GetInt32();
                                var datum = item.GetProperty("datum").GetDateTime();
                                var krankheitKurz = item.TryGetProperty("krankheitKurz", out var kk) ? kk.GetString() : "?";
                                var empName = item.TryGetProperty("mitarbeiterName", out var mn) ? mn.GetString() : "Unknown";
                                
                                fehlzeitList.Add($"Employee {empId} ({empName}): {datum:yyyy-MM-dd} = '{krankheitKurz}'");
                            }
                            catch (Exception itemEx)
                            {
                                fehlzeitList.Add($"ERROR processing item: {itemEx.Message}");
                            }
                        }
                        
                    }
                    catch (Exception parseEx)
                    {
                        // Show parsing error
                        MessageBox.Show($"JSON parsing error: {parseEx.Message}", "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load Fehlzeit data: {response.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Fehlzeit data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Load holidays from JSON ---
        private void LoadHolidaysFromJson()
        {
            try
            {
                // Try multiple possible paths for the JSON file
                string[] possiblePaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Holidays.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "Holidays.json")
                };

                string? path = null;
                foreach (var testPath in possiblePaths)
                {
                    if (File.Exists(testPath))
                    {
                        path = testPath;
                        break;
                    }
                }

                if (path != null)
                {
                    string json = File.ReadAllText(path);
                    germanHolidays = JsonSerializer.Deserialize<List<GermanHoliday>>(json) ?? new List<GermanHoliday>();
                }
                else
                {
                    MessageBox.Show("Holidays.json file not found! Searched in multiple locations.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading Holidays.json: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Objekt mappings ---
        private void InitializeObjektBundeslandMappings()
        {
            objektBundeslandMappings = new List<ObjektBundeslandMapping>
            {
                new() { ObjektName = "Hauptgebäude", Bundesland = "Berlin" },
                new() { ObjektName = "Nebengebäude", Bundesland = "Brandenburg" },
                new() { ObjektName = "Lager", Bundesland = "Bayern" }
            };
        }

        private string GetBundeslandForObjekt(string objektName)
        {
            // First try to find in hardcoded mappings
            var mapping = objektBundeslandMappings.FirstOrDefault(m => m.ObjektName == objektName);
            if (mapping != null)
            {
                return mapping.Bundesland;
            }

            // If not found, try to get from database locations
            // Note: This would need to be async in a real implementation
            // For now, return default since we don't have sync methods
            // var standorte = await _objektService.GetAllAsync();
            // var standort = standorte.Data?.FirstOrDefault(s => s.ObjektName == objektName);

            // For now, return "bundesweit" as default since we don't have location-to-state mapping in database
            return "bundesweit";
        }

        private void InitializeFilters()
        {
            
            try
            {
                // Load locations from database FIRST
                // Note: Making this async call synchronous for now - should be refactored
                List<Objekt> standorte = new List<Objekt>();
                if (_objektService != null)
                {
                    var standorteResponse = Task.Run(async () => await _objektService.GetAllAsync()).Result;
                    standorte = standorteResponse.Success ? standorteResponse.Data ?? new List<Objekt>() : new List<Objekt>();
                }
                
                if (standorte.Count > 0)
                {
                    var objektNames = standorte.Select(s => s.ObjektName).ToList();
                    cmbObjekt.ItemsSource = objektNames;
                    
                    // Add event handler for selection changes
                    cmbObjekt.SelectionChanged += CmbObjekt_SelectionChanged;
                    
                    // IMPORTANT: Set to -1 (no selection) to force user to choose
                    cmbObjekt.SelectedIndex = -1;
                }
                else
                {
                    // Fallback to hardcoded data if no locations in database
                    var fallbackItems = objektBundeslandMappings.Select(m => m.ObjektName).ToList();
                    cmbObjekt.ItemsSource = fallbackItems;
                    cmbObjekt.SelectionChanged += CmbObjekt_SelectionChanged;
                    
                    // Set to -1 (no selection) for fallback data too
                    cmbObjekt.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Standorte: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fallback to hardcoded data
                var fallbackItems = objektBundeslandMappings.Select(m => m.ObjektName).ToList();
                cmbObjekt.ItemsSource = fallbackItems;
                cmbObjekt.SelectionChanged += CmbObjekt_SelectionChanged;
                
                // Set to -1 (no selection) for error fallback too
                cmbObjekt.SelectedIndex = -1;
            }

            // Initialize month ComboBox
            var months = new List<string> { "Januar","Februar","März","April","Mai","Juni",
                                           "Juli","August","September","Oktober","November","Dezember" };
            cmbMonat.ItemsSource = months;
            cmbMonat.SelectedIndex = DateTime.Now.Month - 1;

            // Initialize year ComboBox
            int year = DateTime.Now.Year;
            var years = new List<int> { year - 1, year, year + 1 };
            cmbJahr.ItemsSource = years;
            cmbJahr.SelectedItem = year;

            // Initialize employee TextBox with simple, working placeholder handling
            txtMitarbeiter.IsReadOnly = false;
            txtMitarbeiter.IsEnabled = true;
            txtMitarbeiter.Text = "Mitarbeiter eingeben...";
            txtMitarbeiter.Foreground = Brushes.Gray;
            
            // Simple event handlers
            txtMitarbeiter.GotFocus += (s, e) =>
            {
                if (txtMitarbeiter.Text == "Mitarbeiter eingeben...")
                {
                    txtMitarbeiter.Text = "";
                    txtMitarbeiter.Foreground = Brushes.Black;
                }
            };
            
            txtMitarbeiter.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMitarbeiter.Text))
                {
                    txtMitarbeiter.Text = "Mitarbeiter eingeben...";
                    txtMitarbeiter.Foreground = Brushes.Gray;
                }
            };

            // Add auto-complete functionality to employee text box
            txtMitarbeiter.TextChanged += TxtMitarbeiter_TextChanged;
        }

        private void TxtMitarbeiter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Check if text is cleared or in placeholder state
                if (txtMitarbeiter.Text == "Mitarbeiter eingeben..." || string.IsNullOrWhiteSpace(txtMitarbeiter.Text))
                {
                    // Text is cleared - just return, don't auto-load data
                    return;
                }

                // Only show auto-complete if we have a location selected and employees loaded
                if (cmbObjekt.SelectedIndex == -1 || _availableMitarbeiter.Count == 0)
                {
                    return;
                }

                string searchText = txtMitarbeiter.Text.ToLower();
                
                // Simple auto-complete: show matching employee names
                var matchingEmployees = _availableMitarbeiter
                    .Where(m => m.Name != null && m.Name.ToLower().Contains(searchText))
                    .Take(5) // Limit to 5 suggestions
                    .Select(m => m.Name)
                    .ToList();

                // For now, just log the suggestions (you could implement a dropdown later)
                if (matchingEmployees.Count > 0)
                {
                }
                
                // Show autocomplete suggestions only - don't auto-load data
            }
            catch (Exception)
            {
            }
        }

        // REMOVED: ReloadAttendanceData() method to prevent automatic data loading
        // All data loading should only happen through the "Load" button (BtnLaden_Click)

        private void CmbObjekt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbObjekt.SelectedItem == null)
                {
                    _availableMitarbeiter.Clear();
                    _mitarbeiterNames.Clear();
                    return;
                }

                string selectedLocation = cmbObjekt.SelectedItem?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(selectedLocation))
                {
                    _availableMitarbeiter.Clear();
                    _mitarbeiterNames.Clear();
                    return;
                }

                // Load employees for the selected location from database
                if (_mitarbeiterService != null)
                {
                    // Use async/await properly - convert to async method
                    _ = LoadEmployeesForLocationAsync(selectedLocation);
                    return; // Exit early, LoadEmployeesForLocationAsync will handle the rest
                }

                // Reset employee text box to placeholder if it was previously filled
                if (txtMitarbeiter.Text != "Mitarbeiter eingeben..." && !_mitarbeiterNames.Contains(txtMitarbeiter.Text))
                {
                    txtMitarbeiter.Text = "Mitarbeiter eingeben...";
                    txtMitarbeiter.Foreground = Brushes.Gray;
                }
                
                // Clear DataGrid when Objekt is selected - user must click "Load" to see data
                dgAttendance.ItemsSource = null;
                dgAttendance.Columns.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Mitarbeiter für den Standort: {ex.Message}",
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                _availableMitarbeiter.Clear();
                _mitarbeiterNames.Clear();
            }
        }

        // Performance constants
        private System.Diagnostics.Stopwatch? _loadingStopwatch;

        private void UpdateStatus(string message)
        {
            // Ensure UI updates happen on the UI thread
            if (Dispatcher.CheckAccess())
            {
                // We're on the UI thread, safe to update
                UpdateStatusInternal(message);
            }
            else
            {
                // We're on a background thread, marshal to UI thread
                Dispatcher.BeginInvoke(new Action(() => UpdateStatusInternal(message)));
            }
        }

        private void UpdateStatusInternal(string message)
        {
            // Update performance status
            if (txtPerformanceStatus != null)
            {
                txtPerformanceStatus.Text = message;
            }

            // Update cache info
            if (txtCacheInfo != null)
            {
                int totalCacheEntries = _dataCache.Count + _employeeCache.Count;
                txtCacheInfo.Text = $"{totalCacheEntries} Einträge";
            }

            // For debugging
        }

        private void StartPerformanceTimer()
        {
            _loadingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        }

        private void StopPerformanceTimer()
        {
            if (_loadingStopwatch != null)
            {
                _loadingStopwatch.Stop();
                
                // Ensure UI updates happen on the UI thread
                if (Dispatcher.CheckAccess())
                {
                    UpdateLoadTimeDisplay();
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(UpdateLoadTimeDisplay));
                }
                
                _loadingStopwatch = null;
            }
        }

        private void UpdateLoadTimeDisplay()
        {
            if (txtLoadTime != null && _loadingStopwatch != null)
            {
                txtLoadTime.Text = $"{_loadingStopwatch.ElapsedMilliseconds}ms";
            }
        }

        private async void RefreshDataGridAfterUpdate(AttendanceRecord updatedRecord, SicknessData sicknessData)
        {
            try
            {
                // Clear cache to force fresh data reload
                string selectedObjekt = cmbObjekt.SelectedItem as string ?? "";
                int month = cmbMonat.SelectedIndex + 1;
                int year = cmbJahr.SelectedItem is int selectedYear ? selectedYear : DateTime.Now.Year;
                string? employeeFilter = (txtMitarbeiter.Text == "Mitarbeiter eingeben..." || string.IsNullOrWhiteSpace(txtMitarbeiter.Text)) ? null : txtMitarbeiter.Text;
                
                string cacheKey = $"{selectedObjekt}_{month}_{year}_{employeeFilter ?? "all"}";
                if (_dataCache.ContainsKey(cacheKey))
                {
                    _dataCache.Remove(cacheKey);
                }

                // Reload the data for this specific employee
                await ReloadEmployeeData(updatedRecord.MitarbeiterId, month, year);
                
                // Optimized DataGrid refresh - single efficient operation
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dgAttendance.ItemsSource is ICollectionView view)
                    {
                        view.Refresh(); // This is sufficient for most cases
                    }
                    else
                    {
                        dgAttendance.Items.Refresh(); // Fallback if no ICollectionView
                    }
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Aktualisieren der Anzeige: {ex.Message}", "Fehler",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task ReloadEmployeeData(int mitarbeiterId, int month, int year)
        {
            try
            {
                // Reload Fehlzeit data for this specific employee
                var fehlzeitData = await LoadFehlzeitDataForEmployeeAsync(mitarbeiterId, month, year);
                
                // Find the record in the current DataGrid items
                if (dgAttendance.ItemsSource is IEnumerable<AttendanceRecord> records)
                {
                    var recordToUpdate = records.FirstOrDefault(r => r.MitarbeiterId == mitarbeiterId);
                    if (recordToUpdate != null)
                    {
                        // Update the record with new data
                        await UpdateRecordWithFehlzeitData(recordToUpdate, month, year, fehlzeitData);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async Task UpdateRecordWithFehlzeitData(AttendanceRecord record, int month, int year, List<FehlzeitDay> fehlzeitData)
        {
            string selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
            string selectedBundesland = GetBundeslandForObjekt(selectedObjekt);
            
            // Pre-calculate holidays for the month
            var monthHolidays = new HashSet<DateTime>();
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
            {
                DateTime date = new DateTime(year, month, d);
                if (IsHoliday(date, selectedBundesland))
                {
                    monthHolidays.Add(date);
                }
            }
            
            // Create a lookup for quick date-based access
            var fehlzeitLookup = fehlzeitData.ToDictionary(f => f.Datum.Date, f => f);
            
            // Process each day of the month
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
            {
                DateTime date = new DateTime(year, month, d);
                string dayKey = $"Day{d:D2}";

                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                bool isHoliday = monthHolidays.Contains(date);

                if (isWeekend)
                {
                    record.UpdateDay(dayKey, "", Brushes.LightGray, Brushes.DarkGray);
                }
                else if (isHoliday)
                {
                    record.UpdateDay(dayKey, "", Brushes.LightCoral, Brushes.Black);
                }
                else
                {
                    // Check if we have Fehlzeit data for this specific date
                    if (fehlzeitLookup.ContainsKey(date.Date))
                    {
                        var fehlzeit = fehlzeitLookup[date.Date];
                        string status = fehlzeit.KrankheitKurz ?? "F";
                        
                        // Get colors based on Meldung (using cached lookup for performance)
                        var colors = await GetColorsFromCache(fehlzeit.MeldungId ?? 0);
                        
                        record.UpdateDay(dayKey, status, colors.Background, colors.Text);
                    }
                    else
                    {
                        // No Fehlzeit data - normal working day
                        record.UpdateDay(dayKey, "", Brushes.White, Brushes.Black);
                    }
                }
            }
        }

        // Optional: Add this method to test if updates work at all
        // Simple test method to check if color binding works
        private void TestColorBinding()
        {
            try
            {
                if (dgAttendance.Items.Count > 0)
                {
                    var firstRecord = dgAttendance.Items[0] as AttendanceRecord;
                    if (firstRecord != null)
                    {
                        // Set a test color
                        var testDay = firstRecord.GetDay("Day01");
                        testDay.BackgroundColor = Brushes.Red;
                        testDay.Status = "";

                        // Force refresh
                        firstRecord.TriggerPropertyChange("Day01");

                        // Force DataGrid refresh
                        dgAttendance.Items.Refresh();
                        dgAttendance.UpdateLayout();


                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void ForceVisualRefresh()
        {
            try
            {
                // Force complete visual refresh
                dgAttendance.InvalidateVisual();
                dgAttendance.UpdateLayout();

                // Refresh all cells
                foreach (var item in dgAttendance.Items)
                {
                    var row = dgAttendance.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null)
                    {
                        row.InvalidateVisual();
                        // Force refresh of all cells in the row
                        for (int i = 0; i < dgAttendance.Columns.Count; i++)
                        {
                            var cell = FindVisualChild<DataGridCell>(row, i);
                            if (cell != null)
                            {
                                cell.InvalidateVisual();
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
            }
        }
        // Helper method to find visual child by index
        private T? FindVisualChild<T>(DependencyObject parent, int index) where T : DependencyObject
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T typedChild && i == index)
                    {
                        return typedChild;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgAttendance.Items.Count == 0)
                {
                    MessageBox.Show("Keine Daten zum Drucken vorhanden. Bitte laden Sie zuerst Daten.");
                    return;
                }

                // Create a print-friendly DataGrid with proper data binding
                var printGrid = CreatePrintFriendlyDataGrid();

                // Create print dialog with landscape orientation
                PrintDialog pd = new PrintDialog();
                var printTicket = pd.PrintTicket;
                printTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                pd.PrintTicket = printTicket;

                if (pd.ShowDialog() == true)
                {
                    // Print the optimized DataGrid
                    pd.PrintVisual(printGrid, "Fehlzeit-Übersicht");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Druckfehler: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                              "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataGrid CreatePrintFriendlyDataGrid()
        {
            var printGrid = new DataGrid
            {
                ItemsSource = dgAttendance.ItemsSource,
                RowHeight = 30,
                FontSize = 9,
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                HorizontalGridLinesBrush = Brushes.LightGray,
                VerticalGridLinesBrush = Brushes.LightGray,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.All,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false,
                CanUserResizeColumns = false,
                CanUserResizeRows = false,
                IsReadOnly = true,
                AutoGenerateColumns = false,
                MaxHeight = 800, // Limit height to fit on page
                Margin = new Thickness(10)
            };

            // Clear existing columns
            printGrid.Columns.Clear();

            // Copy all columns from the original DataGrid with proper bindings
            foreach (var column in dgAttendance.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    var newColumn = new DataGridTextColumn
                    {
                        Header = textColumn.Header,
                        Binding = textColumn.Binding,
                        Width = new DataGridLength(80), // Fixed width for print
                        IsReadOnly = true
                    };
                    printGrid.Columns.Add(newColumn);
                }
                else if (column is DataGridTemplateColumn templateColumn)
                {
                    // For template columns, create a text column with appropriate binding
                    var newColumn = new DataGridTextColumn
                    {
                        Header = templateColumn.Header,
                        Width = new DataGridLength(80),
                        IsReadOnly = true
                    };

                    // Try to extract binding from template column
                    if (templateColumn.CellTemplate != null)
                    {
                        // Create a simple text binding for template columns
                        var binding = new Binding();
                        if (templateColumn.Header.ToString() == "Name")
                        {
                            binding.Path = new PropertyPath("Name");
                        }
                        else if (templateColumn.Header.ToString().StartsWith("Day"))
                        {
                            var dayNumber = templateColumn.Header.ToString().Replace("Day", "");
                            binding.Path = new PropertyPath($"Day{dayNumber}.Status");
                        }
                        newColumn.Binding = binding;
                    }

                    printGrid.Columns.Add(newColumn);
                }
            }

            // Apply print-friendly styling
            var printStyle = new Style(typeof(DataGrid));
            printStyle.Setters.Add(new Setter(DataGrid.BackgroundProperty, Brushes.White));
            printStyle.Setters.Add(new Setter(DataGrid.BorderBrushProperty, Brushes.Black));
            printStyle.Setters.Add(new Setter(DataGrid.BorderThicknessProperty, new Thickness(1)));
            printStyle.Setters.Add(new Setter(DataGrid.FontSizeProperty, 9.0));
            printStyle.Setters.Add(new Setter(DataGrid.RowHeightProperty, 30.0));

            // Column header style
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, Brushes.LightGray));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, Brushes.Black));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 10.0));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.PaddingProperty, new Thickness(5)));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderBrushProperty, Brushes.Black));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty, new Thickness(1)));

            // Cell style
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.LightGray));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0.5)));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(3)));
            cellStyle.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Center));
            cellStyle.Setters.Add(new Setter(DataGridCell.HorizontalAlignmentProperty, HorizontalAlignment.Center));

            printGrid.Style = printStyle;
            printGrid.ColumnHeaderStyle = headerStyle;
            printGrid.CellStyle = cellStyle;

            return printGrid;
        }

        private void BtnPrintAdvanced_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgAttendance.Items.Count == 0)
                {
                    MessageBox.Show("Keine Daten zum Drucken vorhanden. Bitte laden Sie zuerst Daten.");
                    return;
                }

                // Create a formatted print document
                var printDocument = CreateFormattedPrintDocument();

                // Create print dialog with landscape orientation
                PrintDialog pd = new PrintDialog();
                var printTicket = pd.PrintTicket;
                printTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                pd.PrintTicket = printTicket;

                if (pd.ShowDialog() == true)
                {
                    // Print the formatted document
                    pd.PrintVisual(printDocument, "Fehlzeit-Übersicht (Formatiert)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Druckfehler: {ex.Message}\n\nStackTrace: {ex.StackTrace}",
                              "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Grid CreateFormattedPrintDocument()
        {
            var printDocument = new Grid
            {
                Background = Brushes.White,
                Margin = new Thickness(20)
            };

            // Add title
            var titleText = new TextBlock
            {
                Text = "Fehlzeit-Übersicht",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Add date range info
            var dateInfo = new TextBlock
            {
                Text = $"Standort: {cmbObjekt.SelectedItem} | Monat: {cmbMonat.SelectedItem} | Jahr: {cmbJahr.SelectedItem}",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Create a formatted table
            var table = CreateFormattedTable();

            printDocument.Children.Add(titleText);
            printDocument.Children.Add(dateInfo);
            printDocument.Children.Add(table);

            return printDocument;
        }

        private Border CreateFormattedTable()
        {
            var table = new Grid
            {
                Background = Brushes.White
            };
            
            // Add border to the table
            var tableBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = table
            };

            // Define columns
            var columns = new List<string> { "Name" };
            for (int i = 1; i <= 31; i++)
            {
                columns.Add($"Day{i}");
            }

            // Add column definitions
            table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Name column
            for (int i = 1; i <= 31; i++)
            {
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // Day columns
            }

            // Add header row
            var headerRow = new Grid
            {
                Background = Brushes.LightGray,
                Height = 30
            };

            // Copy column definitions to header row
            foreach (var colDef in table.ColumnDefinitions)
            {
                headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
            }

            // Add header cells
            for (int i = 0; i < columns.Count; i++)
            {
                var headerCell = new Border
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.5),
                    Child = new TextBlock
                    {
                        Text = i == 0 ? "Name" : i.ToString(),
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                };
                Grid.SetColumn(headerCell, i);
                headerRow.Children.Add(headerCell);
            }

            table.Children.Add(headerRow);

            // Add data rows
            int rowIndex = 1;
            foreach (var item in dgAttendance.Items)
            {
                if (item is AttendanceRecord record)
                {
                    var dataRow = new Grid
                    {
                        Height = 25,
                        Background = rowIndex % 2 == 0 ? Brushes.White : Brushes.LightBlue
                    };

                    // Copy column definitions to data row
                    foreach (var colDef in table.ColumnDefinitions)
                    {
                        dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
                    }

                    // Add name cell
                    var nameCell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Child = new TextBlock
                        {
                            Text = record.Name,
                            FontSize = 8,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(2)
                        }
                    };
                    Grid.SetColumn(nameCell, 0);
                    dataRow.Children.Add(nameCell);

                    // Add day cells
                    for (int day = 1; day <= 31; day++)
                    {
                        var dayProperty = typeof(AttendanceRecord).GetProperty($"Day{day:D2}");
                        if (dayProperty != null)
                        {
                            var dayData = dayProperty.GetValue(record) as DayData;
                            var dayCell = new Border
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = new Thickness(0.5),
                                Child = new TextBlock
                                {
                                    Text = dayData?.Status ?? "",
                                    FontSize = 7,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    TextAlignment = TextAlignment.Center
                                }
                            };
                            Grid.SetColumn(dayCell, day);
                            dataRow.Children.Add(dayCell);
                        }
                    }

                    table.Children.Add(dataRow);
                    rowIndex++;
                }
            }

            return tableBorder;
        }

        private void BtnPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgAttendance.Items.Count == 0)
                {
                    MessageBox.Show("Keine Daten zum Anzeigen vorhanden. Bitte laden Sie zuerst Daten.");
                    return;
                }

                // Create a preview window
                var previewWindow = new Window
                {
                    Title = "Druckvorschau - Fehlzeit-Übersicht",
                    Width = 1000,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White
                };

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                var printDocument = CreateFormattedPrintDocument();
                scrollViewer.Content = printDocument;
                previewWindow.Content = scrollViewer;

                // Add print button to preview window
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };

                var printBtn = new Button
                {
                    Content = "Drucken",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(5)
                };
                printBtn.Click += (s, args) =>
                {
                    PrintDialog pd = new PrintDialog();
                    var printTicket = pd.PrintTicket;
                    printTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                    pd.PrintTicket = printTicket;

                    if (pd.ShowDialog() == true)
                    {
                        pd.PrintVisual(printDocument, "Fehlzeit-Übersicht");
                        previewWindow.Close();
                    }
                };

                var closeBtn = new Button
                {
                    Content = "Schließen",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(5)
                };
                closeBtn.Click += (s, args) => previewWindow.Close();

                buttonPanel.Children.Add(printBtn);
                buttonPanel.Children.Add(closeBtn);

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetRow(scrollViewer, 0);
                Grid.SetRow(buttonPanel, 1);

                mainGrid.Children.Add(scrollViewer);
                mainGrid.Children.Add(buttonPanel);

                previewWindow.Content = mainGrid;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Vorschau-Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Performance Optimized Button click ---
        private async void BtnLaden_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // IMMEDIATE CONSOLE OUTPUT
                
                // CRITICAL FIX: Clear cache to force fresh data from WebAPI
                ClearCache();

                if (_isLoading)
                {
                    MessageBox.Show("Daten werden bereits geladen. Bitte warten Sie.", "Info", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Check if location is selected - now checking for SelectedIndex = -1
                if (cmbObjekt.SelectedIndex == -1 || cmbObjekt.SelectedItem == null || string.IsNullOrEmpty(cmbObjekt.SelectedItem.ToString()))
                {
                    MessageBox.Show(
                        "Bitte wählen Sie zuerst einen Standort aus!\n\n" +
                        "Klicken Sie auf das Dropdown-Menü 'Standort' und wählen Sie einen Eintrag aus der Liste.\n\n" +
                        "Ohne Standort-Auswahl können keine Mitarbeiterdaten geladen werden.",
                        "Standort auswählen erforderlich", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // Focus the ComboBox to help user
                    cmbObjekt.Focus();
                    return;
                }

                // Check if month and year are selected
                if (cmbMonat.SelectedItem == null || cmbJahr.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Bitte wählen Sie Monat und Jahr aus.\n\n" +
                        "Stellen Sie sicher, dass sowohl Monat als auch Jahr ausgewählt sind.",
                        "Datum auswählen", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string selectedLocation = cmbObjekt.SelectedItem?.ToString() ?? "";

                // Show loading message
                UpdateStatus($"Lade Daten für Standort '{selectedLocation}'...");

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Daten: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Async Data Loading with Performance Optimization ---
        private async Task LoadDataAsync()
        {
            _isLoading = true;
            _loadingCancellationToken?.Cancel();
            _loadingCancellationToken = new CancellationTokenSource();
            
            // Reset pagination when loading new data
            _currentPage = 0;
            
            StartPerformanceTimer();
            
            try
            {
                // Show loading indicator
                ShowLoadingIndicator(true);
                UpdateStatus("Lade Mitarbeiterdaten...");
                
                // IMMEDIATE CONSOLE OUTPUT
                
                // Color caching is now handled automatically by GetColorsFromCache method

                // Get filter values
                string selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
                string? employeeFilter = (txtMitarbeiter.Text == "Mitarbeiter eingeben..." || string.IsNullOrWhiteSpace(txtMitarbeiter.Text)) ? null : txtMitarbeiter.Text;
                int month = cmbMonat.SelectedIndex + 1;
                int year = cmbJahr.SelectedItem is int selectedYear ? selectedYear : DateTime.Now.Year;
                
                
                

                // Create cache key
                string cacheKey = $"{selectedObjekt}_{month}_{year}_{employeeFilter ?? "all"}";

                // Check cache first
                if (_dataCache.ContainsKey(cacheKey))
                {
                    UpdateStatus("Lade Daten aus Cache...");
                    await Task.Delay(100); // Small delay to show cache loading
                    
                    var cachedData = _dataCache[cacheKey];
                    PopulateDataGrid(month, year, cachedData);
                    StopPerformanceTimer();
                    UpdateStatus($"{cachedData.Count} Mitarbeiter aus Cache geladen (Schnell!)");
                    return;
                }

                
                // Load employees in background thread
                var allEmployees = await Task.Run(async () =>
                {
                    await Task.Delay(50);
                    return await LoadEmployeesAsync(selectedObjekt, employeeFilter, _loadingCancellationToken.Token);
                }, _loadingCancellationToken.Token);
                
                // Use all employees - pagination will be handled in PopulateDataGrid
                var employees = allEmployees;
                
                if (employees.Count == 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        dgAttendance.ItemsSource = null;
                        dgAttendance.Columns.Clear();
                        UpdateStatus($"Keine Mitarbeiter für Standort '{selectedObjekt}' gefunden");
                        txtCacheInfo.Text = "0 Einträge";
                    });
                    StopPerformanceTimer();
                    return;
                }

                UpdateStatus($"Generiere Kalenderdaten für {employees.Count} Mitarbeiter...");
                
                
                // CHECK: Is Employee 2 (Anna Schmidt) in the list?
                var employee2 = employees.FirstOrDefault(e => e.MitarbeiterId == 2);
                if (employee2 != null)
                {
                }
                else
                {
                }

                // Generate attendance data on UI thread to avoid dependency issues
                var attendanceData = await GenerateAttendanceDataAsync(month, year, employees, _loadingCancellationToken.Token);
                

                if (_loadingCancellationToken.Token.IsCancellationRequested)
                    return;

                // Cache the results for faster future access
                _dataCache[cacheKey] = attendanceData;

                // Update UI on main thread
                await Dispatcher.InvokeAsync(() =>
                {
                    // Populate DataGrid
                    PopulateDataGrid(month, year, attendanceData);
                    
                    StopPerformanceTimer();
                    
                    var totalTime = _loadingStopwatch?.ElapsedMilliseconds ?? 0;
                    var avgTime = _loadTimes.Count > 0 ? _loadTimes.Average() : 0;
                    
                    UpdateStatus($"{attendanceData.Count} Mitarbeiter geladen in {totalTime}ms (Avg: {avgTime:F0}ms)");
                    
                    // Show summary of what was loaded
                    int totalFehlzeitDays = 0;
                    foreach (var record in attendanceData)
                    {
                        for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                        {
                            string dayKey = $"Day{d:D2}";
                            var day = record.GetDay(dayKey);
                            if (!string.IsNullOrEmpty(day.Status))
                            {
                                totalFehlzeitDays++;
                            }
                        }
                    }
                    
                });
            }
            catch (OperationCanceledException)
            {
                StopPerformanceTimer();
                UpdateStatus("Ladevorgang abgebrochen");
            }
            catch (Exception ex)
            {
                StopPerformanceTimer();
                MessageBox.Show($"Fehler beim Laden der Daten: {ex.Message}", "Fehler", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Fehler beim Laden der Daten");
            }
            finally
            {
                _isLoading = false;
                ShowLoadingIndicator(false);
            }
        }

        // --- Optimized Employee Loading ---
        private async Task<List<Mitarbeiter>> LoadEmployeesAsync(string selectedObjekt, string? employeeFilter, CancellationToken cancellationToken)
        {
            // Check employee cache first
            string employeeCacheKey = $"{selectedObjekt}_{employeeFilter ?? "all"}";
            if (_employeeCache.ContainsKey(employeeCacheKey))
            {
                return _employeeCache[employeeCacheKey];
            }


            try
            {
                // Load from database
                List<Mitarbeiter> allMitarbeiter = new List<Mitarbeiter>();
                if (_mitarbeiterService != null)
                {
                    var mitarbeiterResponse = await _mitarbeiterService.GetAllAsync();
                    allMitarbeiter = mitarbeiterResponse.Success ? mitarbeiterResponse.Data ?? new List<Mitarbeiter>() : new List<Mitarbeiter>();
                }
                
                // Debug: Show all employees and their locations
                foreach (var emp in allMitarbeiter.Take(10)) // Show first 10 for debugging
                {
                }
                
                // Special check for Anna Schmidt (ID: 2)
                var annaSchmidt = allMitarbeiter.FirstOrDefault(e => e.MitarbeiterId == 2);
                if (annaSchmidt != null)
                {
                }
                else
                {
                }
                
                // Filter employees - try exact match first, then case-insensitive
                List<Mitarbeiter> employeesToInclude;
                
                if (!string.IsNullOrWhiteSpace(employeeFilter))
                {
                    // First try exact location match
                    employeesToInclude = allMitarbeiter
                        .Where(m => m.Objektname != null && m.Objektname.Equals(selectedObjekt, StringComparison.OrdinalIgnoreCase))
                        .Where(m => m.Name != null && m.Name.ToLower().Contains(employeeFilter.ToLower()))
                        .ToList();
                }
                else
                {
                    // Use exact match only - do not show all employees if Objekt has no employees
                    employeesToInclude = allMitarbeiter
                        .Where(m => m.Objektname != null && m.Objektname.Equals(selectedObjekt, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Cache the results
                _employeeCache[employeeCacheKey] = employeesToInclude;

                return employeesToInclude;
            }
            catch (Exception ex)
            {
                
                // Show error to user
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"Fehler beim Laden der Mitarbeiter aus der Datenbank:\n\n{ex.Message}\n\n" +
                        $"Bitte überprüfen Sie die Datenbankverbindung.",
                        "Datenbankfehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                
                // Return empty list on error
                return new List<Mitarbeiter>();
            }
        }

        // --- Optimized Attendance Data Generation ---
        private async Task<List<AttendanceRecord>> GenerateAttendanceDataAsync(int month, int year, List<Mitarbeiter> employees, CancellationToken cancellationToken)
        {
            var attendanceData = new List<AttendanceRecord>();
            
            // Get the selected object from UI thread before starting background work
            string selectedObjekt = "";
            await Dispatcher.InvokeAsync(() =>
            {
                selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
            });
            
            string selectedBundesland = GetBundeslandForObjekt(selectedObjekt);

            // Pre-calculate holidays for the month (performance optimization)
            var monthHolidays = new HashSet<DateTime>();
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
            {
                DateTime date = new DateTime(year, month, d);
                if (IsHoliday(date, selectedBundesland))
                {
                    monthHolidays.Add(date);
                }
            }

            // Process employees in batches for better performance and progress updates
            int batchSize = Math.Min(PAGE_SIZE, employees.Count);
            int totalBatches = (int)Math.Ceiling((double)employees.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var batch = employees.Skip(batchIndex * batchSize).Take(batchSize);
                
                // Update progress on UI thread
                int currentEmployee = (batchIndex + 1) * batchSize;
                int totalEmployees = employees.Count;
                
                // Use UpdateStatus which is already thread-safe
                UpdateStatus($"Verarbeite Mitarbeiter {Math.Min(currentEmployee, totalEmployees)}/{totalEmployees}...");

                // Process batch - load ALL Fehlzeit data for each employee at once
                foreach (var mitarbeiter in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var record = new AttendanceRecord 
                    { 
                        Name = mitarbeiter.Name ?? "Unbekannt", 
                        MitarbeiterId = mitarbeiter.MitarbeiterId 
                    };

                    // 🚀 PERFORMANCE OPTIMIZATION: Load ALL Fehlzeit data for this employee ONCE
                    var fehlzeitData = await LoadFehlzeitDataForEmployeeAsync(mitarbeiter.MitarbeiterId, month, year);
                    
                    // Create a lookup for quick date-based access (robust against duplicate dates)
                    var fehlzeitLookup = fehlzeitData
                        .GroupBy(f => f.Datum.Date)
                        .ToDictionary(g => g.Key, g => g.First());
                    
                    // Special debug for Anna Schmidt
                    if (mitarbeiter.MitarbeiterId == 2)
                    {
                        foreach (var fz in fehlzeitData)
                        {
                        }
                    }
                    
                    // Process each day of the month
                    for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                    {
                        DateTime date = new DateTime(year, month, d);
                        string dayKey = $"Day{d:D2}";

                        bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                        bool isHoliday = monthHolidays.Contains(date);

                        // Show Fehlzeit even on weekends/holidays
                        if (fehlzeitLookup.ContainsKey(date.Date))
                        {
                            var fehlzeit = fehlzeitLookup[date.Date];
                            string status = fehlzeit.KrankheitKurz ?? "F";
                            var colors = await GetColorsFromCache(fehlzeit.MeldungId ?? 0);

                            if (mitarbeiter.MitarbeiterId == 2 && date.Date == new DateTime(2025, 10, 5))
                            {
                            }

                            record.UpdateDay(dayKey, status, colors.Background, colors.Text);
                        }
                        else if (isWeekend)
                        {
                            record.UpdateDay(dayKey, "", Brushes.LightGray, Brushes.DarkGray);
                        }
                        else if (isHoliday)
                        {
                            record.UpdateDay(dayKey, "", Brushes.LightCoral, Brushes.Black);
                        }
                        else
                        {
                            record.UpdateDay(dayKey, "", Brushes.White, Brushes.Black);
                        }
                    }
                    
                    attendanceData.Add(record);
                }

                // Small delay to prevent UI freezing
                await Task.Delay(10, cancellationToken);
            }

            // Data loading completed

            return attendanceData;
        }

        // --- Batch Load Fehlzeit Data for ALL days of the month ---
        private async Task<List<FehlzeitDay>> LoadFehlzeitDataForEmployeeAsync(int mitarbeiterId, int month, int year)
        {
            
            try
            {
                if (_fehlzeitService == null)
                {
                    return new List<FehlzeitDay>();
                }

                // Create date range for the ENTIRE month
                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                
                var request = new FehlzeitListRequest
                {
                    MitarbeiterId = mitarbeiterId,
                    VonDatum = startDate,
                    BisDatum = endDate
                };
                
                
                var fehlzeitResponse = await _fehlzeitService.GetFehlzeitenAsync(request);
                
                
                if (fehlzeitResponse.Success && fehlzeitResponse.Data != null)
                {
                    var fehlzeitenList = new List<FehlzeitDay>();
                    
                    try
                    {
                        // Handle different response formats
                        if (fehlzeitResponse.Data is JsonElement jsonElement)
                        {
                            if (jsonElement.ValueKind == JsonValueKind.Array)
                            {
                                // Direct array response
                                foreach (var item in jsonElement.EnumerateArray())
                                {
                                    try
                                    {
                                        var fehlzeitDay = ParseFehlzeitDay(item);
                                        if (fehlzeitDay != null)
                                            fehlzeitenList.Add(fehlzeitDay);
                                    }
                                    catch (Exception _)
                                    {
                                    }
                                }
                            }
                            else if (jsonElement.ValueKind == JsonValueKind.Object)
                            {
                                // Object response - try to find data property
                                if (jsonElement.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in dataProp.EnumerateArray())
                                    {
                                        try
                                        {
                                            var fehlzeitDay = ParseFehlzeitDay(item);
                                            if (fehlzeitDay != null)
                                                fehlzeitenList.Add(fehlzeitDay);
                                        }
                                        catch (Exception _)
                                        {
                                        }
                                    }
                                }
                                else
                                {
                                    // Try to parse as single object
                                    var fehlzeitDay = ParseFehlzeitDay(jsonElement);
                                    if (fehlzeitDay != null)
                                        fehlzeitenList.Add(fehlzeitDay);
                                }
                            }
                        }
                        else if (fehlzeitResponse.Data is List<FehlzeitDay> directList)
                        {
                            // Direct list of FehlzeitDay objects
                            fehlzeitenList = directList;
                        }
                        // IMPORTANT: Filter to the requested employee to avoid mixing data across employees
                        int requestedEmployeeId = mitarbeiterId;
                        int beforeFilterCount = fehlzeitenList.Count;
                        fehlzeitenList = fehlzeitenList.Where(f => f.MitarbeiterId == requestedEmployeeId).ToList();
                    }
                    catch (Exception _)
                    {
                    }
                    
                    return fehlzeitenList;
                }
                else
                {
                    return new List<FehlzeitDay>();
                }
            }
            catch (Exception _)
            {
                return new List<FehlzeitDay>();
            }
        }

        // Helper method to parse FehlzeitDay from JsonElement
        private FehlzeitDay? ParseFehlzeitDay(JsonElement item)
        {
            try
            {
                var fehlzeitDay = new FehlzeitDay();
                
                // Parse required properties with error handling
                if (item.TryGetProperty("mitarbeiterId", out var mitarbeiterIdProp))
                    fehlzeitDay.MitarbeiterId = mitarbeiterIdProp.GetInt32();
                
                if (item.TryGetProperty("datum", out var datumProp))
                    fehlzeitDay.Datum = datumProp.GetDateTime();
                
                // Parse optional properties
                if (item.TryGetProperty("krankheitId", out var krankheitIdProp) && krankheitIdProp.ValueKind != JsonValueKind.Null)
                    fehlzeitDay.KrankheitId = krankheitIdProp.GetInt32();
                
                if (item.TryGetProperty("meldungId", out var meldungIdProp) && meldungIdProp.ValueKind != JsonValueKind.Null)
                    fehlzeitDay.MeldungId = meldungIdProp.GetInt32();
                
                if (item.TryGetProperty("krankheitKurz", out var krankheitKurzProp) && krankheitKurzProp.ValueKind != JsonValueKind.Null)
                    fehlzeitDay.KrankheitKurz = krankheitKurzProp.GetString() ?? "";
                else
                    fehlzeitDay.KrankheitKurz = ""; // Empty when no Krankheit selected
                    
                return fehlzeitDay;
            }
            catch (Exception _)
            {
                return null;
            }
        }
        
        private void DebugApiResponse(object responseData, int mitarbeiterId, DateTime date)
        {
            // Debug method - implementation removed for performance
        }



        // --- Loading Indicator Management ---
        private void ShowLoadingIndicator(bool show)
        {
            // Ensure UI updates happen on the UI thread
            if (Dispatcher.CheckAccess())
            {
                ShowLoadingIndicatorInternal(show);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => ShowLoadingIndicatorInternal(show)));
            }
        }

        private void ShowLoadingIndicatorInternal(bool show)
        {
            // You can implement a loading overlay here
            dgAttendance.IsEnabled = !show;
            
            if (show)
            {
                dgAttendance.Opacity = 0.5;
            }
            else
            {
                dgAttendance.Opacity = 1.0;
            }
        }

        
        // --- Performance Monitoring ---
        private long TrackPerformance(string operationName, Action operation)
        {
            _performanceTimer.Restart();
            operation();
            _performanceTimer.Stop();
            
            var elapsedMs = _performanceTimer.ElapsedMilliseconds;
            _loadTimes.Add(elapsedMs);
            
            if (_loadTimes.Count % 5 == 0) // Log every 5 operations
            {
                var avgTime = _loadTimes.TakeLast(5).Average();
            }
            
            return elapsedMs;
        }

        // --- Cache Management ---
        public void ClearCache()
        {
            _dataCache.Clear();
            _employeeCache.Clear();
            _loadTimes.Clear();
            UpdateStatus("Cache geleert");
        }

        // --- Attendance data generation ---
        private List<AttendanceRecord> GenerateAttendanceData(int month, int year, string? employeeFilter)
        {
            var rnd = new Random();
            var list = new List<AttendanceRecord>();

            string selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
            string selectedBundesland = GetBundeslandForObjekt(selectedObjekt);

            // Get employees to include in the report
            List<Mitarbeiter> employeesToInclude;

            if (!string.IsNullOrWhiteSpace(employeeFilter))
            {
                // Filter employees by name
                employeesToInclude = _availableMitarbeiter
                    .Where(m => m.Name != null && m.Name.ToLower().Contains(employeeFilter.ToLower()))
                    .ToList();
            }
            else
            {
                // Include all employees for the selected location
                employeesToInclude = _availableMitarbeiter;
            }

            // If no employees found, show message and return empty list
            if (employeesToInclude.Count == 0)
            {
                UpdateStatus($"Keine Mitarbeiter gefunden für die gewählten Filter");
                return list;
            }

            foreach (var mitarbeiter in employeesToInclude)
            {
                var rec = new AttendanceRecord { Name = mitarbeiter.Name ?? "Unbekannt", MitarbeiterId = mitarbeiter.MitarbeiterId };

                for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                {
                    DateTime date = new DateTime(year, month, d);
                    string dayKey = $"Day{d:D2}";

                    bool isHoliday = IsHoliday(date, selectedBundesland);
                    bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                    if (isWeekend)
                    {
                        // Set weekend days using the new method
                        rec.UpdateDay(dayKey, "", Brushes.LightGray, Brushes.DarkGray);
                    }
                    else if (isHoliday)
                    {
                        rec.UpdateDay(dayKey, "", Brushes.LightCoral, Brushes.Black); // Red background, no text
                    }
                    else
                    {
                        // Default to empty status with white background - no text initially
                        rec.UpdateDay(dayKey, "", Brushes.White, Brushes.Black);
                    }
                }
                list.Add(rec);
            }

            UpdateStatus($"{list.Count} Mitarbeiter für {selectedObjekt} im {month}/{year} geladen");
            return list;
        }

        // --- Sample data generation (fallback) ---
        private List<AttendanceRecord> GenerateSampleData(int month, int year)
        {
            var rnd = new Random();
            var list = new List<AttendanceRecord>();

            string selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
            string selectedBundesland = GetBundeslandForObjekt(selectedObjekt);

            for (int i = 1; i <= 5; i++)
            {
                var rec = new AttendanceRecord { Name = $"Mitarbeiter {i}" };
                for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                {
                    DateTime date = new DateTime(year, month, d);
                    string dayKey = $"Day{d:D2}";

                    bool isHoliday = IsHoliday(date, selectedBundesland);
                    bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

                    if (isWeekend)
                    {
                        rec.UpdateDay(dayKey, "", Brushes.LightGray, Brushes.DarkGray);
                    }
                    else if (isHoliday)
                    {
                        rec.UpdateDay(dayKey, "", Brushes.LightCoral, Brushes.Black); // Red background, no text
                    }
                    else
                    {
                        // Default to empty status with white background - no text initially
                        rec.UpdateDay(dayKey, "", Brushes.White, Brushes.Black);
                    }
                }
                list.Add(rec);
            }
            return list;
        }

        private bool IsHoliday(DateTime date, string bundesland)
        {
            return germanHolidays.Any(h =>
            {
                DateTime holidayDate = GetHolidayDateForYear(h, date.Year);
                return holidayDate.Date == date.Date &&
                       (h.Bundeslaender.Contains("bundesweit") || h.Bundeslaender.Contains(bundesland));
            });
        }

        private string GetHolidayName(DateTime date, string bundesland)
        {
            var holiday = germanHolidays.FirstOrDefault(h =>
            {
                DateTime holidayDate = GetHolidayDateForYear(h, date.Year);
                return holidayDate.Date == date.Date &&
                       (h.Bundeslaender.Contains("bundesweit") || h.Bundeslaender.Contains(bundesland));
            });
            return holiday?.Name ?? string.Empty;
        }

        private DateTime GetHolidayDateForYear(GermanHoliday holiday, int year)
        {
            return year switch
            {
                2025 => holiday.Date2025,
                2026 => holiday.Date2026,
                2027 => holiday.Date2027,
                _ => CalculateHolidayDate(holiday.Name, year)
            };
        }

        private DateTime CalculateHolidayDate(string holidayName, int year)
        {
            DateTime easter = CalculateEaster(year);
            return holidayName switch
            {
                "Karfreitag" => easter.AddDays(-2),
                "Ostersonntag" => easter,
                "Ostermontag" => easter.AddDays(1),
                "Christi Himmelfahrt" => easter.AddDays(39),
                "Pfingstsonntag" => easter.AddDays(49),
                "Pfingstmontag" => easter.AddDays(50),
                "Fronleichnam" => easter.AddDays(60),
                _ => new DateTime(year, 1, 1)
            };
        }

        private DateTime CalculateEaster(int year)
        {
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(year, month, day);
        }


        // --- Button for employee details ---
        private void UpdateAttendanceRecord(AttendanceRecord record, SicknessData sicknessData)
        {
            try
            {

                // Get current month/year from filters for validation
                int currentMonth = cmbMonat.SelectedIndex + 1;
                int currentYear = (int)cmbJahr.SelectedItem;

                // Update each day in the date range - same pattern as weekends/holidays
                for (DateTime date = sicknessData.StartDate; date <= sicknessData.EndDate; date = date.AddDays(1))
                {
                    // Only update if the date is in the currently displayed month/year
                    if (date.Month != currentMonth || date.Year != currentYear)
                    {
                        continue;
                    }

                    string dayKey = $"Day{date.Day:D2}";

                    // Get colors from the selected Meldung in FehlzeitDetailWindow
                    Brush backgroundColor = Brushes.White;
                    Brush textColor = Brushes.Black;


                    // Use the selected Meldung color from FehlzeitDetailWindow
                    if (sicknessData.SelectedMeldung != null && !string.IsNullOrEmpty(sicknessData.SelectedMeldung.Farbe))
                    {
                        try
                        {
                            var color = sicknessData.SelectedMeldung.FarbeColor;
                            var lightColor = Color.FromArgb(80, color.R, color.G, color.B); // Make it lighter for background
                            backgroundColor = new SolidColorBrush(lightColor);
                            textColor = Brushes.Black; // Keep text black for readability
                            
                        }
                        catch (Exception)
                        {
                            backgroundColor = Brushes.LightGray; // Fallback color
                        }
                    }
                    else
                    {
                        backgroundColor = Brushes.White;
                    }

                    // Update using the same method as weekends/holidays - this works!
                    record.UpdateDay(dayKey, sicknessData.Status, backgroundColor, textColor);

                }

            }
            catch (Exception)
            {
                throw;
            }
        }


        private void MitarbeiterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AttendanceRecord record)
            {
                try
                {
                    var detailWindow = new FehlzeitDetailWindow(_authService);
                    detailWindow.Owner = Window.GetWindow(this);
                    
                    // Make it clear which employee is being modified
                    detailWindow.Title = $"Fehlzeit bearbeiten - {record.Name}";
                    detailWindow.SelectedMitarbeiterId = record.MitarbeiterId;
                    detailWindow.SelectedMitarbeiterName = record.Name;


                    bool? result = detailWindow.ShowDialog();

                    // Simple approach: just reload the entire DataGrid to reset all button states
                    if (result == true && detailWindow.SicknessInfo != null)
                    {

                        // CRITICAL FIX: Instead of trying to update just one record, reload ALL data
                        // This ensures we get the latest data from the database
                        ClearCache(); // Clear cache to force fresh data
                        _ = LoadDataAsync(); // Reload all data
                    }
                    else
                    {
                    }
                    
                    // Force DataGrid to refresh and reset all button states
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        dgAttendance.Items.Refresh();
                        dgAttendance.UpdateLayout();
                        this.Focus(); // Move focus away from any buttons
                    }), DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Verarbeiten der Fehlzeit-Daten für {record.Name}: {ex.Message}",
                        "Fehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // --- Populate DataGrid (Using DataGridTemplateColumn for reliable color binding) ---
        private void PopulateDataGrid(int month, int year, List<AttendanceRecord> data)
        {
            dgAttendance.Columns.Clear();
            var buttonCol = new DataGridTemplateColumn { Width = 130, Header = "Mitarbeiter" };
            var btnFactory = new FrameworkElementFactory(typeof(Button));
            btnFactory.SetBinding(Button.ContentProperty, new Binding("Name"));
            
            // Create a completely custom button template to avoid focus/pressed state issues
            var transparentButtonStyle = new Style(typeof(Button));
            
            // Set basic properties
            transparentButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
            transparentButtonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            transparentButtonStyle.Setters.Add(new Setter(Button.BorderBrushProperty, Brushes.Transparent));
            transparentButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB))));
            transparentButtonStyle.Setters.Add(new Setter(Button.FontWeightProperty, FontWeights.Medium));
            transparentButtonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 13.0));
            transparentButtonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            transparentButtonStyle.Setters.Add(new Setter(Button.HorizontalAlignmentProperty, HorizontalAlignment.Left));
            transparentButtonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            
            // Create custom template that ignores focus and pressed states
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
            
            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;
            
            // No triggers in template - keep it simple to avoid focus/pressed issues
            
            transparentButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            
            btnFactory.SetValue(Button.StyleProperty, transparentButtonStyle);
            btnFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(MitarbeiterButton_Click));
            buttonCol.CellTemplate = new DataTemplate { VisualTree = btnFactory };
            dgAttendance.Columns.Add(buttonCol);

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var culture = new CultureInfo("de-DE");
            string selectedObjekt = cmbObjekt.SelectedItem as string ?? "Hauptgebäude";
            string selectedBundesland = GetBundeslandForObjekt(selectedObjekt);

            for (int d = 1; d <= daysInMonth; d++)
            {
                DateTime dt = new DateTime(year, month, d);
                string dayKey = $"Day{d:D2}";
                string weekday = dt.ToString("ddd", culture).Substring(0, 2);
                bool isHoliday = IsHoliday(dt, selectedBundesland);
                string holidayName = isHoliday ? GetHolidayName(dt, selectedBundesland) : string.Empty;

                var headerPanel = new StackPanel { Orientation = Orientation.Vertical };
                headerPanel.Children.Add(new TextBlock { Text = weekday, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = isHoliday ? FontWeights.Bold : FontWeights.Normal });
                headerPanel.Children.Add(new TextBlock { Text = d.ToString(), HorizontalAlignment = HorizontalAlignment.Center, FontWeight = isHoliday ? FontWeights.Bold : FontWeights.Normal });
                if (isHoliday) headerPanel.Children.Add(new TextBlock { Text = "🎉", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 10 });

                // Use DataGridTemplateColumn instead of DataGridTextColumn for better control
                var col = new DataGridTemplateColumn { Header = headerPanel, Width = 35, IsReadOnly = true };

                // Create the data template for the cell
                var cellTemplate = new DataTemplate();
                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));

                // Set text block properties
                textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(2));

                // Bind the text
                textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"{dayKey}.Status"));

                // Bind the foreground (text color) - Direct binding without converter
                textBlockFactory.SetBinding(TextBlock.ForegroundProperty, new Binding($"{dayKey}.TextColor"));

                // Bind the background color - Direct binding without converter
                textBlockFactory.SetBinding(TextBlock.BackgroundProperty, new Binding($"{dayKey}.BackgroundColor"));

                cellTemplate.VisualTree = textBlockFactory;
                col.CellTemplate = cellTemplate;

                dgAttendance.Columns.Add(col);
            }

            // Store all records and apply pagination
            _allRecords = data;
            _totalPages = (int)Math.Ceiling((double)data.Count / PAGE_SIZE);
            ApplyPagination();
            DebugDataState(); // Add this line
        }
        
        private void ApplyPagination()
        {
            if (_allRecords.Count <= PAGE_SIZE)
            {
                // No pagination needed for small datasets
                dgAttendance.ItemsSource = _allRecords;
                UpdatePaginationControls(false);
            }
            else
            {
                // Apply pagination for all datasets (12 records per page)
                var pagedData = _allRecords
                    .Skip(_currentPage * PAGE_SIZE)
                    .Take(PAGE_SIZE)
                    .ToList();
                dgAttendance.ItemsSource = pagedData;
                UpdatePaginationControls(true);
            }
        }
        
        private void UpdatePaginationControls(bool visible)
        {
            if (PaginationPanel != null)
            {
                PaginationPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                
                if (visible)
                {
                    TxtPageInfo.Text = $"Seite {_currentPage + 1} von {_totalPages} ({_allRecords.Count} Mitarbeiter)";
                    BtnPrevPage.IsEnabled = _currentPage > 0;
                    BtnNextPage.IsEnabled = _currentPage < _totalPages - 1;
                }
            }
        }
        
        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                ApplyPagination();
            }
        }
        
        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                ApplyPagination();
            }
        }
        
        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 0;
            ApplyPagination();
        }
        
        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages - 1;
            ApplyPagination();
        }

        private void DebugDataState()
        {
            try
            {
                if (dgAttendance.ItemsSource is IEnumerable<AttendanceRecord> records)
                {
                    int totalRecords = records.Count();
                    int totalFehlzeitDays = 0;
                    
                    foreach (var record in records)
                    {
                        for (int d = 1; d <= 31; d++)
                        {
                            string dayKey = $"Day{d:D2}";
                            var day = record.GetDay(dayKey);
                            if (!string.IsNullOrEmpty(day.Status))
                            {
                                totalFehlzeitDays++;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception _)
            {
            }
        }
    }
}
