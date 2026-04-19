using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerApi.Dtos;
using TaskManagerApi.Services;

namespace TaskManagerApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pageSize = 5, string filter = "all", string search = "")
        {
            var userId = GetUserId();

            var (data, totalCount) = await _taskService.GetAllAsync(userId, page, pageSize, filter, search);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post(CreateTaskDto dto)
        {
            var userId = GetUserId();

            var result = await _taskService.AddAsync(dto, userId);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            await _taskService.DeleteAsync(id, userId);

            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
        {
            var userId = GetUserId();

            await _taskService.UpdateAsync(id, dto, userId);

            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var task = await _taskService.GetByIdAsync(id, userId);

            return Ok(task);
        }

        private int GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userId);
        }
    }
}
