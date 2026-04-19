using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TaskManagerApi.Controllers;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;
using TaskManagerApi.Services;
using Xunit;

public class TaskControllerTests
{
    private readonly Mock<ITaskService> _serviceMock;
    private readonly TaskController _controller;

    public TaskControllerTests()
    {
        _serviceMock = new Mock<ITaskService>();
        _controller = new TaskController(_serviceMock.Object);
        SetAuthenticatedUser(42);
    }

    [Fact]
    public async Task Get_PassesAuthenticatedUserIdToService()
    {
        var tasks = new List<TaskDto>
        {
            new TaskDto { Id = 1, UserId = 42, Title = "Task", IsDone = false }
        };

        _serviceMock
            .Setup(s => s.GetAllAsync(42, 2, 5, "active", "task"))
            .ReturnsAsync((tasks, 1));

        var result = await _controller.Get(2, 5, "active", "task");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _serviceMock.Verify(s => s.GetAllAsync(42, 2, 5, "active", "task"), Times.Once);
    }

    [Fact]
    public async Task Post_PassesAuthenticatedUserIdToService()
    {
        var dto = new CreateTaskDto { Title = "New Task" };
        var created = new TaskItem { Id = 10, UserId = 42, Title = "New Task", IsDone = false };

        _serviceMock.Setup(s => s.AddAsync(dto, 42)).ReturnsAsync(created);

        var result = await _controller.Post(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(created, okResult.Value);
        _serviceMock.Verify(s => s.AddAsync(dto, 42), Times.Once);
    }

    [Fact]
    public async Task Delete_PassesAuthenticatedUserIdToServiceAndReturnsOk()
    {
        var result = await _controller.Delete(10);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(10, 42), Times.Once);
    }

    [Fact]
    public async Task Update_PassesAuthenticatedUserIdToServiceAndReturnsOk()
    {
        var dto = new UpdateTaskDto { Id = 10, Title = "Updated", IsDone = true };

        var result = await _controller.Update(10, dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.UpdateAsync(10, dto, 42), Times.Once);
    }

    [Fact]
    public async Task GetById_PassesAuthenticatedUserIdToServiceAndReturnsTask()
    {
        var task = new TaskItem { Id = 10, UserId = 42, Title = "Task", IsDone = false };
        _serviceMock.Setup(s => s.GetByIdAsync(10, 42)).ReturnsAsync(task);

        var result = await _controller.GetById(10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(task, okResult.Value);
        _serviceMock.Verify(s => s.GetByIdAsync(10, 42), Times.Once);
    }

    private void SetAuthenticatedUser(int userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
