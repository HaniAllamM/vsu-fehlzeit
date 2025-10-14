using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class DashboardPage : UserControl, INotifyPropertyChanged
    {
        // Services
        private readonly AuthService _authService;
        private ObjektService? _objektService;

        // Data Properties for DataGrids
        public ObservableCollection<MainChartData> MainChartDataCollection { get; set; } = new();
        public ObservableCollection<MitarbeiterAbsenceData> MitarbeiterData { get; set; } = new();
        public ObservableCollection<YearlyData> YearlyDataCollection { get; set; } = new();
        public ObservableCollection<TrendData> TrendsData { get; set; } = new();
        public ObservableCollection<TypeData> TypesData { get; set; } = new();

        // Hardcoded Data for Demo
        private readonly List<Objekt> _hardcodedObjekte = new()
        {
            new Objekt { ObjektId = 1, ObjektName = "Hauptgebäude" },
            new Objekt { ObjektId = 2, ObjektName = "Nebengebäude" },
            new Objekt { ObjektId = 3, ObjektName = "Lager" },
            new Objekt { ObjektId = 4, ObjektName = "Werkstatt" }
        };

        private readonly List<string> _months = new()
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };

        // Data Classes for Dashboard
        public class MainChartData
        {
            public string Name { get; set; } = string.Empty;
            public int AbsenceDays { get; set; }
            public int Krankheit { get; set; }
            public int Urlaub { get; set; }
            public int Sonstiges { get; set; }
            public int Gesamt { get; set; }
        }

        public class MitarbeiterAbsenceData
        {
            public string Name { get; set; } = string.Empty;
            public int AbsenceDays { get; set; }
        }

        public class YearlyData
        {
            public string Month { get; set; } = string.Empty;
            public int Absences { get; set; }
        }

        public class TrendData
        {
            public string Type { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class TypeData
        {
            public string Type { get; set; } = string.Empty;
            public int Count { get; set; }
            public string Percentage { get; set; } = string.Empty;
        }

        public DashboardPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            DataContext = this;
            
            InitializeComboBoxes();
            LoadHardcodedData();
        }

        private void InitializeComboBoxes()
        {
            // Initialize Objekt ComboBox
            CmbObjekt.ItemsSource = _hardcodedObjekte;
            CmbObjekt.SelectedIndex = 0;

            // Initialize Year ComboBox (2020-2025)
            var years = new List<int>();
            for (int year = 2020; year <= 2025; year++)
            {
                years.Add(year);
            }
            CmbYear.ItemsSource = years;
            CmbYear.SelectedItem = DateTime.Now.Year;

            // Initialize Month ComboBox
            CmbMonth.ItemsSource = _months;
            CmbMonth.SelectedIndex = DateTime.Now.Month - 1;

            // Add event handlers
            CmbObjekt.SelectionChanged += (s, e) => LoadHardcodedData();
            CmbYear.SelectionChanged += (s, e) => LoadHardcodedData();
            CmbMonth.SelectionChanged += (s, e) => LoadHardcodedData();
        }

        private void LoadHardcodedData()
        {
            var selectedObjekt = CmbObjekt.SelectedItem as Objekt;
            var selectedYear = (int)(CmbYear.SelectedItem ?? DateTime.Now.Year);
            var selectedMonth = CmbMonth.SelectedIndex + 1;

            if (selectedObjekt == null) return;

            // Load Main Chart Data
            LoadMainChartData(selectedObjekt.ObjektName, selectedMonth, selectedYear);
            
            // Load Mitarbeiter Absence Chart
            LoadMitarbeiterAbsenceChart(selectedObjekt.ObjektName, selectedMonth, selectedYear);
            
            // Load Yearly Objekt Statistics
            LoadObjektYearlyStatistics(selectedObjekt.ObjektName, selectedYear);
            
            // Load Monthly Trends
            LoadMonthlyTrends(selectedObjekt.ObjektName, selectedYear);
            
            // Load Absence Types Distribution
            LoadAbsenceTypesDistribution(selectedObjekt.ObjektName, selectedMonth, selectedYear);
        }

        private void LoadMainChartData(string objektName, int month, int year)
        {
            // Hardcoded main chart data - ONLY KRANKHEIT
            var mainChartData = new List<MainChartData>
            {
                new() { Name = "Anna Schmidt", AbsenceDays = 2, Krankheit = 2, Urlaub = 0, Sonstiges = 0, Gesamt = 2 },
                new() { Name = "David Richter", AbsenceDays = 4, Krankheit = 4, Urlaub = 0, Sonstiges = 0, Gesamt = 4 },
                new() { Name = "Lisa Schulz", AbsenceDays = 1, Krankheit = 1, Urlaub = 0, Sonstiges = 0, Gesamt = 1 },
                new() { Name = "Max Mustermann", AbsenceDays = 3, Krankheit = 3, Urlaub = 0, Sonstiges = 0, Gesamt = 3 },
                new() { Name = "Sarah Weber", AbsenceDays = 0, Krankheit = 0, Urlaub = 0, Sonstiges = 0, Gesamt = 0 },
                new() { Name = "Thomas Müller", AbsenceDays = 2, Krankheit = 2, Urlaub = 0, Sonstiges = 0, Gesamt = 2 },
                new() { Name = "Julia Fischer", AbsenceDays = 4, Krankheit = 4, Urlaub = 0, Sonstiges = 0, Gesamt = 4 },
                new() { Name = "Michael Wagner", AbsenceDays = 1, Krankheit = 1, Urlaub = 0, Sonstiges = 0, Gesamt = 1 },
                new() { Name = "Petra Klein", AbsenceDays = 5, Krankheit = 5, Urlaub = 0, Sonstiges = 0, Gesamt = 5 },
                new() { Name = "Stefan Groß", AbsenceDays = 1, Krankheit = 1, Urlaub = 0, Sonstiges = 0, Gesamt = 1 }
            };

            // Populate the data collection
            MainChartDataCollection.Clear();
            foreach (var item in mainChartData)
            {
                MainChartDataCollection.Add(item);
            }

            // Draw the custom chart
            DrawCustomChart(mainChartData);
        }

        private void DrawCustomChart(List<MainChartData> data)
        {
            // Clear existing chart elements
            ChartCanvas.Children.Clear();

            if (data.Count == 0) return;

            // Get actual canvas dimensions
            ChartCanvas.UpdateLayout();
            double chartWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 800;
            double chartHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 300;
            
            // Calculate bar dimensions to fill the width
            double totalSpacing = (data.Count + 1) * 10; // 10px spacing between bars and edges
            double availableWidth = chartWidth - totalSpacing;
            double barWidth = Math.Max(20, availableWidth / data.Count); // Minimum 20px width
            double spacing = 10;
            
            // Find max Krankheit value for scaling
            double maxValue = data.Max(x => x.Krankheit);
            if (maxValue == 0) maxValue = 1; // Avoid division by zero
            double scale = (chartHeight - 40) / maxValue; // Leave 40px padding

            // Draw grid lines
            for (int i = 0; i <= 8; i++)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = i * (chartHeight / 8),
                    X2 = chartWidth,
                    Y2 = i * (chartHeight / 8),
                    Stroke = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);
            }

            // Draw bars for each employee - ONLY KRANKHEIT
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                double x = i * (barWidth + spacing) + spacing;
                double baseY = chartHeight - 20; // 20px from bottom

                // Only Krankheit bars
                if (item.Krankheit > 0)
                {
                    double height = item.Krankheit * scale;
                    var rect = new Rectangle
                    {
                        Width = barWidth,
                        Height = height,
                        Fill = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                        Stroke = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, baseY - height);
                    ChartCanvas.Children.Add(rect);
                }
            }
        }

        private void LoadMitarbeiterAbsenceChart(string objektName, int month, int year)
        {
            // Hardcoded Mitarbeiter absence data
            var mitarbeiterData = new List<MitarbeiterAbsenceData>
            {
                new() { Name = "Anna Schmidt", AbsenceDays = 3 },
                new() { Name = "David Richter", AbsenceDays = 7 },
                new() { Name = "Lisa Schulz", AbsenceDays = 2 },
                new() { Name = "Max Mustermann", AbsenceDays = 5 },
                new() { Name = "Sarah Weber", AbsenceDays = 1 },
                new() { Name = "Thomas Müller", AbsenceDays = 4 },
                new() { Name = "Julia Fischer", AbsenceDays = 6 },
                new() { Name = "Michael Wagner", AbsenceDays = 2 }
            };

            MitarbeiterData.Clear();
            foreach (var item in mitarbeiterData)
            {
                MitarbeiterData.Add(item);
            }

            MitarbeiterDataGrid.ItemsSource = MitarbeiterData;
        }

        private void LoadObjektYearlyStatistics(string objektName, int year)
        {
            // Hardcoded yearly statistics for the selected objekt
            var monthlyData = new List<YearlyData>
            {
                new() { Month = "Jan", Absences = 45 },
                new() { Month = "Feb", Absences = 38 },
                new() { Month = "Mär", Absences = 52 },
                new() { Month = "Apr", Absences = 41 },
                new() { Month = "Mai", Absences = 48 },
                new() { Month = "Jun", Absences = 35 },
                new() { Month = "Jul", Absences = 29 },
                new() { Month = "Aug", Absences = 33 },
                new() { Month = "Sep", Absences = 46 },
                new() { Month = "Okt", Absences = 39 },
                new() { Month = "Nov", Absences = 44 },
                new() { Month = "Dez", Absences = 37 }
            };

            YearlyDataCollection.Clear();
            foreach (var item in monthlyData)
            {
                YearlyDataCollection.Add(item);
            }

            YearlyDataGrid.ItemsSource = YearlyDataCollection;
        }

        private void LoadMonthlyTrends(string objektName, int year)
        {
            // Hardcoded monthly trends comparing different absence types
            var trendsData = new List<TrendData>
            {
                new() { Type = "Krankheit", Count = 25 },
                new() { Type = "Urlaub", Count = 12 },
                new() { Type = "Sonstiges", Count = 3 },
                new() { Type = "Bildung", Count = 8 },
                new() { Type = "Sonderurlaub", Count = 2 }
            };

            TrendsData.Clear();
            foreach (var item in trendsData)
            {
                TrendsData.Add(item);
            }

            TrendsDataGrid.ItemsSource = TrendsData;
        }

        private void LoadAbsenceTypesDistribution(string objektName, int month, int year)
        {
            // Hardcoded absence types distribution
            var typesData = new List<TypeData>
            {
                new() { Type = "Krankheit", Count = 45, Percentage = "45%" },
                new() { Type = "Urlaub", Count = 30, Percentage = "30%" },
                new() { Type = "Sonstiges", Count = 15, Percentage = "15%" },
                new() { Type = "Bildung", Count = 10, Percentage = "10%" }
            };

            TypesData.Clear();
            foreach (var item in typesData)
            {
                TypesData.Add(item);
            }

            TypesDataGrid.ItemsSource = TypesData;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadHardcodedData();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
