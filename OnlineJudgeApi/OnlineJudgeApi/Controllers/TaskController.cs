using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeApi.Dtos;
using OnlineJudgeApi.Helpers;

namespace OnlineJudgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly DataContext _context;
        private IMapper mapper;

        public TaskController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // GET: api/Task
        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var tasks = await _context.Tasks.Include(t => t.User).ToListAsync();
            var taskDtos = mapper.Map<IList<TaskDto>>(tasks);
            return Ok(taskDtos);
        }

        // GET: api/Task/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            _context.Entry(task).Reference(t => t.User).Load();

            TaskDto dto = mapper.Map<TaskDto>(task);

            return dto;
        }

        // PUT: api/Task/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask(int id, Entities.Task task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            if (task.UserId != userId)
            {
                // task doesn't belong to current user, edit not allowed
                return Unauthorized();
            }

            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Task
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<TaskDto>> PostTask([FromBody]TaskDto taskDto)
        {
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            Entities.Task task = mapper.Map<Entities.Task>(taskDto);
            task.UserId = userId;
            task.TimeSubmitted = DateTime.Now;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var dto = mapper.Map<TaskDto>(task);

            return CreatedAtAction("GetTask", new { id = task.Id }, dto);
        }

        // DELETE: api/Task/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            if (task.UserId != userId)
            {
                // task doesn't belong to current user, deletion not allowed
                return Unauthorized();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
