using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Soo_artist.Entities;
using CloudinaryDotNet;
using Soo_artist.Services;
using Soo_artist.Middleware;

var builder = WebApplication.CreateBuilder(args);

// get info from appsettings
var cloudName = builder.Configuration["CloudinarySettings:CloudName"];
var apiKey = builder.Configuration["CloudinarySettings:ApiKey"];
var apiSecret = builder.Configuration["CloudinarySettings:ApiSecret"];

// Configure Cloudinary
Account account = new Account(cloudName, apiKey, apiSecret);
Cloudinary cloudinary = new Cloudinary(account);

// Register as a Singleton service
builder.Services.AddSingleton(cloudinary);

// Register Cloudinary Service
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// configure database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();

// Configure built-in OpenAPI support
builder.Services.AddOpenApi();

// Configure Swagger/Swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Soo Artist API",
        Version = "v1",
        Description = "API for the Soo Artist application"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication using the X-API-Key header. Enter your API Key in the box below.",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",
        In = ParameterLocation.Header
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", doc),
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Soo Artist API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Add ApiKey authentication middleware
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
