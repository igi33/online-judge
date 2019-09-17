using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Get all tasks tagged as tagId
        // GET: api/Task/tag/6
        [HttpGet("tag/{tagId}")]
        public async Task<IActionResult> GetTasksByTag(int tagId)
        {
            var tag = await _context.Tags.FindAsync(tagId);

            if (tag == null)
            {
                return BadRequest();
            }

            var tasks = await _context.Tasks.Where(t => t.TaskTags.Any(tt => tt.TagId == tagId)).Include(t => t.User).ToListAsync();
            var taskDtos = mapper.Map<IList<TaskDto>>(tasks);
            return Ok(taskDtos);
        }

        // Get list of solved tasks by user
        // GET: api/Task/solved/user/5
        [HttpGet("solved/user/{userId}")]
        public async Task<IActionResult> GetSolvedByUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return BadRequest();
            }

            var acceptedSubmissionsByUser = await _context.Submissions.Where(s => s.UserId == userId && s.Status.Equals("AC")).Include(s => s.Task).ToListAsync();

            HashSet<int> taskIds = new HashSet<int>(); // set of solved task IDs
            List<TaskDto> solvedTaskDtos = new List<TaskDto>();

            foreach (Submission submission in acceptedSubmissionsByUser)
            {
                if (!taskIds.Contains(submission.TaskId))
                {
                    taskIds.Add(submission.TaskId);
                    TaskDto dto = mapper.Map<TaskDto>(submission.Task);
                    solvedTaskDtos.Add(dto);
                }
            }

            return Ok(solvedTaskDtos);
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

            // Load tags and insert to DTO
            await _context.Entry(task).Collection(t => t.TaskTags).LoadAsync();
            foreach (TaskTag tt in task.TaskTags)
            {
                await _context.Entry(tt).Reference(t => t.Tag).LoadAsync();
                TagDto tagDto = mapper.Map<TagDto>(tt.Tag);
                dto.Tags.Add(tagDto);
            }

            return dto;
        }

        // Creates task along with test cases and tags
        // POST: api/Task
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<TaskDto>> PostTask([FromBody]TaskDto taskDto)
        {
            if (_context.Tasks.Any(t => t.Name.Equals(taskDto.Name)))
            {
                return BadRequest(new { Message = "There is already a task called " + taskDto.Name });
            }

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            Entities.Task task = mapper.Map<Entities.Task>(taskDto);
            task.UserId = userId;
            task.TimeSubmitted = DateTime.Now;

            _context.Tasks.Add(task);

            // Add tags whose names are not in DB
            foreach (TagDto tagDto in taskDto.Tags)
            {
                if (!await _context.Tags.AnyAsync(t => t.Name.Equals(tagDto.Name)))
                {
                    _context.Tags.Add(new Tag {
                        Name = tagDto.Name,
                        Description = "",
                    });
                }
            }

            // Save task, test cases and new tags to DB (tasks and tags are not yet connected)
            await _context.SaveChangesAsync();

            // Handle task TaskTag entities
            foreach (TagDto tagDto in taskDto.Tags)
            {
                Tag tag = await _context.Tags.FirstAsync(t => t.Name.Equals(tagDto.Name));

                // Put tag ID along with newly added task ID into TaskTag entity
                _context.TaskTags.Add(new TaskTag {
                    TaskId = task.Id,
                    TagId = tag.Id,
                });
            }

            // Save TaskTag entities
            await _context.SaveChangesAsync();

            TaskDto responseDto = mapper.Map<TaskDto>(task);

            // Load tag info to return
            foreach (TaskTag tt in task.TaskTags)
            {
                await _context.Entry(tt).Reference(t => t.Tag).LoadAsync();
                responseDto.Tags.Add(mapper.Map<TagDto>(tt.Tag));
            }

            return CreatedAtAction("GetTask", new { id = task.Id }, responseDto);
        }

        // Edits task along with test cases and tags
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

            if (!taskDto.Name.Equals(task.Name) && await _context.Tasks.AnyAsync(t => t.Name.Equals(taskDto.Name)))
            {
                return BadRequest(new { Message = "There is already a task called " + taskDto.Name });
            }

            // Load creator of task
            await _context.Entry(task).Reference(t => t.User).LoadAsync();

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            if (task.UserId != userId)
            {
                // Task doesn't belong to current user, edit not allowed
                return Unauthorized();
            }

            // Load test cases, tasktags
            await _context.Entry(task).Collection(t => t.TestCases).LoadAsync();
            await _context.Entry(task).Collection(t => t.TaskTags).LoadAsync();

            // Update fields according to DTO
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

            // Handle tags

            // Add tags whose names are not in DB
            foreach (TagDto tagDto in taskDto.Tags)
            {
                if (!await _context.Tags.AnyAsync(t => t.Name.Equals(tagDto.Name)))
                {
                    _context.Tags.Add(new Tag {
                        Name = tagDto.Name,
                        Description = "",
                    });
                }
            }

            // Get old tag ids from TaskTags
            var oldTagIds = new List<int>();
            foreach (TaskTag tt in task.TaskTags)
            {
                oldTagIds.Add(tt.TagId);
            }

            // Remove TaskTags
            _context.TaskTags.RemoveRange(task.TaskTags);

            // Save changes so that new Tags get ids and TaskTags get really removed
            await _context.SaveChangesAsync();

            // Add new TaskTags to task
            foreach (TagDto tagDto in taskDto.Tags)
            {
                Tag tag = await _context.Tags.FirstAsync(t => t.Name.Equals(tagDto.Name));

                _context.TaskTags.Add(new TaskTag {
                    TaskId = task.Id,
                    TagId = tag.Id,
                });
            }

            // Save changes so that TaskTags are really added
            await _context.SaveChangesAsync();

            // Remove unused Tags, check only old ones
            foreach (int tId in oldTagIds)
            {
                if (!await _context.TaskTags.AnyAsync(tt => tt.TagId == tId))
                {
                    _context.Tags.Remove(await _context.Tags.FindAsync(tId));
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
