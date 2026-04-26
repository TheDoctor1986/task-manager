using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagerApi.Middleware;
using TaskManagerApi.Middleware.TaskManagerApi.Middleware;
using TaskManagerApi.Repositories;
using TaskManagerApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TaskManagerApi.Options;
using System.Reflection;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
    Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? ".", "LogFiles", "app-.txt"),
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7,
    fileSizeLimitBytes: 5_000_000, // 5 MB Log File Size Limit
    rollOnFileSizeLimit: true,     // New File When Size Limit Is Reached
    encoding: new System.Text.UTF8Encoding(true)
).CreateLogger();
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("Jwt settings must be configured.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

builder.Host.UseSerilog();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskManager API",
        Version = "v1",
        Description = "Task management API with JWT authentication and user-scoped task access."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token} ÅŸeklinde gir"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.Use(async (context, next) =>
{
    var correlationId = context.Items["CorrelationId"]?.ToString();

    Log.Information("[{CorrelationId}] â¡ï¸ {Method} {Path}",
        correlationId,
        context.Request.Method,
        context.Request.Path);

    await next();

    Log.Information("[{CorrelationId}] â¬…ï¸ {StatusCode}",
        correlationId,
        context.Response.StatusCode);
});

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.UseDefaultFiles();
app.UseStaticFiles();

//app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }

