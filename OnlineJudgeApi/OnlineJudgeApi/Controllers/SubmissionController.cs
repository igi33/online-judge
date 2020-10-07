using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
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
            var query = _context.Submissions.Include(s => s.Task).Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => (taskId != 0 ? s.TaskId == taskId : s.TaskId > 0) && (userId != 0 ? s.UserId == userId : s.UserId > 0))
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

            int currentUserId = 0; // User not logged in
            if (User.Identity.IsAuthenticated)
            {
                // Fetch current user id
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }
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

            int currentUserId = 0; // User not logged in
            if (User.Identity.IsAuthenticated)
            {
                // Fetch current user id
                currentUserId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);
            }
            if (currentUserId == 0 || dto.User.Id != currentUserId)
            {
                // Hide source code
                dto.SourceCode = "";
            }

            return Ok(dto);
        }

        // Get fastest accepted submissions for specific task
        // GET: api/Submission/task/5/best?limit=10
        [HttpGet("task/{taskId}/best")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetBestSubmissionsOfTask(int taskId, int limit = 10)
        {
            var submissions = await _context.Submissions.Include(s => s.User).Include(s => s.ComputerLanguage)
                .Where(s => s.TaskId == taskId && s.Status.Equals("AC"))
                .OrderBy(s => s.ExecutionTime)
                .Take(limit)
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
        public async Task<ActionResult<SubmissionDto>> PostSubmission(int taskId, [FromBody] SubmissionDto submissionDto)
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
            int userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub).Value);

            Submission submission = new Submission
            {
                SourceCode = submissionDto.SourceCode,
                LangId = submissionDto.LangId,
                UserId = userId,
                TimeSubmitted = DateTime.Now,
                TaskId = taskId,
                Status = "UD",
                ExecutionTime = 0,
                ExecutionMemory = 0,
            };

            // Save initial submission to DB to get a unique id
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            string cmdUsername = "whoami".Bash().Trim();
            string rootDir = $"/home/{cmdUsername}/executionroot/";
            string submissionId = submission.Id.ToString();
            string sourceFileName = $"{submissionId}.{lang.Extension}";

            string binaryFilePath = $"{rootDir}{submissionId}";
            string sourceFilePath = $"{rootDir}{sourceFileName}";
            string timeOutputFilePath = $"{rootDir}time{submissionId}.txt";

            // Create file from source code inside rootDir
            System.IO.File.WriteAllText(sourceFilePath, submissionDto.SourceCode);

            // Compile submission
            BashExecutor executor = new BashExecutor($"{lang.CompilerFileName} {string.Format(lang.CompileCmd, binaryFilePath)}");
            executor.Execute();

            if (executor.ExitCode != 0)
            {
                // Compile error
                submission.Status = "CE"; // Mark submission status as Compile Error
                submissionDto.Message = executor.Error; // Set message as compile error message
            }
            else
            {
                // Compile success
                submission.Status = "AC"; // Submission status will stay accepted if all test cases pass
                int maxTimeMs = 0; // Track max execution time of test cases

                // create cgroup
                $"sudo cgcreate -g memory:{submissionId}".Bash();

                // increase the limit a bit due to overhead of calling multiple commands in one
                int pMemLimitB = task.MemoryLimit + 750000;
                // set minimal limit for cgroup
                pMemLimitB = Math.Max(pMemLimitB, 1150000);

                // set memory limit a bit higher than task parameter
                $"sudo cgset -r memory.limit_in_bytes={pMemLimitB} -r memory.swappiness=0 {submissionId}".Bash();

                // timeout value = 2 * task time limit
                float timeoutS = (task.TimeLimit << 1) / 1000.0f;

                // prepare execution command string
                // timeout uses a 2 * time limit value because it measures real time and not cpu time
                // we let the process run longer and only after inspect its cpu time from /usr/bin/time output
                string escapedExecCmd = $"/usr/bin/time -p -o {timeOutputFilePath} sudo timeout --preserve-status {timeoutS} sudo cgexec -g memory:{submissionId} chroot {rootDir} ./{submissionId}".Replace("\"", "\\\"");

                bool correctSoFar = true;

                for (int i = 0; correctSoFar && i < task.TestCases.Count; ++i)
                {
                    TestCase tc = task.TestCases.ElementAt(i);
                    using (Process q = new Process())
                    {
                        string output = "";

                        q.StartInfo.FileName = "/bin/bash";
                        q.StartInfo.Arguments = $"-c \"{escapedExecCmd}\"";
                        q.StartInfo.RedirectStandardInput = true;
                        q.StartInfo.RedirectStandardOutput = true;
                        q.StartInfo.CreateNoWindow = false;
                        q.StartInfo.UseShellExecute = false;
                        q.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                output += e.Data + "\n";
                            }
                        });
                        
                        q.Start();
                        q.BeginOutputReadLine();

                        StreamWriter inputWriter = q.StandardInput;
                        inputWriter.Write(tc.Input);
                        inputWriter.Close();

                        q.WaitForExit();

                        // fetch and check if execution CPU time actually meets the limit
                        double userCpuTimeS = double.Parse($"grep -oP '(?<=user ).*' {timeOutputFilePath}".Bash());
                        double sysCpuTimeS = double.Parse($"grep -oP '(?<=sys ).*' {timeOutputFilePath}".Bash());
                        int totalCpuTimeMs = (int)((userCpuTimeS + sysCpuTimeS) * 1000 + 0.5);
                        maxTimeMs = Math.Max(maxTimeMs, totalCpuTimeMs);

                        if (q.ExitCode != 0)
                        {
                            if (q.ExitCode == 137)
                            {
                                // cgroup sent SIGKILL
                                // the process exited with code 137
                                // meaning the memory limit was breached
                                submission.Status = "MLE";
                            }
                            else if (q.ExitCode == 143 || totalCpuTimeMs > task.TimeLimit)
                            {
                                // timeout sent SIGTERM
                                // the process exited with code 137
                                // meaning the time limit was definitely breached
                                // OR
                                // actual CPU time breaches the limit
                                submission.Status = "TLE";
                                maxTimeMs = task.TimeLimit;
                            }
                            else
                            {
                                // Runtime error
                                // Rejected
                                submission.Status = "RTE";
                            }
                            correctSoFar = false;
                        }
                        else
                        {
                            // Successfully executed

                            // Check if submission output matches the expected output of test case
                            string[] outputLines = output.Trim().Split(
                                new[] { "\r\n", "\r", "\n" },
                                StringSplitOptions.None
                            );
                            string[] tcOutputLines = tc.Output.Trim().Split(
                                new[] { "\r\n", "\r", "\n" },
                                StringSplitOptions.None
                            );

                            if (outputLines.Length != tcOutputLines.Length)
                            {
                                correctSoFar = false;
                            }

                            int idx = 0;
                            while (correctSoFar && idx < outputLines.Length)
                            {
                                if (!outputLines.ElementAt(idx).Equals(tcOutputLines.ElementAt(idx)))
                                {
                                    correctSoFar = false;
                                }
                                ++idx;
                            }

                            if (!correctSoFar)
                            {
                                // Mismatch, set status as rejected
                                submission.Status = "RJ";
                            }
                        }
                    }
                }

                // get max memory used
                string maxMemoryUsed = $"cgget -n -v -r memory.max_usage_in_bytes {submissionId}".Bash().TrimEnd('\r', '\n');

                // delete cgroup
                $"sudo cgdelete -g memory:{submissionId}".Bash();

                submission.ExecutionTime = maxTimeMs; // Set submission execution time as max out of all test cases
                submission.ExecutionMemory = Int32.Parse(maxMemoryUsed); // Set submission execution memory as max out of all test cases

                System.IO.File.Delete(binaryFilePath);
            }

            // Edit submission object status and stats
            await _context.SaveChangesAsync();

            // Delete created files
            System.IO.File.Delete(sourceFilePath);
            System.IO.File.Delete(timeOutputFilePath);

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
