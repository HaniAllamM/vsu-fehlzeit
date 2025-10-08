using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class FehlzeitService : ApiServiceBase
    {
        public FehlzeitService(AuthService authService, ConfigurationService configService) : base(authService, configService)
        {
        }
        
        // === NEW STORED PROCEDURE-BASED METHODS ===
        
        /// <summary>
        /// Create new Fehlzeit record - calls POST /api/fehlzeiten/
        /// </summary>
        public async Task<ApiResponse<object>> CreateFehlzeitAsync(CreateFehlzeitDayRequest request)
        {
            return await PostAsync<object>("fehlzeiten", request);
        }
        
        /// <summary>
        /// Update existing Fehlzeit record - calls PUT /api/fehlzeiten/{id}
        /// </summary>
        public async Task<ApiResponse<object>> UpdateFehlzeitAsync(int id, UpdateFehlzeitDayRequest request)
        {
            return await PutAsync<object>($"fehlzeiten/{id}", request);
        }
        
        /// <summary>
        /// Delete Fehlzeit record - calls DELETE /api/fehlzeiten/{id}
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteFehlzeitAsync(int id)
        {
            return await DeleteAsync($"fehlzeiten/{id}");
        }
        
        /// <summary>
        /// Read Fehlzeit records with filtering - calls GET /api/fehlzeiten/
        /// </summary>
        public async Task<ApiResponse<object>> GetFehlzeitenAsync(FehlzeitListRequest request)
        {
            var queryParams = new List<string>();
            if (request.MitarbeiterId.HasValue) queryParams.Add($"MitarbeiterId={request.MitarbeiterId}");
            if (request.VonDatum.HasValue) queryParams.Add($"VonDatum={request.VonDatum:yyyy-MM-dd}");
            if (request.BisDatum.HasValue) queryParams.Add($"BisDatum={request.BisDatum:yyyy-MM-dd}");
            if (request.KrankheitId.HasValue) queryParams.Add($"KrankheitId={request.KrankheitId}");
            if (request.MeldungId.HasValue) queryParams.Add($"MeldungId={request.MeldungId}");
            
            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            return await GetAsync<object>($"fehlzeiten{queryString}");
        }
        
        /// <summary>
        /// Upsert Fehlzeit record (Insert if not exists, Update if exists) - calls POST /api/fehlzeiten/upsert
        /// </summary>
        public async Task<ApiResponse<object>> UpsertFehlzeitAsync(CreateFehlzeitDayRequest request)
        {
            return await PostAsync<object>("fehlzeiten/upsert", request);
        }
        
        // === EXISTING METHODS (for backward compatibility) ===
        
        public async Task<ApiResponse<List<FehlzeitDay>>> GetAllAsync()
        {
            return await GetAsync<List<FehlzeitDay>>("fehlzeit");
        }
        
        public async Task<ApiResponse<FehlzeitDay>> GetByIdAsync(int id)
        {
            return await GetAsync<FehlzeitDay>($"fehlzeit/{id}");
        }
        
        public async Task<ApiResponse<List<FehlzeitDay>>> GetByMitarbeiterAsync(int mitarbeiterId)
        {
            return await GetAsync<List<FehlzeitDay>>($"fehlzeit/mitarbeiter/{mitarbeiterId}");
        }
        
        public async Task<ApiResponse<List<FehlzeitDay>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetAsync<List<FehlzeitDay>>($"fehlzeit/daterange?start={startDate:yyyy-MM-dd}&end={endDate:yyyy-MM-dd}");
        }
        
        public async Task<ApiResponse<FehlzeitDay>> CreateAsync(FehlzeitDay fehlzeit)
        {
            return await PostAsync<FehlzeitDay>("fehlzeit", fehlzeit);
        }
        
        public async Task<ApiResponse<FehlzeitDay>> UpdateAsync(int id, FehlzeitDay fehlzeit)
        {
            return await PutAsync<FehlzeitDay>($"fehlzeit/{id}", fehlzeit);
        }
        
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            return await DeleteAsync($"fehlzeit/{id}");
        }
    }
}
