// -------------------------------------------------------------------------
// basic stuff for serving from wwwroot directory
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8000);
});
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
// don't forget app.Run(); at the bottom
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// get requests with query parameters 
// app.MapGet("/", () => "Hello World!");
app.MapGet("/custom-path", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.WebRootPath, "index.html");
    return Results.File(path, "text/html");
});

app.MapGet("/example", (string param1, string param2) =>
{
    // Do something with the parameters
    return Results.Ok(new { param1, param2 });
});
// -------------------------------------------------------------------------


// -------------------------------------------------------------------------
// post requests read the body into a class automatically
// See below for how to call this from the client and how the data is stored
app.MapPost("/submit", (MyData data) =>
{
    // Do something with the data
    // data.Property1 and data.Property2 will be populated with the JSON body values
    return Results.Ok(new { property1 = data.Property1 });
});
// -------------------------------------------------------------------------

Console.WriteLine("Running on Port 8000");
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

public class MyData
{
    public string? Property1 { get; set; }
    public int? Property2 { get; set; }
}


