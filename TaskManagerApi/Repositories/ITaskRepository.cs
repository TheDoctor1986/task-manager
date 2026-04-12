using TaskManagerApi.Models;

namespace TaskManagerApi.Repositories
{
        public interface ITaskRepository
        {
            Task<List<TaskItem>> GetAllAsync();
            Task<TaskItem> AddAsync(TaskItem task);
            Task DeleteAsync(int id);
            Task<TaskItem?> GetByIdAsync(int id);
            Task UpdateAsync(TaskItem task);
        }
}
