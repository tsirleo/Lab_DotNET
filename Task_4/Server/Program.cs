using Microsoft.Extensions.Logging;
using Server.Controllers;
using Server.Database;
using Server.Logger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddMvc();
builder.Services.AddSingleton<IDatabase>(new DatabaseManager());

// Register the Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Version = "v1";
        document.Info.Title = "TsirleoService";
        document.Info.Description = "Service for image processing and storage";
        document.Info.TermsOfService = "None";
    };
});

// Configure logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // if you want server to log in console
//builder.Logging.AddFile(Path.Combine(Directory.GetCurrentDirectory(), "http_logger.log")); // if you want server to log in file

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Register the Swagger generator and the Swagger UI middlewares
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Register the Swagger generator and the Swagger UI middlewares
app.UseOpenApi();
app.UseSwaggerUi3();

app.UseAuthorization();

app.MapControllers();

app.Run();
