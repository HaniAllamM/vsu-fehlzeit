using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Models;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class PdfViewerPage : UserControl
    {
        private readonly UnterlageService _unterlageService;
        private readonly Unterlage _unterlage;
        private string? _downloadUrl;

        public event EventHandler? CloseRequested;

        public PdfViewerPage(UnterlageService unterlageService, Unterlage unterlage)
        {
            InitializeComponent();
            _unterlageService = unterlageService;
            _unterlage = unterlage;
            
            // Set title
            TxtTitle.Text = $"PDF Viewer - {_unterlage.Bezeichnung}";
            
            Loaded += PdfViewerPage_Loaded;
        }

        private async void PdfViewerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPdfAsync();
        }

        private async Task LoadPdfAsync()
        {
            try
            {
                // Show loading state
                ShowLoading();

                // Get download URL
                var response = await _unterlageService.GetDownloadUrlAsync(_unterlage.UnterlageId);
                
                if (response.Success && !string.IsNullOrEmpty(response.DownloadUrl))
                {
                    _downloadUrl = response.DownloadUrl;
                    
                    // Navigate to PDF
                    PdfWebBrowser.Navigate(_downloadUrl);
                    
                    // Show PDF panel
                    ShowPdf();
                }
                else
                {
                    ShowError(response.Message ?? "Fehler beim Abrufen der Download-URL");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fehler beim Laden des PDFs: {ex.Message}");
            }
        }

        private void ShowLoading()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            PdfPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            TxtErrorMessage.Text = message;
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            PdfPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowPdf()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            PdfPanel.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            await LoadPdfAsync();
        }
    }
}
