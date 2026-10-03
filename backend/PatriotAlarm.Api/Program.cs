using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Data;
using PatriotAlarm.Api.Domain;
using PatriotAlarm.Api.Hubs;
using PatriotAlarm.Api.Receiver;
using PatriotAlarm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=patriot-alarm.db"));

builder.Services.AddScoped<AlarmService>();
builder.Services.AddHostedService<AlarmReceiverService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Send enums as text, e.g. "High" or "EnRoute", so the frontend reads them easily
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedDemoData(db);
}

// Turn domain errors into HTTP status codes
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (KeyNotFoundException ex)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.MapControllers();
app.MapHub<AlarmHub>("/hubs/alarms");

app.Run();

static void SeedDemoData(AppDbContext db)
{
    if (db.Sites.Any())
    {
        return;
    }

    // Fictional demo accounts for local testing only
    db.Sites.AddRange(
        new Site { AccountNumber = "1001", Name = "Demo Medical Centre", Address = "1 Example Street, Kaiapoi" },
        new Site { AccountNumber = "1002", Name = "Demo Warehouse", Address = "22 Sample Road, Rangiora" },
        new Site { AccountNumber = "1003", Name = "Demo Primary School", Address = "5 Placeholder Avenue, Woodend" });
    db.SaveChanges();
}
