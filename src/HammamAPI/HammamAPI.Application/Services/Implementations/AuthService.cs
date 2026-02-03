using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using HammamAPI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HammamAPI.Application.Services.Implementations;

/// <summary>
/// Implémentation du service d'authentification avec JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly IEmployeRepository _employeRepository;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<Guid, string> _refreshTokens = new(); // En production: stocker en base

    public AuthService(IEmployeRepository employeRepository, IConfiguration configuration)
    {
        _employeRepository = employeRepository;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Récupérer l'employé par username
        var employe = await _employeRepository.GetByUsernameAsync(request.Username);
        
        if (employe == null || !employe.Actif)
            return null;

        // Vérifier le mot de passe avec BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, employe.PasswordHash))
            return null;

        // Mettre à jour la date de dernière connexion
        await _employeRepository.UpdateLastLoginAsync(employe.Id);

        // Générer les tokens
        var token = GenerateJwtToken(employe);
        var refreshToken = GenerateRefreshToken();
        
        // Stocker le refresh token (en production: en base de données)
        _refreshTokens[employe.Id] = refreshToken;

        var expiresAt = DateTime.UtcNow.AddHours(
            int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "8")
        );

        return new LoginResponse(
            Token: token,
            RefreshToken: refreshToken,
            ExpiresAt: expiresAt,
            Employe: new EmployeDto(
                Id: employe.Id,
                Username: employe.Username,
                Nom: employe.Nom,
                Prenom: employe.Prenom,
                HammamId: employe.HammamId,
                HammamNom: employe.Hammam?.Nom ?? "",
                Langue: employe.Langue,
                Role: employe.Role.ToString(),
                Actif: employe.Actif,
                CreatedAt: employe.CreatedAt,
                LastLoginAt: employe.LastLoginAt
            )
        );
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        // Trouver l'employé associé au refresh token
        var employeId = _refreshTokens.FirstOrDefault(x => x.Value == refreshToken).Key;
        
        if (employeId == Guid.Empty)
            return null;

        var employe = await _employeRepository.GetByIdAsync(employeId);
        
        if (employe == null || !employe.Actif)
            return null;

        // Générer de nouveaux tokens
        var newToken = GenerateJwtToken(employe);
        var newRefreshToken = GenerateRefreshToken();
        
        _refreshTokens[employe.Id] = newRefreshToken;

        var expiresAt = DateTime.UtcNow.AddHours(
            int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "8")
        );

        return new LoginResponse(
            Token: newToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: expiresAt,
            Employe: new EmployeDto(
                Id: employe.Id,
                Username: employe.Username,
                Nom: employe.Nom,
                Prenom: employe.Prenom,
                HammamId: employe.HammamId,
                HammamNom: employe.Hammam?.Nom ?? "",
                Langue: employe.Langue,
                Role: employe.Role.ToString(),
                Actif: employe.Actif,
                CreatedAt: employe.CreatedAt,
                LastLoginAt: employe.LastLoginAt
            )
        );
    }

    public Task<bool> LogoutAsync(Guid employeId)
    {
        _refreshTokens.Remove(employeId);
        return Task.FromResult(true);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string GenerateRandomPassword(int length = 10)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateJwtToken(Domain.Entities.Employe employe)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!);
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employe.Id.ToString()),
            new(ClaimTypes.Name, employe.Username),
            new("HammamId", employe.HammamId.ToString()),
            new(ClaimTypes.Role, employe.Role.ToString()),
            new("Langue", employe.Langue)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(
                int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "8")
            ),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
