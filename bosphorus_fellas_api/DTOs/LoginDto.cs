using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email alanı zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur")]
    public string Sifre { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty; // "admin" veya "uye"
    public int UserId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
} 