using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TempSensorDB.Models;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// -------------------------------------------------------------------------
// Configure the application builder
var builder = WebApplication.CreateBuilder(args);
var conf = builder.Configuration;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8000);
});

// configure DB connection:
// builder.Services.AddDbContext<TempSensorDbContext>(options =>
//     options.UseSqlServer(conf.GetConnectionString("MSSQLConnection")));
builder.Services.AddDbContext<TempSensorDbContext>(options =>
        options.UseNpgsql(conf.GetConnectionString("postgresConnection")));

var jsonOptions = new JsonSerializerOptions
{
    // will put null instead of trying to serialize a cyclical reference
    // Other option is "Preserve" which will give a $refID back to the reference
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// JWT Authentication and Authorization

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = conf["Jwt:Issuer"],
        ValidAudience = conf["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(conf["Jwt:Key"]))
    };
});

string GenerateJWT(Farm userInfo, IConfiguration conf)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(conf["Jwt:Key"]));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userInfo.Name),
        new Claim("Farm", userInfo.Name),
    };

    var token = new JwtSecurityToken(
        issuer: conf["Jwt:Issuer"],
        audience: conf["Jwt:Audience"],
        claims: claims,
        expires: DateTime.MaxValue, // never expire
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

bool IsUserValid(TempSensorDbContext dbContext, Farm user)
{
    if (user.Password == "foo") return true;
    return false;
}
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// Build the application
WebApplication app = builder.Build();
// basic stuff for serving from wwwroot directory
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
// don't forget app.Run(); at the bottom
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// get requests with query parameters 

app.MapGet("/locations", async (TempSensorDbContext dbContext, int farmID) =>
{
    var locations = await dbContext.Locations
        .Where(l => l.FarmID == farmID)
        .ToListAsync();
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
                FarmID = s.Location.FarmID,
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
app.MapPost("/token", async (TempSensorDbContext dbContext, Farm user) =>
{
    if (IsUserValid(dbContext, user))
    {
        var tokenString = GenerateJWT(user, conf);
        return Results.Ok(new { Token = tokenString });
    }
    else
    {
        return Results.Unauthorized();
    }
});

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
    return Results.Ok(reading);
})
.RequireAuthorization();


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
    public int FarmID { get; set; }
    public string Name { get; set; }
    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }
}
