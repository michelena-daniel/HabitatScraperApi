using Microsoft.AspNetCore.Mvc;
using HabitatScraperApi.Services;
using System.Text.RegularExpressions;

namespace HabitatScraperApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CsvController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private ILogger<CsvController> _logger;
        private ICsvService _csvService;        

        public CsvController(ILogger<CsvController> logger, IWebHostEnvironment env, ICsvService csvService)
        {
            _logger = logger;
            _env = env;
            _csvService = csvService;
        }

        [HttpPost]
        [Route("import-csv")]
        public async Task<IActionResult> ImportCsv()
        {
            try
            {
                string csvFilesDirectory = Path.Combine(_env.ContentRootPath, "Data", "CsvFiles");

                if (!Directory.Exists(csvFilesDirectory))
                {
                    _logger.LogWarning($"Directory does not exist: {csvFilesDirectory}");
                    return BadRequest($"Directory does not exist: {csvFilesDirectory}");
                }

                //Subdirectories have DD-MM-YY pattern, example 04-11-24
                var datePattern = @"^\d{2}-\d{2}-\d{2}$"; 
                var datedDirectories = Directory.GetDirectories(csvFilesDirectory)
                                                .Where(dir => Regex.IsMatch(Path.GetFileName(dir), datePattern))
                                                .ToList();

                if (datedDirectories.Count == 0)
                {
                    _logger.LogWarning("No dated subfolders found.");
                    return BadRequest("No dated subfolders found.");
                }

                var csvFiles = new List<string>();

                foreach (var dir in datedDirectories)
                {
                    var files = Directory.GetFiles(dir, "*.csv");
                    csvFiles.AddRange(files);
                }

                if (!csvFiles.Any())
                {
                    _logger.LogWarning("No CSV files found in the dated subfolders.");
                    return BadRequest("No CSV files found in the dated subfolders.");
                }

                _logger.LogInformation($"Found {csvFiles.Count} CSV files to import.");
                var totalImported = await _csvService.ProcessCsvFilesAsync(csvFiles);    

                return Ok(new { Message = $"Successfully imported {totalImported} listings into the database." });
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error during CSV import: {ex.Message}");
                return StatusCode(500, "An error occurred while importing CSV data.");
            }
        }
    }
}
