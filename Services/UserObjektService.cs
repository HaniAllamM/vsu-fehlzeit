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
                
                var objektsUrl = $"{_baseUrl}/objekts/users/{userId}/objekts";
                var objektsResponse = await _httpClient.GetAsync(objektsUrl);
                var objektsContent = await objektsResponse.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Response: {objektsResponse.StatusCode}");
                
                if (!objektsResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<List<UserObjektAssignment>>
                    {
                        Success = false,
                        Message = $"Failed to load objekts: {objektsResponse.StatusCode}"
                    };
                }

                var objektsData = System.Text.Json.JsonSerializer.Deserialize<ObjektsForUserResponse>(objektsContent,
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                
                var assignments = new List<UserObjektAssignment>();
                
                if (objektsData?.Data != null)
                {
                    foreach (var objekt in objektsData.Data)
                    {
                        assignments.Add(new UserObjektAssignment
                        {
                            UserId = userId,
                            Username = username,
                            ObjektId = objekt.ObjektId,
                            ObjektName = objekt.ObjektName,
                            AssignedAt = objekt.AssignedAt
                        });
                    }
                }

                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = true,
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
                
                var usersUrl = $"{_baseUrl}/objekts/{objektId}/users";
                var usersResponse = await _httpClient.GetAsync(usersUrl);
                var usersContent = await usersResponse.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserObjektService] Response: {usersResponse.StatusCode}");
                
                if (!usersResponse.IsSuccessStatusCode)
                {
                    return new ApiResponse<List<UserObjektAssignment>>
                    {
                        Success = false,
                        Message = $"Failed to load users: {usersResponse.StatusCode}"
                    };
                }

                var usersData = System.Text.Json.JsonSerializer.Deserialize<UsersForObjektResponse>(usersContent,
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                
                var assignments = new List<UserObjektAssignment>();
                
                if (usersData?.Data != null)
                {
                    foreach (var user in usersData.Data)
                    {
                        assignments.Add(new UserObjektAssignment
                        {
                            UserId = user.UserId,
                            Username = user.Username,
                            ObjektId = objektId,
                            ObjektName = objektName,
                            AssignedAt = user.AssignedAt
                        });
                    }
                }

                return new ApiResponse<List<UserObjektAssignment>>
                {
                    Success = true,
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
                var request = new { userId = userId };
                var response = await PostAsync<AssignmentResponse>($"objekts/{objektId}/users", request);
                
                return new ApiResponse<bool>
                {
                    Success = response.Success && response.Data?.Success == true,
                    Message = response.Data?.Message ?? response.Message
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

        // Helper classes for API responses
        private class UserListResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<UserDto> Users { get; set; } = new();
        }

        private class ObjektsForUserResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<UserObjektInfo> Data { get; set; } = new();
        }

        private class UsersForObjektResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<ObjektUserInfo> Data { get; set; } = new();
        }

        private class UserObjektInfo
        {
            public int ObjektId { get; set; }
            public string ObjektName { get; set; } = string.Empty;
            public DateTime? AssignedAt { get; set; }
        }

        private class ObjektUserInfo
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public DateTime? AssignedAt { get; set; }
        }

        private class AssignmentResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }

    // Model for displaying assignments
    public class UserObjektAssignment
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ObjektId { get; set; }
        public string ObjektName { get; set; } = string.Empty;
        public DateTime? AssignedAt { get; set; }
    }
}
