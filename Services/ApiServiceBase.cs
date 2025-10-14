using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public abstract class ApiServiceBase
    {
        protected readonly HttpClient _httpClient;
        protected readonly ConfigurationService _configService;
        protected readonly string _baseUrl;
        
        protected ApiServiceBase(AuthService authService, ConfigurationService configService)
        {
            _httpClient = new HttpClient();
            _configService = configService;
            _baseUrl = _configService.ApiSettings.BaseUrl;

            _httpClient.Timeout = TimeSpan.FromSeconds(5); // Reduced timeout

            if (authService.IsAuthenticated && !string.IsNullOrEmpty(authService.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authService.Token);
            }
        }
        
        protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var fullUrl = $"{_baseUrl}/{endpoint}";
                Console.WriteLine($"API GET Request: {fullUrl}");
                Console.WriteLine($"Auth Header: {_httpClient.DefaultRequestHeaders.Authorization}");
                Console.WriteLine($"Auth Header: {_httpClient.DefaultRequestHeaders.Authorization?.ToString() ?? "None"}");
                
                var response = await _httpClient.GetAsync(fullUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"API Response Status: {response.StatusCode}");
                Console.WriteLine($"========== FULL API RESPONSE ==========" );
                Console.WriteLine(content);
                Console.WriteLine($"=======================================");

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, GetJsonOptions());
                    return apiResponse ?? new ApiResponse<T> { Success = false, Message = "Failed to deserialize response." };
                }
                else
                {
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content, GetJsonOptions());
                        var errorMsg = errorResponse?.ContainsKey("message") == true 
                            ? errorResponse["message"].ToString() 
                            : content;
                        
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = $"API Error ({response.StatusCode}): {errorMsg}",
                            Errors = new List<string> { content }
                        };
                    }
                    catch
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            Message = $"API Error: {response.StatusCode}\n\n{content}",
                            Errors = new List<string> { content }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Connection error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        
        protected async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var fullUrl = $"{_baseUrl}/{endpoint}";
                System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] POST {fullUrl}");
                System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] Request body: {json}");
                
                var response = await _httpClient.PostAsync(fullUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] Response body: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, GetJsonOptions());
                    System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] Deserialized successfully: {result != null}");
                    return new ApiResponse<T> { Success = true, Data = result };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] ERROR: {response.StatusCode}");
                    
                    // Try to parse error message from response
                    string errorMessage = $"API Error: {response.StatusCode}";
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, GetJsonOptions());
                        if (errorResponse != null)
                        {
                            if (errorResponse.ContainsKey("message"))
                                errorMessage = errorResponse["message"]?.ToString() ?? errorMessage;
                            else if (errorResponse.ContainsKey("Message"))
                                errorMessage = errorResponse["Message"]?.ToString() ?? errorMessage;
                            else if (errorResponse.ContainsKey("title"))
                                errorMessage = errorResponse["title"]?.ToString() ?? errorMessage;
                            else
                                errorMessage = responseContent; // Show raw response if no standard field found
                        }
                    }
                    catch
                    {
                        // If parsing fails, use raw response
                        errorMessage = responseContent;
                    }
                    
                    return new ApiResponse<T> 
                    { 
                        Success = false, 
                        Message = errorMessage,
                        Errors = new List<string> { responseContent }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiServiceBase] EXCEPTION: {ex.Message}");
                return new ApiResponse<T> 
                { 
                    Success = false, 
                    Message = "Connection error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        
        protected async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, GetJsonOptions());
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{_baseUrl}/{endpoint}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, GetJsonOptions());
                    return new ApiResponse<T> { Success = true, Data = result };
                }
                else
                {
                    return new ApiResponse<T> 
                    { 
                        Success = false, 
                        Message = $"API Error: {response.StatusCode}",
                        Errors = new List<string> { responseContent }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> 
                { 
                    Success = false, 
                    Message = "Connection error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        protected async Task<ApiResponse<T>> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content)
        {
            try
            {
                var fullUrl = $"{_baseUrl}/{endpoint}";
                System.Diagnostics.Debug.WriteLine($"API POST Multipart Request: {fullUrl}");
                
                var response = await _httpClient.PostAsync(fullUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"API Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"API Response Content: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, GetJsonOptions());
                    return apiResponse ?? new ApiResponse<T> { Success = false, Message = "Failed to deserialize response." };
                }
                else
                {
                    return new ApiResponse<T>
                    {
                        Success = false,
                        Message = $"API Error: {response.StatusCode}",
                        Errors = new List<string> { responseContent }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Connection error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        
        protected async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}");
                
                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<bool> { Success = true, Data = true };
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<bool> 
                    { 
                        Success = false, 
                        Message = $"API Error: {response.StatusCode}",
                        Errors = new List<string> { content }
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> 
                { 
                    Success = false, 
                    Message = "Connection error",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        
        protected JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
