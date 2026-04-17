using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManagerApi.Dtos;
using TaskManagerApi.Models;
using TaskManagerApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            await _taskService.DeleteAsync(id);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
        {
            await _taskService.UpdateAsync(id, dto);
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var task = await _taskService.GetByIdAsync(id);

            return Ok(task);
        }

        private int GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userId);
        }
    }
}
