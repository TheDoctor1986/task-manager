namespace TaskManagerApi.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using TaskManagerApi.Models;

    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<TaskItem> Items, int TotalCount)> GetPagedForUserAsync(
            int userId,
            int page,
            int pageSize,
            string filter,
            string search)
        {
            var query = _context.Tasks.Where(t => t.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(lowerSearch));
            }

            if (filter == "active")
                query = query.Where(t => !t.IsDone);
            else if (filter == "completed")
                query = query.Where(t => t.IsDone);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<TaskItem> AddAsync(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task DeleteAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            return await _context.Tasks.FindAsync(id);
        }

        public async Task<TaskItem?> GetByIdForUserAsync(int id, int userId)
        {
            return await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task UpdateAsync(TaskItem task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }

    }
}
