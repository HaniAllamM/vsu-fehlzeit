using System;
using System.Windows;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class UserEditDialog : Window
    {
        private readonly UserDto? _existingUser;
        private readonly UserService? _userService;
        private bool _isEditMode;

        public UserEditDialog(UserDto? user, UserService? userService)
        {
            InitializeComponent();
            _existingUser = user;
            _userService = userService;
            _isEditMode = user != null;
            
            InitializeForm();
        }

        private void InitializeForm()
        {
            if (_isEditMode && _existingUser != null)
            {
                // Edit mode
                TxtTitle.Text = $"Benutzer bearbeiten: {_existingUser.Username}";
                TxtUsername.Text = _existingUser.Username;
                TxtUsername.IsReadOnly = true; // Username cannot be changed
                TxtUsername.Background = System.Windows.Media.Brushes.LightGray;
                TxtEmail.Text = _existingUser.Email;
                TxtFirstName.Text = _existingUser.FirstName;
                TxtLastName.Text = _existingUser.LastName;
                ChkIsActive.IsChecked = _existingUser.IsActive;
                ChkIsAdmin.IsChecked = _existingUser.IsAdmin;
            }
            else
            {
                // Create mode
                TxtTitle.Text = "Neuen Benutzer erstellen";
                ChkIsActive.IsChecked = true;
                ChkIsAdmin.IsChecked = false;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                MessageBox.Show("Benutzername ist erforderlich.", "Validierungsfehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                MessageBox.Show("E-Mail ist erforderlich.", "Validierungsfehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
            {
                MessageBox.Show("Vorname ist erforderlich.", "Validierungsfehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFirstName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtLastName.Text))
            {
                MessageBox.Show("Nachname ist erforderlich.", "Validierungsfehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtLastName.Focus();
                return;
            }

            // Email validation
            if (!IsValidEmail(TxtEmail.Text))
            {
                MessageBox.Show("Bitte geben Sie eine gültige E-Mail-Adresse ein.", "Validierungsfehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEmail.Focus();
                return;
            }

            if (_userService == null)
            {
                MessageBox.Show("UserService ist nicht initialisiert.", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                BtnSave.IsEnabled = false;
                BtnCancel.IsEnabled = false;

                if (_isEditMode && _existingUser != null)
                {
                    // Update existing user
                    var updateRequest = new UpdateUserRequest
                    {
                        Email = TxtEmail.Text.Trim(),
                        FirstName = TxtFirstName.Text.Trim(),
                        LastName = TxtLastName.Text.Trim(),
                        IsActive = ChkIsActive.IsChecked ?? true
                        // Note: IsAdmin cannot be changed in this simple implementation
                        // You may want to add admin-only check here
                    };

                    var response = await _userService.UpdateUserAsync(_existingUser.Id, updateRequest);

                    if (response.Success)
                    {
                        MessageBox.Show("Benutzer erfolgreich aktualisiert!", "Erfolg", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Fehler beim Aktualisieren: {response.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Create new user
                    var createRequest = new CreateUserRequest
                    {
                        Username = TxtUsername.Text.Trim(),
                        Email = TxtEmail.Text.Trim(),
                        FirstName = TxtFirstName.Text.Trim(),
                        LastName = TxtLastName.Text.Trim(),
                        IsAdmin = ChkIsAdmin.IsChecked ?? false
                    };

                    System.Diagnostics.Debug.WriteLine($"Creating user: {createRequest.Username}, Email: {createRequest.Email}");
                    var response = await _userService.CreateUserAsync(createRequest);
                    System.Diagnostics.Debug.WriteLine($"Response: Success={response.Success}, Message={response.Message}");

                    if (response.Success)
                    {
                        // Show temporary password
                        var tempPassword = response.Data?.TemporaryPassword ?? "N/A";
                        
                        var passwordWindow = new Window
                        {
                            Title = "Benutzer erstellt",
                            Width = 500,
                            Height = 300,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize
                        };

                        var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
                        
                        stackPanel.Children.Add(new System.Windows.Controls.TextBlock 
                        { 
                            Text = $"✅ Benutzer '{createRequest.Username}' erfolgreich erstellt!",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = System.Windows.Media.Brushes.Green,
                            Margin = new Thickness(0, 0, 0, 20),
                            TextWrapping = TextWrapping.Wrap
                        });
                        
                        stackPanel.Children.Add(new System.Windows.Controls.TextBlock 
                        { 
                            Text = "Temporäres Passwort:",
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Margin = new Thickness(0, 0, 0, 5)
                        });
                        
                        var passwordBox = new System.Windows.Controls.TextBox
                        {
                            Text = tempPassword,
                            IsReadOnly = true,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            FontSize = 16,
                            Padding = new Thickness(10),
                            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
                            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
                            Margin = new Thickness(0, 0, 0, 15)
                        };
                        stackPanel.Children.Add(passwordBox);
                        
                        var copyButton = new System.Windows.Controls.Button
                        {
                            Content = "📋 In Zwischenablage kopieren",
                            Padding = new Thickness(15, 8, 15, 8),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        copyButton.Click += (s, args) =>
                        {
                            Clipboard.SetText(tempPassword);
                            MessageBox.Show("Passwort in Zwischenablage kopiert!", "Erfolg", 
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        };
                        stackPanel.Children.Add(copyButton);
                        
                        stackPanel.Children.Add(new System.Windows.Controls.TextBlock 
                        { 
                            Text = "⚠️ Bitte notieren Sie dieses Passwort und teilen Sie es sicher mit dem Benutzer. Der Benutzer sollte das Passwort beim ersten Login ändern.",
                            FontSize = 12,
                            Foreground = System.Windows.Media.Brushes.Orange,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 10, 0, 0)
                        });

                        passwordWindow.Content = stackPanel;
                        passwordWindow.ShowDialog();
                        
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Fehler beim Erstellen: {response.Message}", "Fehler", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSave.IsEnabled = true;
                BtnCancel.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

