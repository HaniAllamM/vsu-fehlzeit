using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class BenachrichtigungService
    {
        private readonly AuthService _authService;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public BenachrichtigungService(AuthService authService, ConfigurationService configService)
        {
            _authService = authService;
            _baseUrl = configService.ApiSettings.BaseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets all notifications for the current user
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<List<BenachrichtigungDto>>> GetAllAsync(bool? isRead = null, string? type = null)
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen";
                var queryParams = new List<string>();

                if (isRead.HasValue)
                    queryParams.Add($"isRead={isRead.Value}");

                if (!string.IsNullOrEmpty(type))
                    queryParams.Add($"type={type}");

                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                System.Diagnostics.Debug.WriteLine($"[BenachrichtigungService] Calling URL: {url}");
                System.Diagnostics.Debug.WriteLine($"[BenachrichtigungService] Token: {(_authService.Token != null ? "Present" : "NULL")}");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                System.Diagnostics.Debug.WriteLine($"[BenachrichtigungService] Sending request...");
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[BenachrichtigungService] Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[BenachrichtigungService] Response Content: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<List<BenachrichtigungDto>>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<List<BenachrichtigungDto>>
                    {
                        Success = false,
                        Message = "Failed to deserialize response",
                        Data = new List<BenachrichtigungDto>()
                    };
                }

                return new FehlzeitApp.Models.ApiResponse<List<BenachrichtigungDto>>
                {
                    Success = false,
                    Message = $"API Error: {response.StatusCode}",
                    Data = new List<BenachrichtigungDto>()
                };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<List<BenachrichtigungDto>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<BenachrichtigungDto>()
                };
            }
        }

        /// <summary>
        /// Gets the count of unread notifications
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<int>> GetUnreadCountAsync()
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen/unread-count";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<int>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<int> { Success = false, Message = "Failed to deserialize", Data = 0 };
                }

                return new FehlzeitApp.Models.ApiResponse<int> { Success = false, Message = $"API Error: {response.StatusCode}", Data = 0 };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<int> { Success = false, Message = $"Error: {ex.Message}", Data = 0 };
            }
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<bool>> MarkAsReadAsync(int benachrichtigungId)
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen/{benachrichtigungId}/mark-read";

                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<bool>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = "Failed to deserialize", Data = false };
                }

                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"API Error: {response.StatusCode}", Data = false };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"Error: {ex.Message}", Data = false };
            }
        }

        /// <summary>
        /// Marks all notifications as read
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<bool>> MarkAllAsReadAsync()
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen/mark-all-read";

                using var request = new HttpRequestMessage(HttpMethod.Put, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<bool>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = "Failed to deserialize", Data = false };
                }

                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"API Error: {response.StatusCode}", Data = false };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"Error: {ex.Message}", Data = false };
            }
        }

        /// <summary>
        /// Deletes a notification
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<bool>> DeleteAsync(int benachrichtigungId)
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen/{benachrichtigungId}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<bool>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = "Failed to deserialize", Data = false };
                }

                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"API Error: {response.StatusCode}", Data = false };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"Error: {ex.Message}", Data = false };
            }
        }

        /// <summary>
        /// Deletes all notifications
        /// </summary>
        public async Task<FehlzeitApp.Models.ApiResponse<bool>> DeleteAllAsync()
        {
            try
            {
                var url = $"{_baseUrl}/benachrichtigungen/all";

                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FehlzeitApp.Models.ApiResponse<bool>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = "Failed to deserialize", Data = false };
                }

                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"API Error: {response.StatusCode}", Data = false };
            }
            catch (Exception ex)
            {
                return new FehlzeitApp.Models.ApiResponse<bool> { Success = false, Message = $"Error: {ex.Message}", Data = false };
            }
        }
    }

    /// <summary>
    /// Notification model for WPF (API response) - matches backend German property names
    /// </summary>
    public class BenachrichtigungDto
    {
        public int Id { get; set; }
        public int EmpfaengerId { get; set; }
        public int? SenderId { get; set; }
        public string Typ { get; set; } = string.Empty;
        public string Titel { get; set; } = string.Empty;
        public string Nachricht { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool Gelesen { get; set; }
        public DateTime Erstellungsdatum { get; set; }
        public DateTime? GelesenAm { get; set; }
        
        // Related Entity Info
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        
        // Employee and Project Info
        public string? MitarbeiterName { get; set; }
        public int? MitarbeiterId { get; set; }
        public string? ObjektName { get; set; }
        public int? ObjektId { get; set; }
    }

}
