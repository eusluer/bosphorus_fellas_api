using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bosphorus_fellas_api.Models;

[Table("etkinlikler")]
public class Etkinlik
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("baslik")]
    public string Baslik { get; set; } = string.Empty;

    [Required]
    [Column("aciklama")]
    public string Aciklama { get; set; } = string.Empty;

    [Column("fotograf")]
    public string? Fotograf { get; set; }

    [Column("pdf_url")]
    public string? PdfUrl { get; set; }

    [Required]
    [Column("adres")]
    public string Adres { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("zaman")]
    public DateTime Zaman { get; set; }

    [Required]
    [Column("status")]
    public bool Status { get; set; } = true;
} 
