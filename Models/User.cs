using System;

namespace FehlzeitApp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        
        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool IsAdmin => Role?.ToLower() == "admin";
    }
    
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
    
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public User? User { get; set; }
    }
    
    // Web API response format
    public class WebApiLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public WebApiUser? User { get; set; }
    }
    
    public class WebApiUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool MustChangePassword { get; set; }
    }
}
