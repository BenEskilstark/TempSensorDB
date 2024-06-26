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
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlServer(conf.GetConnectionString("MSSQLConnection")));
// builder.Services.AddDbContext<TempSensorDbContext>(options =>
//         options.UseNpgsql(conf.GetConnectionString("postgresConnection")));

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

bool IsUserValid(SensorDbContext dbContext, Farm user)
{
    Farm? match = dbContext.Farms
        .FirstOrDefault(f => f.FarmID == user.FarmID);
    if (match == null) return false;
    return match.Name == user.Name && match.Password == user.Password;
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

app.MapGet("/farms", async (SensorDbContext dbContext) =>
{
    List<FarmDTO> farms = dbContext.Farms
        .Select(f => new FarmDTO()
        {
            FarmID = f.FarmID,
            Name = f.Name,
        }).ToList();
    return Results.Json(farms, jsonOptions);
});

app.MapGet("/sensor/{id}", async (SensorDbContext dbContext, int id) =>
{
    var sensor = await dbContext.Sensors
        .Include(s => s.Readings)
        .FirstOrDefaultAsync(s => s.SensorID == id);
    if (sensor == null)
    {
        return Results.NotFound(new { Message = $"Sensor with ID {id} not found." });
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
        .Select(s => new SensorDTO()
        {
            SensorID = s.SensorID,
            Name = s.Name,
            CalibrationValueF = s.CalibrationValueF,
            LastTempF = s.Readings.Select(r => r.TempF).LastOrDefault(),
            LastTimeStamp = s.Readings.Count != 0
                ? DateTime.SpecifyKind(s.Readings.Last().TimeStamp, DateTimeKind.Utc)
                : null,
        });
    return Results.Json(sensorDTOs, jsonOptions);
});
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// post requests 
app.MapPost("/token", async (SensorDbContext dbContext, Farm user) =>
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
    async (HttpContext httpContext, SensorDbContext dbContext, Reading reading) =>
{
    // authorization
    Sensor? sensor = dbContext.Sensors.Include(s => s.Farm)
        .FirstOrDefault(s => s.SensorID == reading.SensorID);
    if (sensor == null) return Results.BadRequest("No such sensor");
    string? authorizedFarm = httpContext.User.FindFirstValue("Farm");
    if (authorizedFarm != sensor.Farm.Name) return Results.Unauthorized();

    dbContext.Readings.Add(reading);
    await dbContext.SaveChangesAsync();
    return Results.Json(reading, jsonOptions);
}).RequireAuthorization();

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

public class FarmDTO
{
    public int FarmID { get; set; }
    public string Name { get; set; }
}

public class SensorDTO
{
    public int SensorID { get; set; }
    public required string Name { get; set; }
    public double CalibrationValueF { get; set; } = 0;
    public double? LastTempF { get; set; }
    public DateTime? LastTimeStamp { get; set; }
}

