using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using smart_receipt_api;
using smart_receipt_api.Repositories;
using smart_receipt_api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddHttpClient<IOcrService, OcrService>();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "your-secret-key-must-be-at-least-32-characters-long-here");

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        builder =>
        {
            builder
                .WithOrigins(
                    "http://localhost:5007",
                    "http://localhost:5001",
                    "https://localhost:7001",
                    "https://localhost:7118",
                    "http://10.0.2.2:5069",
                    "https://10.0.2.2:7018"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

    // Mobil uygulamalar için daha esnek policy
    options.AddPolicy("AllowMobile",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

// Başlangıçta pending migration'ları otomatik uygula (tablo yoksa oluşturur)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for mobile dev (self-signed cert causes fetch to fail)

app.UseCors("AllowMobile");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
