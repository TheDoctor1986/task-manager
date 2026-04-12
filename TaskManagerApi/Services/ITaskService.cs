using TaskManagerApi.Models;

namespace TaskManagerApi.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetAllAsync();
        Task<TaskItem> AddAsync(TaskItem task);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, TaskItem updated);
    }
}
