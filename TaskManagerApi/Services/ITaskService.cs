using TaskManagerApi.Dtos;
using TaskManagerApi.Models;

namespace TaskManagerApi.Services
{
    public interface ITaskService
    {
        Task<(List<TaskDto> Data, int TotalCount)> GetAllAsync(int page, int pageSize, string filter, string search);
        Task<TaskItem> AddAsync(CreateTaskDto dto);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, UpdateTaskDto dto);
        Task<TaskItem> GetByIdAsync(int id);
    }
}
