using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;

namespace FehlzeitApp.Services
{
    public class UpdateService
    {
        private readonly string _updateUrl = "https://github.com/HaniAllamM/vsu-fehlzeit/releases/latest/download/";

        public UpdateService()
        {
            // Update URL is now set as readonly field above
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                // Check if running from development/publish folder
                var currentExe = System.AppContext.BaseDirectory;
                if (string.IsNullOrEmpty(currentExe) || 
                    currentExe.Contains("\\bin\\Debug\\") || 
                    currentExe.Contains("\\bin\\Release\\") ||
                    currentExe.Contains("\\publish\\"))
                {
                    MessageBox.Show("⚠️ Update-Funktion nur für installierte Versionen verfügbar!\n\n" +
                                  "Diese App läuft im Entwicklungsmodus.\n" +
                                  "Für Updates müssen Sie die installierte Version verwenden:\n" +
                                  "• Installieren Sie die App über Setup.exe\n" +
                                  "• Dann können Sie Updates prüfen", 
                                  "Entwicklungsmodus", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    return false;
                }

                // Set the update feed URL to GitHub releases
                var mgr = new UpdateManager(_updateUrl);
                var updateInfo = await mgr.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    // Show update notification to user
                    var result = MessageBox.Show(
                        $"Eine neue Version ({updateInfo.TargetFullRelease.Version}) ist verfügbar!\n\n" +
                        $"Neue Version: {updateInfo.TargetFullRelease.Version}\n\n" +
                        "Möchten Sie jetzt aktualisieren?",
                        "Update verfügbar",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Show progress dialog
                        var progressDialog = new Views.UpdateProgressDialog();
                        progressDialog.Show();
                        
                        try
                        {
                            // Download and apply update
                            await mgr.DownloadUpdatesAsync(updateInfo);
                            progressDialog.Close();
                            
                            // Apply update and restart
                            mgr.ApplyUpdatesAndRestart(updateInfo);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            progressDialog.Close();
                            MessageBox.Show($"Update fehlgeschlagen: {ex.Message}", "Update Fehler", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }

                return false; // No update or user declined
            }
            catch (Exception ex)
            {
                // Log error and show to user
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                
                // Show error message to user
                MessageBox.Show($"Update-Prüfung fehlgeschlagen: {ex.Message}\n\n" +
                              "Mögliche Ursachen:\n" +
                              "• App ist nicht über Velopack installiert\n" +
                              "• Keine Internetverbindung\n" +
                              "• GitHub Repository nicht erreichbar", 
                              "Update-Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return false;
            }
        }

        private void RestartApplication()
        {
            try
            {
                // Try to get the executable path
                var currentExe = System.AppContext.BaseDirectory;
                
                // If running from Visual Studio or development, use a different approach
                if (string.IsNullOrEmpty(currentExe) || currentExe.Contains("\\bin\\Debug\\") || currentExe.Contains("\\bin\\Release\\"))
                {
                    // For development, just show a message but don't close the app
                    MessageBox.Show("Update simulation completed!\n\nIn a real deployment, the application would restart with the new version.\n\nFor now, the app stays open for testing.",
                        "Update Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                var currentDir = System.IO.Path.GetDirectoryName(currentExe);
                
                // Handle null directory case
                if (string.IsNullOrEmpty(currentDir))
                {
                    currentDir = System.IO.Directory.GetCurrentDirectory();
                }
                
                // Create a batch file to restart the application
                var batchFile = System.IO.Path.Combine(currentDir, "restart_app.bat");
                var batchContent = $@"
@echo off
timeout /t 2 /nobreak >nul
start """" ""{currentExe}""
del ""%~f0""
";
                
                System.IO.File.WriteAllText(batchFile, batchContent);
                
                // Start the batch file
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batchFile,
                    WorkingDirectory = currentDir,
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                
                System.Diagnostics.Process.Start(startInfo);
                
                // Close the current application
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restart application: {ex.Message}");
                MessageBox.Show($"Failed to restart application: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}