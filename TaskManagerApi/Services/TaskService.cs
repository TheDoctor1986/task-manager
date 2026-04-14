using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        public async Task<(List<TaskDto> Data, int TotalCount)> GetAllAsync(int page, int pageSize, string filter, string search)
        {
            var query = _repository.Query(); 

            // SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(lowerSearch));
            }

            // FILTER
            if (filter == "active")
                query = query.Where(t => !t.IsDone);
            else if (filter == "completed")
                query = query.Where(t => t.IsDone);

            var totalCount = await query.CountAsync();

            var tasks = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = _mapper.Map<List<TaskDto>>(tasks);

            return (result, totalCount);
        }

        public async Task<TaskItem> AddAsync(CreateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Görev başlığı boş olamaz");

            var task = _mapper.Map<TaskItem>(dto);
            task.IsDone = false;

            return await _repository.AddAsync(task);
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
                throw new KeyNotFoundException("Task bulunamadı");

            await _repository.DeleteAsync(id);
        }

        public async Task UpdateAsync(int id, UpdateTaskDto dto)
        {
            if (id != dto.Id)
                throw new ArgumentException("Id uyuşmuyor");

            var task = await _repository.GetByIdAsync(id);

            if (task == null)
                throw new Exception("Task bulunamadı");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Görev başlığı boş olamaz");

            // mapping
            _mapper.Map(dto, task);

            await _repository.UpdateAsync(task);
        }
    }
}
