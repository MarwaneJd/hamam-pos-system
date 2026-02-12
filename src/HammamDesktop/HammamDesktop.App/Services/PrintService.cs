using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using Serilog;

namespace HammamDesktop.Services;

/// <summary>
/// Service d'impression de tickets pour imprimante thermique 58mm
/// </summary>
public interface IPrintService
{
    /// <summary>
    /// Imprimer un ticket de vente
    /// </summary>
    Task PrintTicketAsync(TicketPrintData ticketData);
    
    /// <summary>
    /// Imprimer un ticket de clôture (fin de journée)
    /// </summary>
    Task PrintClotureTicketAsync(ClotureTicketData clotureData);
    
    /// <summary>
    /// Vérifier si une imprimante est disponible
    /// </summary>
    bool IsPrinterAvailable();
    
    /// <summary>
    /// Obtenir le nom de l'imprimante par défaut
    /// </summary>
    string? GetDefaultPrinter();
}

/// <summary>
/// Données pour l'impression du ticket
/// </summary>
public class TicketPrintData
{
    public string HammamNom { get; set; } = "";
    public string HammamNomArabe { get; set; } = "";
    public int TicketNumber { get; set; }
    public string TypeTicket { get; set; } = "";
    public decimal Prix { get; set; }
    public DateTime DateHeure { get; set; }
    public string EmployeNom { get; set; } = "";
    public string? Couleur { get; set; }
}

/// <summary>
/// Données pour le ticket de clôture
/// </summary>
public class ClotureTicketData
{
    public string HammamNomArabe { get; set; } = "";
    public string HammamNom { get; set; } = "";
    public string CaissierNom { get; set; } = "";
    public DateTime DateHeure { get; set; }
}

/// <summary>
/// Implémentation du service d'impression thermique 58mm
/// </summary>
public class PrintService : IPrintService
{
    // Largeur de papier 58mm = environ 48mm zone imprimable = ~32 caractères
    private const int TICKET_WIDTH_CHARS = 32;
    private const float PAPER_WIDTH_MM = 58f;
    private const float PRINTABLE_WIDTH_MM = 48f;
    
    private TicketPrintData? _currentTicket;
    private ClotureTicketData? _currentCloture;

    public bool IsPrinterAvailable()
    {
        try
        {
            return PrinterSettings.InstalledPrinters.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public string? GetDefaultPrinter()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }

    public Task PrintTicketAsync(TicketPrintData ticketData)
    {
        return Task.Run(() =>
        {
            try
            {
                _currentTicket = ticketData;

                var printDoc = new PrintDocument();
                printDoc.PrintPage += PrintDocument_PrintPage;
                
                // Configuration pour imprimante thermique 58mm
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("Thermal58", 
                    (int)(PAPER_WIDTH_MM * 3.937), // Conversion mm en 1/100 pouces
                    (int)(120 * 3.937)); // Hauteur estimée ~120mm
                
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.Print();

                Log.Information("Ticket imprimé: #{TicketNumber} - {Type} - {Prix} DH",
                    ticketData.TicketNumber, ticketData.TypeTicket, ticketData.Prix);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de l'impression du ticket");
            }
        });
    }

    /// <summary>
    /// Trouve la meilleure police disponible pour l'arabe
    /// </summary>
    private static string GetArabicFontFamily()
    {
        // Polices classées par qualité de rendu arabe
        string[] candidates = { "Arabic Typesetting", "Sakkal Majalla", "Traditional Arabic", "Simplified Arabic", "Segoe UI" };
        var installed = new System.Drawing.Text.InstalledFontCollection();
        var familyNames = new HashSet<string>(installed.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var name in candidates)
        {
            if (familyNames.Contains(name)) return name;
        }
        return "Segoe UI";
    }

    private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
    {
        if (_currentTicket == null || e.Graphics == null) return;

        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        
        var arabicFamily = GetArabicFontFamily();

        // Polices pour impression thermique
        var fontTitle = new Font("Segoe UI", 12, FontStyle.Bold);
        var fontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        var fontLarge = new Font("Segoe UI", 14, FontStyle.Bold);
        var fontSmall = new Font("Segoe UI", 8, FontStyle.Regular);
        var fontArabic = new Font(arabicFamily, 16, FontStyle.Bold);

        float x = 5; // Marge gauche
        float y = 5; // Position verticale
        float lineHeight = 18;
        float width = PRINTABLE_WIDTH_MM * 3.937f; // Largeur en points

        var brush = Brushes.Black;
        var format = new StringFormat { Alignment = StringAlignment.Center };
        var formatLeft = new StringFormat { Alignment = StringAlignment.Near };
        var formatRight = new StringFormat { Alignment = StringAlignment.Far };
        var formatRTL = new StringFormat { Alignment = StringAlignment.Center };

        // Traduction du type de ticket en arabe
        var typeArabe = _currentTicket.TypeTicket.ToUpper() switch
        {
            "HOMME" => "رجل",
            "FEMME" => "إمرأة",
            "ENFANT" => "طفل",
            "DOUCHE" => "دوش",
            _ => _currentTicket.TypeTicket
        };

        // Nom du hammam en arabe (avec fallback hardcodé)
        var hammamArabe = !string.IsNullOrEmpty(_currentTicket.HammamNomArabe) 
            ? _currentTicket.HammamNomArabe 
            : _currentTicket.HammamNom.ToLower() switch
            {
                "hammame liberte" => "حمام الحرية",
                "hammam centre" => "حمام الوسط",
                "hammam casablanca" => "حمام الدار البيضاء",
                _ => _currentTicket.HammamNom
            };

        // ═══════════════════════════════════════
        // EN-TÊTE - Nom du Hammam en ARABE
        // ═══════════════════════════════════════
        var arabicSize = g.MeasureString(hammamArabe, fontArabic);
        g.DrawString(hammamArabe, fontArabic, brush, 
            new RectangleF(x, y, width, arabicSize.Height + 4), format);
        y += arabicSize.Height + 4;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // NUMÉRO DE TICKET
        // ═══════════════════════════════════════
        g.DrawString($"TICKET N° {_currentTicket.TicketNumber}", fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 5), format);
        y += lineHeight + 10;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // TYPE DE TICKET en ARABE (رجل, إمرأة, etc.)
        // ═══════════════════════════════════════
        var typeSize = g.MeasureString(typeArabe, fontArabic);
        g.DrawString(typeArabe, fontArabic, brush,
            new RectangleF(x, y, width, typeSize.Height + 4), format);
        y += lineHeight + 10;

        // ═══════════════════════════════════════
        // PRIX
        // ═══════════════════════════════════════
        g.DrawString($"{_currentTicket.Prix:F2} DH", fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 5), format);
        y += lineHeight + 10;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // DATE ET HEURE
        // ═══════════════════════════════════════
        g.DrawString($"Date: {_currentTicket.DateHeure:dd/MM/yyyy}", fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight;

        g.DrawString($"Heure: {_currentTicket.DateHeure:HH:mm:ss}", fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight + 5;

        // ═══════════════════════════════════════
        // EMPLOYÉ
        // ═══════════════════════════════════════
        g.DrawString($"Caissier: {_currentTicket.EmployeNom}", fontSmall, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight + 5;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // MESSAGE DE REMERCIEMENT
        // ═══════════════════════════════════════
        g.DrawString("Merci de votre visite!", fontSmall, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight;

        g.DrawString("━━━━━━━━━━━━━━━━", fontSmall, brush,
            new RectangleF(x, y, width, lineHeight), format);

        // Libérer les polices
        fontTitle.Dispose();
        fontNormal.Dispose();
        fontLarge.Dispose();
        fontSmall.Dispose();
        fontArabic.Dispose();

        e.HasMorePages = false;
    }

    public Task PrintClotureTicketAsync(ClotureTicketData clotureData)
    {
        return Task.Run(() =>
        {
            try
            {
                _currentCloture = clotureData;

                var printDoc = new PrintDocument();
                printDoc.PrintPage += PrintCloture_PrintPage;
                
                // Configuration pour imprimante thermique 58mm
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("Thermal58", 
                    (int)(PAPER_WIDTH_MM * 3.937),
                    (int)(100 * 3.937)); // Hauteur plus courte pour ticket clôture
                
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.Print();

                Log.Information("Ticket de clôture imprimé - {Caissier} - {Date}",
                    clotureData.CaissierNom, clotureData.DateHeure);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de l'impression du ticket de clôture");
            }
        });
    }

    private void PrintCloture_PrintPage(object sender, PrintPageEventArgs e)
    {
        if (_currentCloture == null || e.Graphics == null) return;

        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        
        // Log pour debug
        Log.Information("Impression clôture - NomArabe: {NomArabe}", _currentCloture.HammamNomArabe);
        
        // Police qui supporte l'arabe
        var arabicFamily = GetArabicFontFamily();
        var fontArabic = new Font(arabicFamily, 16, FontStyle.Bold);
        var fontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        var fontSmall = new Font("Segoe UI", 9, FontStyle.Regular);

        float x = 5;
        float y = 10;
        float lineHeight = 22;
        float width = PRINTABLE_WIDTH_MM * 3.937f;

        var brush = Brushes.Black;
        var format = new StringFormat { Alignment = StringAlignment.Center };
        var formatLeft = new StringFormat { Alignment = StringAlignment.Near };

        // ═══════════════════════════════════════
        // NOM DU HAMMAM EN ARABE
        // ═══════════════════════════════════════
        string arabicName = _currentCloture.HammamNomArabe;
        if (string.IsNullOrEmpty(arabicName))
        {
            arabicName = _currentCloture.HammamNom; // Fallback au nom français
        }
        
        g.DrawString(arabicName, fontArabic, brush, 
            new RectangleF(x, y, width, lineHeight + 10), format);
        y += lineHeight + 15;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // CAISSIER
        // ═══════════════════════════════════════
        g.DrawString($"Caissier : {_currentCloture.CaissierNom.ToUpper()}", fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), formatLeft);
        y += lineHeight;

        // ═══════════════════════════════════════
        // HEURE
        // ═══════════════════════════════════════
        g.DrawString($"{_currentCloture.DateHeure:HH:mm}", fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), formatLeft);
        y += lineHeight;

        // ═══════════════════════════════════════
        // DATE
        // ═══════════════════════════════════════
        g.DrawString($"{_currentCloture.DateHeure:dd/MM/yyyy}", fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), formatLeft);
        y += lineHeight + 10;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 15;

        // Espace pour écriture manuelle (lignes en pointillés)
        for (int i = 0; i < 4; i++)
        {
            g.DrawString("_ _ _ _ _ _ _ _ _ _ _ _ _ _ _", fontSmall, brush,
                new RectangleF(x, y, width, lineHeight), format);
            y += lineHeight + 5;
        }

        // Libérer les polices
        fontArabic.Dispose();
        fontNormal.Dispose();
        fontSmall.Dispose();

        e.HasMorePages = false;
    }
}
