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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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
        Version = "v1"
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
});
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

var key = Encoding.UTF8.GetBytes("super_secret_key_12345_very_long_key_123456789");

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
        ValidIssuer = "taskmanager-api",
        ValidAudience = "taskmanager-client",
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.UseDefaultFiles();
app.UseStaticFiles();

//app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }

