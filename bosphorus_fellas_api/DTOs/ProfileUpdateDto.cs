using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class ProfileUpdateDto
{
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string? Telefon { get; set; }

    public string? Sehir { get; set; }

    public string? Instagram { get; set; }

    public string? Adres { get; set; }

    public string? AracMarka { get; set; }

    public string? AracModel { get; set; }

    public string? AracYili { get; set; }

    public string? Plaka { get; set; }

    [Range(0, 50, ErrorMessage = "Deneyim 0-50 yıl arasında olmalıdır")]
    public int? Deneyim { get; set; }

    public string? IlgiAlanlari { get; set; }

    public string? AcilDurumKisi { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string? AcilDurumTelefon { get; set; }

    public bool? EmailBildirim { get; set; }

    public string? Fotograf { get; set; }
} 