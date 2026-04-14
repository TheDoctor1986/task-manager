using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;

namespace TaskManagerApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public TaskController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }



        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 5, string filter = "all")
        {
            var query = _context.Tasks.AsQueryable();

            // FILTER
            if (filter == "active")
                query = query.Where(t => !t.IsDone);
            else if (filter == "completed")
                query = query.Where(t => t.IsDone);

            // COUNT 
            var totalCount = await query.CountAsync();

            // DATA (pagination)
            var tasks = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // DTO MAPPING
            var result = _mapper.Map<List<TaskDto>>(tasks);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data = result
            });
        }


        [HttpPost]
        public async Task<IActionResult> Post(CreateTaskDto dto)
        {
            var task = _mapper.Map<TaskItem>(dto);
            task.IsDone = false;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Tasks.FindAsync(id);
            if (item == null) return NotFound();

            _context.Tasks.Remove(item);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id uyuşmuyor");

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return NotFound();

            _mapper.Map(dto, task);

            await _context.SaveChangesAsync();

            return Ok(task);
        }
    }
}
