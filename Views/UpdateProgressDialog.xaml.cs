using System.Windows;

namespace FehlzeitApp.Views
{
    public partial class UpdateProgressDialog : Window
    {
        public UpdateProgressDialog()
        {
            InitializeComponent();
        }

        public void UpdateProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = progress;
                ProgressText.Text = $"{progress:F0}%";
            });
        }
    }
}
