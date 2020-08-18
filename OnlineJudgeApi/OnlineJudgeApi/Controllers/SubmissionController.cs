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

        public SubmissionController(DataContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
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
        public async Task<ActionResult<Submission>> PostSubmission(int taskId, [FromBody] SubmissionDto submissionDto)
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
                Status = "UD",
                ExecutionTime = 0,
                ExecutionMemory = 0,
            };

            // Save initial submission to DB to get a unique id
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            const string rootDir = @"/home/igi33/executionroot/";
            string submissionId = submission.Id.ToString();
            string sourceFileName = string.Format("{0}.{1}", submissionId, lang.Extension);

            string binaryFilePath = string.Format("{0}{1}", rootDir, submissionId);
            string sourceFilePath = string.Format("{0}{1}", rootDir, sourceFileName);
            string timeOutputFilePath = string.Format("{0}time{1}.txt", rootDir, submissionId);

            // Create file from source code inside rootDir
            System.IO.File.WriteAllText(sourceFilePath, submissionDto.SourceCode);

            // Compile submission
            BashExecutor executor = new BashExecutor(string.Format("{0} {1}", lang.CompilerFileName, string.Format(lang.CompileCmd, binaryFilePath)));
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
                string.Format("sudo cgcreate -g memory:{0}", submissionId).Bash();

                int pMemLimitB = task.MemoryLimit + 750000;
                if (pMemLimitB < 1150000)

                // set memory limit a bit higher than task parameter
                string.Format("sudo cgset -r memory.limit_in_bytes={0} -r memory.swappiness=0 {1}", task.MemoryLimit + 750000, submissionId).Bash();

                // timeout value = 2 * task time limit
                float timeoutS = (task.TimeLimit << 1) / 1000.0f;

                // prepare execution command string
                // timeout uses a 2 * time limit value because it measures real time and not cpu time
                // we let the process run longer and only after inspect its cpu time from /usr/bin/time output
                string escapedExecCmd = string.Format("/usr/bin/time -p -o {0} sudo timeout --preserve-status {1} sudo cgexec -g memory:{2} chroot {3} ./{2}", timeOutputFilePath, timeoutS, submissionId, rootDir).Replace("\"", "\\\"");

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

                        /*
                            // Time limit exceeded
                            string qid = q.Id.ToString();
                            submissionDto.Message = string.Format("sudo kill -9 {0}", q.Id).Bash(); // q.Kill() doesn't work because of privileges
                            submission.Status = "TLE";
                            
                            correctSoFar = false;
                        */

                        // fetch and check if execution CPU time really meets the limit
                        double userCpuTimeS = double.Parse(string.Format("grep -oP '(?<=user ).*' {0}", timeOutputFilePath).Bash());
                        double sysCpuTimeS = double.Parse(string.Format("grep -oP '(?<=sys ).*' {0}", timeOutputFilePath).Bash());
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
                string maxMemoryUsed = string.Format("cgget -n -v -r memory.max_usage_in_bytes {0}", submissionId).Bash().TrimEnd('\r', '\n'); ;

                // delete cgroup
                string.Format("sudo cgdelete -g memory:{0}", submissionId).Bash();

                submission.ExecutionTime = maxTimeMs; // Set submission execution time as max out of all test cases
                submission.ExecutionMemory = Int32.Parse(maxMemoryUsed); // Set submission execution memory as max out of all test cases

                System.IO.File.Delete(binaryFilePath);
            }

            // Edit submission object status and stats
            await _context.SaveChangesAsync();

            // Delete created files
            System.IO.File.Delete(sourceFilePath);
            
            //System.IO.File.Delete(timeOutputFilePath);

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
