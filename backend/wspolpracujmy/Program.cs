using Microsoft.EntityFrameworkCore;
using wspolpracujmy.Data;
using wspolpracujmy.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSwaggerGen();

// Register EF Core AppDbContext (Postgres)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<wspolpracujmy.Services.ProjectService>();
builder.Services.AddScoped<wspolpracujmy.Services.ProjectCommentService>();
builder.Services.AddScoped<wspolpracujmy.Services.NotificationService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    // Seed development data (runs only in Development)
    await TestDataSeeder.SeedAsync(app);
    // app.UseHttpsRedirection();
}

// app.Urls.Add("http://+:8080");  // Lub builder.WebHost.UseUrls("http://+:8080"); przed Build()
app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
