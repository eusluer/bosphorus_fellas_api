using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class UyelikBasvurusuDto
{
    [Required(ErrorMessage = "Ad alanı zorunludur")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur")]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur")]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğum tarihi alanı zorunludur")]
    public DateOnly DogumTarihi { get; set; }

    [Required(ErrorMessage = "Şehir alanı zorunludur")]
    public string Sehir { get; set; } = string.Empty;

    public string? Instagram { get; set; }

    [Required(ErrorMessage = "Adres alanı zorunludur")]
    public string Adres { get; set; } = string.Empty;

    public string? AracMarka { get; set; }

    public string? AracModel { get; set; }

    public string? AracYili { get; set; }

    public string? Plaka { get; set; }

    [Required(ErrorMessage = "Deneyim alanı zorunludur")]
    [Range(0, 50, ErrorMessage = "Deneyim 0-50 yıl arasında olmalıdır")]
    public int Deneyim { get; set; }

    public string? IlgiAlanlari { get; set; }

    public string? Neden { get; set; }

    public string? AcilDurumKisi { get; set; }

    public string? AcilDurumTelefon { get; set; }

    [Required(ErrorMessage = "Şartları kabul etmelisiniz")]
    public bool Sartlar { get; set; }

    [Required(ErrorMessage = "Kişisel veri işleme onayı gereklidir")]
    public bool KisiselVeri { get; set; }

    [Required(ErrorMessage = "Email bildirim tercihi belirtmelisiniz")]
    public bool EmailBildirim { get; set; }

    public string? Fotograf { get; set; }
} 