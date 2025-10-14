using System.Collections.Generic;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class ObjektService : ApiServiceBase
    {
        private readonly AuthService _authService;

        public ObjektService(AuthService authService, ConfigurationService configService) : base(authService, configService)
        {
            _authService = authService;
        }
        
        public async Task<ApiResponse<List<Objekt>>> GetAllAsync()
        {
            // Check if current user is admin and call appropriate endpoint
            if (_authService.CurrentUser?.IsAdmin == true)
            {
                return await GetAsync<List<Objekt>>("objekts/all");
            }
            else
            {
                return await GetAsync<List<Objekt>>("objekts");
            }
        }
        
        public async Task<ApiResponse<List<Objekt>>> GetAllObjektsAsync()
        {
            return await GetAllAsync();
        }
        
        public async Task<ApiResponse<Objekt>> GetByIdAsync(int id)
        {
            return await GetAsync<Objekt>($"objekts/{id}");
        }
        
        public async Task<ApiResponse<Objekt>> CreateAsync(Objekt objekt)
        {
            return await PostAsync<Objekt>("objekts", objekt);
        }
        
        public async Task<ApiResponse<Objekt>> UpdateAsync(int id, Objekt objekt)
        {
            return await PutAsync<Objekt>($"objekts/{id}", objekt);
        }
        
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            return await base.DeleteAsync($"objekts/{id}");
        }
        
        public async Task<ApiResponse<List<Objekt>>> SearchAsync(string searchTerm)
        {
            return await GetAsync<List<Objekt>>($"objekt/search?term={searchTerm}");
        }
        
        public async Task<ApiResponse<PagedResponse<Objekt>>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string searchTerm = "")
        {
            var endpoint = $"objekt/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                endpoint += $"&searchTerm={searchTerm}";
            }
            return await GetAsync<PagedResponse<Objekt>>(endpoint);
        }
        
        public async Task<BulkImportResult> BulkImportAsync(List<ImportObjektItem> objekts, bool clearExisting)
        {
            var request = new BulkImportObjektRequest
            {
                Objekts = objekts,
                ClearExisting = clearExisting
            };
            
            var response = await PostAsync<BulkImportResult>("objekts/bulk-import", request);
            
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            return new BulkImportResult
            {
                Success = false,
                Message = response.Message ?? "Import fehlgeschlagen",
                InsertedCount = 0,
                ErrorCount = 0
            };
        }
    }
}
