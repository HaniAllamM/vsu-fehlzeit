using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FehlzeitApp.Models
{
    public class UserObjektAssignment
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public int ObjektId { get; set; }
        public string ObjektName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime LastUpdated { get; set; }

        // Display properties
        public string DisplayName => $"{FirstName} {LastName} ({Username})".Trim();
        public string AssignedAtFormatted => AssignedAt.ToString("dd.MM.yyyy HH:mm");
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class AssignUserToObjektRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ObjektId { get; set; }
    }

    public class RemoveUserFromObjektRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ObjektId { get; set; }
    }

    public class UserObjektResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class AssignedUser
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime LastUpdated { get; set; }

        public string DisplayName => $"{FirstName} {LastName} ({Username})".Trim();
    }

    public class AssignedObjekt
    {
        public int ObjektId { get; set; }
        public string ObjektName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class AccessCheckRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ObjektId { get; set; }
    }

    public class AccessCheckResponse
    {
        public bool HasAccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Response wrapper classes for API calls
    public class UsersForObjektResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AssignedUser>? Data { get; set; }
    }

    public class ObjektsForUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AssignedObjekt>? Data { get; set; }
    }
}
