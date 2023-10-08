using Nest;
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

var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("my_default_index");
var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();