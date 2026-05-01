using AutoMapper;
using Moq;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;
using TaskManagerApi.Repositories;
using TaskManagerApi.Services;
using Xunit;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _repoMock = new Mock<ITaskRepository>();
        _mapperMock = new Mock<IMapper>();
        _service = new TaskService(_repoMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDataForUser()
    {
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 1, UserId = 42, Title = "Task 1", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Task 2", IsDone = true }
        };
        SetupPagedTasks(tasks, 2);

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "all", "");

        Assert.Equal(2, totalCount);
        Assert.Equal(2, data.Count);
        Assert.All(data, d => Assert.Equal(42, d.UserId));
    }

    [Fact]
    public async Task GetAllAsync_WhenFilterIsActive_ReturnsOnlyIncompleteTasks()
    {
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 1, UserId = 42, Title = "Open", IsDone = false }
        };
        SetupPagedTasks(tasks, 1);

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "active", "");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.False(data[0].IsDone);
    }

    [Fact]
    public async Task GetAllAsync_WhenFilterIsCompleted_ReturnsOnlyCompletedTasks()
    {
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 2, UserId = 42, Title = "Done", IsDone = true }
        };
        SetupPagedTasks(tasks, 1);

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "completed", "");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.True(data[0].IsDone);
    }

    [Fact]
    public async Task GetAllAsync_WhenSearchProvided_FiltersByTitleCaseInsensitively()
    {
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 1, UserId = 42, Title = "Buy Milk", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Write Tests", IsDone = false },
            new TaskItem { Id = 3, UserId = 99, Title = "Buy Milk", IsDone = false }
        };
        SetupPagedTasks(tasks.Where(t => t.UserId == 42 && t.Title == "Buy Milk").ToList(), 1);

        var (data, totalCount) = await _service.GetAllAsync(42, 1, 10, "all", "milk");

        Assert.Equal(1, totalCount);
        Assert.Single(data);
        Assert.Equal("Buy Milk", data[0].Title);
        Assert.Equal(42, data[0].UserId);
    }

    [Fact]
    public async Task GetAllAsync_AppliesPagingAfterCountingMatches()
    {
        var tasks = new List<TaskItem>
        {
            new TaskItem { Id = 1, UserId = 42, Title = "Task 1", IsDone = false },
            new TaskItem { Id = 2, UserId = 42, Title = "Task 2", IsDone = false },
            new TaskItem { Id = 3, UserId = 42, Title = "Task 3", IsDone = false }
        };
        SetupPagedTasks(new List<TaskItem> { tasks[1] }, 3);

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
        _repoMock.Setup(r => r.GetByIdForUserAsync(404, 42)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(404, 42));
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenTaskBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdForUserAsync(1, 42)).ReturnsAsync((TaskItem?)null);

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
        _repoMock.Setup(r => r.GetByIdForUserAsync(1, 42)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(1, dto, 42));
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskDoesNotExist_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdForUserAsync(404, 42)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(404, 42));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTaskBelongsToAnotherUser_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdForUserAsync(1, 42)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByIdAsync(1, 42));
    }

    private void SetupPagedTasks(List<TaskItem> tasks, int totalCount)
    {
        _repoMock
            .Setup(r => r.GetPagedForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((tasks, totalCount));
        SetupTaskListMapping();
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
