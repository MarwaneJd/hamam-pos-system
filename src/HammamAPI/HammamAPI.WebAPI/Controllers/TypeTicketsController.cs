using HammamAPI.Application.DTOs;
using HammamAPI.Domain.Entities;
using HammamAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HammamAPI.WebAPI.Controllers;

/// <summary>
/// Controller pour la gestion des types de tickets (produits/tarifs)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TypeTicketsController : ControllerBase
{
    private readonly HammamDbContext _context;
    private readonly ILogger<TypeTicketsController> _logger;
    private readonly IWebHostEnvironment _env;

    public TypeTicketsController(HammamDbContext context, ILogger<TypeTicketsController> logger, IWebHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
    }

    private string? GetFullImageUrl(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;
        if (imageUrl.StartsWith("http")) return imageUrl;
        return $"{Request.Scheme}://{Request.Host}{imageUrl}";
    }

    private TypeTicketDto ToDto(TypeTicket t) => new(
        t.Id, t.Nom, t.Prix, t.Couleur, t.Icone,
        GetFullImageUrl(t.ImageUrl), t.Ordre, t.Actif, t.HammamId
    );

    /// <summary>
    /// Récupère tous les types de tickets (globaux et par hammam)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TypeTicketDto>>> GetAll([FromQuery] Guid? hammamId = null)
    {
        var query = _context.TypeTickets.AsQueryable();
        
        if (hammamId.HasValue)
        {
            // Types spécifiques au hammam + types globaux
            query = query.Where(t => t.HammamId == hammamId.Value || t.HammamId == null);
        }
        
        var types = await query
            .Where(t => t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync();
            
        return Ok(types.Select(ToDto));
    }

    /// <summary>
    /// Récupère les types de tickets d'un hammam spécifique
    /// Priorité: types spécifiques du hammam, sinon types globaux (évite les doublons)
    /// </summary>
    [HttpGet("hammam/{hammamId:guid}")]
    public async Task<ActionResult<IEnumerable<TypeTicketDto>>> GetByHammam(Guid hammamId)
    {
        // Récupérer les types spécifiques du hammam
        var hammamTypes = await _context.TypeTickets
            .Where(t => t.HammamId == hammamId && t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync();
        
        // Si le hammam a ses propres types, les utiliser exclusivement
        if (hammamTypes.Any())
        {
            return Ok(hammamTypes.Select(ToDto));
        }
        
        // Sinon, utiliser les types globaux (HammamId = null)
        var globalTypes = await _context.TypeTickets
            .Where(t => t.HammamId == null && t.Actif)
            .OrderBy(t => t.Ordre)
            .ToListAsync();
            
        return Ok(globalTypes.Select(ToDto));
    }

    /// <summary>
    /// Récupère un type de ticket par son ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TypeTicketDto>> GetById(Guid id)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();
            
        return Ok(ToDto(type));
    }

    /// <summary>
    /// Crée un nouveau type de ticket
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TypeTicketDto>> Create([FromBody] CreateTypeTicketRequest request)
    {
        var type = new TypeTicket
        {
            Id = Guid.NewGuid(),
            Nom = request.Nom,
            Prix = request.Prix,
            Couleur = request.Couleur,
            Icone = request.Icone,
            ImageUrl = request.ImageUrl,
            Ordre = request.Ordre,
            HammamId = request.HammamId,
            Actif = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.TypeTickets.Add(type);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Type de ticket créé: {TypeName} - {Prix} DH", type.Nom, type.Prix);
        
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, ToDto(type));
    }

    /// <summary>
    /// Met à jour un type de ticket
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TypeTicketDto>> Update(Guid id, [FromBody] UpdateTypeTicketRequest request)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();
            
        if (request.Nom != null) type.Nom = request.Nom;
        if (request.Prix.HasValue) type.Prix = request.Prix.Value;
        if (request.Couleur != null) type.Couleur = request.Couleur;
        if (request.Icone != null) type.Icone = request.Icone;
        if (request.ImageUrl != null) type.ImageUrl = request.ImageUrl;
        if (request.Ordre.HasValue) type.Ordre = request.Ordre.Value;
        if (request.Actif.HasValue) type.Actif = request.Actif.Value;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Type de ticket modifié: {TypeName}", type.Nom);
        
        return Ok(ToDto(type));
    }

    /// <summary>
    /// Active/Désactive un type de ticket
    /// </summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TypeTicketDto>> ToggleStatus(Guid id)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();
            
        type.Actif = !type.Actif;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Type de ticket {Status}: {TypeName}", type.Actif ? "activé" : "désactivé", type.Nom);
        
        return Ok(ToDto(type));
    }

    /// <summary>
    /// Supprime un type de ticket (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();
            
        // Soft delete
        type.Actif = false;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Type de ticket supprimé: {TypeName}", type.Nom);
        
        return NoContent();
    }

    /// <summary>
    /// Upload une image pour un type de ticket
    /// </summary>
    [HttpPost("{id:guid}/upload-image")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TypeTicketDto>> UploadImage(Guid id, IFormFile file)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();

        if (file == null || file.Length == 0)
            return BadRequest("Aucun fichier fourni");

        // Valider le type de fichier
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Format non supporté. Utilisez: jpg, png, webp, svg");

        // Limiter la taille (2 MB)
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest("Image trop volumineuse (max 2 MB)");

        try
        {
            // Créer le dossier uploads/typetickets
            var uploadsDir = Path.Combine(_env.ContentRootPath, "uploads", "typetickets");
            Directory.CreateDirectory(uploadsDir);

            // Supprimer l'ancienne image si elle existe
            if (!string.IsNullOrEmpty(type.ImageUrl))
            {
                var oldPath = Path.Combine(_env.ContentRootPath, type.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Sauvegarder le fichier
            var fileName = $"{id}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Mettre à jour l'URL dans la base
            type.ImageUrl = $"/uploads/typetickets/{fileName}";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Image uploadée pour le type de ticket: {TypeName}", type.Nom);

            return Ok(ToDto(type));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'upload d'image");
            return StatusCode(500, "Erreur lors de l'upload");
        }
    }

    /// <summary>
    /// Supprime l'image d'un type de ticket
    /// </summary>
    [HttpDelete("{id:guid}/image")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TypeTicketDto>> DeleteImage(Guid id)
    {
        var type = await _context.TypeTickets.FindAsync(id);
        
        if (type == null)
            return NotFound();

        if (!string.IsNullOrEmpty(type.ImageUrl))
        {
            var filePath = Path.Combine(_env.ContentRootPath, type.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        type.ImageUrl = null;
        await _context.SaveChangesAsync();

        return Ok(ToDto(type));
    }
}
