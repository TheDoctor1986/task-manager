using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;
using TaskManagerApi.Repositories;
using TaskManagerApi.Services;
using Xunit;

public class TaskServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly Mock<ITaskRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _repoMock = new Mock<ITaskRepository>();
        _mapperMock = new Mock<IMapper>();
        _service = new TaskService(_repoMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDataForUser()
    {
        await SeedTasksAsync(
            new TaskItem { Id = 1, UserId = 42, Title = "Task 1", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Task 2", IsDone = true },
            new TaskItem { Id = 3, UserId = 99, Title = "Other user task", IsDone = false });

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "all", "");

        Assert.Equal(2, totalCount);
        Assert.Equal(2, data.Count);
        Assert.All(data, d => Assert.Equal(42, d.UserId));
    }

    [Fact]
    public async Task GetAllAsync_WhenFilterIsActive_ReturnsOnlyIncompleteTasks()
    {
        await SeedTasksAsync(
            new TaskItem { Id = 1, UserId = 42, Title = "Open", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Done", IsDone = true });

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "active", "");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.False(data[0].IsDone);
    }

    [Fact]
    public async Task GetAllAsync_WhenFilterIsCompleted_ReturnsOnlyCompletedTasks()
    {
        await SeedTasksAsync(
            new TaskItem { Id = 1, UserId = 42, Title = "Open", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Done", IsDone = true });

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "completed", "");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.True(data[0].IsDone);
    }

    [Fact]
    public async Task GetAllAsync_WhenSearchProvided_FiltersByTitleCaseInsensitively()
    {
        await SeedTasksAsync(
            new TaskItem { Id = 1, UserId = 42, Title = "Buy Milk", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Write Tests", IsDone = false },
            new TaskItem { Id = 3, UserId = 99, Title = "Buy Milk", IsDone = false });

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "all", "milk");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.Equal("Buy Milk", data[0].Title);
        Assert.Equal(42, data[0].UserId);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagingAfterCountingMatches()
    {
        await SeedTasksAsync(
            new TaskItem { Id = 1, UserId = 42, Title = "Task 1", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Task 2", IsDone = false },
            new TaskItem { Id = 3, UserId = 42, Title = "Task 3", IsDone = false });

        var (data, totalCount) = await _service.GetAllAsync(42, 2, 1, "all", "");

        Assert.Equal(3, totalCount);
        Assert.Single(data);
        Assert.Equal(2, data[0].Id);
    }

    [Fact]
    public async Task AddAsync_ShouldSetUserIdAndReturnTask()
    {
        var dto = new CreateTaskDto { Title = "New Task" };
        var taskItem = new TaskItem { Id = 5, Title = "New Task" };

        _mapperMock.Setup(m => m.Map<TaskItem>(dto)).Returns(taskItem);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem task) => task);

        var result = await _service.AddAsync(dto, 42);

        Assert.Equal(42, result.UserId);
        Assert.False(result.IsDone);
        Assert.Equal("New Task", result.Title);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskDoesNotExist_ThrowsKeyNotFoundException()
    {
        SetupQueryableTasks();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(404, 42));
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        SetupQueryableTasks(new TaskItem { Id = 1, UserId = 99, Title = "Other", IsDone = false });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(1, 42));
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRouteIdDoesNotMatchDtoId_ThrowsArgumentException()
    {
        var dto = new UpdateTaskDto { Id = 2, Title = "Mismatch", IsDone = true };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, dto, 42));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        var dto = new UpdateTaskDto { Id = 1, Title = "Updated", IsDone = true };
        SetupQueryableTasks(new TaskItem { Id = 1, UserId = 99, Title = "Other", IsDone = false });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(1, dto, 42));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ThrowsKeyNotFoundException()
    {
        SetupQueryableTasks();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(404, 42));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        SetupQueryableTasks(new TaskItem { Id = 1, UserId = 99, Title = "Other", IsDone = false });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(1, 42));
    }

    private async Task SeedTasksAsync(params TaskItem[] tasks)
    {
        var context = new AppDbContext(_dbOptions);
        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();

        _repoMock.Setup(r => r.Query()).Returns(context.Tasks);
        SetupTaskListMapping();
    }

    private void SetupQueryableTasks(params TaskItem[] tasks)
    {
        var context = new AppDbContext(_dbOptions);
        context.Tasks.AddRange(tasks);
        context.SaveChanges();

        _repoMock.Setup(r => r.Query()).Returns(context.Tasks);
    }

    private void SetupTaskListMapping()
    {
        _mapperMock
            .Setup(m => m.Map<List<TaskDto>>(It.IsAny<List<TaskItem>>()))
            .Returns((List<TaskItem> source) => source.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                IsDone = t.IsDone,
                UserId = t.UserId
            }).ToList());
    }
}
