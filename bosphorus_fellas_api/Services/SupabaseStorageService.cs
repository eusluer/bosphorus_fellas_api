using Supabase;
using Supabase.Storage;

namespace bosphorus_fellas_api.Services;

public class SupabaseStorageService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly string _bucketName = "urller";

    public SupabaseStorageService(IConfiguration configuration)
    {
        var url = configuration["Supabase:Url"];
        var key = configuration["Supabase:ServiceRoleKey"];
        
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _supabaseClient = new Supabase.Client(url, key, options);
    }

    public async Task<string> UploadFileAsync(byte[] fileData, string fileName, string folder)
    {
        try
        {
            var filePath = $"{folder}/{fileName}";
            
            await _supabaseClient.Storage
                .From(_bucketName)
                .Upload(fileData, filePath, new Supabase.Storage.FileOptions
                {
                    CacheControl = "3600",
                    Upsert = true
                });

            // Public URL'i al
            var publicUrl = _supabaseClient.Storage
                .From(_bucketName)
                .GetPublicUrl(filePath);

            return publicUrl;
        }
        catch (Exception ex)
        {
            throw new Exception($"Dosya yükleme hatası: {ex.Message}");
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            await _supabaseClient.Storage
                .From(_bucketName)
                .Remove(new List<string> { filePath });
            
            return true;
        }
        catch (Exception ex)
        {
            // Log the error but don't throw
            return false;
        }
    }

    public string GetPublicUrl(string filePath)
    {
        return _supabaseClient.Storage
            .From(_bucketName)
            .GetPublicUrl(filePath);
    }
} 