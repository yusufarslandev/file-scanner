using System.ComponentModel.DataAnnotations;

namespace FileScanner.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? ApiKey { get; set; } // Encrypted

    public string? ApiKeyEncrypted { get; set; } // AES encrypted storage

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
}

public class ApiKeyRequest
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
}