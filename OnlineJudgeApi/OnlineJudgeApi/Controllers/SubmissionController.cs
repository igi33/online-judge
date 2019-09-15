using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private IMapper mapper;
        private volatile bool gradingHandled; // Used in POST request, signals when grading completed

        public SubmissionController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
            gradingHandled = false;
        }

        // Get recent submissions, possibly filtered by user and by task (0 as id gets all)
        // GET: api/Submission/task/0/user/0
        [HttpGet("task/{taskId}/user/{userId}")]
        public async Task<IActionResult> GetSubmissions(int taskId, int userId)
        {
            int currentUserId = 0; // User not logged in
            if (User.Identity.IsAuthenticated)
            {
                // Fetch current user id
                currentUserId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);
            }

            var submissions = await _context.Submissions.Include(s => s.Task).Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => (taskId != 0 && s.TaskId == taskId || taskId == 0 && s.TaskId > 0) && (userId != 0 && s.UserId == userId || userId == 0 && s.UserId > 0))
                .OrderByDescending(s => s.Id)
                .ToListAsync();

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

        // GET: api/Submission/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<SubmissionDto>> GetSubmission(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null)
            {
                return NotFound();
            }

            await _context.Entry(submission).Reference(t => t.Task).LoadAsync();
            await _context.Entry(submission).Reference(t => t.User).LoadAsync();
            await _context.Entry(submission).Reference(t => t.ComputerLanguage).LoadAsync();

            SubmissionDto dto = mapper.Map<SubmissionDto>(submission);

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);
            if (dto.User.Id != userId)
            {
                // Hide source code
                dto.SourceCode = "";
            }

            return dto;
        }

        // Get fastest accepted submissions for task
        // GET: api/Submission/task/5/best
        [HttpGet("task/{taskId}/best")]
        public async Task<IActionResult> GetBestSubmissionsOfTask(int taskId)
        {
            var submissions = await _context.Submissions.Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => s.TaskId == taskId && s.Status.Equals("AC"))
                .OrderBy(s => s.ExecutionTime)
                .Take(10)
                .ToListAsync();

            var submissionDtos = mapper.Map<IList<SubmissionDto>>(submissions);

            foreach (SubmissionDto sDto in submissionDtos)
            {
                // Hide source code
                sDto.SourceCode = "";
            }

            return Ok(submissionDtos);
        }

        // POST: api/Submission/task/5
        [Authorize]
        [HttpPost("task/{taskId}")]
        public async Task<ActionResult<Submission>> PostSubmission(int taskId, [FromBody]SubmissionDto submissionDto)
        {
            // Get task info
            Entities.Task task = await _context.Tasks.FindAsync(taskId);
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

            // Fetch current user id
            int userId = int.Parse((User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name).Value);

            Submission submission = new Submission
            {
                SourceCode = submissionDto.SourceCode,
                LangId = submissionDto.LangId,
                UserId = userId,
                TimeSubmitted = DateTime.Now,
                TaskId = taskId,
                ExecutionMemory = 0,
                ExecutionTime = 0,
            };

            string taskIdentifierInFileName = task.Id.ToString();
            string sourceFileName = string.Format("{0}.{1}", taskIdentifierInFileName, lang.Extension);

            // Create file from source code
            System.IO.File.WriteAllText(sourceFileName, submissionDto.SourceCode);

            // Compile, execute and grade submission
            using (Process p = new Process())
            {
                p.StartInfo.EnvironmentVariables["CPATH"] = lang.CompilerPath;
                p.StartInfo.FileName = lang.CompilerFileName;
                p.StartInfo.Arguments = string.Format(lang.CompileCmd, taskIdentifierInFileName);
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.EnableRaisingEvents = true;
                p.StartInfo.RedirectStandardError = true;
                p.Exited += (sender, e) => GradeSolution(sender, e, lang, task, submission, submissionDto, taskIdentifierInFileName);
                p.Start();
                p.WaitForExit();
            }

            // Wait for the grading thread, gradingHandled is guaranteed to become true
            while (!gradingHandled)
            {
                Thread.Sleep(150);
            }

            // Save to DB
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            // Delete created task files
            System.IO.File.Delete(sourceFileName);
            System.IO.File.Delete(string.Format(lang.ExecuteCmd, taskIdentifierInFileName));

            // Prepare response DTO
            SubmissionDto responseDto = mapper.Map<SubmissionDto>(submission);
            responseDto.Message = submissionDto.Message;
            responseDto.Task = null;
            responseDto.ComputerLanguage = null;
            responseDto.User = null;

            return CreatedAtAction("GetSubmission", new { id = submission.Id }, responseDto);
        }

        private void GradeSolution(object sender, EventArgs e, ComputerLanguage lang, Entities.Task task, Submission submission, SubmissionDto submissionDto, string taskIdentifierInFileName)
        {
            Process p = sender as Process;
            if (p.ExitCode != 0)
            {
                // Compile error

                submission.Status = "CE"; // Mark submission status as Compile Error
                submissionDto.Message = p.StandardError.ReadToEnd(); // Set message as compile error message
            }
            else
            {
                // Compile success

                submission.Status = "AC"; // Submission status will stay accepted if all test cases pass
                int maxTimeMs = 0; // Track max execution time of test cases
                int maxMemoryB = 0;

                foreach (TestCase tc in task.TestCases)
                {
                    using (Process q = new Process())
                    {
                        q.StartInfo.FileName = string.Format(lang.ExecuteCmd, taskIdentifierInFileName);
                        q.StartInfo.RedirectStandardInput = true;
                        q.StartInfo.RedirectStandardOutput = true;
                        q.StartInfo.RedirectStandardError = true;
                        q.StartInfo.CreateNoWindow = false;
                        q.StartInfo.UseShellExecute = false;
                        q.Start();

                        StreamWriter inputWriter = q.StandardInput;
                        inputWriter.Write(tc.Input);
                        inputWriter.Close();

                        bool exited = q.WaitForExit(task.TimeLimit);
                        if (!exited)
                        {
                            // Time limit exceeded for whatever reason
                            // Rejected

                            q.Kill();
                            submission.Status = "RJ";
                            break;

                        }
                        else
                        {
                            if (q.ExitCode != 0)
                            {
                                // Runtime error
                                // Rejected

                                submission.Status = "RJ";
                                break;

                            }
                            else
                            {
                                // Success
                                // Stays accepted if all test cases produce correct outputs

                                int executionTimeMs = (int)q.ExitTime.Subtract(q.StartTime).TotalMilliseconds;
                                maxTimeMs = Math.Max(maxTimeMs, executionTimeMs);

                                string output = q.StandardOutput.ReadToEnd();

                                // Check if outputs match
                                if (!output.Trim().Equals(tc.Output.Trim()))
                                {
                                    // No match, set status as rejected
                                    submission.Status = "RJ";
                                    break;
                                }

                            }
                        }
                    }
                }

                submission.ExecutionTime = maxTimeMs; // Set submission execution time as max out of all test cases
                submission.ExecutionMemory = maxMemoryB; // Set submission execution memory as max out of all test cases
            }

            gradingHandled = true;
        }

        private bool SubmissionExists(int id)
        {
            return _context.Submissions.Any(e => e.Id == id);
        }
    }
}
