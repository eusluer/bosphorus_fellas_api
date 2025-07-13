using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class EtkinlikUpdateDto
{
    [Required(ErrorMessage = "Başlık alanı zorunludur.")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    public string Baslik { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string Aciklama { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Fotograf URL'si en fazla 500 karakter olabilir.")]
    public string? Fotograf { get; set; }

    [Required(ErrorMessage = "Adres alanı zorunludur.")]
    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    public string Adres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zaman alanı zorunludur.")]
    public DateTime Zaman { get; set; }

    [Required(ErrorMessage = "Status alanı zorunludur.")]
    public bool Status { get; set; } = true;
} 