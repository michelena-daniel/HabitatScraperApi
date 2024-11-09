using CsvHelper.Configuration;
using CsvHelper;
using HabitatScraperApi.Models;
using HabitatScraperApi.Repository;
using System.Diagnostics;
using System.Globalization;
using HabitatScraperApi.Models.Entities;
using HabitatScraper.Utils.Helpers;

namespace HabitatScraperApi.Services
{
    public interface ICsvService
    {
        Task<int> ProcessCsvFilesAsync(List<string> csvFiles);
    }
    public class CsvService : ICsvService
    {
        private readonly ILogger<CsvService> _logger;
        private IAnuncioRepository _anuncioRepository;
        private IPriceHistoryRepository _priceHistoryRepository;

        public CsvService(IAnuncioRepository anuncioRepository, IPriceHistoryRepository priceHistoryRepository, ILogger<CsvService> logger)
        {
            _logger = logger;
            _anuncioRepository = anuncioRepository;
            _priceHistoryRepository = priceHistoryRepository;
        }

        public async Task<int> ProcessCsvFilesAsync(List<string> csvFiles)
        {
            var importStopwatch = new Stopwatch();
            importStopwatch.Start();
            int totalImported = 0;

            foreach (var filePath in csvFiles)
            {
                try
                {
                    _logger.LogDebug($"Importing file {Path.GetFileName(filePath)}");

                    using var reader = new StreamReader(filePath);
                    using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HeaderValidated = null,
                        MissingFieldFound = null,
                    });

                    //Read
                    var records = csv.GetRecords<AnuncioCsvRequest>().ToList();

                    var anuncios = records.Select(r => new Anuncio
                    {
                        Title = r.Title,
                        Price = ParsePriceHelper.ParsePrice(r.Price),
                        Size = r.Size,
                        Rooms = r.Rooms,
                        PropertyType = r.PropertyType,
                        URL = r.URL,
                        DaysActive = r.DaysActive,
                        AgentName = r.AgentName,
                        Description = r.Description
                    }).ToList();

                    //Convert
                    if (anuncios.Any())
                    {
                        var anunciosToAdd = new List<Anuncio>();

                        foreach (var anuncio in anuncios)
                        {
                            // Check for duplicates based on URL
                            bool exists = await _anuncioRepository.ExistsByUrlAsync(anuncio.URL);
                            if (!exists)
                            {
                                anunciosToAdd.Add(anuncio);
                            }
                            else
                            {
                                var oldAnuncio = await _anuncioRepository.GetAnuncioByUrlAsync(anuncio.URL);
                                if (oldAnuncio != null && oldAnuncio.Price != anuncio.Price)
                                {
                                    _logger.LogInformation($"Price change detected, updating: {anuncio.URL}");
                                    var priceHistory = new PriceHistory
                                    {
                                        Price = anuncio.Price,
                                        AnuncioId = anuncio.Id,
                                        IsPriceRaised = anuncio.Price > oldAnuncio.Price,
                                        PreviousPrice = oldAnuncio.Price
                                    };

                                    await _priceHistoryRepository.Create(priceHistory);
                                    //oldAnuncio.Price = anuncio.Price;
                                    //oldAnuncio.AgentName = anuncio.AgentName;
                                    //oldAnuncio.PropertyType = anuncio.PropertyType;
                                    //oldAnuncio.Title = anuncio.Title;
                                    //oldAnuncio.Size = anuncio.Size;
                                    //oldAnuncio.Rooms = anuncio.Rooms;
                                    //oldAnuncio.URL = anuncio.URL;
                                    //oldAnuncio.DaysActive = anuncio.DaysActive;
                                    //oldAnuncio.Description = anuncio.Description;
                                    await _anuncioRepository.UpdateAnuncioAsync(oldAnuncio, anuncio);
                                }
                                _logger.LogInformation($"Duplicated with no price change, skipping, url: {anuncio.URL}");
                            }
                        }

                        if (anunciosToAdd.Any())
                        {
                            await _anuncioRepository.AddAnunciosAsync(anunciosToAdd);
                            totalImported += anunciosToAdd.Count;

                            _logger.LogInformation($"Imported {anunciosToAdd.Count} records from {Path.GetFileName(filePath)}.");
                        }
                        else
                        {
                            _logger.LogInformation($"No new records to import from {Path.GetFileName(filePath)}.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No records found in {Path.GetFileName(filePath)}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            importStopwatch.Stop();
            _logger.LogInformation($"Imported a total of {totalImported} records in {importStopwatch.Elapsed}.");

            return totalImported;
        }        
    }
}
