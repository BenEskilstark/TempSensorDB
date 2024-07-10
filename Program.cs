using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using TempSensorDB.Models;
using TempSensorDB.Models.DataTransfer;
using TempSensorDB.WebApplication;

using System.Security.Claims;

// -------------------------------------------------------------------------
// Configure the application builder
var builder = WebApplication.CreateBuilder(args);
var conf = builder.Configuration;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8000);
});

// configure DB connection:
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlServer(conf.GetConnectionString("MSSQLConnection")));
// builder.Services.AddDbContext<SensorDbContext>(options =>
//         options.UseNpgsql(conf.GetConnectionString("postgresConnection")));

var jsonOptions = new JsonSerializerOptions
{
    // will put null instead of trying to serialize a cyclical reference
    // Other option is "Preserve" which will give a $refID back to the reference
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CustomCORS",
        builder =>
        {
            builder.WithOrigins(
                "http://localhost:8000", // Your local development origin
                                         // "https://your-production-site.com", // Your production origin
                "http://temperatures.chickenkiller.com" // Your API domain
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Include this if you need to support credentials (cookies, headers)
        });
});

// JWT Authentication and Authorization 
builder.Services.AddAuthorization();
WebAppAuth.AddAuthenticationService(builder, conf);
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// Build the application
WebApplication app = builder.Build();
// basic stuff for serving from wwwroot directory
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("CustomCORS");
// don't forget app.Run(); at the bottom
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// get requests with query parameters 

app.MapGet("/farms", async (SensorDbContext dbContext) =>
{
    List<FarmDTO> farms = [.. dbContext.Farms.Select(f => FarmDTO.FromFarm(f))];
    return Results.Json(farms, jsonOptions);
});


app.MapGet("/sensor/{sensorID}", async (SensorDbContext dbContext, int sensorID) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.Readings)
        .FirstOrDefaultAsync(s => s.SensorID == sensorID);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with ID {sensorID} not found." });
    }
    return Results.Json(sensor, jsonOptions);
});


app.MapGet("/sensors/{farmID}", async (SensorDbContext dbContext, int farmID) =>
{
    var sensors = await dbContext.Sensors
        .Include(s => s.Readings)
        .Where(s => s.FarmID == farmID)
        .ToListAsync();
    var sensorDTOs = sensors
        .Select(s => SensorDTO.FromSensor(s));
    return Results.Json(sensorDTOs, jsonOptions);
});
// -------------------------------------------------------------------------




// -------------------------------------------------------------------------
// post requests 
app.MapPost("/token", async (SensorDbContext dbContext, Farm user) =>
{
    if (WebAppAuth.IsUserAuthorized(dbContext, user))
    {
        var tokenString = WebAppAuth.GenerateJWT(user, conf);
        return Results.Ok(new { Token = tokenString });
    }
    return Results.Unauthorized();
});


app.MapPost("/add-sensor",
    async (HttpContext httpContext, SensorDbContext dbContext, Sensor newSensor) =>
{
    // authorization
    Farm? farm = dbContext.Farms
        .FirstOrDefault(f => f.FarmID == newSensor.FarmID);
    if (farm == null) return Results.BadRequest("No such farm");
    string? authorizedFarm = httpContext.User.FindFirstValue("Farm");
    if (authorizedFarm != farm.Name) return Results.Unauthorized();

    dbContext.Sensors.Add(newSensor);
    await dbContext.SaveChangesAsync();
    return Results.Ok(newSensor);
}).RequireAuthorization();


app.MapPost("/reading",
    async (SensorDbContext dbContext, ReadingDTO readDTO) =>
{
    // authorization
    var res = WebAppAuth.FailIfUnauthorized(dbContext, readDTO);
    if (res != null) return res;

    Reading newReading = readDTO.ToReading();
    dbContext.Readings.Add(newReading);
    await dbContext.SaveChangesAsync();
    return Results.Json(newReading, jsonOptions);
});


app.MapPost("/sensor/{sensorID}/set-calibration",
    async (
        HttpContext httpContext, SensorDbContext dbContext,
        int sensorID, double newCalibrationVal
    ) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.Farm)
        .Include(s => s.Readings)
        .FirstOrDefaultAsync(s => s.SensorID == sensorID);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with ID {sensorID} not found." });
    }

    // authorization
    string? authorizedFarm = httpContext.User.FindFirstValue("Farm");
    if (authorizedFarm != sensor.Farm.Name) return Results.Unauthorized();

    sensor.CalibrationValueF = newCalibrationVal;
    await dbContext.SaveChangesAsync();
    return Results.Ok(sensor);
}).RequireAuthorization();


app.MapPost("/sensor/{sensorID}/set-min-max",
    async (
        HttpContext httpContext, SensorDbContext dbContext,
        int sensorID, double? min, double? max
    ) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.Farm)
        .Include(s => s.Readings)
        .FirstOrDefaultAsync(s => s.SensorID == sensorID);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with ID {sensorID} not found." });
    }

    // authorization
    string? authorizedFarm = httpContext.User.FindFirstValue("Farm");
    if (authorizedFarm != sensor.Farm.Name) return Results.Unauthorized();

    sensor.MinTempF = min;
    sensor.MaxTempF = max;
    await dbContext.SaveChangesAsync();
    return Results.Ok(sensor);
}).RequireAuthorization();

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
