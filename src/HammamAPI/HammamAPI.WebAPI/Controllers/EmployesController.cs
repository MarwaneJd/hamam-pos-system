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
    public async Task<ActionResult<IEnumerable<EmployeListDto>>> GetAll([FromQuery] Guid? hammamId = null)
    {
        var employes = await _employeRepository.GetAllAsync();
        
        if (hammamId.HasValue)
        {
            employes = employes.Where(e => e.HammamId == hammamId.Value && e.Role != EmployeRole.Admin);
        }

        var hammams = await _hammamRepository.GetAllAsync();
        var hammamDict = hammams.ToDictionary(h => h.Id, h => h.Nom);

        var result = employes.Select(e => new EmployeListDto
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
            Icone = e.Icone,
            IsActif = e.Actif,
            DateCreation = e.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Récupère les employés d'un hammam spécifique
    /// </summary>
    [HttpGet("hammam/{hammamId}")]
    public async Task<ActionResult<IEnumerable<EmployeListDto>>> GetByHammam(Guid hammamId)
    {
        var employes = await _employeRepository.GetAllAsync();
        employes = employes.Where(e => e.HammamId == hammamId && e.Role != EmployeRole.Admin);

        var hammam = await _hammamRepository.GetByIdAsync(hammamId);
        var hammamNom = hammam?.Nom ?? "N/A";

        var result = employes.Select(e => new EmployeListDto
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
            Icone = e.Icone,
            IsActif = e.Actif,
            DateCreation = e.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Récupère un employé par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeListDto>> GetById(Guid id)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        var hammam = await _hammamRepository.GetByIdAsync(employe.HammamId);

        return Ok(new EmployeListDto
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
            Icone = employe.Icone,
            IsActif = employe.Actif,
            DateCreation = employe.CreatedAt
        });
    }

    /// <summary>
    /// Crée un nouvel employé avec username auto-généré (Utilisateur1 ou Utilisateur2 par hammam)
    /// Maximum 2 employés par hammam. Mot de passe numérique uniquement.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmployeListDto>> Create([FromBody] CreateEmployeDto dto)
    {
        // Valider que le mot de passe est numérique uniquement
        if (string.IsNullOrEmpty(dto.Password) || !dto.Password.All(char.IsDigit))
            return BadRequest(new { message = "Le mot de passe doit contenir uniquement des chiffres" });

        // Compter les employés de CE hammam uniquement (hors Admin)
        var hammamEmployes = await _employeRepository.GetByHammamIdAsync(dto.HammamId);
        var activeNonAdmin = hammamEmployes.Where(e => e.Role != EmployeRole.Admin && e.Actif).ToList();
        var employeCount = activeNonAdmin.Count;
        
        // Maximum 2 employés par hammam
        if (employeCount >= 2)
            return BadRequest(new { message = "Maximum 2 employés par hammam atteint" });

        // Générer Utilisateur1 ou Utilisateur2 selon le nombre d'employés dans ce hammam
        var newUsername = $"Utilisateur{employeCount + 1}";

        // Vérifier que le mot de passe est unique parmi les employés avec le même username
        var sameUsernameEmployes = await _employeRepository.GetAllByUsernameAsync(newUsername);
        if (sameUsernameEmployes.Any(e => e.Actif && e.PasswordClair == dto.Password))
            return BadRequest(new { message = "Ce code PIN est déjà utilisé par un autre employé. Veuillez en choisir un différent." });
        // Auto-assign icon: User1 (Blue) or User2 (Green)
        var autoIcone = employeCount == 0 ? "User1" : "User2";

        var employe = new Employe
        {
            Id = Guid.NewGuid(),
            Nom = dto.Nom,
            Prenom = dto.Prenom,
            Username = newUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PasswordClair = dto.Password, // Sauvegarder le mot de passe en clair pour affichage admin
            HammamId = dto.HammamId,
            Role = Enum.Parse<EmployeRole>(dto.Role),
            Langue = dto.Langue ?? "FR",
            Actif = true,
            CreatedAt = DateTime.UtcNow,
            Icone = autoIcone
        };

        await _employeRepository.AddAsync(employe);
        _logger.LogInformation("Nouvel employé créé: {Username}", employe.Username);

        return CreatedAtAction(nameof(GetById), new { id = employe.Id }, new EmployeListDto
        {
            Id = employe.Id,
            Nom = employe.Nom,
            Prenom = employe.Prenom,
            Username = employe.Username,
            PasswordClair = employe.PasswordClair,
            HammamId = employe.HammamId,
            Role = employe.Role.ToString(),
            Langue = employe.Langue,
            Icone = employe.Icone,
            IsActif = employe.Actif,
            DateCreation = employe.CreatedAt
        });
    }

    /// <summary>
    /// Met à jour un employé
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeListDto>> Update(Guid id, [FromBody] UpdateEmployeDto dto)
    {
        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        employe.Nom = dto.Nom ?? employe.Nom;
        employe.Prenom = dto.Prenom ?? employe.Prenom;
        employe.HammamId = dto.HammamId ?? employe.HammamId;
        employe.Langue = dto.Langue ?? employe.Langue;
        // Icone is auto-assigned at creation (User1=Blue, User2=Green) and cannot be changed
        employe.UpdatedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(dto.Role))
            employe.Role = Enum.Parse<EmployeRole>(dto.Role);

        await _employeRepository.UpdateAsync(employe);
        _logger.LogInformation("Employé mis à jour: {Id}", id);

        return Ok(new EmployeListDto
        {
            Id = employe.Id,
            Nom = employe.Nom,
            Prenom = employe.Prenom,
            Username = employe.Username,
            HammamId = employe.HammamId,
            Role = employe.Role.ToString(),
            Langue = employe.Langue,
            Icone = employe.Icone,
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
        if (string.IsNullOrEmpty(dto.NewPassword))
            return BadRequest(new { message = "Le mot de passe ne peut pas être vide" });

        var employe = await _employeRepository.GetByIdAsync(id);
        if (employe == null)
            return NotFound(new { message = "Employé non trouvé" });

        // Les employés (non-admin) doivent avoir un mot de passe numérique uniquement
        if (employe.Role != Domain.Entities.EmployeRole.Admin && !dto.NewPassword.All(char.IsDigit))
            return BadRequest(new { message = "Le mot de passe doit contenir uniquement des chiffres" });

        // Vérifier que le mot de passe est unique parmi les employés avec le même username
        var sameUsernameEmployes = await _employeRepository.GetAllByUsernameAsync(employe.Username);
        if (sameUsernameEmployes.Any(e => e.Actif && e.Id != id && e.PasswordClair == dto.NewPassword))
            return BadRequest(new { message = "Ce code PIN est déjà utilisé par un autre employé. Veuillez en choisir un différent." });

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
public class EmployeListDto
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
    public string Icone { get; set; } = "User1";
    public bool IsActif { get; set; }
    public DateTime DateCreation { get; set; }
}

public class CreateEmployeDto
{
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? Username { get; set; } // Optionnel - sera auto-généré si vide
    public string Password { get; set; } = "";
    public Guid HammamId { get; set; }
    public string Role { get; set; } = "Employe";
    public string? Langue { get; set; }
    public string? Icone { get; set; } // User1 (Blue), User2 (Green) — auto-assigned
}

public class UpdateEmployeDto
{
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public Guid? HammamId { get; set; }
    public string? Role { get; set; }
    public string? Langue { get; set; }
    // Icone is auto-assigned at creation and cannot be changed via update
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = "";
}
