using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class MeldungPage : UserControl, INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private MeldungService? _meldungService;
        private readonly ObservableCollection<Meldung> _meldungList;
        private string _searchText = string.Empty;
        private Meldung? _currentEditingMeldung;
        private bool _isEditMode = false;
        private bool _isAdmin = false;
        private readonly DispatcherTimer _searchTimer;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<Meldung> MeldungList => _meldungList;

        public MeldungPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _meldungList = new ObservableCollection<Meldung>();
            DataContext = this;

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); _ = RefreshData(); };

            Loaded += MeldungPage_Loaded;
            PopulateColorComboBox();
        }

        private async void MeldungPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isAdmin = _authService.CurrentUser?.Role == "Admin";
            SetupUserPermissions();

            try
            {
                SetLoadingState(true, "Lade Meldungen vom API...");
                var configService = await ConfigurationService.CreateAsync();
                _meldungService = new MeldungService(_authService, configService);
                await RefreshData();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"API-Fehler, zeige Testdaten: {ex.Message}";
                UpdateMeldungList(GetTestData());
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetupUserPermissions()
        {
            // Hide form panel on load - will show when clicking "Neue Meldung"
            FormPanel.Visibility = Visibility.Collapsed;
            BtnAddMeldung.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (MeldungDataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Aktionen") is DataGridColumn actionsColumn)
            {
                actionsColumn.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // Adjust grid columns when form is hidden
            if (this.Content is Grid grid && grid.ColumnDefinitions.Count > 2)
            {
                if (!_isAdmin)
                {
                    grid.ColumnDefinitions[1].Width = new GridLength(0);
                    grid.ColumnDefinitions[2].Width = new GridLength(0);
                }
                else
                {
                    // Start with form hidden
                    grid.ColumnDefinitions[1].Width = new GridLength(0);
                    grid.ColumnDefinitions[2].Width = new GridLength(0);
                }
            }

            StatusText.Text = _isAdmin ? "Admin-Modus" : "Lese-Modus";
        }

        private async Task RefreshData()
        {
            try
            {
                SetLoadingState(true, "Lade Daten...");
                var data = await GetMeldungenFromApi();
                UpdateMeldungList(data);
                StatusText.Text = $"Geladen: {MeldungList.Count} Einträge";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fehler beim Laden: {ex.Message}";
                UpdateMeldungList(GetTestData());
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task<List<Meldung>> GetMeldungenFromApi()
        {
            if (_meldungService == null) return GetTestData();
            var response = await _meldungService.GetAllAsync();
            return response.Success ? response.Data ?? new List<Meldung>() : GetTestData();
        }

        private void UpdateMeldungList(IEnumerable<Meldung> data)
        {
            var filteredData = data;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filteredData = data.Where(m => m.Beschreibung.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            MeldungList.Clear();
            foreach (var item in filteredData.OrderBy(m => m.Sortierung).ThenBy(m => m.Beschreibung))
            {
                MeldungList.Add(item);
            }
        }

        private List<Meldung> GetTestData()
        {
            return new List<Meldung>
            {
                new Meldung { MeldungId = 1, Beschreibung = "Krankmeldung (Test)", Aktiv = true, Farbe = "#FF6B6B", Sortierung = 10 },
                new Meldung { MeldungId = 2, Beschreibung = "Urlaub (Test)", Aktiv = true, Farbe = "#4ECDC4", Sortierung = 20 },
            };
        }

        private void SetLoadingState(bool isLoading, string? message = null)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            MeldungDataGrid.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
            StatusText.Text = message ?? (isLoading ? "Laden..." : "Bereit");
        }

        private void ShowError(string message) => MessageBox.Show(message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = TxtSearch.Text;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void BtnAddMeldung_Click(object sender, RoutedEventArgs e)
        {
            SetFormMode(false, null);
            ClearForm();
            ShowFormPanel();
            TxtBeschreibung.Focus();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: Meldung meldung })
            {
                SetFormMode(true, meldung);
                LoadMeldungToForm(meldung);
                ShowFormPanel();
                TxtBeschreibung.Focus();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: Meldung meldung }) return;

            var result = MessageBox.Show($"Sind Sie sicher, dass Sie '{meldung.Beschreibung}' löschen möchten?", "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            if (_meldungService != null)
            {
                var response = await _meldungService.DeleteAsync(meldung.MeldungId);
                if (!response.Success) ShowError(response.Message ?? "Unbekannter API-Fehler");
            }
            await RefreshData();
        }

        private void SetFormMode(bool isEdit, Meldung? meldung)
        {
            _isEditMode = isEdit;
            _currentEditingMeldung = meldung;
            FormTitle.Text = isEdit ? "Meldung bearbeiten" : "Neue Meldung";
            FormSubtitle.Text = isEdit ? $"ID: {meldung?.MeldungId}" : "Bitte alle Felder ausfüllen";
            BtnSave.Content = isEdit ? "Änderungen speichern" : "Meldung erstellen";
        }

        private void LoadMeldungToForm(Meldung meldung)
        {
            TxtBeschreibung.Text = meldung.Beschreibung;
            var colorItem = CmbFarbe.Items.Cast<ColorInfo>().FirstOrDefault(c => c.HexValue == meldung.Farbe);
            CmbFarbe.SelectedItem = colorItem;
            TxtSortierung.Text = meldung.Sortierung.ToString();
            ChkAktiv.IsChecked = meldung.Aktiv;
            UpdateColorPreview();
        }

        private void ClearForm()
        {
            _currentEditingMeldung = null;
            _isEditMode = false;
            TxtBeschreibung.Text = string.Empty;
            CmbFarbe.SelectedIndex = 0; // First color
            TxtSortierung.Text = "0";
            ChkAktiv.IsChecked = true;
            ErrorPanel.Visibility = Visibility.Collapsed;
            SetFormMode(false, null);
            UpdateColorPreview();
        }

        private void CmbFarbe_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateColorPreview();

        private void UpdateColorPreview()
        {
            if (ColorPreview == null || CmbFarbe.SelectedItem == null) return;
            try
            {
                if (CmbFarbe.SelectedItem is ColorInfo colorInfo)
                {
                    ColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorInfo.HexValue));
                }
                else
                {
                    ColorPreview.Background = Brushes.Gray;
                }
            }
            catch
            {
                ColorPreview.Background = Brushes.Gray;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            var meldungToSave = _currentEditingMeldung ?? new Meldung();
            meldungToSave.Beschreibung = TxtBeschreibung.Text;
            meldungToSave.Farbe = (CmbFarbe.SelectedItem as ColorInfo)?.HexValue ?? "#FFFFFF";
            meldungToSave.Sortierung = int.TryParse(TxtSortierung.Text, out var sort) ? sort : 0;
            meldungToSave.Aktiv = ChkAktiv.IsChecked ?? false;

            if (_meldungService != null)
            {
                var response = _isEditMode
                    ? await _meldungService.UpdateAsync(meldungToSave.MeldungId, meldungToSave)
                    : await _meldungService.CreateAsync(meldungToSave);

                if (!response.Success) ShowError(response.Message ?? "Unbekannter API-Fehler");
            }

            ClearForm();
            HideFormPanel();
            await RefreshData();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            HideFormPanel();
        }

        private void ShowFormPanel()
        {
            if (this.Content is Grid grid && grid.ColumnDefinitions.Count > 2)
            {
                FormPanel.Visibility = Visibility.Visible;
                grid.ColumnDefinitions[1].Width = new GridLength(5);
                grid.ColumnDefinitions[2].Width = new GridLength(420);
            }
        }

        private void HideFormPanel()
        {
            if (this.Content is Grid grid && grid.ColumnDefinitions.Count > 2)
            {
                FormPanel.Visibility = Visibility.Collapsed;
                grid.ColumnDefinitions[1].Width = new GridLength(0);
                grid.ColumnDefinitions[2].Width = new GridLength(0);
            }
        }

        private void PopulateColorComboBox()
        {
            var colorList = typeof(Colors).GetProperties()
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => 
                {
                    var color = (Color)p.GetValue(null)!;
                    var hexValue = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    return new ColorInfo
                    {
                        Name = p.Name,
                        HexValue = hexValue
                    };
                }).ToList();

            CmbFarbe.ItemsSource = colorList;
        }

        private bool ValidateForm()
        {
            var meldung = new Meldung
            {
                Beschreibung = TxtBeschreibung.Text,
                Farbe = (CmbFarbe.SelectedItem as ColorInfo)?.HexValue ?? string.Empty,
                Sortierung = int.TryParse(TxtSortierung.Text, out var sort) ? sort : -1
            };

            var validationContext = new ValidationContext(meldung);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = Validator.TryValidateObject(meldung, validationContext, validationResults, true);

            if (!isValid)
            {
                ErrorText.Text = string.Join("\n", validationResults.Select(vr => vr.ErrorMessage));
                ErrorPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
            }
            return isValid;
        }
    }
}
