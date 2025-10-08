using System.Collections.Generic;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class MitarbeiterService : ApiServiceBase
    {
        public MitarbeiterService(AuthService authService, ConfigurationService configService) : base(authService, configService)
        {
        }
        
        public async Task<ApiResponse<List<Mitarbeiter>>> GetAllAsync()
        {
            return await GetAsync<List<Mitarbeiter>>("employees?IncludeInactive=true&OrderDescending=false");
        }
        
        public async Task<ApiResponse<Mitarbeiter>> GetByIdAsync(int id)
        {
            return await GetAsync<Mitarbeiter>($"employees/{id}");
        }
        
        public async Task<ApiResponse<Mitarbeiter>> CreateAsync(Mitarbeiter mitarbeiter)
        {
            return await PostAsync<Mitarbeiter>("employees", mitarbeiter);
        }
        
        public async Task<ApiResponse<Mitarbeiter>> UpdateAsync(int id, Mitarbeiter mitarbeiter)
        {
            return await PutAsync<Mitarbeiter>($"employees/{id}", mitarbeiter);
        }
        
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            return await base.DeleteAsync($"employees/{id}");
        }
        
        public async Task<ApiResponse<List<Mitarbeiter>>> SearchAsync(string searchTerm)
        {
            return await GetAsync<List<Mitarbeiter>>($"employees?SearchTerm={searchTerm}&IncludeInactive=true&OrderDescending=false");
        }

        public async Task<BulkImportMitarbeiterResult> BulkImportAsync(List<ImportMitarbeiterItem> mitarbeiters, bool clearExisting)
        {
            var request = new BulkImportMitarbeiterRequest
            {
                Mitarbeiters = mitarbeiters,
                ClearExisting = clearExisting
            };
            
            var response = await PostAsync<BulkImportMitarbeiterResult>("employees/bulk-import", request);
            
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            // Return detailed error information
            var errorList = new List<string> { response.Message ?? "Unknown error" };
            if (response.Errors != null && response.Errors.Count > 0)
            {
                errorList.AddRange(response.Errors);
            }
            
            return new BulkImportMitarbeiterResult
            {
                Success = false,
                Message = response.Message ?? "Import fehlgeschlagen",
                InsertedCount = 0,
                ErrorCount = errorList.Count,
                Errors = errorList
            };
        }
    }
}
