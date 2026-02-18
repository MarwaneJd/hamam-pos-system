using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
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
    public string? TypeTicketImagePath { get; set; }
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
                    (int)(150 * 3.937)); // Hauteur estimée ~150mm (espace pour logo)
                
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
        var fontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        var fontLarge = new Font("Segoe UI", 14, FontStyle.Bold);
        var fontSmall = new Font("Segoe UI", 8, FontStyle.Regular);
        var fontArabicTitle = new Font(arabicFamily, 20, FontStyle.Bold);
        var fontArabicType = new Font(arabicFamily, 14, FontStyle.Bold);
        var fontArabicSmall = new Font(arabicFamily, 10, FontStyle.Regular);
        var fontArabicLabel = new Font(arabicFamily, 10, FontStyle.Regular);

        float x = 5; // Marge gauche
        float y = 5; // Position verticale
        float lineHeight = 18;
        float width = PRINTABLE_WIDTH_MM * 3.937f; // Largeur en points

        var brush = Brushes.Black;
        var format = new StringFormat { Alignment = StringAlignment.Center };

        // Nom du hammam en arabe (avec fallback)
        var hammamArabe = !string.IsNullOrEmpty(_currentTicket.HammamNomArabe) 
            ? _currentTicket.HammamNomArabe 
            : _currentTicket.HammamNom;

        // ═══════════════════════════════════════
        // LOGO DU PRODUIT (TypeTicket image)
        // ═══════════════════════════════════════
        if (!string.IsNullOrEmpty(_currentTicket.TypeTicketImagePath) && File.Exists(_currentTicket.TypeTicketImagePath))
        {
            try
            {
                using var logo = Image.FromFile(_currentTicket.TypeTicketImagePath);
                // Taille cible : ~110px de hauteur, proportionnel
                float targetHeight = 110f;
                float scale = targetHeight / logo.Height;
                float scaledWidth = logo.Width * scale;
                float scaledHeight = targetHeight;
                float logoX = x + (width - scaledWidth) / 2; // Centrer horizontalement
                g.DrawImage(logo, logoX, y, scaledWidth, scaledHeight);
                y += scaledHeight + 8;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Impossible de charger le logo du ticket: {Path}", _currentTicket.TypeTicketImagePath);
            }
        }

        // ═══════════════════════════════════════
        // NOM DU HAMMAM EN ARABE
        // ═══════════════════════════════════════
        var arabicSize = g.MeasureString(hammamArabe, fontArabicTitle);
        g.DrawString(hammamArabe, fontArabicTitle, brush, 
            new RectangleF(x, y, width, arabicSize.Height + 4), format);
        y += arabicSize.Height + 6;

        // ═══════════════════════════════════════
        // TYPE DE TICKET (nom affiché directement)
        // ═══════════════════════════════════════
        var typeName = _currentTicket.TypeTicket;
        var typeSize = g.MeasureString(typeName, fontArabicType);
        g.DrawString(typeName, fontArabicType, brush,
            new RectangleF(x, y, width, typeSize.Height + 4), format);
        y += typeSize.Height + 8;

        // ═══════════════════════════════════════
        // NUMÉRO DE TICKET (label arabe)
        // ═══════════════════════════════════════
        var numberText = $"{_currentTicket.TicketNumber}  :  الرقم";
        g.DrawString(numberText, fontArabicLabel, brush,
            new RectangleF(x, y, width, lineHeight + 2), format);
        y += lineHeight + 6;

        // ═══════════════════════════════════════
        // PRIX (label arabe + valeur)
        // ═══════════════════════════════════════
        var priceFormatted = _currentTicket.Prix.ToString("F2").Replace('.', ',');
        var priceText = $"{priceFormatted}  :  الثمن";
        g.DrawString(priceText, fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 8), format);
        y += lineHeight + 12;

        // ═══════════════════════════════════════
        // DATE ET HEURE (sur une seule ligne)
        // ═══════════════════════════════════════
        var dateTimeLine = $"{_currentTicket.DateHeure:HH:mm}    {_currentTicket.DateHeure:dd/MM/yyyy}";
        g.DrawString(dateTimeLine, fontNormal, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight + 4;

        // ═══════════════════════════════════════
        // CAISSIER
        // ═══════════════════════════════════════
        g.DrawString($"Caissier : {_currentTicket.EmployeNom}", fontSmall, brush,
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight + 6;

        // ═══════════════════════════════════════
        // MESSAGE DE REMERCIEMENT EN ARABE
        // ═══════════════════════════════════════
        var merciArabe = "شكرا على زيارتكم";
        var merciSize = g.MeasureString(merciArabe, fontArabicSmall);
        g.DrawString(merciArabe, fontArabicSmall, brush,
            new RectangleF(x, y, width, merciSize.Height + 4), format);

        // Libérer les polices
        fontNormal.Dispose();
        fontLarge.Dispose();
        fontSmall.Dispose();
        fontArabicTitle.Dispose();
        fontArabicType.Dispose();
        fontArabicSmall.Dispose();
        fontArabicLabel.Dispose();

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
        
        // Polices - plus grandes que l'original mais adaptées au papier 48mm
        var arabicFamily = GetArabicFontFamily();
        var fontArabicTitle = new Font(arabicFamily, 20, FontStyle.Bold);
        var fontLarge = new Font("Segoe UI", 12, FontStyle.Bold);
        var fontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        var fontSmall = new Font("Segoe UI", 9, FontStyle.Regular);

        float x = 5;
        float y = 10;
        float lineHeight = 24;
        float width = PRINTABLE_WIDTH_MM * 3.937f;

        var brush = Brushes.Black;
        var format = new StringFormat { Alignment = StringAlignment.Center };

        // ═══════════════════════════════════════
        // NOM DU HAMMAM EN ARABE (gros, centré)
        // ═══════════════════════════════════════
        string arabicName = _currentCloture.HammamNomArabe;
        if (string.IsNullOrEmpty(arabicName))
        {
            arabicName = _currentCloture.HammamNom; // Fallback au nom français
        }
        
        var arabicSize = g.MeasureString(arabicName, fontArabicTitle);
        g.DrawString(arabicName, fontArabicTitle, brush, 
            new RectangleF(x, y, width, arabicSize.Height + 4), format);
        y += arabicSize.Height + 10;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 10;

        // ═══════════════════════════════════════
        // CAISSIER (label + nom sur 2 lignes)
        // ═══════════════════════════════════════
        g.DrawString("Caissier", fontNormal, brush,
            new RectangleF(x, y, width, 20), format);
        y += 20;
        g.DrawString(_currentCloture.CaissierNom.ToUpper(), fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 4), format);
        y += lineHeight + 6;

        // ═══════════════════════════════════════
        // HEURE (gros, centré)
        // ═══════════════════════════════════════
        g.DrawString($"{_currentCloture.DateHeure:HH:mm}", fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 4), format);
        y += lineHeight + 4;

        // ═══════════════════════════════════════
        // DATE (gros, centré)
        // ═══════════════════════════════════════
        g.DrawString($"{_currentCloture.DateHeure:dd/MM/yyyy}", fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 4), format);
        y += lineHeight + 12;

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
        fontArabicTitle.Dispose();
        fontLarge.Dispose();
        fontNormal.Dispose();
        fontSmall.Dispose();

        e.HasMorePages = false;
    }
}
