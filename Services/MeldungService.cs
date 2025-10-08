using System.Collections.Generic;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class MeldungService : ApiServiceBase
    {
        public MeldungService(AuthService authService, ConfigurationService configService) : base(authService, configService)
        {
        }
        
        public async Task<ApiResponse<List<Meldung>>> GetAllAsync()
        {
            return await GetAsync<List<Meldung>>("meldungen");
        }
        
        public async Task<ApiResponse<Meldung>> GetByIdAsync(int id)
        {
            return await GetAsync<Meldung>($"meldungen/{id}");
        }
        
        public async Task<ApiResponse<Meldung>> CreateAsync(Meldung meldung)
        {
            return await PostAsync<Meldung>("meldungen", meldung);
        }
        
        public async Task<ApiResponse<Meldung>> UpdateAsync(int id, Meldung meldung)
        {
            return await PutAsync<Meldung>($"meldungen/{id}", meldung);
        }
        
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            return await base.DeleteAsync($"meldungen/{id}");
        }
        
        public async Task<ApiResponse<List<Meldung>>> SearchAsync(string searchTerm)
        {
            return await GetAsync<List<Meldung>>($"meldungen/search?term={searchTerm}");
        }
        
        public async Task<ApiResponse<PagedResponse<Meldung>>> GetPagedAsync(int pageNumber = 1, int pageSize = 20, string searchTerm = "")
        {
            var endpoint = $"meldungen/paged?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                endpoint += $"&searchTerm={searchTerm}";
            }
            return await GetAsync<PagedResponse<Meldung>>(endpoint);
        }
    }
}
