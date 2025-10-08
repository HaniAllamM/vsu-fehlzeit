using System.Windows;
using FehlzeitApp.Services;

namespace FehlzeitApp.Views
{
    public partial class CreateEditUserDialog : Window
    {
        private readonly UserDto? _existingUser;
        public CreateUserRequest UserRequest { get; private set; }

        // Constructor for creating new user
        public CreateEditUserDialog(UserDto? existingUser)
        {
            InitializeComponent();
            _existingUser = existingUser;

            if (_existingUser != null)
            {
                // Edit mode
                TxtTitle.Text = "✏️ Benutzer bearbeiten";
                BtnSave.Content = "Speichern";
                TxtInfoMessage.Text = "Änderungen werden gespeichert.";
                PanelStatus.Visibility = Visibility.Visible;

                // Populate fields
                TxtUsername.Text = _existingUser.Username;
                TxtUsername.IsEnabled = false; // Username cannot be changed
                TxtEmail.Text = _existingUser.Email;
                TxtFirstName.Text = _existingUser.FirstName;
                TxtLastName.Text = _existingUser.LastName;
                
                if (_existingUser.IsAdmin)
                    RbAdmin.IsChecked = true;
                else
                    RbUser.IsChecked = true;

                ChkIsActive.IsChecked = _existingUser.IsActive;
            }
            else
            {
                // Create mode
                TxtTitle.Text = "➕ Neuer Benutzer";
                BtnSave.Content = "Erstellen";
                TxtInfoMessage.Text = "Ein temporäres Passwort wird automatisch generiert.";
                PanelStatus.Visibility = Visibility.Collapsed;
            }

            UserRequest = new CreateUserRequest();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                MessageBox.Show("Bitte geben Sie einen Benutzernamen ein.", 
                              "Validierungsfehler", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                TxtUsername.Focus();
                return;
            }

            // Build request
            UserRequest = new CreateUserRequest
            {
                Username = TxtUsername.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim(),
                FirstName = string.IsNullOrWhiteSpace(TxtFirstName.Text) ? null : TxtFirstName.Text.Trim(),
                LastName = string.IsNullOrWhiteSpace(TxtLastName.Text) ? null : TxtLastName.Text.Trim(),
                IsAdmin = RbAdmin.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
