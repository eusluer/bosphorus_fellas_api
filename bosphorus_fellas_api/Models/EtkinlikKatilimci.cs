using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bosphorus_fellas_api.Models;

[Table("etkinlik_katilimcilari")]
public class EtkinlikKatilimci
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("etkinlik_id")]
    public int EtkinlikId { get; set; }

    [Required]
    [Column("katilimci_id")]
    public int KatilimciId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("EtkinlikId")]
    public virtual Etkinlik Etkinlik { get; set; } = null!;

    [ForeignKey("KatilimciId")]
    public virtual Uye Katilimci { get; set; } = null!;
} 