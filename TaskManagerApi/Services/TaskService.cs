using TaskManagerApi.Models;
using TaskManagerApi.Repositories;

namespace TaskManagerApi.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;

        public TaskService(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _repository.GetAllAsync(); 
        }

        public async Task<TaskItem> AddAsync(TaskItem task)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
                throw new Exception("Task boş olamaz");

            return await _repository.AddAsync(task);
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task UpdateAsync(int id, TaskItem updated)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                throw new Exception("Task bulunamadı");

            existing.Title = updated.Title;
            existing.IsDone = updated.IsDone;

            await _repository.UpdateAsync(existing);
        }
    }
}
