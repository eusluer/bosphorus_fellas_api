using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using bosphorus_fellas_api.Data;
using bosphorus_fellas_api.Models;
using bosphorus_fellas_api.DTOs;
using bosphorus_fellas_api.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway için port yapılandırması
var port = Environment.GetEnvironmentVariable("PORT") ?? "5050";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Bosphorus Fellas API", 
        Version = "v1" 
    });
    
    // JWT Authentication için Swagger konfigürasyonu
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Sadece Supabase PostgreSQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// JWT Service'i ekle
builder.Services.AddScoped<JwtService>();

// Supabase Storage Service'i ekle
builder.Services.AddScoped<SupabaseStorageService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"];
var key = Encoding.UTF8.GetBytes(jwtKey!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = "BosphorusFellasAPI",
            ValidateAudience = true,
            ValidAudience = "BosphorusFellasAPI",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS politikasını ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger'ı her zaman aktif et
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bosphorus Fellas API v1");
    c.RoutePrefix = string.Empty; // Root adresinde Swagger açılacak
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for Railway
app.MapGet("/health", () => 
{
    try
    {
        return Results.Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            port = Environment.GetEnvironmentVariable("PORT") ?? "5000"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Health check failed: {ex.Message}");
    }
});

// Login endpoint'i
app.MapPost("/api/login", async (LoginDto loginDto, ApplicationDbContext context, JwtService jwtService, ILogger<Program> logger) =>
{
    // Validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(loginDto);
    
    if (!Validator.TryValidateObject(loginDto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    try
    {
        logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);
        
        // Önce admin tablosunda ara
        var admin = await context.Adminler
            .FirstOrDefaultAsync(a => a.Email == loginDto.Email);
        
        logger.LogInformation("Admin found: {AdminFound}", admin != null);
        
        if (admin != null)
        {
            bool isPasswordValid = false;
            bool needsPasswordUpdate = false;
            
            // Önce BCrypt hash olup olmadığını kontrol et
            if (admin.Sifre.StartsWith("$2"))
            {
                // BCrypt hash formatında, normal doğrulama yap
                isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Sifre, admin.Sifre);
            }
            else
            {
                // Düz metin şifre, direkt karşılaştır ve hash'le
                isPasswordValid = admin.Sifre == loginDto.Sifre;
                if (isPasswordValid)
                {
                    needsPasswordUpdate = true;
                }
            }
            
            logger.LogInformation("Password valid for admin: {PasswordValid}", isPasswordValid);
            
            if (isPasswordValid)
            {
                // Şifre düz metin formatındaysa hash'leyerek güncelle
                if (needsPasswordUpdate)
                {
                    try
                    {
                        admin.Sifre = BCrypt.Net.BCrypt.HashPassword(loginDto.Sifre);
                        await context.SaveChangesAsync();
                        logger.LogInformation("Admin password updated to BCrypt hash for email: {Email}", admin.Email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to update admin password hash for email: {Email}", admin.Email);
                    }
                }
                
                var token = jwtService.GenerateToken(admin.Id, "admin", admin.Email, admin.Ad, admin.Soyad);
                
                return Results.Ok(new LoginResponseDto
                {
                    Token = token,
                    UserType = "admin",
                    UserId = admin.Id,
                    Ad = admin.Ad,
                    Soyad = admin.Soyad,
                    Email = admin.Email,
                    ExpiresAt = jwtService.GetExpiryTime()
                });
            }
        }

        // Admin bulunamazsa veya şifre yanlışsa üye tablosunda ara
        var uye = await context.Uyeler
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        
        logger.LogInformation("Uye found: {UyeFound}", uye != null);
        
        if (uye != null)
        {
            bool isPasswordValid = false;
            bool needsPasswordUpdate = false;
            
            // Önce BCrypt hash olup olmadığını kontrol et
            if (uye.Sifre.StartsWith("$2"))
            {
                // BCrypt hash formatında, normal doğrulama yap
                isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Sifre, uye.Sifre);
            }
            else
            {
                // Düz metin şifre, direkt karşılaştır ve hash'le
                isPasswordValid = uye.Sifre == loginDto.Sifre;
                if (isPasswordValid)
                {
                    needsPasswordUpdate = true;
                }
            }
            
            logger.LogInformation("Password valid for uye: {PasswordValid}", isPasswordValid);
            
            if (isPasswordValid)
            {
                // Üye durumu kontrolü - pasif üyeler giriş yapamaz
                if (!uye.Status)
                {
                    logger.LogWarning("Login blocked for inactive member: {Email}", uye.Email);
                    return Results.BadRequest(new { message = "Üyeliğiniz pasif duruma getirilmiştir. Lütfen yöneticiye başvurunuz." });
                }
                
                // Şifre düz metin formatındaysa hash'leyerek güncelle
                if (needsPasswordUpdate)
                {
                    try
                    {
                        uye.Sifre = BCrypt.Net.BCrypt.HashPassword(loginDto.Sifre);
                        await context.SaveChangesAsync();
                        logger.LogInformation("Uye password updated to BCrypt hash for email: {Email}", uye.Email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to update uye password hash for email: {Email}", uye.Email);
                    }
                }
                
                var token = jwtService.GenerateToken(uye.Id, "uye", uye.Email, uye.Ad, uye.Soyad);
                
                return Results.Ok(new LoginResponseDto
                {
                    Token = token,
                    UserType = "uye",
                    UserId = uye.Id,
                    Ad = uye.Ad,
                    Soyad = uye.Soyad,
                    Email = uye.Email,
                    ExpiresAt = jwtService.GetExpiryTime()
                });
            }
        }

        logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
        return Results.BadRequest(new { message = "Email veya şifre hatalı." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
        return Results.Problem(
            detail: $"Giriş yapılırken bir hata oluştu: {ex.Message}",
            statusCode: 500
        );
    }
})
.WithName("Login")
.WithOpenApi()
.WithTags("Authentication");

// Profil bilgilerini getirme endpoint'i (JWT korumalı)
app.MapGet("/api/profile", async (HttpContext context, ApplicationDbContext dbContext) =>
{
    var userType = context.User.FindFirst("UserType")?.Value;
    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userType))
    {
        return Results.Unauthorized();
    }

    var userIdInt = int.Parse(userId);

    if (userType == "admin")
    {
        var admin = await dbContext.Adminler.FirstOrDefaultAsync(a => a.Id == userIdInt);
        if (admin == null)
        {
            return Results.NotFound(new { message = "Admin bulunamadı." });
        }

        return Results.Ok(new
        {
            userId = admin.Id,
            userType = "admin",
            ad = admin.Ad,
            soyad = admin.Soyad,
            email = admin.Email,
            createdAt = admin.CreatedAt
        });
    }
    else if (userType == "uye")
    {
        var uye = await dbContext.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
        if (uye == null)
        {
            return Results.NotFound(new { message = "Üye bulunamadı." });
        }

        return Results.Ok(new
        {
            userId = uye.Id,
            userType = "uye",
            ad = uye.Ad,
            soyad = uye.Soyad,
            email = uye.Email,
            telefon = uye.Telefon,
            dogumTarihi = uye.DogumTarihi,
            sehir = uye.Sehir,
            instagram = uye.Instagram,
            adres = uye.Adres,
            aracMarka = uye.AracMarka,
            aracModel = uye.AracModel,
            aracYili = uye.AracYili,
            plaka = uye.Plaka,
            deneyim = uye.Deneyim,
            ilgiAlanlari = uye.IlgiAlanlari,
            acilDurumKisi = uye.AcilDurumKisi,
            acilDurumTelefon = uye.AcilDurumTelefon,
            status = uye.Status,
            fotograf = uye.Fotograf,
            createdAt = uye.CreatedAt
        });
    }

    return Results.BadRequest(new { message = "Geçersiz kullanıcı tipi." });
})
.RequireAuthorization()
.WithName("GetProfile")
.WithOpenApi()
.WithTags("Authentication");

// Profil güncelleme endpoint'i (JWT korumalı)
app.MapPut("/api/profile", async (HttpContext context, ApplicationDbContext dbContext, [FromBody] ProfileUpdateDto dto) =>
{
    var userType = context.User.FindFirst("UserType")?.Value;
    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    if (userType != "uye" || string.IsNullOrEmpty(userId))
    {
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    var userIdInt = int.Parse(userId);
    var uye = await dbContext.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
    
    if (uye == null)
    {
        return Results.NotFound(new { message = "Üye bulunamadı." });
    }

    // Pasif üye kontrolü
    if (!uye.Status)
    {
        return Results.Forbid();
    }

    try
    {
        // Güncelleme yapılacak alanlar
        if (!string.IsNullOrEmpty(dto.Telefon)) uye.Telefon = dto.Telefon;
        if (!string.IsNullOrEmpty(dto.Sehir)) uye.Sehir = dto.Sehir;
        if (!string.IsNullOrEmpty(dto.Instagram)) uye.Instagram = dto.Instagram;
        if (!string.IsNullOrEmpty(dto.Adres)) uye.Adres = dto.Adres;
        if (!string.IsNullOrEmpty(dto.AracMarka)) uye.AracMarka = dto.AracMarka;
        if (!string.IsNullOrEmpty(dto.AracModel)) uye.AracModel = dto.AracModel;
        if (!string.IsNullOrEmpty(dto.AracYili)) uye.AracYili = dto.AracYili;
        if (!string.IsNullOrEmpty(dto.Plaka)) uye.Plaka = dto.Plaka;
        if (!string.IsNullOrEmpty(dto.IlgiAlanlari)) uye.IlgiAlanlari = dto.IlgiAlanlari;
        if (!string.IsNullOrEmpty(dto.AcilDurumKisi)) uye.AcilDurumKisi = dto.AcilDurumKisi;
        if (!string.IsNullOrEmpty(dto.AcilDurumTelefon)) uye.AcilDurumTelefon = dto.AcilDurumTelefon;
        if (!string.IsNullOrEmpty(dto.Fotograf)) uye.Fotograf = dto.Fotograf;
        
        if (dto.Deneyim.HasValue) uye.Deneyim = dto.Deneyim.Value;
        if (dto.EmailBildirim.HasValue) uye.EmailBildirim = dto.EmailBildirim.Value;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { message = "Profil başarıyla güncellendi." });
    }
    catch (Exception ex)
    {
        return Results.Problem("Profil güncellenirken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateProfile")
.WithOpenApi()
.WithTags("Authentication");

// Üye başvurusu endpoint'i
app.MapPost("/api/uyelik-basvurusu", async (UyelikBasvurusuDto dto, ApplicationDbContext context) =>
{
    // Validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    // Email kontrolü
    var existingUser = await context.UyelikBasvurulari
        .FirstOrDefaultAsync(u => u.Email == dto.Email);
    
    if (existingUser != null)
    {
        return Results.BadRequest(new { message = "Bu email adresi ile daha önce başvuru yapılmış." });
    }

    // Yeni başvuru oluştur
    var basvuru = new UyelikBasvurusu
    {
        Ad = dto.Ad,
        Soyad = dto.Soyad,
        Email = dto.Email,
        Telefon = dto.Telefon,
        DogumTarihi = dto.DogumTarihi,
        Sehir = dto.Sehir,
        Instagram = dto.Instagram,
        Adres = dto.Adres,
        AracMarka = dto.AracMarka,
        AracModel = dto.AracModel,
        AracYili = dto.AracYili,
        Plaka = dto.Plaka,
        Deneyim = dto.Deneyim,
        IlgiAlanlari = dto.IlgiAlanlari,
        Neden = dto.Neden,
        AcilDurumKisi = dto.AcilDurumKisi,
        AcilDurumTelefon = dto.AcilDurumTelefon,
        Sartlar = dto.Sartlar,
        KisiselVeri = dto.KisiselVeri,
        EmailBildirim = dto.EmailBildirim,
        Fotograf = dto.Fotograf,
        CreatedAt = DateTime.UtcNow,
        Durum = "bekliyor"
    };

    try
    {
        context.UyelikBasvurulari.Add(basvuru);
        await context.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = "Başvurunuz başarıyla alındı. En kısa sürede size dönüş yapılacaktır.",
            id = basvuru.Id,
            durum = basvuru.Durum
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: "Başvuru kaydedilirken bir hata oluştu. Lütfen tekrar deneyiniz.",
            statusCode: 500
        );
    }
})
.WithName("UyelikBasvurusu")
.WithOpenApi()
.WithTags("Üyelik");

// Başvuru durumu sorgulama endpoint'i
app.MapGet("/api/uyelik-basvurusu/{id}", async (int id, ApplicationDbContext context) =>
{
    var basvuru = await context.UyelikBasvurulari
        .FirstOrDefaultAsync(u => u.Id == id);

    if (basvuru == null)
    {
        return Results.NotFound(new { message = "Başvuru bulunamadı." });
    }

    return Results.Ok(new
    {
        id = basvuru.Id,
        ad = basvuru.Ad,
        soyad = basvuru.Soyad,
        email = basvuru.Email,
        durum = basvuru.Durum,
        basvuru_tarihi = basvuru.CreatedAt
    });
})
.WithName("BasvuruDurumu")
.WithOpenApi()
.WithTags("Üyelik");

// Test için şifre hash'i oluşturma endpoint'i
app.MapPost("/api/hash-password", (string password) =>
{
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
    return Results.Ok(new { password, hashedPassword });
})
.WithName("HashPassword")
.WithOpenApi()
.WithTags("Test");

// DOSYA YÖNETİMİ ENDPOINT'LERİ

// Fotoğraf yükleme (Admin ve Üye)
app.MapPost("/api/upload", async (HttpContext context, SupabaseStorageService storageService) =>
{
    var userType = context.User.FindFirst("UserType")?.Value;
    if (string.IsNullOrEmpty(userType))
    {
        return Results.Unauthorized();
    }

    try
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        var folder = form["folder"].ToString();

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "Dosya seçilmedi." });
        }

        if (string.IsNullOrEmpty(folder))
        {
            return Results.BadRequest(new { message = "Klasör adı gereklidir." });
        }

        // Dosya boyutu kontrolü (5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return Results.BadRequest(new { message = "Dosya boyutu 5MB'dan büyük olamaz." });
        }

        // Dosya türü kontrolü
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return Results.BadRequest(new { message = "Sadece resim dosyaları yüklenebilir (JPEG, PNG, GIF, WebP)." });
        }

        // Klasör kontrolü
        var allowedFolders = new[] { "profil_fotolari", "etkinlikler", "haberler", "sponsorlu" };
        if (!allowedFolders.Contains(folder))
        {
            return Results.BadRequest(new { message = "Geçersiz klasör adı." });
        }

        // Dosya adını benzersiz yap
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

        // Dosyayı byte array'e çevir
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileData = memoryStream.ToArray();

        // Supabase Storage'a yükle
        var publicUrl = await storageService.UploadFileAsync(fileData, uniqueFileName, folder);

        return Results.Ok(new FileUploadResponseDto
        {
            Url = publicUrl,
            FileName = uniqueFileName,
            FilePath = $"{folder}/{uniqueFileName}",
            FileSize = file.Length
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"Dosya yükleme hatası: {ex.Message}",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("FileUpload")
.WithOpenApi()
.WithTags("Dosya");

// Fotoğraf silme (Admin ve Üye)
app.MapDelete("/api/upload", async ([FromBody] FileDeleteDto dto, SupabaseStorageService storageService, HttpContext context) =>
{
    var userType = context.User.FindFirst("UserType")?.Value;
    if (string.IsNullOrEmpty(userType))
    {
        return Results.Unauthorized();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    try
    {
        var deleted = await storageService.DeleteFileAsync(dto.FilePath);
        
        if (deleted)
        {
            return Results.Ok(new { message = "Dosya başarıyla silindi." });
        }
        else
        {
            return Results.Problem("Dosya silinirken bir hata oluştu.", statusCode: 500);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"Dosya silme hatası: {ex.Message}",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("FileDelete")
.WithOpenApi()
.WithTags("Dosya");

// ADMIN ENDPOINT'LERİ - Sadece admin kullanıcılar erişebilir

// Bekleyen başvuruları listele (Admin)
app.MapGet("/api/admin/basvurular", async (ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var basvurular = await context.UyelikBasvurulari
        .Where(b => b.Durum == "bekliyor")
        .OrderBy(b => b.CreatedAt)
        .Select(b => new BasvuruListeDto
        {
            Id = b.Id,
            Ad = b.Ad,
            Soyad = b.Soyad,
            Email = b.Email,
            Telefon = b.Telefon,
            Sehir = b.Sehir,
            Durum = b.Durum,
            Fotograf = b.Fotograf,
            BasvuruTarihi = b.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(basvurular);
})
.RequireAuthorization()
.WithName("AdminBasvurular")
.WithOpenApi()
.WithTags("Admin");

// Başvuru detayını görüntüle (Admin)
app.MapGet("/api/admin/basvuru/{id}", async (int id, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var basvuru = await context.UyelikBasvurulari
        .FirstOrDefaultAsync(b => b.Id == id);

    if (basvuru == null)
    {
        return Results.NotFound(new { message = "Başvuru bulunamadı." });
    }

    return Results.Ok(new
    {
        id = basvuru.Id,
        ad = basvuru.Ad,
        soyad = basvuru.Soyad,
        email = basvuru.Email,
        telefon = basvuru.Telefon,
        dogumTarihi = basvuru.DogumTarihi,
        sehir = basvuru.Sehir,
        instagram = basvuru.Instagram,
        adres = basvuru.Adres,
        aracMarka = basvuru.AracMarka,
        aracModel = basvuru.AracModel,
        aracYili = basvuru.AracYili,
        plaka = basvuru.Plaka,
        deneyim = basvuru.Deneyim,
        ilgiAlanlari = basvuru.IlgiAlanlari,
        neden = basvuru.Neden,
        acilDurumKisi = basvuru.AcilDurumKisi,
        acilDurumTelefon = basvuru.AcilDurumTelefon,
        sartlar = basvuru.Sartlar,
        kisiselVeri = basvuru.KisiselVeri,
        emailBildirim = basvuru.EmailBildirim,
        fotograf = basvuru.Fotograf,
        durum = basvuru.Durum,
        basvuruTarihi = basvuru.CreatedAt
    });
})
.RequireAuthorization()
.WithName("AdminBasvuruDetay")
.WithOpenApi()
.WithTags("Admin");

// Başvuruyu onayla/reddet (Admin)
app.MapPost("/api/admin/basvuru-karar", async (BasvuruOnayDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    // Validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    if (dto.Karar != "onayla" && dto.Karar != "reddet")
    {
        return Results.BadRequest(new { message = "Karar 'onayla' veya 'reddet' olmalıdır." });
    }

    var basvuru = await context.UyelikBasvurulari
        .FirstOrDefaultAsync(b => b.Id == dto.BasvuruId);

    if (basvuru == null)
    {
        return Results.NotFound(new { message = "Başvuru bulunamadı." });
    }

    if (basvuru.Durum != "bekliyor")
    {
        return Results.BadRequest(new { message = "Bu başvuru zaten işlenmiş." });
    }

    try
    {
        if (dto.Karar == "onayla")
        {
            // Başvuruyu onayla
            basvuru.Durum = "onaylandı";
            
            // Üye tablosuna ekle
            var yeniUye = new Uye
            {
                Ad = basvuru.Ad,
                Soyad = basvuru.Soyad,
                Email = basvuru.Email,
                Telefon = basvuru.Telefon,
                DogumTarihi = basvuru.DogumTarihi,
                Sehir = basvuru.Sehir,
                Instagram = basvuru.Instagram,
                Adres = basvuru.Adres,
                AracMarka = basvuru.AracMarka,
                AracModel = basvuru.AracModel,
                AracYili = basvuru.AracYili,
                Plaka = basvuru.Plaka,
                Deneyim = basvuru.Deneyim,
                IlgiAlanlari = basvuru.IlgiAlanlari,
                AcilDurumKisi = basvuru.AcilDurumKisi,
                AcilDurumTelefon = basvuru.AcilDurumTelefon,
                UyelikSartlari = basvuru.Sartlar,
                KisiselVeri = basvuru.KisiselVeri,
                EmailBildirim = basvuru.EmailBildirim,
                Fotograf = basvuru.Fotograf,
                Status = true, // Onaylanan üye aktif durumda
                Sifre = BCrypt.Net.BCrypt.HashPassword("123456"), // Geçici şifre
                CreatedAt = DateTime.UtcNow
            };

            context.Uyeler.Add(yeniUye);
            await context.SaveChangesAsync();

            return Results.Ok(new 
            { 
                message = "Başvuru onaylandı ve üye kaydı oluşturuldu.",
                uyeId = yeniUye.Id,
                geciciSifre = "123456"
            });
        }
        else
        {
            // Başvuruyu reddet
            basvuru.Durum = "reddedildi";
            await context.SaveChangesAsync();

            return Results.Ok(new 
            { 
                message = "Başvuru reddedildi.",
                reddetmeNedeni = dto.ReddetmeNedeni
            });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: "Başvuru işlenirken bir hata oluştu.",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("AdminBasvuruKarar")
.WithOpenApi()
.WithTags("Admin");

// Tüm üyeleri listele (Admin)
app.MapGet("/api/admin/uyeler", async (ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var uyeler = await context.Uyeler
        .OrderBy(u => u.Ad)
        .Select(u => new UyeListeDto
        {
            Id = u.Id,
            Ad = u.Ad,
            Soyad = u.Soyad,
            Email = u.Email,
            Telefon = u.Telefon,
            Sehir = u.Sehir,
            Status = u.Status,
            Fotograf = u.Fotograf,
            UyelikTarihi = u.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(uyeler);
})
.RequireAuthorization()
.WithName("AdminUyeler")
.WithOpenApi()
.WithTags("Admin");

// Üye durumunu değiştir (Admin)
app.MapPost("/api/admin/uye-status", async (UyeStatusDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    // Validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    var uye = await context.Uyeler
        .FirstOrDefaultAsync(u => u.Id == dto.UyeId);

    if (uye == null)
    {
        return Results.NotFound(new { message = "Üye bulunamadı." });
    }

    try
    {
        uye.Status = dto.Status;
        await context.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = $"Üye durumu {(dto.Status ? "aktif" : "pasif")} olarak güncellendi.",
            uyeId = uye.Id,
            yeniStatus = dto.Status
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: "Üye durumu güncellenirken bir hata oluştu.",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("AdminUyeStatus")
.WithOpenApi()
.WithTags("Admin");

// SPONSOR YÖNETİMİ - Admin Endpoint'leri

// Sponsor ekleme (Admin)
app.MapPost("/api/admin/sponsor", async (SponsorDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    var sponsor = new Sponsor
    {
        Baslik = dto.Baslik,
        Icerik = dto.Icerik,
        Fotograf = dto.Fotograf,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        context.Sponsorlar.Add(sponsor);
        await context.SaveChangesAsync();
        return Results.Ok(new { message = "Sponsor içeriği başarıyla eklendi.", id = sponsor.Id });
    }
    catch (Exception ex)
    {
        return Results.Problem("Sponsor eklenirken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("AdminSponsorEkle")
.WithOpenApi()
.WithTags("Admin");

// HABER YÖNETİMİ - Admin Endpoint'leri

// Haber ekleme (Admin)
app.MapPost("/api/admin/haber", async (HaberDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    var haber = new Haber
    {
        Baslik = dto.Baslik,
        Aciklama = dto.Aciklama,
        Fotograf = dto.Fotograf,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        context.Haberler.Add(haber);
        await context.SaveChangesAsync();
        return Results.Ok(new { message = "Haber başarıyla eklendi.", id = haber.Id });
    }
    catch (Exception ex)
    {
        return Results.Problem("Haber eklenirken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("AdminHaberEkle")
.WithOpenApi()
.WithTags("Admin");

// Haber güncelleme (Admin)
app.MapPut("/api/admin/haber/{id}", async (int id, [FromBody] HaberUpdateDto dto, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        logger.LogWarning("Unauthorized access attempt to admin haber update endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    try
    {
        var haber = await context.Haberler.FirstOrDefaultAsync(h => h.Id == id);
        if (haber == null)
        {
            logger.LogWarning("Haber not found with ID: {HaberId}", id);
            return Results.NotFound(new { message = "Haber bulunamadı." });
        }
        haber.Baslik = dto.Baslik;
        haber.Aciklama = dto.Aciklama;
        haber.Fotograf = dto.Fotograf;
        await context.SaveChangesAsync();
        logger.LogInformation("Haber updated successfully for haber ID: {HaberId}", id);
        return Results.Ok(new { message = "Haber başarıyla güncellendi.", haberId = haber.Id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating haber for haber ID: {HaberId}", id);
        return Results.Problem(detail: $"Haber güncellenirken bir hata oluştu: {ex.Message}", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateHaberById")
.WithOpenApi()
.WithTags("Admin");

// Sponsor güncelleme (Admin)
app.MapPut("/api/admin/sponsor/{id}", async (int id, [FromBody] SponsorUpdateDto dto, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        logger.LogWarning("Unauthorized access attempt to admin sponsor update endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    try
    {
        var sponsor = await context.Sponsorlar.FirstOrDefaultAsync(s => s.Id == id);
        if (sponsor == null)
        {
            logger.LogWarning("Sponsor not found with ID: {SponsorId}", id);
            return Results.NotFound(new { message = "Sponsor bulunamadı." });
        }
        sponsor.Baslik = dto.Baslik;
        sponsor.Icerik = dto.Icerik;
        sponsor.Fotograf = dto.Fotograf;
        await context.SaveChangesAsync();
        logger.LogInformation("Sponsor updated successfully for sponsor ID: {SponsorId}", id);
        return Results.Ok(new { message = "Sponsor başarıyla güncellendi.", sponsorId = sponsor.Id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating sponsor for sponsor ID: {SponsorId}", id);
        return Results.Problem(detail: $"Sponsor güncellenirken bir hata oluştu: {ex.Message}", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("UpdateSponsorById")
.WithOpenApi()
.WithTags("Admin");

// ETKİNLİK YÖNETİMİ - Admin Endpoint'leri

// Etkinlik ekleme (Admin)
app.MapPost("/api/admin/etkinlik", async (EtkinlikDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    // Etkinlik tarihi kontrolü
    if (dto.Zaman < DateTime.UtcNow)
    {
        return Results.BadRequest(new { message = "Etkinlik tarihi bugünden sonra olmalıdır." });
    }

    var etkinlik = new Etkinlik
    {
        Baslik = dto.Baslik,
        Aciklama = dto.Aciklama,
        Fotograf = dto.Fotograf,
        Adres = dto.Adres,
        Zaman = dto.Zaman,
        Status = true,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        context.Etkinlikler.Add(etkinlik);
        await context.SaveChangesAsync();
        return Results.Ok(new { message = "Etkinlik başarıyla eklendi.", id = etkinlik.Id });
    }
    catch (Exception ex)
    {
        return Results.Problem("Etkinlik eklenirken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("AdminEtkinlikEkle")
.WithOpenApi()
.WithTags("Admin");

// Etkinlik katılımcılarını görüntüleme (Admin)
app.MapGet("/api/admin/etkinlik/{id}/katilimcilar", async (int id, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin")
    {
        return Results.Forbid();
    }

    var etkinlik = await context.Etkinlikler.FirstOrDefaultAsync(e => e.Id == id);
    if (etkinlik == null)
    {
        return Results.NotFound(new { message = "Etkinlik bulunamadı." });
    }

    var katilimcilar = await context.EtkinlikKatilimcilari
        .Where(ek => ek.EtkinlikId == id)
        .Join(context.Uyeler, ek => ek.KatilimciId, u => u.Id, (ek, u) => new EtkinlikKatilimciDto
        {
            Id = u.Id,
            Ad = u.Ad,
            Soyad = u.Soyad,
            Email = u.Email,
            Telefon = u.Telefon,
            KatilimTarihi = ek.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(new
    {
        etkinlik = new { id = etkinlik.Id, baslik = etkinlik.Baslik, zaman = etkinlik.Zaman },
        katilimcilar,
        toplamKatilimci = katilimcilar.Count
    });
})
.RequireAuthorization()
.WithName("AdminEtkinlikKatilimcilar")
.WithOpenApi()
.WithTags("Admin");

// ÜYE ENDPOINT'LERİ - Sadece giriş yapmış üyeler erişebilir

// Sponsorları listeleme (Üye)
app.MapGet("/api/sponsorlar", async (ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userType) || string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    // Üye ise pasif durumu kontrol et
    if (userType == "uye")
    {
        var userIdInt = int.Parse(userId);
        var uye = await context.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
        if (uye == null || !uye.Status)
        {
            return Results.Forbid();
        }
    }

    var sponsorlar = await context.Sponsorlar
        .OrderByDescending(s => s.CreatedAt)
        .Select(s => new SponsorListeDto
        {
            Id = s.Id,
            Baslik = s.Baslik,
            Icerik = s.Icerik,
            Fotograf = s.Fotograf,
            CreatedAt = s.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(sponsorlar);
})
.RequireAuthorization()
.WithName("SponsorlarListesi")
.WithOpenApi()
.WithTags("İçerik");

// Haberleri listeleme (Üye)
app.MapGet("/api/haberler", async (ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userType) || string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }
    
    // Üye ise pasif durumu kontrol et
    if (userType == "uye")
    {
        var userIdInt = int.Parse(userId);
        var uye = await context.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
        if (uye == null || !uye.Status)
        {
            return Results.Forbid();
        }
    }

    var haberler = await context.Haberler
        .OrderByDescending(h => h.CreatedAt)
        .Select(h => new HaberListeDto
        {
            Id = h.Id,
            Baslik = h.Baslik,
            Aciklama = h.Aciklama,
            Fotograf = h.Fotograf,
            CreatedAt = h.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(haberler);
})
.RequireAuthorization()
.WithName("HaberlerListesi")
.WithOpenApi()
.WithTags("İçerik");

// Etkinlikleri listeleme (Üye)
app.MapGet("/api/etkinlikler", async (ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userType) || string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var userIdInt = int.Parse(userId);
    
    // Üye ise pasif durumu kontrol et
    if (userType == "uye")
    {
        var uye = await context.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
        if (uye == null || !uye.Status)
        {
            return Results.Forbid();
        }
    }
    var now = DateTime.UtcNow;

    // Etkinlik status'unu güncelle (geçmiş etkinlikler)
    var pastEvents = await context.Etkinlikler
        .Where(e => e.Zaman < now && e.Status == true)
        .ToListAsync();

    foreach (var pastEvent in pastEvents)
    {
        pastEvent.Status = false;
    }

    if (pastEvents.Any())
    {
        await context.SaveChangesAsync();
    }

    var etkinlikler = await context.Etkinlikler
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => new EtkinlikListeDto
        {
            Id = e.Id,
            Baslik = e.Baslik,
            Aciklama = e.Aciklama,
            Fotograf = e.Fotograf,
            Adres = e.Adres,
            Zaman = e.Zaman,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            KatilimciSayisi = context.EtkinlikKatilimcilari.Count(ek => ek.EtkinlikId == e.Id),
            KatilimDurumu = context.EtkinlikKatilimcilari.Any(ek => ek.EtkinlikId == e.Id && ek.KatilimciId == userIdInt)
        })
        .ToListAsync();

    return Results.Ok(etkinlikler);
})
.RequireAuthorization()
.WithName("EtkinliklerListesi")
.WithOpenApi()
.WithTags("İçerik");

// Etkinliğe katılma (Üye)
app.MapPost("/api/etkinlik/katil", async (EtkinlikKatilimDto dto, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (userType != "uye" || string.IsNullOrEmpty(userId))
    {
        return Results.Forbid();
    }

    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    var userIdInt = int.Parse(userId);

    // Etkinlik kontrolü
    var etkinlik = await context.Etkinlikler.FirstOrDefaultAsync(e => e.Id == dto.EtkinlikId);
    if (etkinlik == null)
    {
        return Results.NotFound(new { message = "Etkinlik bulunamadı." });
    }

    // Etkinlik aktif mi kontrolü
    if (!etkinlik.Status)
    {
        return Results.BadRequest(new { message = "Bu etkinlik artık aktif değil." });
    }

    // Etkinlik tarihi kontrolü
    if (etkinlik.Zaman < DateTime.UtcNow)
    {
        return Results.BadRequest(new { message = "Geçmiş etkinliklere katılım sağlanamaz." });
    }

    // Zaten katılmış mı kontrolü
    var mevcutKatilim = await context.EtkinlikKatilimcilari
        .FirstOrDefaultAsync(ek => ek.EtkinlikId == dto.EtkinlikId && ek.KatilimciId == userIdInt);

    if (mevcutKatilim != null)
    {
        return Results.BadRequest(new { message = "Bu etkinliğe zaten katılım sağladınız." });
    }

    // Üye var mı kontrolü
    var uye = await context.Uyeler.FirstOrDefaultAsync(u => u.Id == userIdInt);
    if (uye == null)
    {
        return Results.BadRequest(new { message = "Üye bulunamadı." });
    }

    try
    {
        var katilim = new EtkinlikKatilimci
        {
            EtkinlikId = dto.EtkinlikId,
            KatilimciId = userIdInt,
            CreatedAt = DateTime.UtcNow
        };

        context.EtkinlikKatilimcilari.Add(katilim);
        await context.SaveChangesAsync();

        return Results.Ok(new { message = "Etkinliğe başarıyla katıldınız.", katilimId = katilim.Id });
    }
    catch (Exception ex)
    {
        return Results.Problem("Katılım sağlanırken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("EtkinligeKatil")
.WithOpenApi()
.WithTags("İçerik");

// Etkinlikten ayrılma (Üye)
app.MapDelete("/api/etkinlik/{id}/ayril", async (int id, ApplicationDbContext context, HttpContext httpContext) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (userType != "uye" || string.IsNullOrEmpty(userId))
    {
        return Results.Forbid();
    }

    var userIdInt = int.Parse(userId);

    // Etkinlik kontrolü
    var etkinlik = await context.Etkinlikler.FirstOrDefaultAsync(e => e.Id == id);
    if (etkinlik == null)
    {
        return Results.NotFound(new { message = "Etkinlik bulunamadı." });
    }

    // Etkinlik tarihi kontrolü
    if (etkinlik.Zaman < DateTime.UtcNow)
    {
        return Results.BadRequest(new { message = "Geçmiş etkinliklerden ayrılım sağlanamaz." });
    }

    // Katılım kontrolü
    var katilim = await context.EtkinlikKatilimcilari
        .FirstOrDefaultAsync(ek => ek.EtkinlikId == id && ek.KatilimciId == userIdInt);

    if (katilim == null)
    {
        return Results.BadRequest(new { message = "Bu etkinliğe katılım sağlamamışsınız." });
    }

    try
    {
        context.EtkinlikKatilimcilari.Remove(katilim);
        await context.SaveChangesAsync();

        return Results.Ok(new { message = "Etkinlikten başarıyla ayrıldınız." });
    }
    catch (Exception ex)
    {
        return Results.Problem("Etkinlikten ayrılırken bir hata oluştu.", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("EtkinliktenAyril")
.WithOpenApi()
.WithTags("İçerik");

// Admin: Üye bilgilerini ID'ye göre getirme endpoint'i
app.MapGet("/api/admin/uye/{id}", async (int id, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var adminId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    // Sadece admin kullanıcılar erişebilir
    if (userType != "admin" || string.IsNullOrEmpty(adminId))
    {
        logger.LogWarning("Unauthorized access attempt to admin endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }

    try
    {
        logger.LogInformation("Admin {AdminId} requesting member information for member ID: {MemberId}", adminId, id);
        
        // Üye bilgilerini getir
        var uye = await context.Uyeler.FirstOrDefaultAsync(u => u.Id == id);
        
        if (uye == null)
        {
            logger.LogWarning("Member not found with ID: {MemberId}", id);
            return Results.NotFound(new { message = "Üye bulunamadı." });
        }

        // Üye bilgilerini döndür (şifre hariç)
        var uyeBilgileri = new
        {
            id = uye.Id,
            ad = uye.Ad,
            soyad = uye.Soyad,
            email = uye.Email,
            telefon = uye.Telefon,
            dogumTarihi = uye.DogumTarihi,
            sehir = uye.Sehir,
            instagram = uye.Instagram,
            adres = uye.Adres,
            aracMarka = uye.AracMarka,
            aracModel = uye.AracModel,
            aracYili = uye.AracYili,
            plaka = uye.Plaka,
            deneyim = uye.Deneyim,
            ilgiAlanlari = uye.IlgiAlanlari,
            acilDurumKisi = uye.AcilDurumKisi,
            acilDurumTelefon = uye.AcilDurumTelefon,
            uyelikSartlari = uye.UyelikSartlari,
            kisiselVeri = uye.KisiselVeri,
            emailBildirim = uye.EmailBildirim,
            status = uye.Status,
            fotograf = uye.Fotograf,
            createdAt = uye.CreatedAt
        };

        logger.LogInformation("Member information retrieved successfully for member ID: {MemberId}", id);
        return Results.Ok(uyeBilgileri);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving member information for member ID: {MemberId}", id);
        return Results.Problem(
            detail: $"Üye bilgileri getirilirken bir hata oluştu: {ex.Message}",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("GetMemberById")
.WithOpenApi()
.WithTags("Admin");

// Admin: Etkinlik bilgilerini ID'ye göre getirme endpoint'i
app.MapGet("/api/admin/etkinlik/{id}", async (int id, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var adminId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    // Sadece admin kullanıcılar erişebilir
    if ((userType != "admin" && userType != "uye") || string.IsNullOrEmpty(adminId))
    {
        logger.LogWarning("Unauthorized access attempt to etkinlik detail endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }

    try
    {
        logger.LogInformation("User {UserId} requesting event information for event ID: {EventId}", adminId, id);
        
        // Etkinlik bilgilerini getir
        var etkinlik = await context.Etkinlikler.FirstOrDefaultAsync(e => e.Id == id);
        
        if (etkinlik == null)
        {
            logger.LogWarning("Event not found with ID: {EventId}", id);
            return Results.NotFound(new { message = "Etkinlik bulunamadı." });
        }

        // Katılımcı sayısını hesapla
        var katilimciSayisi = await context.EtkinlikKatilimcilari
            .CountAsync(ek => ek.EtkinlikId == id);

        // Etkinlik bilgilerini döndür
        var etkinlikBilgileri = new
        {
            id = etkinlik.Id,
            baslik = etkinlik.Baslik,
            aciklama = etkinlik.Aciklama,
            fotograf = etkinlik.Fotograf,
            adres = etkinlik.Adres,
            zaman = etkinlik.Zaman,
            status = etkinlik.Status,
            createdAt = etkinlik.CreatedAt,
            katilimciSayisi = katilimciSayisi
        };

        logger.LogInformation("Event information retrieved successfully for event ID: {EventId}", id);
        return Results.Ok(etkinlikBilgileri);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving event information for event ID: {EventId}", id);
        return Results.Problem(
            detail: $"Etkinlik bilgileri getirilirken bir hata oluştu: {ex.Message}",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("GetEventById")
.WithOpenApi()
.WithTags("İçerik");

// Admin: Etkinlik bilgilerini güncelleme endpoint'i
app.MapPut("/api/admin/etkinlik/{id}", async (int id, [FromBody] EtkinlikUpdateDto dto, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    var adminId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    // Sadece admin kullanıcılar erişebilir
    if (userType != "admin" || string.IsNullOrEmpty(adminId))
    {
        logger.LogWarning("Unauthorized access attempt to admin endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }

    // Validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(v => v.ErrorMessage).ToList();
        return Results.BadRequest(new { errors });
    }

    try
    {
        logger.LogInformation("Admin {AdminId} updating event information for event ID: {EventId}", adminId, id);
        
        // Etkinlik var mı kontrol et
        var etkinlik = await context.Etkinlikler.FirstOrDefaultAsync(e => e.Id == id);
        
        if (etkinlik == null)
        {
            logger.LogWarning("Event not found with ID: {EventId}", id);
            return Results.NotFound(new { message = "Etkinlik bulunamadı." });
        }

        // Etkinlik bilgilerini güncelle
        etkinlik.Baslik = dto.Baslik;
        etkinlik.Aciklama = dto.Aciklama;
        etkinlik.Fotograf = dto.Fotograf;
        etkinlik.Adres = dto.Adres;
        etkinlik.Zaman = dto.Zaman;
        etkinlik.Status = dto.Status;

        await context.SaveChangesAsync();

        logger.LogInformation("Event updated successfully for event ID: {EventId}", id);
        return Results.Ok(new { 
            message = "Etkinlik başarıyla güncellendi.",
            etkinlikId = etkinlik.Id
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating event information for event ID: {EventId}", id);
        return Results.Problem(
            detail: $"Etkinlik güncellenirken bir hata oluştu: {ex.Message}",
            statusCode: 500
        );
    }
})
.RequireAuthorization()
.WithName("UpdateEventById")
.WithOpenApi()
.WithTags("Admin");

// Haber detayını getirme (Admin ve Üye)
app.MapGet("/api/admin/haber/{id}", async (int id, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin" && userType != "uye")
    {
        logger.LogWarning("Unauthorized access attempt to haber detail endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }
    try
    {
        var haber = await context.Haberler.FirstOrDefaultAsync(h => h.Id == id);
        if (haber == null)
        {
            logger.LogWarning("Haber not found with ID: {HaberId}", id);
            return Results.NotFound(new { message = "Haber bulunamadı." });
        }
        var haberDetay = new {
            id = haber.Id,
            baslik = haber.Baslik,
            aciklama = haber.Aciklama,
            fotograf = haber.Fotograf,
            createdAt = haber.CreatedAt
        };
        logger.LogInformation("Haber detail retrieved successfully for haber ID: {HaberId}", id);
        return Results.Ok(haberDetay);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving haber detail for haber ID: {HaberId}", id);
        return Results.Problem(detail: $"Haber detayı getirilirken bir hata oluştu: {ex.Message}", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetHaberById")
.WithOpenApi()
.WithTags("İçerik");

// Sponsor detayını getirme (Admin ve Üye)
app.MapGet("/api/admin/sponsor/{id}", async (int id, ApplicationDbContext context, HttpContext httpContext, ILogger<Program> logger) =>
{
    var userType = httpContext.User.FindFirst("UserType")?.Value;
    if (userType != "admin" && userType != "uye")
    {
        logger.LogWarning("Unauthorized access attempt to sponsor detail endpoint by user type: {UserType}", userType);
        return Results.Forbid();
    }
    try
    {
        var sponsor = await context.Sponsorlar.FirstOrDefaultAsync(s => s.Id == id);
        if (sponsor == null)
        {
            logger.LogWarning("Sponsor not found with ID: {SponsorId}", id);
            return Results.NotFound(new { message = "Sponsor bulunamadı." });
        }
        var sponsorDetay = new {
            id = sponsor.Id,
            baslik = sponsor.Baslik,
            icerik = sponsor.Icerik,
            fotograf = sponsor.Fotograf,
            createdAt = sponsor.CreatedAt
        };
        logger.LogInformation("Sponsor detail retrieved successfully for sponsor ID: {SponsorId}", id);
        return Results.Ok(sponsorDetay);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving sponsor detail for sponsor ID: {SponsorId}", id);
        return Results.Problem(detail: $"Sponsor detayı getirilirken bir hata oluştu: {ex.Message}", statusCode: 500);
    }
})
.RequireAuthorization()
.WithName("GetSponsorById")
.WithOpenApi()
.WithTags("İçerik");

// Landing page için etkinlik adı ve adresi dönen endpoint (herkese açık)
app.MapGet("/api/landing-page-etkinlikler", async (ApplicationDbContext context) =>
{
    var etkinlikler = await context.Etkinlikler
        .Where(e => e.Status == true)
        .OrderByDescending(e => e.CreatedAt)
        .Select(e => new bosphorus_fellas_api.DTOs.LandingPageEtkinlikDto
        {
            Id = e.Id,
            Baslik = e.Baslik,
            Adres = e.Adres
        })
        .ToListAsync();

    return Results.Ok(etkinlikler);
})
.WithName("LandingPageEtkinlikler")
.WithOpenApi()
.WithTags("LandingPage");

app.Run();