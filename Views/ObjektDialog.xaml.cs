using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FehlzeitApp.Models;

namespace FehlzeitApp.Views
{
    public partial class ObjektDialog : Window
    {
        public Objekt? Objekt { get; private set; }
        private readonly bool _isEditMode;

        public ObjektDialog(Objekt? objekt = null)
        {
            InitializeComponent();
            
            _isEditMode = objekt != null;
            
            if (_isEditMode && objekt != null)
            {
                DialogTitle.Text = "Objekt bearbeiten";
                LoadObjektData(objekt);
            }
            else
            {
                DialogTitle.Text = "Neues Objekt erstellen";
            }
            
            TxtName.Focus();
        }

        private void LoadObjektData(Objekt objekt)
        {
            TxtName.Text = objekt.ObjektName;
        }

        private void ValidateForm(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Validate Name
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                errors.Add("Name ist erforderlich");
            }
            else if (TxtName.Text.Length > 100)
            {
                errors.Add("Name darf maximal 100 Zeichen lang sein");
            }



            // Show/hide errors
            if (errors.Any())
            {
                ErrorText.Text = string.Join("\n• ", errors.Select(e => "• " + e));
                ErrorPanel.Visibility = Visibility.Visible;
                BtnSave.IsEnabled = false;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Collapsed;
                BtnSave.IsEnabled = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create or update objekt
                if (_isEditMode && Objekt != null)
                {
                    // Update existing objekt
                    Objekt.ObjektName = TxtName.Text.Trim();
                    Objekt.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Create new objekt
                    Objekt = new Objekt
                    {
                        ObjektName = TxtName.Text.Trim(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                }

                // Validate using data annotations
                var validationContext = new ValidationContext(Objekt);
                var validationResults = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
                
                if (Validator.TryValidateObject(Objekt, validationContext, validationResults, true))
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToList();
                    ErrorText.Text = string.Join("\n", errors.Select(e => "• " + e));
                    ErrorPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Initial validation
            ValidateForm();
        }
    }
}
