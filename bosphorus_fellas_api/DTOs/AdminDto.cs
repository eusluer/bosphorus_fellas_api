using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class BasvuruOnayDto
{
    [Required(ErrorMessage = "Başvuru ID'si zorunludur")]
    public int BasvuruId { get; set; }

    [Required(ErrorMessage = "Karar alanı zorunludur")]
    public string Karar { get; set; } = string.Empty; // "onayla" veya "reddet"

    public string? ReddetmeNedeni { get; set; }
}

public class UyeStatusDto
{
    [Required(ErrorMessage = "Üye ID'si zorunludur")]
    public int UyeId { get; set; }

    [Required(ErrorMessage = "Status alanı zorunludur")]
    public bool Status { get; set; }
}

public class BasvuruListeDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public string? Fotograf { get; set; }
    public DateTime BasvuruTarihi { get; set; }
}

public class UyeListeDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public bool Status { get; set; }
    public string? Fotograf { get; set; }
    public DateTime UyelikTarihi { get; set; }
} 