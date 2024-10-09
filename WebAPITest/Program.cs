using Microsoft.Extensions.Configuration;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register a service for database operations
builder.Services.AddTransient<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton(new DatabaseConfig { ConnectionString = connectionString });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

// Example endpoint to test database connection
app.MapGet("/dbversion", (IDatabaseService dbService) =>
{
    var version = dbService.GetPostgresVersion();
    return Results.Ok(new { PostgreSQLVersion = version });
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public interface IDatabaseService
{
    string GetPostgresVersion();
}

public class DatabaseService : IDatabaseService
{
    private readonly DatabaseConfig _databaseConfig;

    public DatabaseService(DatabaseConfig databaseConfig)
    {
        _databaseConfig = databaseConfig;
    }

    public string GetPostgresVersion()
    {
        try
        {
            using (var connection = new NpgsqlConnection(_databaseConfig.ConnectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand("SELECT version()", connection))
                {
                    return command.ExecuteScalar().ToString()!;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exception (logging, etc.)
            throw new ApplicationException("An error occurred while getting PostgreSQL version", ex);
        }
    }
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; }
}
