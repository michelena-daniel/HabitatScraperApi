using AspNetCoreRateLimit;
using HabitatScraperApi.Repository;
using HabitatScraperApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

namespace HabitatScraperApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.AddTransient<ICsvService, CsvService>();
            builder.Services.AddScoped<IAnuncioRepository, AnuncioRepository>();
            builder.Services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

            // Register DbContext with PostgreSQL provider
            builder.Services.AddDbContext<HabitatScraperDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure Rate Limiting
            //builder.Services.AddMemoryCache();
            //builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            //builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
            //builder.Services.AddInMemoryRateLimiting();
            //builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HabitatScraperApi", Version = "v1" });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseIpRateLimiting();

            //app.UseMiddleware<ApiKeyMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
