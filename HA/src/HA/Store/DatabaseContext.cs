using Microsoft.EntityFrameworkCore;

namespace HA.Store;

public class DatabaseContext : DbContext
{
    private readonly string _databaseName;

    public DbSet<MeasurementEntity> MeasurementEntities { get; set; }

    public DatabaseContext(string databaseName = "Measurements.db")
    {
        _databaseName = databaseName;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_databaseName}");
    }
}