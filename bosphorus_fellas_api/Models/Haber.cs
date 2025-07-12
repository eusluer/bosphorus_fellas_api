using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bosphorus_fellas_api.Models;

[Table("haberler")]
public class Haber
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

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 