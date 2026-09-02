using System.Linq;
using HomeTaskManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeTaskManager.Api.Tests;

// Isolated SQLite file per instance; migrations are applied explicitly below regardless of
// environment, so this doesn't depend on Program.cs's own Development-only migration call.
public class HomeTaskManagerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"htm-tests-{Guid.NewGuid():N}.db");

    // Overridable so a subclass can boot with a non-Development environment (e.g. to exercise DevelopmentOnlyAttribute).
    protected virtual string WebHostEnvironmentName => "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(WebHostEnvironmentName);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<HomeTaskManagerDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<HomeTaskManagerDbContext>(options => options.UseSqlite($"Data Source={_databasePath};Pooling=False"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<HomeTaskManagerDbContext>().Database.Migrate();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup: the OS may briefly hold the file handle after disposal; leaving a stray temp file is harmless.
        }
    }
}

// Boots the app as if deployed outside Development, to verify DevelopmentOnlyAttribute-gated endpoints are disabled.
public sealed class HomeTaskManagerApiProductionFactory : HomeTaskManagerApiFactory
{
    protected override string WebHostEnvironmentName => "Production";
}


