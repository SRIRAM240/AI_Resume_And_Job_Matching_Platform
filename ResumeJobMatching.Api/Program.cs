using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Services.Auth;
using ResumeJobMatching.Api.Services.Resume;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI
builder.Services.AddOpenApi();

// Register SQL Server and Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register password hashing
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Register authentication service
builder.Services.AddScoped<AuthService>();

// Read JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is missing. Configure Jwt:Key, Jwt:Issuer, and Jwt:Audience.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

// Configure JWT bearer authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });

// Register authorization
builder.Services.AddAuthorization();

// Register controllers
builder.Services.AddControllers();
builder.Services.AddScoped<ResumeTextExtractor>();
builder.Services.AddScoped<ResumeService>();
builder.Services.AddScoped<ResumeJobMatching.Api.Services.Jobs.JobService>();
builder.Services.AddScoped<ResumeJobMatching.Api.Services.Matching.ResumeMatchingService>();

// Build application
var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Temporary health-check endpoint
app.MapGet("/", () => Results.Ok(new
{
    Message = "Resume & Job Matching API is running!"
}));

app.Run();
public partial class Program { }