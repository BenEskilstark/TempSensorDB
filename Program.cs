using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TempSensorDB.Models;

// -------------------------------------------------------------------------
// Build the application
var builder = WebApplication.CreateBuilder(args);
var conf = builder.Configuration;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8000);
});

// configure DB connection:
builder.Services.AddDbContext<TempSensorDbContext>(options =>
    options.UseSqlServer(conf.GetConnectionString("DefaultConnection")));

var jsonOptions = new JsonSerializerOptions
{
    // will put null instead of trying to serialize a cyclical reference
    // Other option is "Preserve" which will give a $refID back to the reference
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};


WebApplication app = builder.Build();
// basic stuff for serving from wwwroot directory
app.UseDefaultFiles();
app.UseStaticFiles();
// don't forget app.Run(); at the bottom
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// get requests with query parameters 

app.MapGet("/locations", async (TempSensorDbContext dbContext, string? farm) =>
{
    var locations = await dbContext.Locations
        .Where(l => farm == null || l.Farm == farm)
        .ToListAsync();
    return Results.Ok(locations);
})
.DisableAntiforgery();

app.MapGet("/location/{name}", async (TempSensorDbContext dbContext, string name) =>
{
    var location = await dbContext.Locations
        .Include(l => l.Sensors)
        .FirstOrDefaultAsync(l => l.Name == name);
    if (location == null)
    {
        return Results.NotFound(new { Message = $"Location with Name {name} not found." });
    }
    return Results.Json(location, jsonOptions);
});

app.MapGet("/sensor/{name}", async (TempSensorDbContext dbContext, string name) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.TempReadings)
        .Include(s => s.Location)
        .FirstOrDefaultAsync(s => s.Name == name);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with Name {name} not found." });
    }
    return Results.Json(sensor, jsonOptions);
});

app.MapGet("/sensors", async (TempSensorDbContext dbContext) =>
{
    var sensors = await dbContext.Sensors
        .Include(s => s.TempReadings)
        .Include(s => s.Location)
        .ToListAsync();
    var sensorDTOs = sensors
        .Select(s => new SensorDTO()
        {
            SensorID = s.SensorID,
            Name = s.Name,
            LocationID = s.LocationID,
            Location = new LocationDTO()
            {
                LocationID = s.Location.LocationID,
                Farm = s.Location.Farm,
                Name = s.Location.Name,
                MinTempF = s.Location.MinTempF,
                MaxTempF = s.Location.MaxTempF
            },
            CalibrationValueF = s.CalibrationValueF,
            LastTempF = s.TempReadings.Select(r => r.TempF).LastOrDefault(),
            LastTimeStamp = s.TempReadings.Count != 0
                ? DateTime.SpecifyKind(s.TempReadings.Last().TimeStamp, DateTimeKind.Utc)
                : null,
        });
    return Results.Json(sensorDTOs, jsonOptions);
});
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// post requests 
app.MapPost("/add-location", async (TempSensorDbContext dbContext, Location newLocation) =>
{
    dbContext.Locations.Add(newLocation);
    await dbContext.SaveChangesAsync();
    return Results.Ok(newLocation);
});

app.MapPost("/add-sensor", async (TempSensorDbContext dbContext, Sensor newSensor) =>
{
    dbContext.Sensors.Add(newSensor);
    await dbContext.SaveChangesAsync();
    return Results.Ok(newSensor);
});

app.MapPost("/temp-reading", async (TempSensorDbContext dbContext, TempReading reading) =>
{
    dbContext.TempReadings.Add(reading);
    await dbContext.SaveChangesAsync();

    // Determine the threshold times.
    // DateTime oneDayAgo = DateTime.UtcNow.AddDays(-1);
    // DateTime oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

    // var readingsOlderThanADay = dbContext.TempReadings
    //     .Where(r => r.SensorID == reading.SensorID)
    //     .Where(tr => tr.TimeStamp < oneDayAgo && tr.TimeStamp >= oneMonthAgo);

    // // We group by year, month, day, and hour to get one reading per hour.
    // List<TempReading> readingsToKeep = await readingsOlderThanADay
    //     .GroupBy(tr => new { tr.TimeStamp.Year, tr.TimeStamp.Month, tr.TimeStamp.Day, tr.TimeStamp.Hour })
    //     .Select(g => g.OrderByDescending(tr => tr.TimeStamp).First()) // Take the last reading of each hour
    //     .ToListAsync();

    // dbContext.TempReadings.RemoveRange(readingsOlderThanADay);
    // dbContext.TempReadings.AddRange(readingsToKeep);

    // // Keep only the reading closest to noon for readings older than one month.
    // var noon = new TimeSpan(12, 0, 0); // Noon time
    // var readingsOlderThanAMonth = dbContext.TempReadings
    //     .Where(r => r.SensorID == reading.SensorID)
    //     .Where(tr => tr.TimeStamp < oneMonthAgo);

    // var readingsOlderToKeep = await readingsOlderThanAMonth
    //     .GroupBy(tr => new { tr.TimeStamp.Year, tr.TimeStamp.Month, tr.TimeStamp.Day })
    //     .Select(g => g.OrderBy(tr => Math.Abs((tr.TimeStamp.TimeOfDay - noon).Ticks)) // Closest to noon
    //                    .First())
    //     .ToListAsync();

    // dbContext.TempReadings.RemoveRange(readingsOlderThanAMonth); // Prepare to remove all readings older than a month
    // dbContext.TempReadings.AddRange(readingsOlderToKeep); // Add back the readings we want to keep (closest to noon)
    // await dbContext.SaveChangesAsync();


    return Results.Ok(reading);
});


app.MapPost("/sensor/{name}/set-location", async (TempSensorDbContext dbContext, string name, int newLocationID) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.TempReadings)
        .FirstOrDefaultAsync(s => s.Name == name);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with Name {name} not found." });
    }
    sensor.LocationID = newLocationID;
    await dbContext.SaveChangesAsync();
    return Results.Ok(sensor);
});

app.MapPost("/sensor/{name}/set-calibration", async (TempSensorDbContext dbContext, string name, double newCalibrationVal) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.TempReadings)
        .FirstOrDefaultAsync(s => s.Name == name);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with Name {name} not found." });
    }
    sensor.CalibrationValueF = newCalibrationVal;
    await dbContext.SaveChangesAsync();
    return Results.Ok(sensor);
});

// -------------------------------------------------------------------------


app.Run();


// -------------------------------------------------------------------------
// Handling JSON
// ClientSide:
// 
// fetch("/temp-reading", {
//   method: 'POST',
//   headers: {
//     'Content-Type': 'application/json'
//   },
//   body: JSON.stringify({ SensorID: 1, TempF: 39.5, TimeStamp: new Date().toISOString() })
// }).then(res => res.json()).then(console.log)
//   .catch(console.error);

public class SensorDTO
{
    public int SensorID { get; set; }
    public required string Name { get; set; }
    public int LocationID { get; set; }
    public LocationDTO? Location { get; set; }
    public double CalibrationValueF { get; set; } = 0;
    public double? LastTempF { get; set; }
    public DateTime? LastTimeStamp { get; set; }
}

public class LocationDTO
{
    public int LocationID { get; set; }
    public string Farm { get; set; }
    public string Name { get; set; }
    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }
}
