using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

// Sponsor DTO'ları
public class SponsorDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik alanı zorunludur")]
    public string Icerik { get; set; } = string.Empty;

    public string? Fotograf { get; set; }
}

public class SponsorListeDto
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public string? Fotograf { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Haber DTO'ları
public class HaberDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur")]
    public string Aciklama { get; set; } = string.Empty;

    public string? Fotograf { get; set; }
}

public class HaberListeDto
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string? Fotograf { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HaberUpdateDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur")]
    public string Aciklama { get; set; } = string.Empty;

    public string? Fotograf { get; set; }
}

public class SponsorUpdateDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik alanı zorunludur")]
    public string Icerik { get; set; } = string.Empty;

    public string? Fotograf { get; set; }
}

// Etkinlik DTO'ları
public class EtkinlikDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur")]
    public string Aciklama { get; set; } = string.Empty;

    public string? Fotograf { get; set; }

    [Required(ErrorMessage = "Adres alanı zorunludur")]
    public string Adres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zaman alanı zorunludur")]
    public DateTime Zaman { get; set; }
}

public class EtkinlikListeDto
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public string? Fotograf { get; set; }
    public string Adres { get; set; } = string.Empty;
    public DateTime Zaman { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int KatilimciSayisi { get; set; }
    public bool KatilimDurumu { get; set; } = false; // Kullanıcının katılım durumu
}

public class EtkinlikKatilimDto
{
    [Required(ErrorMessage = "Etkinlik ID'si zorunludur")]
    public int EtkinlikId { get; set; }
}

public class EtkinlikKatilimciDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public DateTime KatilimTarihi { get; set; }
} 