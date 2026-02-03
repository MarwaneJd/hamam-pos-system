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
    public int TicketNumber { get; set; }
    public string TypeTicket { get; set; } = "";
    public decimal Prix { get; set; }
    public DateTime DateHeure { get; set; }
    public string EmployeNom { get; set; } = "";
    public string? Couleur { get; set; }
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

    private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
    {
        if (_currentTicket == null || e.Graphics == null) return;

        var g = e.Graphics;
        
        // Polices pour impression thermique
        var fontTitle = new Font("Arial", 12, FontStyle.Bold);
        var fontNormal = new Font("Arial", 10, FontStyle.Regular);
        var fontLarge = new Font("Arial", 14, FontStyle.Bold);
        var fontSmall = new Font("Arial", 8, FontStyle.Regular);

        float x = 5; // Marge gauche
        float y = 5; // Position verticale
        float lineHeight = 18;
        float width = PRINTABLE_WIDTH_MM * 3.937f; // Largeur en points

        var brush = Brushes.Black;
        var format = new StringFormat { Alignment = StringAlignment.Center };
        var formatLeft = new StringFormat { Alignment = StringAlignment.Near };
        var formatRight = new StringFormat { Alignment = StringAlignment.Far };

        // ═══════════════════════════════════════
        // EN-TÊTE - Nom du Hammam
        // ═══════════════════════════════════════
        g.DrawString(_currentTicket.HammamNom.ToUpper(), fontTitle, brush, 
            new RectangleF(x, y, width, lineHeight), format);
        y += lineHeight + 5;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // NUMÉRO DE TICKET
        // ═══════════════════════════════════════
        g.DrawString($"TICKET N° {_currentTicket.TicketNumber:D4}", fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 5), format);
        y += lineHeight + 10;

        // Ligne de séparation
        g.DrawLine(Pens.Black, x, y, x + width, y);
        y += 8;

        // ═══════════════════════════════════════
        // TYPE DE TICKET (HOMME, FEMME, etc.)
        // ═══════════════════════════════════════
        g.DrawString(_currentTicket.TypeTicket.ToUpper(), fontLarge, brush,
            new RectangleF(x, y, width, lineHeight + 5), format);
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

        e.HasMorePages = false;
    }
}
