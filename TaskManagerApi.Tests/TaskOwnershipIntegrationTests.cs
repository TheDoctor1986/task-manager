using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagerApi.Dtos;
using Xunit;

public class TaskOwnershipIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskOwnershipIntegrationTests()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=TaskManagerApiTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "taskmanager-api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "taskmanager-client");
        Environment.SetEnvironmentVariable("Jwt__Key", "test_only_jwt_key_for_integration_tests_12345");

        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetById_WhenTaskBelongsToAnotherUser_DoesNotReturnOk()
    {
        var taskId = await CreateTaskForFirstUserAsync();
        await AuthenticateSecondUserAsync();

        var response = await _client.GetAsync($"/api/v1/Task/{taskId}");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenTaskBelongsToAnotherUser_DoesNotReturnOk()
    {
        var taskId = await CreateTaskForFirstUserAsync();
        await AuthenticateSecondUserAsync();

        var response = await _client.PutAsJsonAsync($"/api/v1/Task/{taskId}", new UpdateTaskDto
        {
            Id = taskId,
            Title = "Updated by another user",
            IsDone = true
        });

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenTaskBelongsToAnotherUser_DoesNotReturnOk()
    {
        var taskId = await CreateTaskForFirstUserAsync();
        await AuthenticateSecondUserAsync();

        var response = await _client.DeleteAsync($"/api/v1/Task/{taskId}");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<int> CreateTaskForFirstUserAsync()
    {
        await RegisterAndLoginAsync("first@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Task", new CreateTaskDto
        {
            Title = "First user's task"
        });

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private async Task AuthenticateSecondUserAsync()
    {
        await RegisterAndLoginAsync("second@example.com", "Password123!");
    }

    private async Task RegisterAndLoginAsync(string email, string password)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = email,
            Password = password
        });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
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
