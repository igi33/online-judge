using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        private readonly IMapper mapper;

        public TaskController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // Get list of tasks, possibly paged and filtered by tag id
        // GET: api/Task?tagId=6&limit=0&offset=0
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(int tagId = 0, int limit = 0, int offset = 0)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            var query = _context.Tasks
                .Where(t => (tagId != 0 ? t.TaskTags.Any(tt => tt.TagId == tagId) : t.Id > 0) && (t.IsPublic || t.UserId == currentUserId))
                .Include(t => t.User)
                .OrderBy(t => t.Id);

            List<Entities.Task> tasks;
            if (limit != 0)
            {
                tasks = await query.Skip(offset).Take(limit).ToListAsync();
            }
            else
            {
                tasks = await query.ToListAsync();
            }

            var taskDtos = mapper.Map<IList<TaskDto>>(tasks);
            return Ok(taskDtos);
        }

        // Get list of solved public tasks by user, possibly paged
        // GET: api/Task/solvedby/5?limit=0&offset=0
        [HttpGet("solvedby/{userId}")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetSolvedByUser(int userId, int limit = 0, int offset = 0)
        {
            var query = _context.Submissions.Where(s => s.UserId == userId && s.Status.Equals("AC") && s.Task.IsPublic)
                .OrderBy(s => s.Id)
                .Select(s => s.TaskId)
                .Distinct();

            HashSet<int> solvedTaskIds;
            if (limit != 0)
            {
                solvedTaskIds = query.Skip(offset).Take(limit).ToHashSet();
            }
            else
            {
                solvedTaskIds = query.ToHashSet();
            }

            List<Entities.Task> tasks = await _context.Tasks
                .Where(t => solvedTaskIds.Contains(t.Id))
                .Include(t => t.User)
                .OrderBy(t => t.Id)
                .ToListAsync();

            var solvedTaskDtos = mapper.Map<IList<TaskDto>>(tasks);
            return Ok(solvedTaskDtos);
        }

        // Returns basic task info along with test case IDs if task belongs to user sending request
        // GET: api/Task/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            Entities.Task task = await _context.Tasks.SingleOrDefaultAsync(t => t.Id == id && (t.IsPublic || t.UserId == currentUserId));
            if (task == null)
            {
                return NotFound();
            }

            await _context.Entry(task).Reference(t => t.User).LoadAsync();

            TaskDto dto = mapper.Map<TaskDto>(task);

            // Load tc IDs only, to avoid potential long texts from tc input/output
            if (currentUserId != 0)
            {
                // Load test cases only if they belong to current user
                if (task.UserId == currentUserId)
                {
                    IEnumerable<int> tcIds = await _context.TestCases
                        .Where(tc => tc.TaskId == id)
                        .Select(tc => tc.Id)
                        .ToListAsync();

                    foreach (int tcId in tcIds)
                    {
                        dto.TestCases.Add(new TestCaseDto { Id = tcId });
                    }
                }
            }

            // Load tags and add to DTO
            await _context.Entry(task).Collection(t => t.TaskTags).LoadAsync();
            foreach (TaskTag tt in task.TaskTags)
            {
                await _context.Entry(tt).Reference(t => t.Tag).LoadAsync();
                TagDto tagDto = mapper.Map<TagDto>(tt.Tag);
                dto.Tags.Add(tagDto);
            }

            return Ok(dto);
        }

        // Creates task along with test cases and tags
        // POST: api/Task
        [Authorize]
        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostTask([FromForm] IList<IFormFile> inputs, [FromForm] IList<IFormFile> outputs, [FromForm] string taskDtoStr)
        {
            TaskDto taskDto = JsonConvert.DeserializeObject<TaskDto>(taskDtoStr);
            if (_context.Tasks.Any(t => t.Name.Equals(taskDto.Name)))
            {
                return BadRequest(new { Message = "There is already a task called " + taskDto.Name });
            }
            
            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            Entities.Task task = mapper.Map<Entities.Task>(taskDto);
            task.UserId = currentUserId;
            task.TimeSubmitted = DateTime.Now;
            task.IsPublic = false;

            // Add test cases to task object
            int n = inputs.Count;
            for (int i = 0; i < n; ++i)
            {
                StreamReader srin = new StreamReader(inputs[i].OpenReadStream());
                Task<string> inputText = srin.ReadToEndAsync();

                StreamReader srout = new StreamReader(outputs[i].OpenReadStream());
                Task<string> outputText = srout.ReadToEndAsync();

                string input = await inputText;
                string output = await outputText;

                srin.Close();
                srout.Close();

                task.TestCases.Add(new TestCase { Input = input, Output = output });
            }

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
            responseDto.TestCases = null;

            return CreatedAtAction("GetTask", new { id = task.Id }, responseDto);
        }

        // Edits task along with test cases and tags
        // PUT: api/Task/5
        [Authorize]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask(int id, [FromForm] IList<IFormFile> inputs, [FromForm] IList<IFormFile> outputs, [FromForm] string taskDtoStr)
        {
            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            Entities.Task task = await _context.Tasks.SingleOrDefaultAsync(t => t.Id == id && (t.IsPublic || t.UserId == currentUserId));
            if (task == null)
            {
                return NotFound();
            }

            if (task.UserId != currentUserId)
            {
                // Task doesn't belong to current user, edit not allowed
                return Unauthorized();
            }

            TaskDto taskDto = JsonConvert.DeserializeObject<TaskDto>(taskDtoStr);
            if (await _context.Tasks.AnyAsync(t => t.Id != id && t.Name.ToLower().Equals(taskDto.Name.ToLower())))
            {
                return BadRequest(new { Message = "There is already a task called " + taskDto.Name });
            }

            // Load creator of task
            await _context.Entry(task).Reference(t => t.User).LoadAsync();

            // Remove marked TCs
            foreach (TestCaseDto tcDto in taskDto.TestCases)
            {
                TestCase tc = new TestCase() { Id = tcDto.Id };
                _context.TestCases.Attach(tc);
                _context.TestCases.Remove(tc);
            }

            // Add new TCs
            int n = inputs.Count;
            for (int i = 0; i < n; ++i)
            {
                StreamReader srin = new StreamReader(inputs[i].OpenReadStream());
                Task<string> inputText = srin.ReadToEndAsync();

                StreamReader srout = new StreamReader(outputs[i].OpenReadStream());
                Task<string> outputText = srout.ReadToEndAsync();

                string input = await inputText;
                string output = await outputText;

                srin.Close();
                srout.Close();

                task.TestCases.Add(new TestCase { Input = input, Output = output });
            }

            // Load tasktags
            await _context.Entry(task).Collection(t => t.TaskTags).LoadAsync();

            // Update fields according to DTO
            task.Name = taskDto.Name;
            task.Description = taskDto.Description;
            task.MemoryLimit = taskDto.MemoryLimit;
            task.TimeLimit = taskDto.TimeLimit;
            task.Origin = taskDto.Origin;

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
            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            Entities.Task task = await _context.Tasks.SingleOrDefaultAsync(t => t.Id == id && (t.IsPublic || t.UserId == currentUserId));
            if (task == null)
            {
                return NotFound();
            }

            if (task.UserId != currentUserId)
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
