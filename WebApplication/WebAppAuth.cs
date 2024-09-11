namespace TempSensorDB.WebApplication;

using System.Text;

using Microsoft.EntityFrameworkCore;
using TempSensorDB.Models;
using TempSensorDB.Models.DataTransfer;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

public class WebAppAuth
{

    public static void AddAuthenticationService(
        WebApplicationBuilder builder,
        ConfigurationManager conf
    )
    {
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
    }


    public static string GenerateJWT(Farm userInfo, IConfiguration conf)
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


    public static bool IsUserAuthorized(SensorDbContext dbContext, Farm user)
    {
        Farm? match = dbContext.Farms
            .FirstOrDefault(f => f.FarmID == user.FarmID);
        if (match == null) return false;
        return match.Name == user.Name && match.Password == user.Password;
    }


    public static IResult? FailIfUnauthorized(SensorDbContext dbContext, ReadingDTO reading)
    {
        Sensor? sensor = dbContext.Sensors.Include(s => s.Farm)
            .FirstOrDefault(s => s.SensorID == reading.SensorID);
        if (sensor == null) return Results.BadRequest("No such sensor");
        Farm user = new()
        {
            FarmID = sensor.Farm.FarmID,
            Name = sensor.Farm.Name,
            Password = reading.Password,
        };
        if (!WebAppAuth.IsUserAuthorized(dbContext, user)) return Results.Unauthorized();
        return null;
    }

    public static IResult? FailIfUnauthorized(SensorDbContext dbContext, HeartbeatDTO reading)
    {
        Sensor? sensor = dbContext.Sensors.Include(s => s.Farm)
            .FirstOrDefault(s => s.SensorID == reading.SensorID);
        if (sensor == null) return Results.BadRequest("No such sensor");
        Farm user = new()
        {
            FarmID = sensor.Farm.FarmID,
            Name = sensor.Farm.Name,
            Password = reading.Password,
        };
        if (!WebAppAuth.IsUserAuthorized(dbContext, user)) return Results.Unauthorized();
        return null;
    }
}