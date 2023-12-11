using Portfolio.DataAccess.Helpers;
using Portfolio.Web.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.InitLogsWithSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// todo: Add auto registration for services and integration tests!
builder.Services.AddScoped<GetIotDirectivesService>();
builder.Services.AddScoped<IotReportService>();
builder.Services.AddScoped<SunriseAlarmClockService>();

builder.DbInitWithPostgres();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
