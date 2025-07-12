using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bosphorus_fellas_api.Models;

[Table("uyeler")]
public class Uye
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
    [Column("telefon")]
    public string Telefon { get; set; } = string.Empty;

    [Required]
    [Column("dogum_tarihi")]
    public DateOnly DogumTarihi { get; set; }

    [Required]
    [Column("sehir")]
    public string Sehir { get; set; } = string.Empty;

    [Column("instagram")]
    public string? Instagram { get; set; }

    [Required]
    [Column("adres")]
    public string Adres { get; set; } = string.Empty;

    [Column("arac_marka")]
    public string? AracMarka { get; set; }

    [Column("arac_model")]
    public string? AracModel { get; set; }

    [Column("arac_yili")]
    public string? AracYili { get; set; }

    [Column("plaka")]
    public string? Plaka { get; set; }

    [Required]
    [Column("deneyim")]
    public int Deneyim { get; set; }

    [Column("ilgi_alanlari")]
    public string? IlgiAlanlari { get; set; }

    [Column("acil_durum_kisi")]
    public string? AcilDurumKisi { get; set; }

    [Column("acil_durum_telefon")]
    public string? AcilDurumTelefon { get; set; }

    [Required]
    [Column("uyelik_sartlari")]
    public bool UyelikSartlari { get; set; }

    [Required]
    [Column("kisisel_veri")]
    public bool KisiselVeri { get; set; }

    [Required]
    [Column("email_bildirim")]
    public bool EmailBildirim { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("sifre")]
    public string Sifre { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    public bool Status { get; set; } = true;

    [Column("fotograf")]
    public string? Fotograf { get; set; }
} 