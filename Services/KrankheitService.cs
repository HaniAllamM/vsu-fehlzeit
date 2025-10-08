using System.Collections.Generic;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class KrankheitService : ApiServiceBase
    {
        public KrankheitService(AuthService authService, ConfigurationService configService) : base(authService, configService)
        {
        }
        
        public async Task<ApiResponse<List<Krankheit>>> GetAllAsync()
        {
            return await GetAsync<List<Krankheit>>("krankheiten");
        }
        
        public async Task<ApiResponse<Krankheit>> GetByIdAsync(int id)
        {
            return await GetAsync<Krankheit>($"krankheiten/{id}");
        }
        
        public async Task<ApiResponse<Krankheit>> CreateAsync(Krankheit krankheit)
        {
            return await PostAsync<Krankheit>("krankheiten", krankheit);
        }
        
        public async Task<ApiResponse<Krankheit>> UpdateAsync(int id, Krankheit krankheit)
        {
            return await PutAsync<Krankheit>($"krankheiten/{id}", krankheit);
        }
        
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            return await base.DeleteAsync($"krankheiten/{id}");
        }
        
        public async Task<ApiResponse<List<Krankheit>>> SearchAsync(string searchTerm)
        {
            return await GetAsync<List<Krankheit>>($"krankheiten/search?term={searchTerm}");
        }
        
        public async Task<ApiResponse<PagedResponse<Krankheit>>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string searchTerm = "")
        {
            var endpoint = $"krankheiten/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                endpoint += $"&searchTerm={searchTerm}";
            }
            return await GetAsync<PagedResponse<Krankheit>>(endpoint);
        }
    }
}
