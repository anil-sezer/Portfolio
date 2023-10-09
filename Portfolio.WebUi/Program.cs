using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.DataAccess;
using Portfolio.Domain.Helpers;
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

using var connection = new SqliteConnection("Data Source=mydb.db");
connection.Open();

var command = connection.CreateCommand();
command.CommandText =
    @"
CREATE TABLE IF NOT EXISTS Request (
    Id TEXT PRIMARY KEY,
    UserAgent TEXT NOT NULL,
    AcceptLanguage TEXT NOT NULL,
    ClientIp TEXT NOT NULL,
    ClientResolution TEXT NOT NULL
);
";
command.ExecuteNonQuery();

builder.Services.AddDbContext<WebAppDbContext>(options =>
    {
        options.UseSqlite("Data Source=mydb.db");
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