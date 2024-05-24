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

app.MapGet("/locations", async (TempSensorDbContext dbContext) =>
{
    var locations = await dbContext.Locations.ToListAsync();
    return Results.Ok(locations);
});

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
        .FirstOrDefaultAsync(s => s.Name == name);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with Name {name} not found." });
    }
    return Results.Json(sensor, jsonOptions);
});

app.MapGet("/sensors", async (TempSensorDbContext dbContext) =>
{
    var sensors = await dbContext.Sensors.Include(s => s.TempReadings).ToListAsync();
    var sensorDTOs = sensors
        .Select(s => new SensorDTO()
        {
            SensorID = s.SensorID,
            Name = s.Name,
            LocationID = s.LocationID,
            LastTempF = s.TempReadings.Select(r => r.TempF).LastOrDefault(),
        });
    return Results.Ok(sensorDTOs);
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
    // TODO: circular buffer for temp readings
    dbContext.TempReadings.Add(reading);
    await dbContext.SaveChangesAsync();
    return Results.Ok(reading);
});
// -------------------------------------------------------------------------


app.Run();


// -------------------------------------------------------------------------
// Handling JSON
// ClientSide:
// 
// fetch("/submit", {
//   method: 'POST',
//   headers: {
//     // Set the content type header
//     'Content-Type': 'application/json'
//   },
//   // Stringify the body to JSON
//   body: JSON.stringify({ Property1: "hello", Property2: 2 })
// }).then(res => res.json()).then(console.log)
//   .catch(console.error);

public class SensorDTO
{
    public int SensorID { get; set; }

    public required string Name { get; set; }

    public int LocationID { get; set; }

    public double? LastTempF { get; set; }
}

