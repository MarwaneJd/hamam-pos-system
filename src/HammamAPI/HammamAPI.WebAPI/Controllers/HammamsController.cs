using HammamAPI.Domain.Entities;
using HammamAPI.Domain.Interfaces;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HammamsController : ControllerBase
{
    private readonly HammamDbContext _context;
    private readonly IHammamRepository _hammamRepository;
    private readonly IEmployeRepository _employeRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<HammamsController> _logger;

    public HammamsController(
        HammamDbContext context,
        IHammamRepository hammamRepository,
        IEmployeRepository employeRepository,
        ITicketRepository ticketRepository,
        ILogger<HammamsController> logger)
    {
        _context = context;
        _hammamRepository = hammamRepository;
        _employeRepository = employeRepository;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les hammams avec leurs statistiques
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HammamDto>>> GetAll()
    {
        var hammams = await _context.Hammams
            .Include(h => h.Employes)
            .Include(h => h.Tickets)
            .Include(h => h.TypeTickets)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        var result = hammams.Select(h => new HammamDto
        {
            Id = h.Id,
            Code = h.Code,
            Nom = h.Nom,
            NomArabe = h.NomArabe,
            PrefixeTicket = h.PrefixeTicket,
            Adresse = h.Adresse,
            IsActif = h.Actif,
            NombreEmployes = h.Employes.Count(e => e.Actif && e.Role != EmployeRole.Admin),
            TicketsAujourdhui = h.Tickets.Count(t => t.CreatedAt.Date == today),
            RecetteAujourdhui = h.Tickets.Where(t => t.CreatedAt.Date == today).Sum(t => t.Prix),
            TypeTickets = h.TypeTickets.Where(t => t.Actif).OrderBy(t => t.Ordre).Select(t => new HammamTypeTicketDto
            {
                Id = t.Id,
                Nom = t.Nom,
                Prix = t.Prix,
                Couleur = t.Couleur,
                Icone = t.Icone,
                Ordre = t.Ordre
            }).ToList(),
            DateCreation = h.CreatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Récupère un hammam par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HammamDto>> GetById(Guid id)
    {
        var hammam = await _context.Hammams
            .Include(h => h.Employes)
            .Include(h => h.Tickets)
            .Include(h => h.TypeTickets)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hammam == null)
            return NotFound(new { message = "Hammam non trouvé" });

        var today = DateTime.UtcNow.Date;

        return Ok(new HammamDto
        {
            Id = hammam.Id,
            Code = hammam.Code,
            Nom = hammam.Nom,
            NomArabe = hammam.NomArabe,
            PrefixeTicket = hammam.PrefixeTicket,
            Adresse = hammam.Adresse,
            IsActif = hammam.Actif,
            NombreEmployes = hammam.Employes.Count(e => e.Actif && e.Role != EmployeRole.Admin),
            TicketsAujourdhui = hammam.Tickets.Count(t => t.CreatedAt.Date == today),
            RecetteAujourdhui = hammam.Tickets.Where(t => t.CreatedAt.Date == today).Sum(t => t.Prix),
            TypeTickets = hammam.TypeTickets.Where(t => t.Actif).OrderBy(t => t.Ordre).Select(t => new HammamTypeTicketDto
            {
                Id = t.Id,
                Nom = t.Nom,
                Prix = t.Prix,
                Couleur = t.Couleur,
                Icone = t.Icone,
                Ordre = t.Ordre
            }).ToList(),
            DateCreation = hammam.CreatedAt
        });
    }

    /// <summary>
    /// Crée un nouveau hammam (avec employés et types de tickets optionnels)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<HammamDto>> Create([FromBody] CreateHammamDto dto)
    {
        try
        {
            // Créer le hammam
            var hammam = new Hammam
            {
                Id = Guid.NewGuid(),
                Code = dto.Code ?? $"HAM{DateTime.Now:yyyyMMddHHmmss}",
                Nom = dto.Nom,
                NomArabe = dto.NomArabe ?? "",
                Adresse = dto.Adresse ?? "",
                Actif = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Hammams.Add(hammam);

            // Créer les types de tickets si fournis
            if (dto.TypeTickets != null && dto.TypeTickets.Any())
            {
                foreach (var typeDto in dto.TypeTickets)
                {
                    var type = new TypeTicket
                    {
                        Id = Guid.NewGuid(),
                        Nom = typeDto.Nom,
                        Prix = typeDto.Prix,
                        Couleur = typeDto.Couleur ?? "#3B82F6",
                        Icone = typeDto.Icone ?? "User",
                        Ordre = typeDto.Ordre,
                        HammamId = hammam.Id,
                        Actif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TypeTickets.Add(type);
                }
            }

            // Créer les employés si fournis (max 2 par hammam: Utilisateur1, Utilisateur2)
            if (dto.Employes != null && dto.Employes.Any())
            {
                var employesToCreate = dto.Employes.Take(2).ToList(); // Max 2 employés
                var counter = 1;
                var icones = new[] { "User1", "User2" }; // Blue, Green

                foreach (var empDto in employesToCreate)
                {
                    // Valider mot de passe numérique
                    if (string.IsNullOrEmpty(empDto.Password) || !empDto.Password.All(char.IsDigit))
                    {
                        return BadRequest(new { message = $"Le mot de passe de l'employé {counter} doit contenir uniquement des chiffres" });
                    }

                    var newUsername = $"Utilisateur{counter}";

                    // Vérifier que le mot de passe est unique parmi les employés avec le même username
                    var sameUsernameEmployes = await _employeRepository.GetAllByUsernameAsync(newUsername);
                    if (sameUsernameEmployes.Any(e => e.Actif && e.PasswordClair == empDto.Password))
                    {
                        return BadRequest(new { message = $"Le code PIN de l'employé {counter} est déjà utilisé par un autre employé. Veuillez en choisir un différent." });
                    }
                    
                    var employe = new Employe
                    {
                        Id = Guid.NewGuid(),
                        Username = newUsername,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(empDto.Password),
                        PasswordClair = empDto.Password, // Sauvegarder le mot de passe en clair pour affichage admin
                        Nom = empDto.Nom,
                        Prenom = empDto.Prenom,
                        HammamId = hammam.Id,
                        Langue = empDto.Langue ?? "FR",
                        Role = Enum.Parse<EmployeRole>(empDto.Role ?? "Employe"),
                        Icone = icones[counter - 1], // Auto-assign: User1 or User2
                        Actif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Employes.Add(employe);
                    counter++;
                }
            }

            // Sauvegarder le tout en une fois
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nouveau hammam créé: {Nom} avec {TypeCount} types et {EmpCount} employés",
                hammam.Nom, dto.TypeTickets?.Count ?? 0, dto.Employes?.Count ?? 0);

            return CreatedAtAction(nameof(GetById), new { id = hammam.Id }, new HammamDto
            {
                Id = hammam.Id,
                Code = hammam.Code,
                Nom = hammam.Nom,
                NomArabe = hammam.NomArabe,
                PrefixeTicket = hammam.PrefixeTicket,
                Adresse = hammam.Adresse,
                IsActif = hammam.Actif,
                DateCreation = hammam.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du hammam");
            return StatusCode(500, new { message = "Erreur lors de la création du hammam", detail = ex.Message });
        }
    }

    /// <summary>
    /// Met à jour un hammam
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<HammamDto>> Update(Guid id, [FromBody] UpdateHammamDto dto)
    {
        var hammam = await _hammamRepository.GetByIdAsync(id);
        if (hammam == null)
            return NotFound(new { message = "Hammam non trouvé" });

        hammam.Nom = dto.Nom ?? hammam.Nom;
        hammam.NomArabe = dto.NomArabe ?? hammam.NomArabe;
        hammam.PrefixeTicket = dto.PrefixeTicket ?? hammam.PrefixeTicket;
        hammam.Adresse = dto.Adresse ?? hammam.Adresse;
        hammam.UpdatedAt = DateTime.UtcNow;

        await _hammamRepository.UpdateAsync(hammam);
        _logger.LogInformation("Hammam mis à jour: {Id}", id);

        return Ok(new HammamDto
        {
            Id = hammam.Id,
            Code = hammam.Code,
            Nom = hammam.Nom,
            NomArabe = hammam.NomArabe,
            PrefixeTicket = hammam.PrefixeTicket,
            Adresse = hammam.Adresse,
            IsActif = hammam.Actif,
            DateCreation = hammam.CreatedAt
        });
    }

    /// <summary>
    /// Active/Désactive un hammam
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ToggleStatus(Guid id)
    {
        var hammam = await _hammamRepository.GetByIdAsync(id);
        if (hammam == null)
            return NotFound(new { message = "Hammam non trouvé" });

        hammam.Actif = !hammam.Actif;
        hammam.UpdatedAt = DateTime.UtcNow;
        await _hammamRepository.UpdateAsync(hammam);

        _logger.LogInformation("Statut hammam {Id} changé en: {Status}", id, hammam.Actif ? "Actif" : "Inactif");

        return Ok(new { isActif = hammam.Actif });
    }

    /// <summary>
    /// Supprime un hammam
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var hammam = await _hammamRepository.GetByIdAsync(id);
        if (hammam == null)
            return NotFound(new { message = "Hammam non trouvé" });

        // Vérifier s'il y a des employés associés
        var employes = await _employeRepository.GetAllAsync();
        if (employes.Any(e => e.HammamId == id))
            return BadRequest(new { message = "Impossible de supprimer un hammam avec des employés associés" });

        await _hammamRepository.DeleteAsync(id);
        _logger.LogInformation("Hammam supprimé: {Id}", id);

        return NoContent();
    }
}

// DTOs
public class HammamDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Nom { get; set; } = "";
    public string NomArabe { get; set; } = "";
    public int PrefixeTicket { get; set; }
    public string Adresse { get; set; } = "";
    public bool IsActif { get; set; }
    public int NombreEmployes { get; set; }
    public int TicketsAujourdhui { get; set; }
    public decimal RecetteAujourdhui { get; set; }
    public List<HammamTypeTicketDto> TypeTickets { get; set; } = new();
    public DateTime DateCreation { get; set; }
}

public class HammamTypeTicketDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = "";
    public decimal Prix { get; set; }
    public string Couleur { get; set; } = "#3B82F6";
    public string Icone { get; set; } = "User";
    public int Ordre { get; set; }
}

public class CreateHammamDto
{
    public string Nom { get; set; } = "";
    public string? NomArabe { get; set; }
    public string? Code { get; set; }
    public string? Adresse { get; set; }
    public List<CreateTypeTicketDto>? TypeTickets { get; set; }
    public List<CreateHammamEmployeDto>? Employes { get; set; }
}

public class CreateTypeTicketDto
{
    public string Nom { get; set; } = "";
    public decimal Prix { get; set; }
    public string? Couleur { get; set; }
    public string? Icone { get; set; }
    public int Ordre { get; set; }
}

public class CreateHammamEmployeDto
{
    public string? Username { get; set; } // Optionnel - sera auto-généré
    public string Password { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";
    public string? Langue { get; set; }
    public string? Role { get; set; }
    public string? Icone { get; set; } // User1 (Blue), User2 (Green) — auto-assigned
}

public class UpdateHammamDto
{
    public string? Nom { get; set; }
    public string? NomArabe { get; set; }
    public int? PrefixeTicket { get; set; }
    public string? Adresse { get; set; }
}
