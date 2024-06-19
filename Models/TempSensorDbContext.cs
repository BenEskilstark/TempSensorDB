using Microsoft.EntityFrameworkCore;

namespace TempSensorDB.Models;

public class TempSensorDbContext(DbContextOptions<TempSensorDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<TempReading> TempReadings { get; set; }
    public DbSet<Farm> Farms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>()
            .HasOne(s => s.Location)
            .WithMany(l => l.Sensors)
            .HasForeignKey(s => s.LocationID);
        modelBuilder.Entity<Sensor>()
            .HasOne(s => s.Farm)
            .WithMany(l => l.Sensors)
            .HasForeignKey(s => s.FarmID);

        modelBuilder.Entity<Location>()
            .HasOne(s => s.Farm)
            .WithMany(l => l.Locations)
            .HasForeignKey(s => s.FarmID);

        modelBuilder.Entity<TempReading>()
            .HasOne(t => t.Sensor)
            .WithMany(s => s.TempReadings)
            .HasForeignKey(t => t.SensorID);
    }
}