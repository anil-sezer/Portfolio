using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.DataAccess;
using Portfolio.Domain.Helpers;
using Portfolio.WebUi.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

using var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog(log);

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddScoped<BackgroundImageFromBingService>();

const string connectionString = "Data Source=WebApp.db";
// var connectionString = "Data Source=/app/Data/WebApp.db"; // todo: Get this from Kubernetes ENV values?

using var connection = new SqliteConnection(connectionString);
connection.Open();

var command = connection.CreateCommand();
command.CommandText =
    @"
CREATE TABLE IF NOT EXISTS Request (
    Id TEXT PRIMARY KEY,
    CreationTime DATETIME NOT NULL,
    UserAgent TEXT NOT NULL,
    AcceptLanguage TEXT NOT NULL,
    ClientIp TEXT NOT NULL,
    DeviceType TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS BingDailyBackground (
    Id TEXT PRIMARY KEY,
    CreationTime DATETIME NOT NULL,
    ImageUrl DATETIME NOT NULL,
    UrlWorks BOOL NOT NULL
);
";
command.ExecuteNonQuery();

builder.Services.AddDbContext<WebAppDbContext>(options =>
    {
        options.UseSqlite(connectionString);
        // .EnableDetailedErrors(); // todo: Add this up, trigger some errors & observe the difference. It should be nicer yea?
        if (EnvironmentHelper.IsDevelopment())
            options
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
                // .AddInterceptors(new TaggedQueryCommandInterceptor());
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();