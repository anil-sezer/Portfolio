using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapPost("/data", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var data = JsonSerializer.Deserialize<SensorData>(body);

    // You would add your database logic here
    // For now, we just log to the console
    Console.WriteLine($"Temperature: {data?.Temperature}, Humidity: {data?.Humidity}");

    return Results.Ok(new { message = "Data received successfully!" });
});

app.Run();

public class SensorData
{
    public string Temperature { get; set; }
    public string Humidity { get; set; }
}