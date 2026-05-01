using TaskManagerApi.Models;

namespace TaskManagerApi.Repositories
{
    public interface ITaskRepository
    {
        Task<(List<TaskItem> Items, int TotalCount)> GetPagedForUserAsync(
            int userId,
            int page,
            int pageSize,
            string filter,
            string search);

        Task<TaskItem> AddAsync(TaskItem task);

        Task DeleteAsync(int id);

        Task<TaskItem?> GetByIdAsync(int id);

        Task<TaskItem?> GetByIdForUserAsync(int id, int userId);

        Task UpdateAsync(TaskItem task);
    }
}
