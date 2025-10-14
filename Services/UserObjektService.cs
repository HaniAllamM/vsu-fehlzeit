using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class UserObjektService : ApiServiceBase
    {
        public UserObjektService(AuthService authService, ConfigurationService configService) 
            : base(authService, configService)
        {
        }

        // Get assignments for a specific user (show which objekts the user is assigned to)
        public async Task<ApiResponse<List<UserObjektAssignment>>> GetAssignmentsForUserAsync(int userId, string username)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Fetching objekts for user {userId}");
                
                // Use the working legacy endpoint: /api/objekts/users/{userId}/objekts
                var fullUrl = $"{_configService.ApiSettings.BaseUrl}/objekts/users/{userId}/objekts";
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] GET {fullUrl}");
                
                var httpResponse = await _httpClient.GetAsync(fullUrl);
                var content = await httpResponse.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Response: {httpResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Content: {content}");
                
                var assignments = new List<UserObjektAssignment>();
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Handle different response formats
                        JsonElement dataArray;
                        if (jsonElement.TryGetProperty("data", out dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                        {
                            // Response format: { success: true, data: [...] }
                            foreach (var element in dataArray.EnumerateArray())
                            {
                                var objektId = element.TryGetProperty("objektId", out var objIdProp) ? objIdProp.GetInt32() : 0;
                                var objektName = element.TryGetProperty("objektName", out var objNameProp) ? objNameProp.GetString() ?? "" : "";
                                var createdAt = element.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetDateTime() : DateTime.Now;
                                var updatedAt = element.TryGetProperty("updatedAt", out var updatedProp) ? updatedProp.GetDateTime() : DateTime.Now;
                                
                                assignments.Add(new UserObjektAssignment
                                {
                                    UserId = userId,
                                    Username = username,
                                    ObjektId = objektId,
                                    ObjektName = objektName,
                                    AssignedAt = createdAt,
                                    LastUpdated = updatedAt
                                });
                            }
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            // Direct array response
                            foreach (var element in jsonElement.EnumerateArray())
                            {
                                var objektId = element.TryGetProperty("objektId", out var objIdProp) ? objIdProp.GetInt32() : 0;
                                var objektName = element.TryGetProperty("objektName", out var objNameProp) ? objNameProp.GetString() ?? "" : "";
                                var createdAt = element.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetDateTime() : DateTime.Now;
                                var updatedAt = element.TryGetProperty("updatedAt", out var updatedProp) ? updatedProp.GetDateTime() : DateTime.Now;
                                
                                assignments.Add(new UserObjektAssignment
                                {
                                    UserId = userId,
                                    Username = username,
                                    ObjektId = objektId,
                                    ObjektName = objektName,
                                    AssignedAt = createdAt,
                                    LastUpdated = updatedAt
                                });
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UserObjektService] Parse error: {parseEx.Message}");
                    }
                }

                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = httpResponse.IsSuccessStatusCode,
                    Message = httpResponse.IsSuccessStatusCode ? "Success" : $"API Error: {httpResponse.StatusCode}",
                    Data = assignments
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] ERROR: {ex.Message}");
                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // Get assignments for a specific objekt (show which users are assigned to the objekt)
        public async Task<ApiResponse<List<UserObjektAssignment>>> GetAssignmentsForObjektAsync(int objektId, string objektName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Fetching users for objekt {objektId}");
                
                // Use the working legacy endpoint: /api/objekts/{id}/users
                var fullUrl = $"{_configService.ApiSettings.BaseUrl}/objekts/{objektId}/users";
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] GET {fullUrl}");
                
                var httpResponse = await _httpClient.GetAsync(fullUrl);
                var content = await httpResponse.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Response: {httpResponse.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Content: {content}");
                
                var assignments = new List<UserObjektAssignment>();
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        // Handle different response formats
                        JsonElement dataArray;
                        if (jsonElement.TryGetProperty("data", out dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                        {
                            // Response format: { success: true, data: [...] }
                            foreach (var element in dataArray.EnumerateArray())
                            {
                                var userId = element.TryGetProperty("userId", out var userIdProp) ? userIdProp.GetInt32() : 0;
                                var username = element.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() ?? "" : "";
                                var userDisplayName = element.TryGetProperty("userDisplayName", out var displayNameProp) ? displayNameProp.GetString() ?? "" : "";
                                var createdAt = element.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetDateTime() : DateTime.Now;
                                var updatedAt = element.TryGetProperty("updatedAt", out var updatedProp) ? updatedProp.GetDateTime() : DateTime.Now;
                                
                                // Parse display name to get first and last name
                                var nameParts = userDisplayName.Split(' ', 2);
                                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                                
                                assignments.Add(new UserObjektAssignment
                                {
                                    UserId = userId,
                                    Username = username,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    Email = "", // Not provided by legacy endpoint
                                IsAdmin = username.ToLower() == "admin", // Simple check
                                ObjektId = objektId,
                                ObjektName = objektName,
                                AssignedAt = createdAt,
                                LastUpdated = updatedAt
                            });
                        }
                        }
                        else if (jsonElement.ValueKind == JsonValueKind.Array)
                        {
                            // Direct array response
                            foreach (var element in jsonElement.EnumerateArray())
                            {
                                var userId = element.TryGetProperty("userId", out var userIdProp) ? userIdProp.GetInt32() : 0;
                                var username = element.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() ?? "" : "";
                                var userDisplayName = element.TryGetProperty("userDisplayName", out var displayNameProp) ? displayNameProp.GetString() ?? "" : "";
                                var createdAt = element.TryGetProperty("createdAt", out var createdProp) ? createdProp.GetDateTime() : DateTime.Now;
                                var updatedAt = element.TryGetProperty("updatedAt", out var updatedProp) ? updatedProp.GetDateTime() : DateTime.Now;
                                
                                // Parse display name to get first and last name
                                var nameParts = userDisplayName.Split(' ', 2);
                                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                                var lastName = nameParts.Length > 1 ? nameParts[1] : "";
                                
                                assignments.Add(new UserObjektAssignment
                                {
                                    UserId = userId,
                                    Username = username,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    Email = "", // Not provided by legacy endpoint
                                    IsAdmin = username.ToLower() == "admin", // Simple check
                                    ObjektId = objektId,
                                    ObjektName = objektName,
                                    AssignedAt = createdAt,
                                    LastUpdated = updatedAt
                                });
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UserObjektService] Parse error: {parseEx.Message}");
                    }
                }

                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = httpResponse.IsSuccessStatusCode,
                    Message = httpResponse.IsSuccessStatusCode ? "Success" : $"API Error: {httpResponse.StatusCode}",
                    Data = assignments
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] ERROR: {ex.Message}");
                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // Assign user to objekt
        public async Task<ApiResponse<bool>> AssignUserToObjektAsync(int userId, int objektId)
        {
            try
            {
                // Use the working legacy endpoint: POST /api/objekts/{objektId}/users
                var request = new AssignUserToObjektRequest { UserId = userId, ObjektId = objektId };
                var response = await PostAsync<ApiResponse<object>>($"objekts/{objektId}/users", request);
                
                return new ApiResponse<bool>
                {
                    Success = response.Success,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        // Remove user from objekt
        public async Task<ApiResponse<bool>> RemoveUserFromObjektAsync(int userId, int objektId)
        {
            try
            {
                // Use the working legacy endpoint: DELETE /api/objekts/{objektId}/users/{userId}
                var response = await DeleteAsync($"objekts/{objektId}/users/{userId}");
                
                return response;
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }


        // Check if user has access to objekt
        public async Task<ApiResponse<bool>> CheckUserAccessAsync(int userId, int objektId)
        {
            try
            {
                // Use the working legacy endpoint: GET /api/objekts/{objektId}/users/{userId}/access
                var response = await GetAsync<ApiResponse<object>>($"objekts/{objektId}/users/{userId}/access");
                
                return new ApiResponse<bool>
                {
                    Success = response.Success,
                    Message = response.Message,
                    Data = response.Success // If the call succeeds, user has access
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}
