using Microsoft.EntityFrameworkCore;

namespace TempSensorDB.Models;

public class SqliteDbContext(DbContextOptions<SqliteDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<Reading> Readings { get; set; }
    public DbSet<Farm> Farms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>()
            .HasOne(s => s.Farm)
            .WithMany(l => l.Sensors)
            .HasForeignKey(s => s.FarmID);

        modelBuilder.Entity<Reading>()
            .HasOne(t => t.Sensor)
            .WithMany(s => s.Readings)
            .HasForeignKey(t => t.SensorID);
    }
}