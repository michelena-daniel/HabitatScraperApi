using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HabitatScraperApi.Models.Entities
{
    public class PriceHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PreviousPrice { get; set; } = 0;

        public int AnuncioId { get; set; }

        public bool IsPriceRaised { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Anuncio? Anuncio { get; set; }
    }
}
