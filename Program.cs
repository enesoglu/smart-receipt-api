using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using smart_receipt_api;
using smart_receipt_api.Repositories;
using smart_receipt_api.Services;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddHttpClient<IOcrService, OcrService>();
builder.Services.Configure<DocumentIntelligenceOptions>(
    builder.Configuration.GetSection("DocumentIntelligence"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var issuer = jwtSettings["Issuer"] ?? "smart-receipt-api";
var audience = jwtSettings["Audience"] ?? "smart-receipt-users";
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "REPLACE_WITH_AT_LEAST_32_CHAR_RANDOM_SECRET");

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        policy =>
        {
            policy
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

    options.AddPolicy("AllowMobile",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

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

app.UseCors("AllowMobile");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
