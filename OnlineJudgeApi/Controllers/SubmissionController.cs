using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
    public class SubmissionController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IMapper mapper;

        public SubmissionController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        // Get recent submissions, possibly paged and filtered by user id or task id
        // GET: api/Submission?taskId=0&userId=0&limit=0&offset=0
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetSubmissions(int taskId = 0, int userId = 0, int limit = 0, int offset = 0)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            var query = _context.Submissions.Include(s => s.Task).Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => (taskId != 0 ? s.TaskId == taskId : s.TaskId > 0) && (userId != 0 ? s.UserId == userId : s.UserId > 0) && (s.Task.IsPublic || s.Task.UserId == currentUserId))
                .OrderByDescending(s => s.Id);

            List<Submission> submissions;
            if (limit != 0)
            {
                submissions = await query.Skip(offset).Take(limit).ToListAsync();
            }
            else
            {
                submissions = await query.ToListAsync();
            }

            var submissionDtos = mapper.Map<IList<SubmissionDto>>(submissions);
            foreach (SubmissionDto sDto in submissionDtos)
            {
                if (currentUserId == 0 || sDto.User.Id != currentUserId)
                {
                    // Hide source code if shouldn't be seen
                    sDto.SourceCode = "";
                }
            }

            return Ok(submissionDtos);
        }

        // Get recent submissions count, possibly filtered by user id or task id
        // GET: api/Submission/count?taskId=0&userId=0
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount(int taskId = 0, int userId = 0)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            int count = await _context.Submissions.CountAsync(s => (taskId != 0 ? s.TaskId == taskId : s.TaskId > 0) && (userId != 0 ? s.UserId == userId : s.UserId > 0) && (s.Task.IsPublic || s.Task.UserId == currentUserId));
            return Ok(count);
        }

        // GET: api/Submission/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SubmissionDto>> GetSubmission(int id)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            var submission = await _context.Submissions.SingleOrDefaultAsync(s => s.Id == id && (s.Task.IsPublic || s.Task.UserId == currentUserId));
            if (submission == null)
            {
                return NotFound();
            }

            await _context.Entry(submission).Reference(t => t.Task).LoadAsync();
            await _context.Entry(submission).Reference(t => t.User).LoadAsync();
            await _context.Entry(submission).Reference(t => t.ComputerLanguage).LoadAsync();

            SubmissionDto dto = mapper.Map<SubmissionDto>(submission);

            // Hide source code if submission somebody else's
            if (currentUserId == 0 || dto.User.Id != currentUserId)
            {
                dto.SourceCode = "";
            }

            return Ok(dto);
        }

        // Get fastest accepted submissions for specific task
        // GET: api/Submission/task/5/best?limit=10
        [HttpGet("task/{taskId}/best")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetBestSubmissionsOfTask(int taskId, int limit = 10)
        {
            int currentUserId = 0;
            if (User.Identity.IsAuthenticated)
            {
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }

            var submissions = await _context.Submissions.Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => s.TaskId == taskId && s.Status.Equals("AC") && (s.Task.IsPublic || currentUserId > 0 && s.Task.UserId == currentUserId))
                .OrderBy(s => s.ExecutionTime)
                .Take(limit)
                .ToListAsync();

            var submissionDtos = mapper.Map<IList<SubmissionDto>>(submissions);

            // Hide source codes
            foreach (SubmissionDto sDto in submissionDtos)
            {
                sDto.SourceCode = "";
            }

            return Ok(submissionDtos);
        }

        // POST: api/Submission/task/5
        [Authorize]
        [HttpPost("task/{taskId}")]
        public async Task<ActionResult<SubmissionDto>> PostSubmission(int taskId, [FromBody] SubmissionDto submissionDto)
        {
            int currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            // Get task info
            Entities.Task task = await _context.Tasks.SingleOrDefaultAsync(t => t.Id == taskId && (t.IsPublic || t.UserId == currentUserId));
            if (task == null)
            {
                return BadRequest();
            }

            // Get language info
            ComputerLanguage lang = await _context.ComputerLanguages.FindAsync(submissionDto.LangId);
            if (lang == null)
            {
                return BadRequest();
            }

            // Get test cases
            await _context.Entry(task).Collection(t => t.TestCases).LoadAsync();

            Submission submission = new Submission
            {
                SourceCode = submissionDto.SourceCode,
                LangId = submissionDto.LangId,
                UserId = currentUserId,
                TimeSubmitted = DateTime.Now,
                TaskId = taskId,
                Status = "UD",
                ExecutionTime = 0,
                ExecutionMemory = 0,
            };

            // Save initial submission to DB to get a unique id
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            string executionRootDir = Grader.GetExecutionRootDir();

            string sourceFileName = $"{submission.Id}.{lang.Extension}";
            string binaryFileName = submission.Id.ToString();

            string sourceFilePath = $"{executionRootDir}{sourceFileName}";
            string binaryFilePath = $"{executionRootDir}{binaryFileName}";

            // Create file from source code inside rootDir
            System.IO.File.WriteAllText(sourceFilePath, submissionDto.SourceCode);

            bool readyToRun = true;

            // Check if compiled language
            if (!string.IsNullOrEmpty(lang.CompileCmd))
            {
                // Compile submission
                CompilationOutputDto co = Grader.Compile(lang, sourceFileName, binaryFileName);

                if (co.ExitCode != 0)
                {
                    // Compile error
                    submission.Status = "CE"; // Mark status as Compile Error
                    submissionDto.Message = co.Error; // Set message as compile error message
                    readyToRun = false;
                }
            }

            if (readyToRun)
            {
                // Compiled successfully or interpreted language, so we're ready to run the solution

                submission.Status = "AC"; // Submission status will stay accepted if all test cases pass (or if there aren't any TCs)
                string fileName = string.IsNullOrEmpty(lang.CompileCmd) ? sourceFileName : binaryFileName;
                int maxTimeMs = 0; // Track max execution time of test cases
                int maxMemoryB = 0; // Track max execution memory of test cases

                bool correctSoFar = true;
                for (int i = 0; correctSoFar && i < task.TestCases.Count; ++i)
                {
                    TestCase tc = task.TestCases.ElementAt(i);

                    GradeDto grade = Grader.Grade(lang, fileName, tc.Input, tc.Output, task.TimeLimit, task.MemoryLimit);

                    maxTimeMs = Math.Max(maxTimeMs, grade.ExecutionTime);
                    maxMemoryB = Math.Max(maxMemoryB, grade.ExecutionMemory);
                    submission.Status = grade.Status;

                    correctSoFar = grade.Status.Equals("AC");
                }

                submission.ExecutionTime = maxTimeMs; // Set submission execution time as max out of all test cases
                submission.ExecutionMemory = maxMemoryB; // Set submission execution memory as max out of all test cases

                // Delete binary file if compiled
                if (!string.IsNullOrEmpty(lang.CompileCmd))
                {
                    System.IO.File.Delete(binaryFilePath);
                }
            }

            // Edit submission object status and stats
            await _context.SaveChangesAsync();

            // Delete source file
            System.IO.File.Delete(sourceFilePath);

            // Prepare response DTO
            SubmissionDto responseDto = mapper.Map<SubmissionDto>(submission);
            responseDto.Message = submissionDto.Message;
            responseDto.Task = null;
            responseDto.ComputerLanguage = null;
            responseDto.User = null;

            return CreatedAtAction("GetSubmission", new { id = submission.Id }, responseDto);
        }

        private bool SubmissionExists(int id)
        {
            return _context.Submissions.Any(e => e.Id == id);
        }
    }
}
