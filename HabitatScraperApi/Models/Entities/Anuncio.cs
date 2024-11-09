using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HabitatScraperApi.Models.Entities
{
    public class Anuncio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }

        public string? Size { get; set; }

        public string? Rooms { get; set; }

        [MaxLength(50)]
        public string? PropertyType { get; set; }

        [Required]
        [MaxLength(255)]
        public string URL { get; set; }

        public string? DaysActive { get; set; }

        public string? AgentName { get; set; }

        public string? Description { get; set; }
        public string? Street { get; set; }
        public int? StreetNumber { get; set; }
        public string? Location { get; set; }
        public string? Source { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    }
}
