using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bosphorus_fellas_api.Models;

[Table("adminler")]
public class Admin
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("ad")]
    public string Ad { get; set; } = string.Empty;

    [Required]
    [Column("soyad")]
    public string Soyad { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("sifre")]
    public string Sifre { get; set; } = string.Empty;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 