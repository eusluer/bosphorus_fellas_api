using Microsoft.EntityFrameworkCore;
using bosphorus_fellas_api.Models;

namespace bosphorus_fellas_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<UyelikBasvurusu> UyelikBasvurulari { get; set; }
    public DbSet<Admin> Adminler { get; set; }
    public DbSet<Uye> Uyeler { get; set; }
    public DbSet<Sponsor> Sponsorlar { get; set; }
    public DbSet<Haber> Haberler { get; set; }
    public DbSet<Etkinlik> Etkinlikler { get; set; }
    public DbSet<EtkinlikKatilimci> EtkinlikKatilimcilari { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Tablo adlarını snake_case olarak ayarlıyoruz
        modelBuilder.Entity<UyelikBasvurusu>()
            .ToTable("uyelik_basvurusu");
            
        modelBuilder.Entity<Admin>()
            .ToTable("adminler");
            
        modelBuilder.Entity<Uye>()
            .ToTable("uyeler");
            
        modelBuilder.Entity<Sponsor>()
            .ToTable("sponsorlar");
            
        modelBuilder.Entity<Haber>()
            .ToTable("haberler");
            
        modelBuilder.Entity<Etkinlik>()
            .ToTable("etkinlikler");
            
        modelBuilder.Entity<EtkinlikKatilimci>()
            .ToTable("etkinlik_katilimcilari");
    }
} 