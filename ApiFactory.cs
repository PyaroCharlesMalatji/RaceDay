using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayApi.Data;

namespace RaceDayApi.Tests;

// Starts the real API for tests, but uses a fake in-memory database
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
            s.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName)));
    }
}