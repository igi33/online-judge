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
using OnlineJudgeApi.Entities;
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

            await _context.Entry(task).Reference(t => t.User).LoadAsync();

            if (User.Identity.IsAuthenticated)
            {
                // Fetch current user id
                int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

                // Load test cases only if they belong to current user
                if (task.UserId == userId)
                {
                    await _context.Entry(task).Collection(t => t.TestCases).LoadAsync();
                }
            }

            TaskDto dto = mapper.Map<TaskDto>(task);

            return dto;
        }

        // POST: api/Task
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<TaskDto>> PostTask([FromBody]TaskDto taskDto)
        {
            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            Entities.Task task = mapper.Map<Entities.Task>(taskDto);
            task.UserId = userId;
            task.TimeSubmitted = DateTime.Now;

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            TaskDto dto = mapper.Map<TaskDto>(task);

            return CreatedAtAction("GetTask", new { id = task.Id }, dto);
        }

        // PUT: api/Task/5
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask(int id, [FromBody]TaskDto taskDto)
        {
            if (id != taskDto.Id)
            {
                return BadRequest();
            }

            Entities.Task task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }


            await _context.Entry(task).Reference(t => t.User).LoadAsync();
            await _context.Entry(task).Collection(t => t.TestCases).LoadAsync();

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            if (task.UserId != userId)
            {
                // Task doesn't belong to current user, edit not allowed
                return Unauthorized();
            }

            // Update fields according to DTO
            // TODO: Validate
            task.Name = taskDto.Name;
            task.Description = taskDto.Description;
            task.MemoryLimit = taskDto.MemoryLimit;
            task.TimeLimit = taskDto.TimeLimit;
            task.Origin = taskDto.Origin;

            // Handle test cases
            int tcDiff = taskDto.TestCases.Count - task.TestCases.Count;
            int tcCountMin = Math.Min(taskDto.TestCases.Count, task.TestCases.Count);

            // Modify existing test cases
            for (int i = 0; i < tcCountMin; ++i)
            {
                task.TestCases.ElementAt(i).Input = taskDto.TestCases.ElementAt(i).Input;
                task.TestCases.ElementAt(i).Output = taskDto.TestCases.ElementAt(i).Output;
            }

            if (tcDiff > 0)
            {
                // Add new test cases if needed
                for (int j = 0; j < tcDiff; ++j)
                {
                    task.TestCases.Add(new TestCase
                    {
                        Input = taskDto.TestCases.ElementAt(tcCountMin + j).Input,
                        Output = taskDto.TestCases.ElementAt(tcCountMin + j).Output
                    });
                }
            }
            else if (tcDiff < 0)
            {
                // Delete excess test cases if needed
                int numToDelete = -tcDiff;

                for (int j = 0; j < numToDelete; ++j)
                {
                    task.TestCases.Remove(task.TestCases.Last());
                }
            }

            // Save changes
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

        // DELETE: api/Task/5
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            Entities.Task task = await _context.Tasks.FindAsync(id);
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
