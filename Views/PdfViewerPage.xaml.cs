using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Models;
using FehlzeitApp.Services;
using Microsoft.Web.WebView2.Core;

namespace FehlzeitApp.Views
{
    public partial class PdfViewerPage : UserControl
    {
        private readonly UnterlageService _unterlageService;
        private readonly Unterlage _unterlage;
        private bool _isWebViewInitialized = false;

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
            await InitializeWebViewAsync();
            await LoadPdfAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                await PdfWebView.EnsureCoreWebView2Async(null);
                _isWebViewInitialized = true;

                PdfWebView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        // Loaded successfully
                    }
                    else
                    {
                        ShowError($"Fehler beim Laden des PDFs: {args.WebErrorStatus}");
                    }
                };
            }
            catch (Exception ex)
            {
                ShowError($"Fehler bei der WebView2 Initialisierung: {ex.Message}");
            }
        }

        private async Task LoadPdfAsync()
        {
            try
            {
                // Show loading state
                ShowLoading();

                if (!_isWebViewInitialized)
                    await InitializeWebViewAsync();

                // Build inline view URL
                var viewUrl = _unterlageService.GetViewUrl(_unterlage.UnterlageId);

                // Navigate to inline view URL
                PdfWebView.CoreWebView2.Navigate(viewUrl);

                // Show PDF panel
                ShowPdf();
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
