using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResumeJobMatching.Api.Data;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Services.Auth;
using ResumeJobMatching.Api.Services.Resume;
using ResumeJobMatching.Api.Services.AI;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI
// Register OpenAPI and Swagger JWT authentication
// Register Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ResumeJobMatching API",
        Version = "v1",
        Description = "API for resume management, job listings, and resume-job matching."
    });

    options.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(document =>
    new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

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
builder.Services.AddHttpClient<GeminiAiService>();

// Build application
var app = builder.Build();

// Configure HTTP request pipeline
// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ResumeJobMatching API v1");
        options.RoutePrefix = "swagger";
    });
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