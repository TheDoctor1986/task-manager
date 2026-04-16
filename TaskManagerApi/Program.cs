using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TaskManagerApi.Middleware;
using TaskManagerApi.Repositories;
using TaskManagerApi.Services;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? ".", "LogFiles", "app-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        encoding: System.Text.Encoding.UTF8
    )
    .CreateLogger();
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
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

app.Use(async (context, next) =>
{
    Log.Information("➡️ {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    Log.Information("⬅️ {StatusCode}", context.Response.StatusCode);
});
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    c.RoutePrefix = "swagger";
});


app.UseAuthorization();
app.MapControllers();


app.UseDefaultFiles();
app.UseStaticFiles();

//app.MapFallbackToFile("index.html");

app.Run();