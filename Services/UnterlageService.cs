using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class UnterlageService : ApiServiceBase
    {
        public UnterlageService(AuthService authService, ConfigurationService configService) 
            : base(authService, configService)
        {
        }

        public async Task<ApiResponse<List<Unterlage>>> GetAllAsync(int? mitarbeiterId = null, string? kategorie = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (mitarbeiterId.HasValue)
                    queryParams.Add($"mitarbeiterId={mitarbeiterId.Value}");
                if (!string.IsNullOrEmpty(kategorie))
                    queryParams.Add($"kategorie={Uri.EscapeDataString(kategorie)}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"unterlagen{queryString}";

                return await GetAsync<List<Unterlage>>(endpoint);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Unterlage>>
                {
                    Success = false,
                    Message = $"Fehler beim Laden der Unterlagen: {ex.Message}",
                    Data = new List<Unterlage>()
                };
            }
        }

        public async Task<ApiResponse<Unterlage>> GetByIdAsync(int unterlageId)
        {
            try
            {
                return await GetAsync<Unterlage>($"unterlagen/{unterlageId}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Unterlage>
                {
                    Success = false,
                    Message = $"Fehler beim Laden der Unterlage: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Unterlage>> CreateAsync(Unterlage unterlage)
        {
            try
            {
                var createRequest = new
                {
                    MitarbeiterId = unterlage.MitarbeiterId,
                    Bezeichnung = unterlage.Bezeichnung,
                    Dateiname = unterlage.Dateiname,
                    Dateityp = unterlage.Dateityp,
                    Dateigroesse = unterlage.Dateigroesse,
                    ObjectKey = unterlage.ObjectKey,
                    Speicherpfad = unterlage.Speicherpfad,
                    Kategorie = unterlage.Kategorie,
                    GueltigAb = unterlage.GueltigAb,
                    GueltigBis = unterlage.GueltigBis
                };

                return await PostAsync<Unterlage>("unterlagen", createRequest);
            }
            catch (Exception ex)
            {
                return new ApiResponse<Unterlage>
                {
                    Success = false,
                    Message = $"Fehler beim Erstellen der Unterlage: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Unterlage>> UpdateAsync(int unterlageId, Unterlage unterlage)
        {
            try
            {
                var updateRequest = new
                {
                    UnterlageId = unterlageId,
                    Bezeichnung = unterlage.Bezeichnung,
                    Kategorie = unterlage.Kategorie,
                    GueltigAb = unterlage.GueltigAb,
                    GueltigBis = unterlage.GueltigBis,
                    IstAktiv = unterlage.IstAktiv
                };

                return await PutAsync<Unterlage>($"unterlagen/{unterlageId}", updateRequest);
            }
            catch (Exception ex)
            {
                return new ApiResponse<Unterlage>
                {
                    Success = false,
                    Message = $"Fehler beim Aktualisieren der Unterlage: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int unterlageId)
        {
            try
            {
                return await base.DeleteAsync($"unterlagen/{unterlageId}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Fehler beim Löschen der Unterlage: {ex.Message}"
                };
            }
        }

        // Get filtered unterlagen using the /filter endpoint
        public async Task<ApiResponse<List<Unterlage>>> GetFilteredAsync(int? mitarbeiterId = null, string? kategorie = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (mitarbeiterId.HasValue)
                    queryParams.Add($"mitarbeiterId={mitarbeiterId.Value}");
                if (!string.IsNullOrEmpty(kategorie))
                    queryParams.Add($"kategorie={Uri.EscapeDataString(kategorie)}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var endpoint = $"unterlagen/filter{queryString}";

                return await GetAsync<List<Unterlage>>(endpoint);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Unterlage>>
                {
                    Success = false,
                    Message = $"Fehler beim Laden der gefilterten Unterlagen: {ex.Message}",
                    Data = new List<Unterlage>()
                };
            }
        }

        // Get download URL for a document
        public async Task<DownloadUrlResponse> GetDownloadUrlAsync(int unterlageId, int expiryMinutes = 60)
        {
            try
            {
                var endpoint = $"unterlagen/{unterlageId}/download-url?expiryMinutes={expiryMinutes}";
                var response = await GetAsync<DownloadUrlResponse>(endpoint);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data;
                }
                
                return new DownloadUrlResponse
                {
                    Success = false,
                    Message = response.Message ?? "Fehler beim Abrufen der Download-URL"
                };
            }
            catch (Exception ex)
            {
                return new DownloadUrlResponse
                {
                    Success = false,
                    Message = $"Fehler beim Abrufen der Download-URL: {ex.Message}"
                };
            }
        }

        // Get view URL for opening document in browser (especially PDFs)
        public string GetViewUrl(int unterlageId)
        {
            // Use the base URL from the configuration
            return $"{_baseUrl}/unterlagen/{unterlageId}/view";
        }

        // Create unterlage with file upload
        public async Task<ApiResponse<CreateUnterlageResponse>> CreateWithFileAsync(CreateUnterlageWithFileRequest request)
        {
            try
            {
                if (!File.Exists(request.FilePath))
                {
                    return new ApiResponse<CreateUnterlageResponse>
                    {
                        Success = false,
                        Message = "Die angegebene Datei existiert nicht"
                    };
                }

                using var form = new MultipartFormDataContent();
                
                // Add file
                var fileBytes = await File.ReadAllBytesAsync(request.FilePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", Path.GetFileName(request.FilePath));
                
                // Add form fields
                form.Add(new StringContent(request.MitarbeiterId.ToString()), "MitarbeiterId");
                form.Add(new StringContent(request.Bezeichnung), "Bezeichnung");
                
                if (!string.IsNullOrEmpty(request.Kategorie))
                    form.Add(new StringContent(request.Kategorie), "Kategorie");
                    
                if (request.GueltigAb.HasValue)
                    form.Add(new StringContent(request.GueltigAb.Value.ToString("yyyy-MM-dd")), "GueltigAb");
                    
                if (request.GueltigBis.HasValue)
                    form.Add(new StringContent(request.GueltigBis.Value.ToString("yyyy-MM-dd")), "GueltigBis");
                    
                form.Add(new StringContent(request.IstAktiv.ToString()), "IstAktiv");

                return await PostMultipartAsync<CreateUnterlageResponse>("unterlagen/upload", form);
            }
            catch (Exception ex)
            {
                return new ApiResponse<CreateUnterlageResponse>
                {
                    Success = false,
                    Message = $"Fehler beim Hochladen der Datei: {ex.Message}"
                };
            }
        }

        // Get unterlagen by employee ID
        public async Task<ApiResponse<List<Unterlage>>> GetByMitarbeiterAsync(int mitarbeiterId, string? kategorie = null)
        {
            try
            {
                var queryString = !string.IsNullOrEmpty(kategorie) ? $"?kategorie={Uri.EscapeDataString(kategorie)}" : "";
                var endpoint = $"unterlagen/mitarbeiter/{mitarbeiterId}{queryString}";

                return await GetAsync<List<Unterlage>>(endpoint);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Unterlage>>
                {
                    Success = false,
                    Message = $"Fehler beim Laden der Mitarbeiter-Unterlagen: {ex.Message}",
                    Data = new List<Unterlage>()
                };
            }
        }
    }

    // Response models for download functionality
    public class DownloadUrlResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public int ExpiresInMinutes { get; set; }
        public string? FileName { get; set; }
    }

    public class CreateUnterlageWithFileRequest
    {
        public int MitarbeiterId { get; set; }
        public string Bezeichnung { get; set; } = string.Empty;
        public string? Kategorie { get; set; }
        public DateTime? GueltigAb { get; set; }
        public DateTime? GueltigBis { get; set; }
        public bool IstAktiv { get; set; } = true;
        public string FilePath { get; set; } = string.Empty;
    }

    public class CreateUnterlageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UnterlageId { get; set; }
    }
}
