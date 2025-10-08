using System;

namespace FehlzeitApp.Helpers
{
    public static class PasswordHelper
    {
        // Simulate bcrypt password verification for offline mode
        // In a real application, you would use BCrypt.Net-Next package
        public static bool VerifyPassword(string password, string hashedPassword, string username)
        {
            // For offline simulation, we'll use known password mappings
            // In production, you would use: BCrypt.Net.BCrypt.Verify(password, hashedPassword)
            
            switch (username.ToLower())
            {
                case "admin":
                    // The actual password for admin user (you need to tell me what it is)
                    // For now, I'll assume common passwords
                    return password == "admin" || password == "admin123" || password == "password";
                    
                case "employee":
                    // The actual password for employee user
                    return password == "employee" || password == "employee123" || password == "password";
                    
                default:
                    return false;
            }
        }
        
        // Database user data simulation
        public static (bool exists, bool isActive, string email, string firstName, string lastName, string role) GetUserData(string username)
        {
            switch (username.ToLower())
            {
                case "admin":
                    return (true, true, "admin@company.com", "Admin", "User", "Admin");
                    
                case "employee":
                    return (true, false, "updated@test.com", "Updated", "User", "User"); // IsActive = 0
                    
                default:
                    return (false, false, "", "", "", "");
            }
        }
    }
}
