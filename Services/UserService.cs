using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FehlzeitApp.Models;

namespace FehlzeitApp.Services
{
    public class UserService : ApiServiceBase
    {
        public UserService(AuthService authService, ConfigurationService configService) 
            : base(authService, configService)
        {
        }

        // Get all users (Admin only)
        public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync(string? searchTerm = null, string? roleFilter = null, bool activeOnly = false)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(searchTerm))
                    queryParams.Add($"searchTerm={searchTerm}");
                
                if (!string.IsNullOrEmpty(roleFilter))
                    queryParams.Add($"roleFilter={roleFilter}");
                
                // Always include activeOnly parameter
                queryParams.Add($"activeOnly={activeOnly.ToString().ToLower()}");
                
                var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                
                // Use users endpoint for actual user data
                var fullUrl = $"{_configService.ApiSettings.BaseUrl}/users{query}";
                System.Diagnostics.Debug.WriteLine($"[UserService] GET {fullUrl}");
                
                var response = await _httpClient.GetAsync(fullUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"[UserService] Response: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[UserService] Content: {content}");
                
                if (response.IsSuccessStatusCode)
                {
                    // Users API returns { success, message, users } format
                    var userResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(content, 
                        new System.Text.Json.JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true
                        });
                    
                    if (userResponse.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        var users = new List<UserDto>();
                        
                        if (userResponse.TryGetProperty("users", out var usersProp) && usersProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var user in usersProp.EnumerateArray())
                            {
                                users.Add(new UserDto
                                {
                                    Id = user.GetProperty("id").GetInt32(),
                                    Username = user.GetProperty("username").GetString() ?? "",
                                    Email = user.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                                    FirstName = user.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : null,
                                    LastName = user.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : null,
                                    IsActive = user.TryGetProperty("isActive", out var activeProp) && activeProp.GetBoolean(),
                                    IsAdmin = user.TryGetProperty("isAdmin", out var adminProp) && adminProp.GetBoolean()
                                });
                            }
                        }
                        
                        return new ApiResponse<List<UserDto>>
                        {
                            Success = true,
                            Data = users,
                            Message = userResponse.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "Success" : "Success"
                        };
                    }
                    
                    return new ApiResponse<List<UserDto>>
                    {
                        Success = false,
                        Message = userResponse.TryGetProperty("message", out var errorMsgProp) ? errorMsgProp.GetString() ?? "Failed to load users" : "Failed to load users",
                        Data = new List<UserDto>()
                    };
                }
                else
                {
                    return new ApiResponse<List<UserDto>>
                    {
                        Success = false,
                        Message = $"API Error: {response.StatusCode}",
                        Errors = new List<string> { content }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserService] Exception: {ex.Message}");
                return new ApiResponse<List<UserDto>>
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}",
                    Data = new List<UserDto>()
                };
            }
        }
        
        private class UserListResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<UserDto> Users { get; set; } = new();
        }

        // Get user by ID
        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        {
            return await GetAsync<UserDto>($"users/{id}");
        }

        // Create new user (Admin only)
        public async Task<ApiResponse<CreateUserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            return await PostAsync<CreateUserResponse>("users", request);
        }

        // Update user profile
        public async Task<ApiResponse<bool>> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            return await PutAsync<bool>($"users/{id}", request);
        }

        // Delete user (Admin only)
        public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
        {
            return await DeleteAsync($"users/{id}");
        }

        // Change own password
        public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            return await PostAsync<bool>($"users/{userId}/change-password", request);
        }

        // Reset user password (Admin only)
        public async Task<ApiResponse<ResetPasswordResponse>> ResetPasswordAsync(int userId)
        {
            return await PostAsync<ResetPasswordResponse>($"users/{userId}/reset-password", new { });
        }

        // Get current user profile
        public async Task<ApiResponse<UserDto>> GetMyProfileAsync()
        {
            return await GetAsync<UserDto>("users/me");
        }

        // Bulk import users (Admin only)
        public async Task<BulkImportBenutzerResult> BulkImportAsync(List<ImportBenutzerItem> benutzer, bool clearExisting = false)
        {
            try
            {
                var request = new BulkImportBenutzerRequest
                {
                    Benutzer = benutzer,
                    ClearExisting = clearExisting
                };

                var response = await PostAsync<BulkImportBenutzerResult>("users/bulk-import", request);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data;
                }
                
                return new BulkImportBenutzerResult
                {
                    Success = false,
                    Message = response.Message,
                    InsertedCount = 0,
                    ErrorCount = 0,
                    Errors = response.Errors ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                return new BulkImportBenutzerResult
                {
                    Success = false,
                    Message = $"Fehler beim Bulk-Import: {ex.Message}",
                    InsertedCount = 0,
                    ErrorCount = 0,
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }

    // DTOs
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public System.DateTime? CreatedAt { get; set; }
        public System.DateTime? LastLoginAt { get; set; }
        public System.DateTime? PasswordChangedAt { get; set; }
        public bool MustChangePassword { get; set; }
        public int FailedLoginAttempts { get; set; }
        
        public string DisplayName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName) 
            ? Username 
            : $"{FirstName} {LastName}".Trim();
        
        public string RoleText => IsAdmin ? "Administrator" : "Benutzer";
        public string StatusText => !IsActive ? "Inaktiv" : (FailedLoginAttempts >= 5 ? "Gesperrt" : "Aktiv");
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class CreateUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }  // API returns "userId" not "newUserId"
        public string? TemporaryPassword { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
    }
}
