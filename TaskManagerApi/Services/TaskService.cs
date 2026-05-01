using AutoMapper;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;
using TaskManagerApi.Repositories;

namespace TaskManagerApi.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repository;
        private readonly IMapper _mapper;

        public TaskService(ITaskRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<(List<TaskDto> Data, int TotalCount)> GetAllAsync(int userId, int page, int pageSize, string filter, string search)
        {
            var (items, totalCount) = await _repository.GetPagedForUserAsync(userId, page, pageSize, filter, search);
            var result = _mapper.Map<List<TaskDto>>(items);

            return (result, totalCount);
        }

        public async Task<TaskItem> AddAsync(CreateTaskDto dto, int userId)
        {
            var task = _mapper.Map<TaskItem>(dto);

            task.IsDone = false;
            task.UserId = userId;

            return await _repository.AddAsync(task);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var item = await _repository.GetByIdForUserAsync(id, userId);

            if (item == null)
                throw new KeyNotFoundException("Task not found");

            await _repository.DeleteAsync(id);
        }

        public async Task UpdateAsync(int id, UpdateTaskDto dto, int userId)
        {
            if (id != dto.Id)
                throw new ArgumentException("Id not matching");

            var task = await _repository.GetByIdForUserAsync(id, userId);

            if (task == null)
                throw new KeyNotFoundException("Task not found");

            _mapper.Map(dto, task);

            await _repository.UpdateAsync(task);
        }

        public async Task<TaskItem> GetByIdAsync(int id, int userId)
        {
            var task = await _repository.GetByIdForUserAsync(id, userId);

            if (task == null)
                throw new KeyNotFoundException("Task not found");

            return task;
        }
    }
}
