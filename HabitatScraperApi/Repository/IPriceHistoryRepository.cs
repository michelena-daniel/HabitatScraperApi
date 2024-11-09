using HabitatScraperApi.Models.Entities;

namespace HabitatScraperApi.Repository
{
    public interface IPriceHistoryRepository
    {
        Task<PriceHistory> Create(PriceHistory priceHistory);
    }
    public class PriceHistoryRepository : IPriceHistoryRepository
    {
        private HabitatScraperDbContext _context;
        public PriceHistoryRepository(HabitatScraperDbContext context) {
            _context = context;
        }

        public async Task<PriceHistory> Create(PriceHistory priceHistory)
        {
            await _context.PriceHistory.AddAsync(priceHistory);
            await _context.SaveChangesAsync();

            return priceHistory;
        }
    }
}
