using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagerApi.Dtos;
using Xunit;

public class ValidationIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ValidationIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        await RegisterAsync("duplicate@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("missing-password@example.com", "")]
    [InlineData("short-password@example.com", "123")]
    public async Task Register_WhenPayloadIsInvalid_ReturnsBadRequest(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("missing-password@example.com", "")]
    public async Task Login_WhenPayloadIsInvalid_ReturnsBadRequest(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTask_WhenTitleIsInvalid_ReturnsBadRequest(string title)
    {
        await AuthenticateAsync("create-validation@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Task", new CreateTaskDto
        {
            Title = title
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WhenTitleIsTooLong_ReturnsBadRequest()
    {
        await AuthenticateAsync("long-create-validation@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Task", new CreateTaskDto
        {
            Title = new string('a', 101)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WhenTitleIsInvalid_ReturnsBadRequest()
    {
        var taskId = await CreateTaskAsync("update-validation@example.com", "Password123!");

        var response = await _client.PutAsJsonAsync($"/api/v1/Task/{taskId}", new UpdateTaskDto
        {
            Id = taskId,
            Title = "",
            IsDone = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<int> CreateTaskAsync(string email, string password)
    {
        await AuthenticateAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/v1/Task", new CreateTaskDto
        {
            Title = "Valid task"
        });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private async Task RegisterAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task AuthenticateAsync(string email, string password)
    {
        await RegisterAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
