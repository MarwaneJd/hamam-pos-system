using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HammamAPI.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployesController : ControllerBase
{
    private readonly IEmployeRepository _employeRepository;
    private readonly IHammamRepository _hammamRepository;
    private readonly ILogger<EmployesController> _logger;

    public EmployesController(
        IEmployeRepository employeRepository,
        IHammamRepository hammamRepository,
        ILogger<EmployesController> logger)
    {
        _employeRepository = employeRepository;
        _hammamRepository = hammamRepository;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les employés
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeDto>>> GetAll([FromQuery] Guid? hammamId = null)
    {
        var employes = await _employeRepository.GetAllAsync();
        
        if (hammamId.HasValue)
        {
            employes = employes.Where(e => e.HammamId == hammamId.Value);
        }

        var hammams = await _hammamRepository.GetAllAsync();
        var hammamDict = hammams.ToDictionary(h => h.Id, h => h.Nom);

        var result = employes.Select(e => new EmployeDto
        {
            Id = e.Id,
            Nom = e.Nom,
            Prenom = e.Prenom,
            Username = e.Username,
            PasswordClair = e.PasswordClair,
            HammamId = e.HammamId,
            HammamNom = hammamDict.GetValueOrDefault(e.HammamId, "N/A"),
            Role = e.Role.ToString(),
            Langue = e.Langue,
            IsActif = e.Actif,
            DateCreation = e.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Récupère les employés d'un hammam spécifique
    /// </summary>
    [HttpGet("hammam/{hammamId}")]
    public async Task<ActionResult<IEnumerable<EmployeDto>>> GetByHammam(Guid hammamId)
    {
        var employes = await _employeRepository.GetAllAsync();
        employes = employes.Where(e => e.HammamId == hammamId);

        var hammam = await _hammamRepository.GetByIdAsync(hammamId);
        var hammamNom = hammam?.Nom ?? "N/A";

        var result = employes.Select(e => new EmployeDto
        {
            Id = e.Id,
            Nom = e.Nom,
            Prenom = e.Prenom,
            Username = e.Username,
            PasswordClair = e.PasswordClair,
            HammamId = e.HammamId,
            HammamNom = hammamNom,
            Role = e.Role.ToString(),
            Langue = e.Langue,
            IsActif = e.Actif,
            DateCreation = e.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Récupère un employé par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeDto>> GetById(Guid id)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        var hammam = await _hammamRepository.GetByIdAsync(employe.HammamId);

        return Ok(new EmployeDto
        {
            Id = employe.Id,
            Nom = employe.Nom,
            Prenom = employe.Prenom,
            Username = employe.Username,
            PasswordClair = employe.PasswordClair,
            HammamId = employe.HammamId,
            HammamNom = hammam?.Nom ?? "N/A",
            Role = employe.Role.ToString(),
            Langue = employe.Langue,
            IsActif = employe.Actif,
            DateCreation = employe.CreatedAt
        });
    }

    /// <summary>
    /// Crée un nouvel employé
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmployeDto>> Create([FromBody] CreateEmployeDto dto)
    {
        // Vérifier si le username existe déjà
        var existing = await _employeRepository.GetByUsernameAsync(dto.Username);
        if (existing != null)
            return BadRequest(new { message = "Ce nom d'utilisateur existe déjà" });

        var employe = new Employe
        {
            Id = Guid.NewGuid(),
            Nom = dto.Nom,
            Prenom = dto.Prenom,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PasswordClair = dto.Password, // Sauvegarder le mot de passe en clair pour affichage admin
            HammamId = dto.HammamId,
            Role = Enum.Parse<EmployeRole>(dto.Role),
            Langue = dto.Langue ?? "FR",
            Actif = true,
            CreatedAt = DateTime.UtcNow
        };

        await _employeRepository.AddAsync(employe);
        _logger.LogInformation("Nouvel employé créé: {Username}", employe.Username);

        return CreatedAtAction(nameof(GetById), new { id = employe.Id }, new EmployeDto
        {
            Id = employe.Id,
            Nom = employe.Nom,
            Prenom = employe.Prenom,
            Username = employe.Username,
            PasswordClair = employe.PasswordClair,
            HammamId = employe.HammamId,
            Role = employe.Role.ToString(),
            Langue = employe.Langue,
            IsActif = employe.Actif,
            DateCreation = employe.CreatedAt
        });
    }

    /// <summary>
    /// Met à jour un employé
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeDto>> Update(Guid id, [FromBody] UpdateEmployeDto dto)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        employe.Nom = dto.Nom ?? employe.Nom;
        employe.Prenom = dto.Prenom ?? employe.Prenom;
        employe.HammamId = dto.HammamId ?? employe.HammamId;
        employe.Langue = dto.Langue ?? employe.Langue;
        employe.UpdatedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(dto.Role))
            employe.Role = Enum.Parse<EmployeRole>(dto.Role);

        await _employeRepository.UpdateAsync(employe);
        _logger.LogInformation("Employé mis à jour: {Id}", id);

        return Ok(new EmployeDto
        {
            Id = employe.Id,
            Nom = employe.Nom,
            Prenom = employe.Prenom,
            Username = employe.Username,
            HammamId = employe.HammamId,
            Role = employe.Role.ToString(),
            Langue = employe.Langue,
            IsActif = employe.Actif,
            DateCreation = employe.CreatedAt
        });
    }

    /// <summary>
    /// Active/Désactive un employé
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult> ToggleStatus(Guid id)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        employe.Actif = !employe.Actif;
        employe.UpdatedAt = DateTime.UtcNow;
        await _employeRepository.UpdateAsync(employe);

        _logger.LogInformation("Statut employé {Id} changé en: {Status}", id, employe.Actif ? "Actif" : "Inactif");

        return Ok(new { isActif = employe.Actif });
    }

    /// <summary>
    /// Réinitialise le mot de passe d'un employé
    /// </summary>
    [HttpPatch("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        employe.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        employe.PasswordClair = dto.NewPassword; // Sauvegarder le mot de passe en clair pour affichage admin
        employe.UpdatedAt = DateTime.UtcNow;
        await _employeRepository.UpdateAsync(employe);

        _logger.LogInformation("Mot de passe réinitialisé pour l'employé: {Id}", id);

        return Ok(new { message = "Mot de passe réinitialisé avec succès" });
    }

    /// <summary>
    /// Supprime un employé
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        await _employeRepository.DeleteAsync(id);
        _logger.LogInformation("Employé supprimé: {Id}", id);

        return NoContent();
    }
}

// DTOs
public class EmployeDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Username { get; set; } = "";
    public string? PasswordClair { get; set; } // Mot de passe en clair pour affichage
    public Guid HammamId { get; set; }
    public string HammamNom { get; set; } = "";
    public string Role { get; set; } = "";
    public string Langue { get; set; } = "FR";
    public bool IsActif { get; set; }
    public DateTime DateCreation { get; set; }
}

public class CreateEmployeDto
{
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public Guid HammamId { get; set; }
    public string Role { get; set; } = "Employe";
    public string? Langue { get; set; }
}

public class UpdateEmployeDto
{
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public Guid? HammamId { get; set; }
    public string? Role { get; set; }
    public string? Langue { get; set; }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = "";
}
