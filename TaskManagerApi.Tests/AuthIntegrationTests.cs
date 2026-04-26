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

public class AuthIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=TaskManagerApiTests;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "taskmanager-api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "taskmanager-client");
        Environment.SetEnvironmentVariable("Jwt__Key", "test_only_jwt_key_for_integration_tests_12345");

        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenUserIsValid_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/register", new RegisterDto
        {
            Email = "register@example.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsToken()
    {
        await RegisterAsync("login@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = "login@example.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = "missing@example.com",
            Password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ReturnsUnauthorized()
    {
        await RegisterAsync("wrong-password@example.com", "Password123!");

        var response = await _client.PostAsJsonAsync("/api/v1/Auth/login", new LoginDto
        {
            Email = "wrong-password@example.com",
            Password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/Task")]
    [InlineData("GET", "/api/v1/Task/1")]
    [InlineData("POST", "/api/v1/Task")]
    [InlineData("PUT", "/api/v1/Task/1")]
    [InlineData("DELETE", "/api/v1/Task/1")]
    public async Task TaskEndpoints_WhenTokenIsMissing_ReturnUnauthorized(string method, string url)
    {
        var response = await SendTaskRequestAsync(method, url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WhenTokenIsValid_ReturnsOk()
    {
        await AuthenticateAsync("authorized@example.com", "Password123!");

        var response = await _client.GetAsync("/api/v1/Task");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_WhenTokenIsInvalid_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await _client.GetAsync("/api/v1/Task");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<HttpResponseMessage> SendTaskRequestAsync(string method, string url)
    {
        return method switch
        {
            "GET" => await _client.GetAsync(url),
            "POST" => await _client.PostAsJsonAsync(url, new CreateTaskDto { Title = "New task" }),
            "PUT" => await _client.PutAsJsonAsync(url, new UpdateTaskDto { Id = 1, Title = "Updated task", IsDone = true }),
            "DELETE" => await _client.DeleteAsync(url),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
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
