using System;
using System.Windows;
using FehlzeitApp.Models;
using FehlzeitApp.Services;
using ChangePasswordRequest = FehlzeitApp.Services.ChangePasswordRequest;

namespace FehlzeitApp.Views
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly UserService _userService;
        private readonly User _currentUser;

        public ChangePasswordDialog(UserService userService, User currentUser)
        {
            InitializeComponent();
            _userService = userService;
            _currentUser = currentUser;
            
            // Display username
            TxtUsername.Text = $"Benutzer: {_currentUser.Username}";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                BtnSave.IsEnabled = false;
                BtnSave.Content = "Wird geändert...";
                
                var request = new ChangePasswordRequest
                {
                    CurrentPassword = TxtCurrentPassword.Password,
                    NewPassword = TxtNewPassword.Password
                };

                var response = await _userService.ChangePasswordAsync(_currentUser.UserId, request);

                if (response.Success)
                {
                    MessageBox.Show("Ihr Passwort wurde erfolgreich geändert.", 
                                  "Erfolg", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show($"Fehler beim Ändern des Passworts: {response.Message}", 
                                  "Fehler", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", 
                              "Fehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnSave.Content = "💾 Passwort ändern";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TxtCurrentPassword.Password))
            {
                MessageBox.Show("Bitte geben Sie Ihr aktuelles Passwort ein.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtCurrentPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtNewPassword.Password))
            {
                MessageBox.Show("Bitte geben Sie ein neues Passwort ein.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            if (TxtNewPassword.Password.Length < 6)
            {
                MessageBox.Show("Das neue Passwort muss mindestens 6 Zeichen lang sein.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtConfirmPassword.Password))
            {
                MessageBox.Show("Bitte bestätigen Sie Ihr neues Passwort.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            if (TxtNewPassword.Password != TxtConfirmPassword.Password)
            {
                MessageBox.Show("Die neuen Passwörter stimmen nicht überein.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtConfirmPassword.Focus();
                return false;
            }

            if (TxtCurrentPassword.Password == TxtNewPassword.Password)
            {
                MessageBox.Show("Das neue Passwort muss sich vom aktuellen Passwort unterscheiden.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtNewPassword.Focus();
                return false;
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

