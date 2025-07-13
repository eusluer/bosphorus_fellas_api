using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class SifreDegistirmeDto
{
    [Required(ErrorMessage = "Mevcut şifre alanı zorunludur")]
    public string MevcutSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre alanı zorunludur")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    public string YeniSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı alanı zorunludur")]
    [Compare("YeniSifre", ErrorMessage = "Şifreler eşleşmiyor")]
    public string YeniSifreTekrar { get; set; } = string.Empty;
} 