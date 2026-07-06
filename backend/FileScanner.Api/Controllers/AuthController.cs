using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FileScanner.Api.Data;
using FileScanner.Api.Models;

namespace FileScanner.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private static readonly string JwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
        ?? "FileScannerSuperSecretKey2026!@#$%^&*()";

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { error = "Bu email zaten kayıtlı." });

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return Ok(new LoginResponse
        {
            Token = token,
            User = new UserInfo { Id = user.Id, Email = user.Email, HasApiKey = false }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Email veya şifre yanlış." });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return Ok(new LoginResponse
        {
            Token = token,
            User = new UserInfo 
            { 
                Id = user.Id, 
                Email = user.Email, 
                HasApiKey = !string.IsNullOrEmpty(user.ApiKey) 
            }
        });
    }

    [HttpPost("apikey")]
    [Authorize]
    public async Task<ActionResult> SaveApiKey([FromBody] ApiKeyRequest req)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Encrypt API key with AES
        user.ApiKey = Encrypt(req.ApiKey);
        await _db.SaveChangesAsync();

        return Ok(new { message = "API key kaydedildi." });
    }

    [HttpPost("preferences")]
    [Authorize]
    public async Task<ActionResult> SavePreferences([FromBody] UserPreferencesRequest req)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Provider = req.Provider ?? "9router";
        user.PreferredVisionModel = req.VisionModel ?? "my-combo";
        user.PreferredTextModel = req.TextModel ?? "my-combo";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Tercihler kaydedildi." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetMe()
    {
        var user = await _db.Users.FindAsync(GetUserId());
        if (user == null) return NotFound();

        return Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            HasApiKey = !string.IsNullOrEmpty(user.ApiKey),
            Provider = user.Provider,
            VisionModel = user.PreferredVisionModel,
            TextModel = user.PreferredTextModel
        });
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: "FileScanner",
            audience: "FileScanner",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        
        const string key = "FileScanner2026!"; // 16 chars for AES-128
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, 16);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        
        try
        {
            const string key = "FileScanner2026!";
            var fullCipher = Convert.FromBase64String(cipherText);
            
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }
}