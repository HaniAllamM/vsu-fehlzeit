using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FehlzeitApp.Models
{
    // Main entities based on your database schema
    
    public class Mitarbeiter
    {
        public int MitarbeiterId { get; set; }
        public string? Personalnummer { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ObjektId { get; set; }
        public string? Objektname { get; set; }
        public DateTime? Eintritt { get; set; }
        public DateTime? Austritt { get; set; }
        public string? Notes { get; set; }
        public bool Aktive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int? LastModifiedBy { get; set; }
        public int OwnerUserId { get; set; }
        
        // Computed properties for display (matching EmployeeView)
        public string DisplayName => Name;
        public string DepartmentInfo => string.IsNullOrEmpty(Objektname) ? "Keine Abteilung" : Objektname;
        public string StatusText => Aktive ? "Aktiv" : "Inaktiv";
        public bool IsActive => Aktive && (!Austritt.HasValue || Austritt.Value > DateTime.Today);
        
        // For backward compatibility
        public string FullName => Name;
    }
    
    public class FehlzeitDay
    {
        public int FehlzeitDayId { get; set; }
        public int MitarbeiterId { get; set; }
        public string? Personalnummer { get; set; }
        public DateTime Datum { get; set; }
        public int? KrankheitId { get; set; }
        public int? MeldungId { get; set; }
        public string? Bemerkung { get; set; }
        public int? OwnerUserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }  // FIXED: Changed from UpdatedAt to match WebAPI
        public int? LastModifiedBy { get; set; }  // ADDED: Missing property
        
        // API response properties (from joined data)
        public string? MitarbeiterName { get; set; }  // ADDED: Missing property
        public string? KrankheitKurz { get; set; }
        public string? KrankheitBeschreibung { get; set; }
        public string? MeldungBeschreibung { get; set; }
        public string? MeldungFarbe { get; set; }
        public string? LastModifiedByUser { get; set; }  // ADDED: Missing property
        
        // Navigation properties
        public Mitarbeiter? Mitarbeiter { get; set; }
        public Krankheit? Krankheit { get; set; }
        public Meldung? Meldung { get; set; }
    }
    
    public class Krankheit
    {
        public int KrankheitId { get; set; }

        [Required(ErrorMessage = "Kurz ist erforderlich")]
        [StringLength(50, ErrorMessage = "Kurz darf maximal 50 Zeichen lang sein")]
        public string Kurz { get; set; } = string.Empty;

        [Required(ErrorMessage = "Beschreibung ist erforderlich")]
        [StringLength(255, ErrorMessage = "Beschreibung darf maximal 255 Zeichen lang sein")]
        public string Beschreibung { get; set; } = string.Empty;

        public bool Aktiv { get; set; } = true;

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public int? LastModifiedBy { get; set; }
    }
    
    public class Meldung
    {
        public int MeldungId { get; set; }

        [Required(ErrorMessage = "Beschreibung ist erforderlich")]
        [StringLength(255, ErrorMessage = "Beschreibung darf maximal 255 Zeichen lang sein")]
        public string Beschreibung { get; set; } = string.Empty;

        public bool Aktiv { get; set; } = true;

        [StringLength(7, ErrorMessage = "Farbe muss ein 7-stelliger Hex-Code sein")]
        public string? Farbe { get; set; }

        public int Sortierung { get; set; } = 0;

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public int? LastModifiedBy { get; set; }

        // Helper property for UI color binding
        public System.Windows.Media.Color FarbeColor
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(Farbe) && Farbe.StartsWith("#") && Farbe.Length == 7)
                    {
                        return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Farbe);
                    }
                }
                catch { }
                return System.Windows.Media.Colors.Gray;
            }
        }
    }
    
    // Model for Excel import items
    public class ImportObjektItem
    {
        public string ExcelObjektId { get; set; } = string.Empty; // For display only
        public string ObjektName { get; set; } = string.Empty;
    }

    // Model for bulk import result
    public class BulkImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Request model for bulk import
    public class BulkImportObjektRequest
    {
        public List<ImportObjektItem> Objekts { get; set; } = new();
        public bool ClearExisting { get; set; } = false;
    }

    // Mitarbeiter bulk import models
    public class ImportMitarbeiterItem
    {
        public int MitarbeiterId { get; set; }
        public string? Personalnummer { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ObjektId { get; set; }
        public DateTime? Eintritt { get; set; }
        public DateTime? Austritt { get; set; }
        public string? Notes { get; set; }
        public bool Aktive { get; set; } = true;
    }

    public class BulkImportMitarbeiterRequest
    {
        public List<ImportMitarbeiterItem> Mitarbeiters { get; set; } = new();
        public bool ClearExisting { get; set; } = false;
    }

    public class BulkImportMitarbeiterResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Benutzer bulk import models
    public class ImportBenutzerItem
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public string Password { get; set; } = string.Empty;
    }

    public class BulkImportBenutzerRequest
    {
        public List<ImportBenutzerItem> Benutzer { get; set; } = new();
        public bool ClearExisting { get; set; } = false;
    }

    public class BulkImportBenutzerResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class Objekt : INotifyPropertyChanged
    {
        private int _objektId;
        private string _name = string.Empty;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public int ObjektId
        {
            get => _objektId;
            set { _objektId = value; OnPropertyChanged(nameof(ObjektId)); }
        }

        [Required(ErrorMessage = "Name ist erforderlich")]
        [StringLength(100, ErrorMessage = "Name darf maximal 100 Zeichen lang sein")]
        [DisplayName("Name")]
        public string ObjektName
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(ObjektName)); }
        }


        [DisplayName("Erstellt am")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        [DisplayName("Aktualisiert am")]
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(nameof(UpdatedAt)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class Unterlage
    {
        public int UnterlageId { get; set; }
        public int MitarbeiterId { get; set; }
        public int? ObjektId { get; set; }
        public string Bezeichnung { get; set; } = string.Empty;
        public string Dateiname { get; set; } = string.Empty;
        public string? Dateityp { get; set; }
        public int? Dateigroesse { get; set; }
        public string ObjectKey { get; set; } = string.Empty;
        public string? Speicherpfad { get; set; }
        public string? Kategorie { get; set; }
        public DateTime? GueltigAb { get; set; }
        public DateTime? GueltigBis { get; set; }
        public bool IstAktiv { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public int? LastModifiedBy { get; set; }
        
        // Navigation properties
        public string? MitarbeiterName { get; set; }
        public string? ObjektName { get; set; }
        
        // Download link from API
        public string? DownloadLink { get; set; }
        
        // UI property for status display
        public string StatusText { get; set; } = string.Empty;
    }
    
    // For GET /api/unterlagen/ endpoint
    public class UnterlageListRequest
    {
        public int? MitarbeiterId { get; set; }
        public string? Kategorie { get; set; }
        public DateTime? VonDatum { get; set; }
        public DateTime? BisDatum { get; set; }
    }
    
    // === API REQUEST/RESPONSE MODELS (matching your web API) ===
    
    // For GET /api/fehlzeiten/ endpoint
    public class FehlzeitListRequest
    {
        public int? MitarbeiterId { get; set; }
        public DateTime? VonDatum { get; set; }
        public DateTime? BisDatum { get; set; }
        public int? KrankheitId { get; set; }
        public int? MeldungId { get; set; }
    }
    
    // For POST /api/fehlzeiten/ endpoint
    public class CreateFehlzeitDayRequest
    {
        public int MitarbeiterId { get; set; }
        public DateTime Datum { get; set; }
        public int KrankheitId { get; set; }
        public int MeldungId { get; set; }
        public string? Bemerkung { get; set; }
    }
    
    // For PUT /api/fehlzeiten/{id} endpoint
    public class UpdateFehlzeitDayRequest
    {
        public int FehlzeitDayId { get; set; }
        public int MitarbeiterId { get; set; }
        public DateTime Datum { get; set; }
        public int KrankheitId { get; set; }
        public int MeldungId { get; set; }
        public string? Bemerkung { get; set; }
    }
    
    public class FixObjektIdsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int FixedCount { get; set; }
    }
    
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    // Standard API response wrapper used across services
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }
}
