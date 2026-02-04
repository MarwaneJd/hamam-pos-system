using HammamAPI.Application.DTOs;
using HammamAPI.Application.Services;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Controller d'authentification
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly HammamDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, HammamDbContext context, ILogger<AuthController> logger)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Récupère les profils uniques pour l'écran de login (Utilisateur1, Utilisateur2)
    /// </summary>
    [HttpGet("profiles")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<EmployeProfileDto>>> GetProfiles()
    {
        try
        {
            // Récupérer tous les employés actifs non-admin
            var allEmployes = await _context.Employes
                .Where(e => e.Actif && e.Role != Domain.Entities.EmployeRole.Admin)
                .ToListAsync();
            
            // Grouper par username et prendre le premier de chaque groupe
            var profiles = allEmployes
                .GroupBy(e => e.Username)
                .Select(g => g.First())
                .OrderBy(e => e.Username)
                .Select(e => new EmployeProfileDto
                {
                    Id = Guid.Empty, // Pas d'ID spécifique car ce n'est qu'un profil type
                    Username = e.Username,
                    Prenom = "",
                    Nom = "",
                    Icone = e.Icone,
                    HammamId = Guid.Empty,
                    HammamNom = ""
                })
                .ToList();

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des profils");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Connexion employé/admin
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            if (result == null)
            {
                _logger.LogWarning("Échec de connexion pour l'utilisateur: {Username}", request.Username);
                return Unauthorized(new { message = "Identifiants invalides" });
            }

            _logger.LogInformation("Connexion réussie pour l'utilisateur: {Username}", request.Username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Rafraîchissement du token JWT
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (result == null)
            {
                return Unauthorized(new { message = "Token de rafraîchissement invalide" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du rafraîchissement du token");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Déconnexion
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (Guid.TryParse(userId, out var employeId))
            {
                await _authService.LogoutAsync(employeId);
                _logger.LogInformation("Déconnexion réussie pour l'employé: {EmployeId}", employeId);
            }

            return Ok(new { message = "Déconnexion réussie" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déconnexion");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Validation du token actuel
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        return Ok(new { valid = true });
    }
}
