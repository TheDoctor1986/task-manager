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

        /// <summary>
        /// Returns the authenticated user's tasks with paging, filter, and search support.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="filter">Task filter: all, active, or completed.</param>
        /// <param name="search">Optional title search text.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponseDto<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Creates a task for the authenticated user.
        /// </summary>
        /// <param name="dto">Task creation payload.</param>
        [HttpPost]
        [ProducesResponseType(typeof(Models.TaskItem), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Post(CreateTaskDto dto)
        {
            var userId = GetUserId();

            var result = await _taskService.AddAsync(dto, userId);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a task owned by the authenticated user.
        /// </summary>
        /// <param name="id">Task identifier.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();

            await _taskService.DeleteAsync(id, userId);

            return Ok();
        }

        /// <summary>
        /// Updates a task owned by the authenticated user.
        /// </summary>
        /// <param name="id">Task identifier.</param>
        /// <param name="dto">Task update payload.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
        {
            var userId = GetUserId();

            await _taskService.UpdateAsync(id, dto, userId);

            return Ok();
        }

        /// <summary>
        /// Returns a task by id when it belongs to the authenticated user.
        /// </summary>
        /// <param name="id">Task identifier.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Models.TaskItem), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
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
