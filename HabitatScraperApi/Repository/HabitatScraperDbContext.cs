using HabitatScraperApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitatScraperApi.Repository
{
    public class HabitatScraperDbContext : DbContext
    {
        public HabitatScraperDbContext(DbContextOptions<HabitatScraperDbContext> options) : base(options) 
        {

        }

        public DbSet<Anuncio> Anuncios { get; set; } = null!;
        public DbSet<PriceHistory> PriceHistory { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Anuncio>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                    .IsRequired();

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.URL)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<PriceHistory>()
                .HasOne(ph => ph.Anuncio)
                .WithMany(a => a.PriceHistories)
                .HasForeignKey(ph => ph.AnuncioId);
            }
    }
}
