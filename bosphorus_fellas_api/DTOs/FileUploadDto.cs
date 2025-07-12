using System.ComponentModel.DataAnnotations;

namespace bosphorus_fellas_api.DTOs;

public class FileUploadDto
{
    [Required(ErrorMessage = "Dosya gereklidir")]
    public IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = "Klasör adı gereklidir")]
    public string Folder { get; set; } = string.Empty; // "profil_fotolari", "etkinlikler", "haberler", "sponsorlu"
}

public class FileUploadResponseDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

public class FileDeleteDto
{
    [Required(ErrorMessage = "Dosya yolu gereklidir")]
    public string FilePath { get; set; } = string.Empty;
} 