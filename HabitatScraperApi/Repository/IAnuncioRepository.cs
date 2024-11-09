using HabitatScraperApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitatScraperApi.Repository
{
    public interface IAnuncioRepository
    {
        Task<Anuncio?> GetAnuncioByIdAsync(int id);
        Task<Anuncio?> GetAnuncioByUrlAsync(string url);
        Task<Anuncio> UpdateAnuncioAsync(Anuncio oldAnuncio, Anuncio newAnuncio);
        Task<int> DeleteAnuncioAsync(int id);
        Task<bool> ExistsByUrlAsync(string url);
        Task<List<Anuncio>> AddAnunciosAsync(List<Anuncio> anuncioList);
    }
    public class AnuncioRepository : IAnuncioRepository
    {
        private readonly HabitatScraperDbContext _context;
        public AnuncioRepository(HabitatScraperDbContext context)
        {
            _context = context;
        }

        public async Task<Anuncio?> GetAnuncioByIdAsync(int id)
        {
            return await _context.Anuncios.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Anuncio?> GetAnuncioByUrlAsync(string url)
        {
            return await _context.Anuncios.FirstOrDefaultAsync(a => a.URL == url);
        }

        public async Task<Anuncio> UpdateAnuncioAsync(Anuncio oldAnuncio, Anuncio newAnuncio)
        {
            _context.Entry(oldAnuncio).CurrentValues.SetValues(newAnuncio);
            await _context.SaveChangesAsync();
            return oldAnuncio;
        }

        public async Task<List<Anuncio>> AddAnunciosAsync(List<Anuncio> anuncioList)
        {
            await _context.Anuncios.AddRangeAsync(anuncioList);
            await _context.SaveChangesAsync();
            return anuncioList;
        }

        public async Task<int> DeleteAnuncioAsync(int id)
        {
            return await _context.Anuncios.Where(a => a.Id == id).ExecuteDeleteAsync();
        }

        public async Task<bool> ExistsByUrlAsync(string url)
        {
            return await _context.Anuncios.AnyAsync(a => a.URL == url);
        }
    }
}
