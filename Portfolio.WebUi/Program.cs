using Portfolio.Domain.Helpers;
using Portfolio.WebUi;
using Portfolio.WebUi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.InitLogsWithSerilog();

if (EnvironmentHelper.IsDevelopment())
    builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
else
    builder.Services.AddRazorPages();

builder.Services.AddScoped<BackgroundImageFromBingService>();

builder.Services.DbInitWithPostgres();

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


try
{
    Log.Information("App Starting.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The app failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
