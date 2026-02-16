using DashboardApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient<DashboardService>();
builder.Services.AddHttpClient<ConfluenceService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ConfluenceService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteApp",
        builder =>
        {
            builder.WithOrigins("http://localhost:5173") // Adjust the port if your Vite app runs on a different one
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi(); // This can be uncommented if you want to use OpenAPI
}

app.UseHttpsRedirection();
app.UseCors("AllowViteApp");
app.MapControllers();

app.Run();
